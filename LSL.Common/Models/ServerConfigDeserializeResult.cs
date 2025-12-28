using LSL.Common.Models.ServerConfigs;

namespace LSL.Common.Models;

public enum ServerConfigParseResultType
{
    Success,
    ServerNotFound,
    ConfigFileNotFound,
    MissingKey,
    Unparsable,
    EmptyConfig,
    NoReadAccess
}

public record ServerConfigDeserializeResult
{
    public ServerConfigParseResultType Status { get; }
    public IndexedServerConfig Config { get; }
    
    public ServerConfigDeserializeResult(ServerConfigParseResultType status, IndexedServerConfig? config = null)
    {
        Status = status;
        Config = config ?? IndexedServerConfig.None;
    }
}