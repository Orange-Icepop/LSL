using Newtonsoft.Json;

namespace LSL.Common.Models.AppConfigs;

public interface IConfig
{
    public static abstract string ConfigFileName { get; }
}

public interface IConfig<TConfig> : IConfig where TConfig : IConfig<TConfig>
{
    public ServiceResult Validate();
    public static abstract ServiceResult<TConfig> Deserialize(string json);
}