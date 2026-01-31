using System;
using System.IO;
using System.Threading.Tasks;
using LSL.Common;
using LSL.Common.Models.AppConfig;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace LSL.Services.ConfigServices;

public abstract class ConfigManagerComponentBase<T, TConfig> : IConfigManager<TConfig>
    where T : ConfigManagerComponentBase<T, TConfig> where TConfig : IConfig<TConfig>, new()
{
    private ConfigManagerComponentBase(ILogger<T> logger)
    {
        Logger = logger;
    }

    protected abstract string ConfigPath { get; }
    protected readonly ILogger<T> Logger;
    protected readonly AsyncReaderWriterLock Lock = new();
    private TConfig _config = new();

    protected TConfig Config
    {
        get
        {
            using (Lock.ReaderLock())
            {
                return _config;
            }
        }
        set
        {
            using (Lock.WriterLock())
            {
                _config = value;
            }
        }
    }

    public TConfig CloneConfig()
    {
        using (Lock.ReaderLock())
        {
            return _config.Clone();
        }
    }

    public virtual async Task<Result<TConfig>> LoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Logger.LogWarning("Config file {configPath} doesn't exist. Generating default config...", ConfigPath);
                return await SetAndWriteAsync(new TConfig()).AsGeneric().Match(
                        _ => Logger.LogInformation("Config file {configPath} of type {type} generated", ConfigPath,
                            typeof(TConfig).Name),
                        (_, _) => Logger.LogInformation("Config file {configPath} of type {type} generated", ConfigPath,
                            typeof(TConfig).Name),
                        ex => Logger.LogError(ex, "Cannot write config."))
                    .Bind(_ => Result.Success(Config.Clone()));
            }

            return TConfig.Deserialize(await File.ReadAllTextAsync(ConfigPath))
                .Bind(config => config.Validate().Bind(_ => Result.Success(config)))
                .Match(config =>
                    {
                        Logger.LogInformation("Config file of type {type} loaded", typeof(TConfig).Name);
                        Config = config;
                    }, (config, ex) =>
                    {
                        Logger.LogWarning(ex, "Config file of type {type} loaded with warning.", typeof(TConfig).Name);
                        Config = config;
                    },
                    ex => Logger.LogError(ex, "An error occured while reading config file of type {type}.",
                        typeof(TConfig).Name))
                .Bind(config => Result.Success(config.Clone()));
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An error occured while reading config file of type {type}.", typeof(TConfig).Name);
            return Result.Fail<TConfig>(e);
        }
    }

    public virtual async Task<Result> SetAndWriteAsync(TConfig config)
    {
        try
        {
            return await config.ValidateAndFix()
                .BindAsync<TConfig, Unit>(async c =>
                {
                    try
                    {
                        await File.WriteAllTextAsync(ConfigPath, c.Serialize());
                        return Result.Success();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "An error occured while writing config file of type {type}.",
                            typeof(TConfig).Name);
                        return Result.Fail(e);
                    }
                }).Match(
                    _ => Logger.LogInformation("{type} written.", typeof(TConfig).Name),
                    (_, e) => Logger.LogWarning(e, "{type} written with warnings.", typeof(TConfig).Name),
                    e => Logger.LogError(e, "An error occured while writing config file of type {type}.",
                        typeof(TConfig).Name))
                .AsSimple();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An error occured while writing config file of type {type}.", typeof(TConfig).Name);
            return Result.Fail(e);
        }
    }
}