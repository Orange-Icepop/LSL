using LSL.Common.Models.ServerConfig;

namespace LSL.Common.Models.Minecraft;

public record ServerConfigList : Result<IDictionary<int, IndexedServerConfig>>
{
    private ServerConfigList(
        IDictionary<int, IndexedServerConfig>? configs, 
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
    

    public static ServerConfigList Success(IDictionary<int, IndexedServerConfig> configs) 
        => new(configs);
    
    public static ServerConfigList Fail(Exception error) 
        => new(null, ResultType.Error, error);
    
    public static ServerConfigList PartialError(
        IDictionary<int, IndexedServerConfig> configs, 
        string errors, 
        string warnings) 
        => new(configs, ResultType.Warning, null, errors, warnings);
}