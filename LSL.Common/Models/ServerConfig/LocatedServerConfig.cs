using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using LSL.Common.Utilities.Minecraft;
using LSL.Common.Validation;

namespace LSL.Common.Models.ServerConfig;

public class LocatedServerConfig
{
    internal LocatedServerConfig(string serverPath,
        string serverName,
        ServerCoreType serverType,
        CommonCoreConfigV1? commonInfo,
        ForgeCoreConfigV1? forgeInfo,
        string javaPath,
        uint minMemory,
        uint maxMemory,
        List<string> extJvm,
        bool enablePreLaunchProtection)
    {
        ServerPath = serverPath;
        ServerName = serverName;
        ServerType = serverType;
        CommonCoreInfo = commonInfo;
        ForgeCoreInfo = forgeInfo;
        JavaPath = javaPath;
        MinMemory = minMemory;
        MaxMemory = maxMemory;
        ExtraJvmArgs = extJvm;
        EnablePreLaunchProtection = enablePreLaunchProtection;
    }

    public string ServerPath { get; set; }
    public string ServerName { get; set; }
    public ServerCoreType ServerType { get; set; }

    [MemberNotNullWhen(true, nameof(ForgeCoreInfo))]
    public bool IsForge => ServerType is ServerCoreType.Forge;

    public CommonCoreConfigV1? CommonCoreInfo { get; set; }
    public ForgeCoreConfigV1? ForgeCoreInfo { get; set; }
    public string JavaPath { get; set; }
    public uint MinMemory { get; set; }
    public uint MaxMemory { get; set; }
    public List<string> ExtraJvmArgs { get; set; }
    public bool EnablePreLaunchProtection { get; set; }

    public static LocatedServerConfig Empty =>
        new(string.Empty, string.Empty, ServerCoreType.Unknown, null, null, string.Empty, 1024, 4096, [], true);

    public IndexedServerConfig AsIndexed(int serverId) => new(serverId, this);

    public static Task<ServiceResult<LocatedServerConfig>> CreateAsync(string serverPath,
        string? serverName,
        ServerCoreType? serverType,
        CommonCoreConfigV1? commonInfo,
        ForgeCoreConfigV1? forgeInfo,
        string? javaPath,
        uint? minMemory,
        uint? maxMemory,
        List<string>? extJvm,
        bool? enablePreLaunchProtection)
    {
        if (string.IsNullOrEmpty(serverName))
            return Task.FromResult(ServiceResult.Fail<LocatedServerConfig>("This server doesn't have a name"));
        if (minMemory is null) return Task.FromResult(ServiceResult.Fail<LocatedServerConfig>("Minimum memory is missing"));
        if (maxMemory is null) return Task.FromResult(ServiceResult.Fail<LocatedServerConfig>("Maximum memory is missing"));
        return new LocatedServerConfig(serverPath, serverName, serverType ?? ServerCoreType.Error, commonInfo, forgeInfo,
            javaPath ?? string.Empty, minMemory.Value, maxMemory.Value, extJvm ?? [],
            enablePreLaunchProtection ?? true).CheckAndFixAsync();
    }
    
    public async Task<ServiceResult<LocatedServerConfig>> CheckAndFixAsync()
    {
        for (int tries = 0; tries < 3; tries++)
        {
            var validationResult = Validate();
            if (validationResult.IsError) return ServiceResult.Fail<LocatedServerConfig>(validationResult.Error);
            switch (ServerType)
            {
                case ServerCoreType.Forge or ServerCoreType.ForgeInstaller or ServerCoreType.ForgeShim:
                {
                    if (ForgeCoreInfo is null || !File.Exists(ForgeCoreInfo.WinLibraryArgsPath) ||
                        !File.Exists(ForgeCoreInfo.UnixLibraryArgsPath))
                    {
                        var detectResult = await ForgeConfigHelper.GetForgeConfig(ServerPath);
                        if (detectResult.IsError)
                            return ServiceResult.Fail<LocatedServerConfig>("Cannot get the correct core info of the forge server");
                        ForgeCoreInfo = ForgeCoreConfigV1.FromTuple(detectResult.Result);
                    }

                    break;
                }
                case ServerCoreType.Error when CommonCoreInfo is not null && File.Exists(CommonCoreInfo.JarName):
                {
                    var detectResult = await CoreTypeHelper.GetCoreType(CommonCoreInfo.JarName);
                    if (detectResult.IsError) return ServiceResult.Fail<LocatedServerConfig>("Cannot get the core type");
                    ServerType = detectResult.Result;
                    continue;
                }
                case ServerCoreType.Error when ForgeCoreInfo is not null &&
                                               File.Exists(ForgeCoreInfo.WinLibraryArgsPath) &&
                                               File.Exists(ForgeCoreInfo.UnixLibraryArgsPath):
                    ServerType = ServerCoreType.Forge;
                    break;
                case ServerCoreType.Error:
                    return ServiceResult.Fail<LocatedServerConfig>("Neither core file nor forge arguments is valid");
                default:
                {
                    if (CommonCoreInfo is null || !File.Exists(CommonCoreInfo.JarName))
                        return ServiceResult.Fail<LocatedServerConfig>("The jar info of the server is invalid");
                    break;
                }
            }
            return ServiceResult.Success(this);
        }

        return ServiceResult.Fail<LocatedServerConfig>("Failed to check server configuration after multiple attempts");
    }

    public ServiceResult Validate()
    {
        List<string> warnings = [];
        if (!Path.Exists(ServerPath)) warnings.Add($"Server {ServerPath} does not exist");
        if (MinMemory > MaxMemory) warnings.Add("Minimum memory shouldn't be greater than maximum memory");
        if (!CheckComponents.IsValidJava(JavaPath)) warnings.Add("The configured Java is not valid");
        warnings.AddRange(from arg in ExtraJvmArgs
            where !CheckComponents.ExtJvm(arg).Passed
            select $"Invalid extra JVM argument {arg}");
        if (warnings.Count != 0) return ServiceResult.Fail(new StringBuilder().AppendJoin('\n', warnings).ToString());
        return ServiceResult.Success();
    }

    public ServiceResult<ProcessStartInfo> GetStartInfo()
    {
        if (!CheckComponents.IsValidJava(JavaPath))
            return ServiceResult.Fail<ProcessStartInfo>(new ArgumentException("Java is not valid"));
        if (ServerType is not ServerCoreType.Forge && File.Exists(CommonCoreInfo?.JarName))
        {
            return ServiceResult.Success(new ProcessStartInfo()
            {
                FileName = JavaPath,
                Arguments =
                    $"-server -Xms{MinMemory}M -Xmx{MaxMemory} {new StringBuilder().AppendJoin(' ', ExtraJvmArgs)} -jar {Path.Combine(ServerPath, CommonCoreInfo.JarName)} nogui",
                WorkingDirectory = ServerPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = null,
                StandardOutputEncoding = null,
                StandardErrorEncoding = null
            });
        }

        if (ServerType is ServerCoreType.Forge && ForgeCoreInfo is not null)
        {
            if (OperatingSystem.IsWindows() && File.Exists(ForgeCoreInfo.WinLibraryArgsPath))
            {
                return ServiceResult.Success(new ProcessStartInfo()
                {
                    FileName = JavaPath,
                    Arguments =
                        $"-server -Xms{MinMemory}M -Xmx{MaxMemory} {new StringBuilder().AppendJoin(' ', ExtraJvmArgs)} @{ForgeCoreInfo.WinLibraryArgsPath} nogui",
                    WorkingDirectory = ServerPath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardInputEncoding = null,
                    StandardOutputEncoding = null,
                    StandardErrorEncoding = null
                });
            }
            if (File.Exists(ForgeCoreInfo.UnixLibraryArgsPath))
            {
                return ServiceResult.Success(new ProcessStartInfo()
                {
                    FileName = JavaPath,
                    Arguments =
                        $"-server -Xms{MinMemory}M -Xmx{MaxMemory} {new StringBuilder().AppendJoin(' ', ExtraJvmArgs)} @{ForgeCoreInfo.UnixLibraryArgsPath} nogui",
                    WorkingDirectory = ServerPath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardInputEncoding = null,
                    StandardOutputEncoding = null,
                    StandardErrorEncoding = null
                });
            }
        }
        
        return ServiceResult.Fail<ProcessStartInfo>("Server configuration is not valid to create a Minecraft server process");
    }
}