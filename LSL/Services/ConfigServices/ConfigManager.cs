﻿using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LSL.Common.Models;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;

/// <summary>
/// The wrapper of detailed ConfigManagers.
/// </summary>
public class ConfigManager
{

    private readonly MainConfigManager _mcm;
    private readonly ServerConfigManager _scm;
    private readonly JavaConfigManager _jcm;
    private readonly ILogger<ConfigManager> _logger;

    public ConfigManager(MainConfigManager mcm, ServerConfigManager scm, JavaConfigManager jcm, ILogger<ConfigManager> logger)
    {
        _mcm = mcm;
        _scm = scm;
        _jcm = jcm;
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
            _logger.LogCritical("{}", error);
            return ServiceResult.Fail(new UnauthorizedAccessException(error));
        }
        if (!ConfigPathProvider.HasReadWriteAccess(ConfigPathProvider.ServersFolder))
        {
            var error = $"LSL does not have write access to the servers folder:{ConfigPathProvider.ServersFolder}";
            _logger.LogCritical("{}", error);
            return ServiceResult.Fail(new UnauthorizedAccessException(error));
        }
        // 确保LSL文件夹存在  
        Directory.CreateDirectory(ConfigPathProvider.LSLFolder);
        Directory.CreateDirectory(ConfigPathProvider.ServersFolder);
        if (!File.Exists(ConfigPathProvider.ConfigFilePath))
        {
            File.WriteAllText(ConfigPathProvider.ConfigFilePath, "{}");
            var mainRes = _mcm.Init();
            if (mainRes.ResultType == ServiceResultType.Error) return mainRes;
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
    public FrozenDictionary<string, object> MainConfigs => _mcm.CurrentConfigs;
    public ServiceResult<FrozenDictionary<string, object>> ConfirmMainConfig(IDictionary<string, object> confs) => _mcm.ConfirmConfig(confs);
    public ServiceResult ReadMainConfig() => _mcm.LoadConfig();
    // 服务器配置
    public FrozenDictionary<int, ServerConfig> ServerConfigs => _scm.ServerConfigs;
    public ServerConfigReadResult ReadServerConfig() => _scm.ReadServerConfig();

    public ServiceResult RegisterServer(FormedServerConfig config) => _scm.RegisterServer(config.ServerName,
        config.JavaPath, config.CorePath, uint.Parse(config.MinMem), uint.Parse(config.MaxMem), config.ExtJvm);
    public ServiceResult EditServer(int id, FormedServerConfig config) => _scm.EditServer(id, config.ServerName, config.JavaPath, 
        uint.Parse(config.MinMem),
        uint.Parse(config.MaxMem), config.ExtJvm);
    public ServiceResult DeleteServer(int id) => _scm.DeleteServer(id);

    public async Task<ServiceResult> AddExistedServer(FormedServerConfig config) => await _scm.AddExistedServer(
        config.ServerName, config.JavaPath, config.CorePath, uint.Parse(config.MinMem), uint.Parse(config.MaxMem), config.ExtJvm);
    // Java配置
    public FrozenDictionary<int, JavaInfo> JavaConfigs => _jcm.JavaDict;
    public ServiceResult<JavaConfigReadResult> ReadJavaConfig() => _jcm.ReadJavaConfig();
    public async Task<ServiceResult> DetectJava() => await _jcm.DetectJava();

    #endregion
}

