using System.Runtime.Serialization;
using Tomlyn;

namespace LSL.Common.Models.AppConfig;

public interface IConfig
{
    public string Serialize();
}

public interface IConfig<TConfig> : IConfig where TConfig : class, IConfig<TConfig>, new()
{
    public Result Validate();
    public Result<TConfig> ValidateAndFix();
    public static abstract Result<TConfig> Deserialize(string content);
}