using Newtonsoft.Json;

namespace LSL.Common.Models.AppConfigs;

public interface IConfig<TConfig> where TConfig : IConfig<TConfig>
{
    [JsonIgnore]
    public string ConfigFileName { get; }
    public ServiceResult Validate();
    public static abstract ServiceResult<TConfig> Deserialize(string json);
}