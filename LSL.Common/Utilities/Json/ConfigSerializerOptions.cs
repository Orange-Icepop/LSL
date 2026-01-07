using System.Text.Json;
using System.Text.Json.Serialization;

namespace LSL.Common.Utilities.Json;

public static class ConfigSerializerOptions
{
    public static readonly JsonSerializerOptions Base = new()
    {
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    public static readonly JsonSerializerOptions Loose = new()
    {
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    public static readonly JsonSerializerOptions Strict = new()
    {
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    
}