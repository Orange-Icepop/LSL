using System.Text;
using System.Text.Json;
using LSL.Common.Extensions;

namespace LSL.Common.Models.ServerConfigs;

public class CommonCoreConfigV1
{
    public int ConfigVersion { get; } = 1;
    public string JarName { get; set; } = string.Empty;

    public static ServiceResult<CommonCoreConfigV1> Deserialize(JsonElement configRoot)
    {
        var result = new CommonCoreConfigV1();
        List<string> errors = [];
        configRoot.ParseFileProperty("jarName", s => result.JarName = s, _ => errors.Add("Cannot get jarName Property."));
        if (errors.Count > 0) return ServiceResult.Fail<CommonCoreConfigV1>(new StringBuilder().AppendJoin('\n', errors).ToString());
        return !result.JarName.EndsWith(".jar") ? ServiceResult.Warning(result, "Warn that the selected server core is not a .jar file") : ServiceResult.Success(result);
    }
}