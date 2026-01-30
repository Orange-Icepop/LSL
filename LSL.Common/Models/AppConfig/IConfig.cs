using System.Runtime.Serialization;

namespace LSL.Common.Models.AppConfig;

public interface IConfig
{
    [IgnoreDataMember]
    public static abstract string ConfigFileName { get; }
}

public interface IConfig<TConfig> : IConfig where TConfig : IConfig<TConfig>
{
    public Result Validate();
    public static abstract Result<TConfig> Deserialize(string content);
}