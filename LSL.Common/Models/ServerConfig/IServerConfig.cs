namespace LSL.Common.Models.ServerConfig;

public interface IServerConfig
{
    static abstract string ConfigFileName { get; }
    Task<Result<LocatedServerConfig>> StandardizeAsync(string path);
    string Serialize();
    public static abstract bool Exists(string path);
    public Task<Result> WriteToFileAsync(string path);
    public static abstract Task<Result<LocatedServerConfig>> ReadFromFileAsync(string path);
}

public interface IServerConfig<TConfig> : IServerConfig where TConfig : IServerConfig<TConfig>
{
    static abstract Result<TConfig> Deserialize(string content);
}