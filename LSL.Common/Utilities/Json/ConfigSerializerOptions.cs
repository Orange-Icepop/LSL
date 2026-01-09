using System.Text.Json;
using System.Text.Json.Serialization;

namespace LSL.Common.Utilities.Json;

public static class ConfigSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}