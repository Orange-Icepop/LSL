using System.Collections.Frozen;

namespace LSL.Common.Models;

public record ServerConfigReadResult : ServiceResult<FrozenDictionary<int, ServerConfig>>
{
    private ServerConfigReadResult(
        FrozenDictionary<int, ServerConfig> configs, 
        ServiceResultType resultType = ServiceResultType.Success, 
        Exception? error = null, 
        IList<string>? notFoundServers = null, 
        IList<string>? configErrorServers = null)
        : base(resultType, configs, error)
    {
        NotFoundServers = notFoundServers ?? [];
        ConfigErrorServers = configErrorServers ?? [];
    }

    public IList<string> NotFoundServers { get; }
    public IList<string> ConfigErrorServers { get; }
    

    public static ServerConfigReadResult Success(FrozenDictionary<int, ServerConfig> configs) 
        => new(configs);
    
    public static ServerConfigReadResult Fail(Exception error) 
        => new(FrozenDictionary<int, ServerConfig>.Empty, ServiceResultType.Error, error);
    
    public static ServerConfigReadResult PartialError(
        FrozenDictionary<int, ServerConfig> configs, 
        IList<string> notFoundServers, 
        IList<string> configErrorServers) 
        => new(configs, ServiceResultType.FinishWithWarning, null, notFoundServers, configErrorServers);
}