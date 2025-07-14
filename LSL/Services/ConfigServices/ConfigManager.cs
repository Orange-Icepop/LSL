using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LSL.Common.Models;
using LSL.Common.Utilities;
using LSL.Common.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LSL.Services.ConfigServices;

/// <summary>
/// The wrapper of detailed ConfigManagers.
/// </summary>
public class ConfigManager
{

    private MainConfigManager MCM { get; }
    private ServerConfigManager SCM { get; }
    private JavaConfigManager JCM { get; }
    private ILogger<ConfigManager> _logger { get; }

    public ConfigManager(MainConfigManager mcm, ServerConfigManager scm, JavaConfigManager jcm, ILogger<ConfigManager> logger)
    {
        MCM = mcm;
        SCM = scm;
        JCM = jcm;
        _logger = logger;
        //初始化配置文件
        Initialize();
    }

    #region 初始化配置文件
    private ServiceResult Initialize()
    {
        // 检查权限
        if (!ConfigPathProvider.HasReadWriteAccess(ConfigPathProvider.LSLFolder))
        {
            var error = $"LSL does not have write access to config folder:{ConfigPathProvider.LSLFolder}";
            _logger.LogError("{}", error);
            return ServiceResult.Fail(new UnauthorizedAccessException(error));
        }
        if (!ConfigPathProvider.HasReadWriteAccess(ConfigPathProvider.ServersFolder))
        {
            var error = $"LSL does not have write access to the servers folder:{ConfigPathProvider.ServersFolder}";
            _logger.LogError("{}", error);
            return ServiceResult.Fail(new UnauthorizedAccessException(error));
        }
        // 确保LSL文件夹存在  
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPathProvider.ConfigFilePath));
        Directory.CreateDirectory(ConfigPathProvider.ServersFolder);
        if (!File.Exists(ConfigPathProvider.ConfigFilePath))
        {
            File.WriteAllText(ConfigPathProvider.ConfigFilePath, "{}");
            var mainRes = MCM.Init();
            if (mainRes.ErrorCode == ServiceResultType.Error) return mainRes;
            _logger.LogInformation("Config.json initialized.");
        }

        if (!File.Exists(ConfigPathProvider.ServerConfigPath))
        {
            File.WriteAllText(ConfigPathProvider.ServerConfigPath, "{}");
            _logger.LogInformation("ServerConfig.json initialized.");
        }

        if (!File.Exists(ConfigPathProvider.JavaListPath))
        {
            File.WriteAllText(ConfigPathProvider.JavaListPath, "{}");
            _logger.LogInformation("JavaList.json initialized.");
        }
        return ServiceResult.Success();
    }

    #endregion
        
    #region 配置文件代理操作
    // 主配置文件
    public ConcurrentDictionary<string, object> MainConfigs => MCM.CurrentConfigs;
    public ServiceResult<ConcurrentDictionary<string, object>> ConfirmMainConfig(IDictionary<string, object> confs) => MCM.ConfirmConfig(confs);
    public ServiceResult ReadMainConfig() => MCM.LoadConfig();
    // 服务器配置
    public ConcurrentDictionary<int, ServerConfig> ServerConfigs => SCM.ServerConfigs;
    public ServiceResult ReadServerConfig() => SCM.ReadServerConfig();

    public ServiceResult RegisterServer(FormedServerConfig config) => SCM.RegisterServer(config.ServerName,
        config.JavaPath, config.CorePath, uint.Parse(config.MinMem), uint.Parse(config.MaxMem), config.ExtJvm);
    public ServiceResult EditServer(int id, FormedServerConfig config) => SCM.EditServer(id, config.ServerName, config.JavaPath, 
        uint.Parse(config.MinMem),
        uint.Parse(config.MaxMem), config.ExtJvm);
    public ServiceResult DeleteServer(int id) => SCM.DeleteServer(id);
    // Java配置
    public ConcurrentDictionary<int, JavaInfo> JavaConfigs => JCM.JavaDict;
    public ServiceResult ReadJavaConfig() => JCM.ReadJavaConfig();
    public async Task<ServiceResult> DetectJava() => await JCM.DetectJava();

    #endregion
}

