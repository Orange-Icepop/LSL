namespace LSL.Common.Models;

public enum ServerConfigParseResultType
{
    Success,
    ServerNotFound,
    ConfigFileNotFound,
    MissingKey,
    Unparsable,
    EmptyConfig,
}

public record ServerConfigParseResult
{
    public ServerConfigParseResultType Status { get; }
    public ServerConfig Config { get; }
    
    public ServerConfigParseResult(ServerConfigParseResultType status, ServerConfig? config = null)
    {
        Status = status;
        Config = config ?? ServerConfig.None;
    }
}