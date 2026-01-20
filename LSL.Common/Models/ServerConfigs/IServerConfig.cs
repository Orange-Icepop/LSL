using System.Text.Json;

namespace LSL.Common.Models.ServerConfigs;

public interface IServerConfig
{
    public int ConfigVersion { get; }
    public Task<ServiceResult<PathedServerConfig>> StandardizeAsync(string path);
    public string Serialize();
}

public interface IServerConfig<out TConfig> : IServerConfig where TConfig : IServerConfig<TConfig>
{
    public static abstract TConfig Deserialize(string json);
}