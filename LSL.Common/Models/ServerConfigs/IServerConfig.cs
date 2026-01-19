using System.Text.Json;

namespace LSL.Common.Models.ServerConfigs;

public interface IServerConfig
{
    public int ConfigVersion { get; }
    public PathedServerConfig Standardize(string path);
    public string Serialize();
}

public interface IServerConfig<TConfig> : IServerConfig where TConfig : IServerConfig<TConfig>
{
    public static abstract ServiceResult<TConfig> Deserialize(JsonElement configRoot);
}