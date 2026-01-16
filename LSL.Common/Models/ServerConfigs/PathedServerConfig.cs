using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using LSL.Common.Utilities;
using LSL.Common.Validation;
using Newtonsoft.Json;

namespace LSL.Common.Models.ServerConfigs;

public class PathedServerConfig(
    string serverPath,
    string serverName,
    ServerCoreType serverType,
    CommonCoreConfigV1? commonInfo,
    ForgeCoreConfigV1? forgeInfo,
    string javaPath,
    uint minMemory,
    uint maxMemory,
    string[] extJvm,
    bool enablePreLaunchProtection)
{
    public string ServerPath { get; set; } = serverPath;
    public string ServerName { get; set; } = serverName;
    public ServerCoreType ServerType { get; set; } = serverType;

    [MemberNotNullWhen(true, nameof(ForgeCoreInfo))]
    public bool IsForge => ServerType is ServerCoreType.Forge;

    public CommonCoreConfigV1? CommonCoreInfo { get; set; } = commonInfo;
    public ForgeCoreConfigV1? ForgeCoreInfo { get; set; } = forgeInfo;
    public string JavaPath { get; set; } = javaPath;
    public uint MinMemory { get; set; } = minMemory;
    public uint MaxMemory { get; set; } = maxMemory;
    public List<string> ExtraJvmArgs { get; set; } = [..extJvm];
    public bool EnablePreLaunchProtection { get; set; } = enablePreLaunchProtection;

    public static PathedServerConfig Empty =>
        new(string.Empty, string.Empty, ServerCoreType.Unknown, null, null, string.Empty, 1024, 4096, [], true);

    public IndexedServerConfig AsIndexed(int serverId) => new(serverId, this);

    public async Task<ServiceResult<PathedServerConfig>> CheckAndFixAsync()
    {
        for (int tries = 0; tries < 3; tries++)
        {
            if (!Path.Exists(ServerPath))
                return ServiceResult.Fail<PathedServerConfig>(new DirectoryNotFoundException($"Server {ServerPath} does not exist"));
            List<string> warnings = [];
            if (MinMemory > MaxMemory) warnings.Add("Minimum memory shouldn't be greater than maximum memory");
            if (!CheckComponents.IsValidJava(JavaPath)) warnings.Add("The configured Java is not valid");
            warnings.AddRange(from arg in ExtraJvmArgs
                where !CheckComponents.ExtJvm(arg).Passed
                select $"Invalid extra JVM argument {arg}");
            switch (ServerType)
            {
                case ServerCoreType.Forge or ServerCoreType.ForgeInstaller or ServerCoreType.ForgeShim:
                {
                    if (ForgeCoreInfo is null || !File.Exists(ForgeCoreInfo.WinLibraryArgsPath) ||
                        !File.Exists(ForgeCoreInfo.UnixLibraryArgsPath))
                    {
                        var detectResult = await ForgeConfigHelper.GetForgeConfig(ServerPath);
                        if (detectResult.IsError)
                            return ServiceResult.Fail<PathedServerConfig>("Cannot get the correct core info of the forge server");
                        ForgeCoreInfo = ForgeCoreConfigV1.FromTuple(detectResult.Result);
                    }

                    break;
                }
                case ServerCoreType.Error when CommonCoreInfo is not null && File.Exists(CommonCoreInfo.JarName):
                {
                    var detectResult = await CoreTypeHelper.GetCoreType(CommonCoreInfo.JarName);
                    if (detectResult.IsError) return ServiceResult.Fail<PathedServerConfig>("Cannot get the core type");
                    ServerType = detectResult.Result;
                    continue;
                }
                case ServerCoreType.Error when ForgeCoreInfo is not null &&
                                               File.Exists(ForgeCoreInfo.WinLibraryArgsPath) &&
                                               File.Exists(ForgeCoreInfo.UnixLibraryArgsPath):
                    ServerType = ServerCoreType.Forge;
                    break;
                case ServerCoreType.Error:
                    return ServiceResult.Fail<PathedServerConfig>("Neither core file nor forge arguments is valid");
                default:
                {
                    if (CommonCoreInfo is null || !File.Exists(CommonCoreInfo.JarName))
                        return ServiceResult.Fail<PathedServerConfig>("The jar info of the server is invalid");
                    break;
                }
            }

            return warnings.Count > 0
                ? ServiceResult.Warning(this, new StringBuilder().AppendJoin('\n', warnings).ToString())
                : ServiceResult.Success(this);
        }

        return ServiceResult.Fail<PathedServerConfig>("Failed to check server configuration after multiple attempts");
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
        
        return ServiceResult.Fail<ProcessStartInfo>("Server configuration is not valid");
    }
}