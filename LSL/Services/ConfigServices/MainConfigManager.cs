using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    private ILogger<MainConfigManager> _logger { get; } = logger;
    
    #region 默认配置字典

    private static readonly IReadOnlyDictionary<string, object> DefaultConfigs = new Dictionary<string, object>()
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
    }.AsReadOnly();

    #endregion

    #region 使用的键的列表

    private static readonly IReadOnlyList<string> ConfigKeys = new List<string>()
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

    public ServiceResult<ConcurrentDictionary<string, object>> ConfirmConfig(IDictionary<string, object> confs)
    {
        ConcurrentDictionary<string, object> fin = [];
        foreach (var conf in confs)
        {
            if (CheckService.VerifyConfig(conf.Key, conf.Value))
            {
                fin.TryAdd(conf.Key, conf.Value);
            }
        }
        CurrentConfigs = fin;
        File.WriteAllText(ConfigPathProvider.ConfigFilePath,
            JsonConvert.SerializeObject(fin, Formatting.Indented));
        _logger.LogInformation("New LSL main config is written.");
        return ServiceResult.Success(fin);
    }

    #endregion

    #region 读取配置键值

    // 当前配置字典
    public ConcurrentDictionary<string, object> CurrentConfigs { get; private set; } = [];

    public ServiceResult LoadConfig()
    {
        _logger.LogInformation("Loading main config...");
        JObject configs;
        try
        {
            configs = JObject.Parse(File.ReadAllText(ConfigPathProvider.ConfigFilePath));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "LSL main config file not found.");
            return ServiceResult.Fail(ex);
        }
        catch (JsonReaderException ex)
        {
            _logger.LogError(ex, "LSL main config file is not parsable.");
            return ServiceResult.Fail(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured while reading main config file.");
            return ServiceResult.Fail(ex);
        }

        ConcurrentDictionary<string, object> cache = [];
        List<string> KNTR = [];
        foreach (var key in ConfigKeys)
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
                if (!DefaultConfigs.TryGetValue(key, out var defaultConfig))
                {
                    string msg = $"No such key found in default config:{key}";
                    _logger.LogError("{msg}", msg);
                    return ServiceResult.Fail(new Exception(msg));
                }

                cache.AddOrUpdate(key, k => defaultConfig, (k, v) => defaultConfig);
                KNTR.Add(key);
            }
            else
            {
                cache.AddOrUpdate(key, k => keyValue, (k, v) => keyValue);
            }
        }

        if (KNTR.Count > 0) // 修复配置
        {
            File.WriteAllText(ConfigPathProvider.ConfigFilePath, JsonConvert.SerializeObject(cache, Formatting.Indented));
            CurrentConfigs = cache;
            var list = new StringBuilder("The following main config keys are reset due to value type mismatch:");
            list.AppendJoin(", ", KNTR);
            _logger.LogWarning("{}", list.ToString());
            _logger.LogInformation("Config.json loaded and repaired.");
            return ServiceResult.FinishWithWarning(new Exception(list.ToString()));
        }
        CurrentConfigs = cache;
        _logger.LogInformation("Config.json loaded.");
        return ServiceResult.Success();
    }

    #endregion

    #region 初始化
    public ServiceResult Init()
    {
        // 将初始配置字典序列化成JSON字符串并写入文件  
        string configString = JsonConvert.SerializeObject(DefaultConfigs, Formatting.Indented);
        File.WriteAllText(ConfigPathProvider.ConfigFilePath, configString);
        return ServiceResult.Success();
    }

    #endregion
}
