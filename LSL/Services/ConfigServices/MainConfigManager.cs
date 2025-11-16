using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LSL.Common.Models;
using LSL.Common.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LSL.Services.ConfigServices;
/// <summary>
/// The config manager of LSL's main config.
/// </summary>
/// <param name="logger">An ILogger that logs logs. （拜托，想个更好的双关语吧（彼得帕克音））</param> 
public class MainConfigManager(ILogger<MainConfigManager> logger)
{
    #region 默认配置字典

    private static readonly FrozenDictionary<string, object> s_defaultConfigs = new Dictionary<string, object>()
    {
        //Common
        { "auto_eula", true },
        { "app_priority", 1 },
        { "end_server_when_close", false },
        { "daemon", true },
        { "coloring_terminal", true },
        //Download
        { "download_source", 0 },
        { "download_threads", 16 },
        { "download_limit", 0 },
        //Panel
        { "panel_enable", false },
        { "panel_port", 25000 },
        { "panel_monitor", true },
        { "panel_terminal", true },
        //Style:off
        //About
        { "auto_update", true },
        { "beta_update", false }
    }.ToFrozenDictionary();

    #endregion

    #region 使用的键的列表

    private static readonly IReadOnlyList<string> s_configKeys = new List<string>()
    {
        //Common
        "auto_eula",
        "app_priority",
        "end_server_when_close",
        "daemon",
        "coloring_terminal",
        //Download
        "download_source",
        "download_threads",
        "download_limit",
        //Panel
        "panel_enable",
        "panel_port",
        "panel_monitor",
        "panel_terminal",
        //About
        "auto_update",
        "beta_update"
    }.AsReadOnly();

    #endregion

    #region 集体修改配置方法 ConfirmConfig(Dictionary<string, object> confs)

    public async Task<ServiceResult<FrozenDictionary<string, object>>> ConfirmConfig(IDictionary<string, object> configs)
    {
        try
        {
            Dictionary<string, object> fin = [];
            foreach (var conf in configs)
            {
                if (CheckService.VerifyConfig(conf.Key, conf.Value))
                {
                    fin.TryAdd(conf.Key, conf.Value);
                }
            }
            CurrentConfigs = fin.ToFrozenDictionary();
            await File.WriteAllTextAsync(ConfigPathProvider.ConfigFilePath,
                JsonConvert.SerializeObject(fin, Formatting.Indented));
            logger.LogInformation("New LSL main config is written.");
            return ServiceResult.Success(fin.ToFrozenDictionary());
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occured while writing new main config.");
            return ServiceResult.Fail<FrozenDictionary<string, object>>(e);
        }
    }

    #endregion

    #region 读取配置键值

    // 当前配置字典
    public FrozenDictionary<string, object> CurrentConfigs { get; private set; } = FrozenDictionary<string, object>.Empty;

    public async Task<ServiceResult> LoadConfig()
    {
        try
        {
            logger.LogInformation("Loading main config...");
            JObject configs;
            try
            {
                configs = JObject.Parse(await File.ReadAllTextAsync(ConfigPathProvider.ConfigFilePath));
            }
            catch (FileNotFoundException ex)
            {
                logger.LogError(ex, "LSL main config file not found.");
                return ServiceResult.Fail(ex);
            }
            catch (JsonReaderException ex)
            {
                logger.LogError(ex, "LSL main config file is not parsable.");
                return ServiceResult.Fail(ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occured while reading main config file.");
                return ServiceResult.Fail(ex);
            }

            Dictionary<string, object> cache = [];
            List<string> keysNeedToRepair = [];
            foreach (var key in s_configKeys)
            {
                var config = configs.TryGetValue(key, out var value) ? value : null;
                object? keyValue = config?.Type switch // 根据值类型读取
                {
                    JTokenType.Boolean => config.Value<bool>(),
                    JTokenType.Integer => config.Value<int>(),
                    JTokenType.String => config.Value<string>(),
                    _ => null,
                };
                if (keyValue == null || !CheckService.VerifyConfig(key, keyValue))
                {
                    if (!s_defaultConfigs.TryGetValue(key, out var defaultConfig))
                    {
                        var msg = new KeyNotFoundException($"No such key found in default config:{key}");
                        logger.LogError(msg, "");
                        return ServiceResult.Fail(msg);
                    }

                    cache.Add(key, defaultConfig);
                    keysNeedToRepair.Add(key);
                }
                else
                {
                    cache.Add(key, keyValue);
                }
            }

            if (keysNeedToRepair.Count > 0) // 修复配置
            {
                await File.WriteAllTextAsync(ConfigPathProvider.ConfigFilePath, JsonConvert.SerializeObject(cache, Formatting.Indented));
                CurrentConfigs = cache.ToFrozenDictionary();
                var list = new StringBuilder("The following main config keys are reset due to value type mismatch:");
                list.AppendJoin(",", keysNeedToRepair);
                logger.LogWarning("{}", list.ToString());
                logger.LogInformation("Config.json loaded and repaired.");
                return ServiceResult.FinishWithWarning(new Exception(list.ToString()));
            }
            CurrentConfigs = cache.ToFrozenDictionary();
            logger.LogInformation("Config.json loaded.");
            return ServiceResult.Success();

        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occured while loading main config.");
            return ServiceResult.Fail(e);
        }
    }

    #endregion

    #region 初始化
    internal static async Task<ServiceResult> InitAsync()
    {
        // 将初始配置字典序列化成JSON字符串并写入文件  
        string configString = JsonConvert.SerializeObject(s_defaultConfigs, Formatting.Indented);
        await File.WriteAllTextAsync(ConfigPathProvider.ConfigFilePath, configString);
        return ServiceResult.Success();
    }

    #endregion
}
