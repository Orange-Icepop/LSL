using System.Text.Json.Serialization;
using LSL.Common.Models;
using LSL.Common.Models.ServerConfig;

namespace LSL.Common.Options;

[JsonSerializable(typeof(JavaInfo))]
[JsonSerializable(typeof(Dictionary<int, JavaInfo>))]
[JsonSerializable(typeof(Dictionary<string, JavaInfo>))]
[JsonSerializable(typeof(ServerConfigV1))]
[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public partial class SnakeJsonOptions : JsonSerializerContext
{
}