using System.Collections.Immutable;
using LSL.Common.Models.ServerConfig;
using LSL.Common.Results;

namespace LSL.Common.Models.Minecraft;

public class ServerConfigList : Result<ImmutableDictionary<int, IndexedServerConfig>>
{
    private ServerConfigList(
        ImmutableDictionary<int, IndexedServerConfig>? configs, 
        ResultType resultType = ResultType.Success, 
        Exception? error = null, 
        string? errors = null, 
        string? warnings = null)
        : base(resultType, configs, error)
    {
        ErrorMessages = errors ?? string.Empty;
        WarningMessages = warnings ?? string.Empty;
    }

    public string ErrorMessages { get; }
    public string WarningMessages { get; }
    

    public static ServerConfigList Success(ImmutableDictionary<int, IndexedServerConfig> configs) 
        => new(configs);
    
    public static ServerConfigList Fail(Exception error) 
        => new(null, ResultType.Error, error);
    
    public static ServerConfigList PartialError(
        ImmutableDictionary<int, IndexedServerConfig> configs, 
        string errors,
        string warnings) 
        => new(configs, ResultType.Warning, null, errors, warnings);
}