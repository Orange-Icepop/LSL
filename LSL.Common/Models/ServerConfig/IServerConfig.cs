namespace LSL.Common.Models.ServerConfig;

public interface IServerConfig
{
    public static abstract string ConfigFileName { get; }
    public Task<ServiceResult<LocatedServerConfig>> StandardizeAsync(string path);
    public string Serialize();
}

public interface IServerConfig<TConfig> : IServerConfig where TConfig : IServerConfig<TConfig>
{
    public static abstract ServiceResult<TConfig> Deserialize(string content);
}