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
    string usingJava,
    uint minMemory,
    uint maxMemory,
    string[] extJvm,
    bool enablePreLaunchProtection)
{
    public string ServerPath { get; set; } = serverPath;
    public string ServerName { get; set; } = serverName;
    public ServerCoreType ServerType { get; set; } = ServerCoreType.Error;
    
    [MemberNotNullWhen(true, nameof(ForgeCoreInfo))]
    public bool IsForge => ServerType is ServerCoreType.Forge;
    
    public CommonCoreConfigV1? CommonCoreInfo { get; set; } = null;
    public ForgeCoreConfigV1? ForgeCoreInfo { get; set; } = null;
    public string UsingJava { get; set; } = usingJava;
    public uint MinMemory { get; set; } = minMemory;
    public uint MaxMemory { get; set; } = maxMemory;
    public List<string> ExtJvm { get; set; } = [..extJvm];
    public bool EnablePreLaunchProtection { get; set; } = enablePreLaunchProtection;

    public static PathedServerConfig Empty =>
        new(string.Empty, string.Empty, ServerCoreType.Unknown, null, null, string.Empty, 1024, 4096, [], true);

    public IndexedServerConfig AsIndexed(int serverId) => new(serverId, this);

    public async Task<ServiceResult> CheckAndFixAsync()
    {
        for(int tries = 0; tries < 3; tries++)
        {
            if (!Path.Exists(ServerPath)) return ServiceResult.Fail(new DirectoryNotFoundException($"Server {ServerPath} does not exist"));
            List<string> warnings = [];
            if (MinMemory > MaxMemory) warnings.Add("Minimum memory shouldn't be greater than maximum memory");
            if (!CheckComponents.IsValidJava(UsingJava)) warnings.Add("The configured Java is not valid");
            warnings.AddRange(from arg in ExtJvm where !CheckComponents.ExtJvm(arg).Passed select $"Invalid extra JVM argument {arg}");
            if (ServerType is ServerCoreType.Forge or ServerCoreType.ForgeInstaller or ServerCoreType.ForgeShim)
            {
                if (ForgeCoreInfo is null || !File.Exists(ForgeCoreInfo.WinLibraryArgsPath) || !File.Exists(ForgeCoreInfo.UnixLibraryArgsPath))
                {
                    var detectResult = await ForgeConfigHelper.GetForgeConfig(ServerPath);
                    if (detectResult.IsError) return ServiceResult.Fail("Cannot get the correct core info of the forge server");
                    ForgeCoreInfo = ForgeCoreConfigV1.FromTuple(detectResult.Result);
                }
            }
            else if (ServerType is ServerCoreType.Error)
            {
                if (CommonCoreInfo is not null && File.Exists(CommonCoreInfo.JarName))
                {
                    var detectResult = await CoreTypeHelper.GetCoreType(CommonCoreInfo.JarName);
                    if (detectResult.IsError) return ServiceResult.Fail("Cannot get the core type");
                    ServerType = detectResult.Result;
                    continue;
                }

                if (ForgeCoreInfo is not null && File.Exists(ForgeCoreInfo.WinLibraryArgsPath) && File.Exists(ForgeCoreInfo.UnixLibraryArgsPath))
                {
                    ServerType = ServerCoreType.Forge;
                }
                else return ServiceResult.Fail("Neither core file nor forge arguments is valid");
            }
            else if (CommonCoreInfo is null || !File.Exists(CommonCoreInfo.JarName))
                return ServiceResult.Fail("The jar info of the server is invalid");
            return warnings.Count > 0 ? ServiceResult.Warning(new StringBuilder().AppendJoin('\n', warnings).ToString()) : ServiceResult.Success();
        }
        return ServiceResult.Fail("Failed to check server configuration after multiple attempts");
    }
}