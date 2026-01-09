using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LSL.Common.Exceptions;
using LSL.Common.Extensions;
using LSL.Common.Validation;

namespace LSL.Common.Models.ServerConfigs;

public class ServerConfigV1 : IServerConfig<ServerConfigV1>
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
    
    [JsonIgnore] public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        NumberHandling = JsonNumberHandling.Strict,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public static ServiceResult<ServerConfigV1> Deserialize(JsonElement configRoot, bool ignoreWarnings)
    {
        var result = new ServerConfigV1();
        List<string> warnings = [];
        foreach (var prop in configRoot.EnumerateObject())
        {
            switch (prop.Name)
            {
                case "name":
                {
                    if (!prop.Value.TryGetString(out var name))
                    {
                        var ex = "Property \"name\" doesn't exists or is not a string.";
                        if (!ignoreWarnings)
                            return ServiceResult.Fail<ServerConfigV1>(
                                new Exception(ex));
                        warnings.Add(ex);
                        break;
                    }

                    result.Name = name;
                    break;
                }
                case "using_java":
                {
                    if (!prop.Value.TryGetString(out var usingJava))
                    {
                        var ex = "Property \"using_java\" doesn't exists or is not a string.";
                        if (!ignoreWarnings)
                            return ServiceResult.Fail<ServerConfigV1>(
                                new Exception(ex));
                        warnings.Add(ex);
                        break;
                    }

                    if (!CheckComponents.IsValidJava(usingJava))
                    {
                        var ex = "The target java is not valid.";
                        if (!ignoreWarnings)
                            return ServiceResult.Fail<ServerConfigV1>(
                                new Exception(ex));
                        warnings.Add(ex);
                        break;
                    }

                    result.UsingJava = usingJava;
                    break;
                }
                case "core_name":
                {
                    if (!prop.Value.TryGetString(out var coreName))
                    {
                        var ex = "Property \"core_name\" doesn't exists or is not a string.";
                        if (!ignoreWarnings)
                            return ServiceResult.Fail<ServerConfigV1>(
                                new Exception(ex));
                        warnings.Add(ex);
                        break;
                    }

                    result.CoreName = coreName;
                    break;
                }
                case "min_memory":
                {
                    if (prop.Value.ValueKind != JsonValueKind.Number || !prop.Value.TryGetUInt32(out var minMemory))
                    {
                        var ex = "Property \"min_memory\" doesn't exists or is not a string.";
                        if (!ignoreWarnings)
                            return ServiceResult.Fail<ServerConfigV1>(
                                new Exception(ex));
                        warnings.Add(ex);
                        break;
                    }

                    result.MinMemory = minMemory;
                    break;
                }
                case "max_memory":
                {
                    if (prop.Value.ValueKind != JsonValueKind.Number || !prop.Value.TryGetUInt32(out var maxMemory)))
                    {
                        var ex = "Property \"max_memory\" doesn't exists or is not a string.";
                        if (!ignoreWarnings)
                            return ServiceResult.Fail<ServerConfigV1>(
                                new Exception(ex));
                        warnings.Add(ex);
                        break;
                    }
                }
                case "ext_jvm":
                {
                    if (!prop.Value.TryGetString(out var extJvm))
                    {
                        warnings.Add("Property \"ext_jvm\" doesn't exists or is not a string.");
                        break;
                    }
                    result.ExtJvm = extJvm;
                    break;
                }
            }
        }
        if (warnings.Count > 0) return ServiceResult.Warning(result,
            new Exception(new StringBuilder().AppendJoin('\n', warnings).ToString()));
        return ServiceResult.Success(result);
    }

    public PathedServerConfig WrapPath(string path)
    {
    }

    public string Serialize()
    {
    }
}