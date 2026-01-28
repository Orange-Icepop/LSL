namespace LSL.Common.Models.ServerConfig;

public interface IServerConfig
{
    static abstract string ConfigFileName { get; }
    Task<ServiceResult<LocatedServerConfig>> StandardizeAsync(string path);
    string Serialize();
    public static abstract bool Exists(string path);
    public Task<ServiceResult> WriteToFileAsync(string path);
    public static abstract Task<ServiceResult<LocatedServerConfig>> ReadFromFileAsync(string path);
}

public interface IServerConfig<TConfig> : IServerConfig where TConfig : IServerConfig<TConfig>
{
    static abstract ServiceResult<TConfig> Deserialize(string content);
}