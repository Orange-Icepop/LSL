namespace LSL.Common.Models.ServerConfig;

public interface IServerConfig
{
    public int ConfigVersion { get; }
    public Task<ServiceResult<LocatedServerConfig>> StandardizeAsync(string path);
    public string Serialize();
}

public interface IServerConfig<TConfig> : IServerConfig where TConfig : IServerConfig<TConfig>
{
    public static abstract ServiceResult<TConfig> Deserialize(string content);
}