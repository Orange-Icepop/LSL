using System.Runtime.Serialization;

namespace LSL.Common.Models.AppConfig;

public interface IConfig
{
    [IgnoreDataMember]
    public static abstract string ConfigFileName { get; }
}

public interface IConfig<TConfig> : IConfig where TConfig : IConfig<TConfig>
{
    public ServiceResult Validate();
    public static abstract ServiceResult<TConfig> Deserialize(string content);
}