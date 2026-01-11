using System.Diagnostics.CodeAnalysis;
using System.Text;
using LSL.Common.Utilities;
using LSL.Common.Validation;
using Newtonsoft.Json;

namespace LSL.Common.Models.ServerConfigs;

public class PathedServerConfig(
    string serverPath,
    string serverName,
    string usingJava,
    string coreName,
    uint minMemory,
    uint maxMemory,
    string[] extJvm,
    bool enablePreLaunchProtection,
    ServerCoreType serverType,
    ForgeConfigV1? forgeInfo)
{
    public string ServerPath { get; set; } = serverPath;
    public string ServerName { get; set; } = serverName;
    public string UsingJava { get; set; } = usingJava;
    public string CoreName { get; set; } = coreName;
    public uint MinMemory { get; set; } = minMemory;
    public uint MaxMemory { get; set; } = maxMemory;
    public List<string> ExtJvm { get; set; } = [..extJvm];
    public bool EnablePreLaunchProtection { get; set; } = enablePreLaunchProtection;
    public ServerCoreType ServerType { get; set; } = serverType;

    [MemberNotNullWhen(true, nameof(ForgeInfo))]
    public bool IsForge => ServerType == ServerCoreType.Forge;
    public ForgeConfigV1? ForgeInfo { get; set; } = forgeInfo;

    public static PathedServerConfig Empty =>
        new(string.Empty, string.Empty, string.Empty, string.Empty, 1024, 4096, [], true,
            ServerCoreType.Unknown, null);

    public IndexedServerConfig AsIndexed(int serverId) => new(serverId, this);

    public async Task<ServiceResult> FixAsync()
    {
        if (!Path.Exists(ServerPath)) return ServiceResult.Fail(new DirectoryNotFoundException($"Server {ServerPath} does not exist"));
        List<string> warnings = [];
        if (MinMemory > MaxMemory) warnings.Add("Minimum memory shouldn't be greater than maximum memory");
        if (!CheckComponents.IsValidJava(UsingJava)) warnings.Add("The configured Java is not valid");
        warnings.AddRange(from arg in ExtJvm where !CheckComponents.ExtJvm(arg).Passed select $"Invalid extra JVM argument {arg}");
        if (ServerType == ServerCoreType.Error)
        {
            var coreTypeResult = await CoreTypeHelper.GetCoreType(Path.Combine(ServerPath, CoreName));
            if (coreTypeResult.IsError) return ServiceResult.Fail(coreTypeResult.Error);
            if (coreTypeResult.Result == ServerCoreType.ForgeInstaller)
            {
                if (ForgeInfo != null) ServerType = ServerCoreType.Forge;
                else
                {
                    var forgeDetectResult = await ForgeConfigHelper.GetForgeConfig(ServerPath);
                    if (forgeDetectResult.IsError) return ServiceResult.Fail(forgeDetectResult.Error);
                    ServerType = ServerCoreType.Forge;
                    ForgeInfo = ForgeConfigV1.FromTuple(forgeDetectResult.Result);
                }
            }
            else ServerType = coreTypeResult.Result;
        }

        if (warnings.Count > 0) return ServiceResult.Warning(new StringBuilder().AppendJoin('\n', warnings).ToString());
        return ServiceResult.Success();
    }
}