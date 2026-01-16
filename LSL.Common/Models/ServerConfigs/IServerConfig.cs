using System.Text.Json;

namespace LSL.Common.Models.ServerConfigs;

public interface IServerConfig
{
    PathedServerConfig Standardize(string path);
    string Serialize();
}

public interface IServerConfig<TConfig> : IServerConfig where TConfig : IServerConfig<TConfig>
{
    static abstract ServiceResult<TConfig> Deserialize(JsonElement configRoot);
}