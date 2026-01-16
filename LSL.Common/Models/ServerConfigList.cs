using LSL.Common.Models.ServerConfigs;

namespace LSL.Common.Models;

public record ServerConfigList : ServiceResult<IDictionary<int, IndexedServerConfig>>
{
    private ServerConfigList(
        IDictionary<int, IndexedServerConfig>? configs, 
        ServiceResultType resultType = ServiceResultType.Success, 
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
    

    public static ServerConfigList Success(IDictionary<int, IndexedServerConfig> configs) 
        => new(configs);
    
    public static ServerConfigList Fail(Exception error) 
        => new(null, ServiceResultType.Error, error);
    
    public static ServerConfigList PartialError(
        IDictionary<int, IndexedServerConfig> configs, 
        string errors, 
        string warnings) 
        => new(configs, ServiceResultType.Warning, null, errors, warnings);
}