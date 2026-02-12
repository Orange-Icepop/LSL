using FluentResults;

namespace LSL.Common.Models.ServerConfig;

public record CommonCoreConfigV1
{
    public int ConfigVersion => 1;
    public string JarName { get; init; } = string.Empty;
}