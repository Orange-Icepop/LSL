using LSL.Common.Utilities.Json;
using Newtonsoft.Json;

namespace LSL.Common.Models.ServerConfigs;

public class ServerConfigV2 : IServerConfig<ServerConfigV2>
{
    private ServerConfigV2()
    {
        EnablePreLaunchProtection = ServerType is not ServerCoreType.Mohist;
    }
    
    public int ConfigVersion => 2;
    public string? Name { get; set; } = string.Empty;
    public ServerCoreType? ServerType { get; set; } = ServerCoreType.Error;
    
    public CommonCoreConfigV1? CommonCoreInfo { get; set; }
    public ForgeCoreConfigV1? ForgeCoreInfo { get; set; }
    public string? JavaPath { get; set; }
    public uint? MinMemory { get; set; }
    public uint? MaxMemory { get; set; }
    public string[]? ExtraJvmArgs { get; set; }
    public bool? EnablePreLaunchProtection { get; set; }

    public static ServerConfigV2 Deserialize(string json) =>
        JsonConvert.DeserializeObject<ServerConfigV2>(json, ConfigSerializerOptions.DefaultOptions) ?? new ServerConfigV2();

    public Task<ServiceResult<LocatedServerConfig>> StandardizeAsync(string path) => LocatedServerConfig.CreateAsync(path,
        Name, ServerType, CommonCoreInfo, ForgeCoreInfo, JavaPath, MinMemory, MaxMemory, ExtraJvmArgs,
        EnablePreLaunchProtection);

    public string Serialize() => JsonConvert.SerializeObject(this, ConfigSerializerOptions.DefaultOptions);
}