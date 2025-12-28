using System.Collections.Frozen;
using LSL.Common.Models.ServerConfigs;

namespace LSL.Common.Models;

public record ServerConfigList : ServiceResult<FrozenDictionary<int, IndexedServerConfig>>
{
    private ServerConfigList(
        FrozenDictionary<int, IndexedServerConfig> configs, 
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
    

    public static ServerConfigList Success(FrozenDictionary<int, IndexedServerConfig> configs) 
        => new(configs);
    
    public static ServerConfigList Fail(Exception error) 
        => new(FrozenDictionary<int, IndexedServerConfig>.Empty, ServiceResultType.Error, error);
    
    public static ServerConfigList PartialError(
        FrozenDictionary<int, IndexedServerConfig> configs, 
        IList<string> notFoundServers, 
        IList<string> configErrorServers) 
        => new(configs, ServiceResultType.FinishWithWarning, null, notFoundServers, configErrorServers);
}