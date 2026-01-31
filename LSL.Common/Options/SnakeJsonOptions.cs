using System.Text.Json.Serialization;
using LSL.Common.Models.Api;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;

namespace LSL.Common.Options;

[JsonSerializable(typeof(JavaInfo))]
[JsonSerializable(typeof(Dictionary<int, JavaInfo>))]
[JsonSerializable(typeof(Dictionary<string, JavaInfo>))]
[JsonSerializable(typeof(Dictionary<int, string>))]
[JsonSerializable(typeof(ServerConfigV1))]
[JsonSerializable(typeof(GitHubApiAsset))]
[JsonSerializable(typeof(List<GitHubApiAsset>))]
[JsonSerializable(typeof(UpdateApiResponse))]
[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, 
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.Always)]
public partial class SnakeJsonOptions : JsonSerializerContext
{
}