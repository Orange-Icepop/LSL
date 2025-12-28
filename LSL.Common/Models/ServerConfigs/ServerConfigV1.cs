using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using LSL.Common.Exceptions;

namespace LSL.Common.Models.ServerConfigs;

public class ServerConfigV1
{
    public string Name { get; set; } = string.Empty;
    public string UsingJava { get; set; } = string.Empty;
    public string CoreName { get; set; } = string.Empty;
    public uint MinMemory { get; set; } = 1024;
    public uint MaxMemory { get; set; } = 4096;
    public string ExtJvm { get; set; } = string.Empty;

    public static ServerConfigV1 Create(string serverName, string usingJava, string coreName, uint minMemory,
        uint maxMemory,
        string extJvm) => new()
    {
        Name = serverName,
        UsingJava = usingJava,
        CoreName = coreName,
        MinMemory = minMemory,
        MaxMemory = maxMemory,
        ExtJvm = extJvm
    };

    [JsonIgnore] public static readonly JsonSerializerOptions LooseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    [JsonIgnore] public static readonly JsonSerializerOptions StrictSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        NumberHandling = JsonNumberHandling.Strict,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public static ServerConfigV1 Parse(string configFile, bool ignoreWarnings)
    {
        using var doc = JsonDocument.Parse(configFile);
        return Parse(doc.RootElement, ignoreWarnings);
    }

    public static ServerConfigV1 Parse(JsonElement configRoot, bool ignoreWarnings)
    {
        try
        {
            var res = configRoot.Deserialize<ServerConfigV1>(ignoreWarnings
                ? LooseSerializerOptions
                : StrictSerializerOptions);
            if (res is null && !ignoreWarnings)
                throw new ServerConfigUnparsableException("Cannot parse the selected ServerConfigV1");
            return res ?? new ServerConfigV1();
        }
        catch (JsonException ex)
        {
            throw new ServerConfigUnparsableException("Cannot parse the selected ServerConfigV1", ex);
        }
    }

    public static bool TryParse(JsonElement configRoot, bool ignoreWarnings,
        [NotNullWhen(true)] out ServerConfigV1? result)
    {
        try
        {
            var res = configRoot.Deserialize<ServerConfigV1>(ignoreWarnings
                ? LooseSerializerOptions
                : StrictSerializerOptions);
            if (res is null && !ignoreWarnings)
            {
                result = null;
                return false;
            }

            result = res ?? new ServerConfigV1();
            return true;
        }
        catch (JsonException)
        {
            result = null;
            return false;
        }
    }
}