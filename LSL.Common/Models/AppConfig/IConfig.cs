using LSL.Common.Results;

namespace LSL.Common.Models.AppConfig;

public interface IConfig
{
    public string Serialize();
}

public interface IConfig<TConfig> : IConfig where TConfig : class, IConfig<TConfig>, new()
{
    public Result Validate();
    public Result<TConfig> ValidateAndFix();
    /// <summary>
    /// Deserialize the config string into config class.
    /// </summary>
    /// <param name="content">The content of config. Format is automatically managed by the class's serializer.</param>
    /// <returns>The deserialized config. Automatically fixed any format issue and packaged with warning, except IOExceptions.</returns>
    public static abstract Result<TConfig> Deserialize(string content);
}