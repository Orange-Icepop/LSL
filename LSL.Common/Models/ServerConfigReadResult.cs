using System.Collections.Frozen;

namespace LSL.Common.Models;

public record ServerConfigReadResult : IServiceResult
{
    private ServerConfigReadResult(FrozenDictionary<int, ServerConfig> configs, ServiceResultType errorCode = ServiceResultType.Success, Exception? error = null, IList<string>? notFoundServers = null, IList<string>? configErrorServers = null)
    {
        Configs = configs;
        ResultType = errorCode;
        Error = error;
        NotFoundServers = notFoundServers ?? [];
        ConfigErrorServers = configErrorServers ?? [];
    }

    public FrozenDictionary<int, ServerConfig> Configs { get; }
    public ServiceResultType ResultType { get; }
    public Exception? Error { get; }
    public IList<string> NotFoundServers { get; }
    public IList<string> ConfigErrorServers { get; }
    public static ServerConfigReadResult Success(FrozenDictionary<int, ServerConfig> configs) => new(configs);
    public static ServerConfigReadResult Fail(Exception error) => new(FrozenDictionary<int, ServerConfig>.Empty, ServiceResultType.Error, error);
    public static ServerConfigReadResult PartConfigError(FrozenDictionary<int, ServerConfig> configs, IList<string> nfs, IList<string> ces) => new(configs, notFoundServers:nfs, configErrorServers:ces);
}