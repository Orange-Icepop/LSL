using System;
using System.IO;
using System.Threading.Tasks;
using FluentResults;
using LSL.Common.Extensions;
using LSL.Common.Models.AppConfig;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;

public abstract class ConfigManagerComponentBase<T, TConfig> : IConfigManager<TConfig>
    where T : ConfigManagerComponentBase<T, TConfig> where TConfig : class, IConfig<TConfig>, new()
{
    protected readonly ILogger<T> Logger;

    protected ConfigManagerComponentBase(ILogger<T> logger)
    {
        Logger = logger;
        Config = new TConfig();
    }

    protected abstract string ConfigPath { get; }

    public TConfig Config { get; protected set; }

    public virtual async Task<Result<TConfig>> LoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Logger.LogWarning("Config file {configPath} doesn't exist. Generating default config...", ConfigPath);
                return await SetAndWriteAsync(new TConfig()).Handle(
                    _ => Logger.LogInformation("Config file {configPath} of type {type} generated", ConfigPath,
                        typeof(TConfig).Name),
                    (_, _) => Logger.LogInformation("Config file {configPath} of type {type} generated", ConfigPath,
                        typeof(TConfig).Name),
                    ex => Logger.LogError("Cannot write {type}.\n{ex}", typeof(TConfig).Name, ex.FlattenToString()));
            }

            return await TConfig.Deserialize(await File.ReadAllTextAsync(ConfigPath))
                .Bind(config => config.ValidateAndFix())
                .Handle(config =>
                    {
                        Logger.LogInformation("Config file of type {type} loaded", typeof(TConfig).Name);
                        Config = config;
                        return Task.CompletedTask;
                    }, async (config, ex) =>
                    {
                        Logger.LogWarning("Config file of type {type} loaded with warning.\n{ex}", typeof(TConfig).Name,
                            ex.FlattenToString());
                        Config = config;
                        await SetAndWriteAsync(config);
                    },
                    ex =>
                    {
                        Logger.LogError("A fatal error occured while reading config file of type {type}.\n{ex}",
                            typeof(TConfig).Name, ex.FlattenToString());
                        return Task.CompletedTask;
                    });
        }
        catch (Exception e)
        {
            Logger.LogError(e, "A fatal error occured while reading config file of type {type}.", typeof(TConfig).Name);
            return Result.Fail<TConfig>(new ExceptionalError(e));
        }
    }

    public virtual async Task<Result<TConfig>> SetAndWriteAsync(TConfig config)
    {
        try
        {
            return await config.ValidateAndFix()
                .Bind(async Task<Result<TConfig>> (c) =>
                {
                    try
                    {
                        await File.WriteAllTextAsync(ConfigPath, c.Serialize());
                        return Result.Ok(c);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "An error occured while writing config file of type {type}.",
                            typeof(TConfig).Name);
                        return Result.Fail<TConfig>(new ExceptionalError(e));
                    }
                }).Handle(
                    _ => Logger.LogInformation("{type} written.", typeof(TConfig).Name),
                    (_, e) => Logger.LogWarning("{type} written with warnings.\n{ex}", typeof(TConfig).Name, e),
                    e => Logger.LogError("An error occured while writing config file of type {type}.\n{ex}",
                        typeof(TConfig).Name, e));
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An error occured while writing config file of type {type}.", typeof(TConfig).Name);
            return Result.Fail<TConfig>(new ExceptionalError(e));
        }
    }
}