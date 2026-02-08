using System;
using System.IO;
using System.Threading.Tasks;
using LSL.Common;
using LSL.Common.Models.AppConfig;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;

public abstract class ConfigManagerComponentBase<T, TConfig> : IConfigManager<TConfig>
    where T : ConfigManagerComponentBase<T, TConfig> where TConfig : class, IConfig<TConfig>, new()
{
    protected ConfigManagerComponentBase(ILogger<T> logger)
    {
        Logger = logger;
        Config = new TConfig();
    }

    protected abstract string ConfigPath { get; }
    protected readonly ILogger<T> Logger;

    public TConfig Config { get; protected set; }

    public virtual async Task<Result<TConfig>> LoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Logger.LogWarning("Config file {configPath} doesn't exist. Generating default config...", ConfigPath);
                return await SetAndWriteAsync(new TConfig()).Match(
                        _ => Logger.LogInformation("Config file {configPath} of type {type} generated", ConfigPath,
                            typeof(TConfig).Name),
                        (_, _) => Logger.LogInformation("Config file {configPath} of type {type} generated", ConfigPath,
                            typeof(TConfig).Name),
                        ex => Logger.LogError(ex, "Cannot write {type}.", typeof(TConfig).Name))
                    .Bind(_ => Result.Success(Config));
            }

            return await TConfig.Deserialize(await File.ReadAllTextAsync(ConfigPath))
                .Bind(config => config.ValidateAndFix())
                .MatchAsync(config =>
                    {
                        Logger.LogInformation("Config file of type {type} loaded", typeof(TConfig).Name);
                        Config = config;
                        return Task.CompletedTask;
                    }, async (config, ex) =>
                    {
                        Logger.LogWarning(ex, "Config file of type {type} loaded with warning.", typeof(TConfig).Name);
                        Config = config;
                        await SetAndWriteAsync(config);
                    },
                    ex =>
                    {
                        Logger.LogError(ex, "A fatal error occured while reading config file of type {type}.",
                            typeof(TConfig).Name);
                        return Task.CompletedTask;
                    });
        }
        catch (Exception e)
        {
            Logger.LogError(e, "A fatal error occured while reading config file of type {type}.", typeof(TConfig).Name);
            return Result.Fail<TConfig>(e);
        }
    }

    public virtual async Task<Result<TConfig>> SetAndWriteAsync(TConfig config)
    {
        try
        {
            return await config.ValidateAndFix()
                .BindAsync(async c =>
                {
                    try
                    {
                        await File.WriteAllTextAsync(ConfigPath, c.Serialize());
                        return Result.Success(c);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "An error occured while writing config file of type {type}.",
                            typeof(TConfig).Name);
                        return Result.Fail<TConfig>(e);
                    }
                }).Match(
                    _ => Logger.LogInformation("{type} written.", typeof(TConfig).Name),
                    (_, e) => Logger.LogWarning(e, "{type} written with warnings.", typeof(TConfig).Name),
                    e => Logger.LogError(e, "An error occured while writing config file of type {type}.",
                        typeof(TConfig).Name));
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An error occured while writing config file of type {type}.", typeof(TConfig).Name);
            return Result.Fail<TConfig>(e);
        }
    }
}