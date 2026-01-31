using System.Runtime.Serialization;

namespace LSL.Common.Models.AppConfig;

public interface IConfig
{
    [IgnoreDataMember]
    public static abstract string ConfigFileName { get; }
    public string Serialize();
}

public interface IConfig<TConfig> : IConfig where TConfig : IConfig<TConfig>, new()
{
    public Result Validate();
    public Result<TConfig> ValidateAndFix();
    public static abstract Result<TConfig> Deserialize(string content);
    public TConfig Clone();
}