using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LSL.Common;
using LSL.Common.Models;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;

/// <summary>
/// The wrapper of detailed ConfigManagers.
/// </summary>
public class ConfigManager(
    MainConfigManager mcm,
    ServerConfigManager scm,
    JavaConfigManager jcm,
    ILogger<ConfigManager> logger)
{
    #region 初始化配置文件
    public async Task<Result> Initialize()
    {
        // 检查权限
        if (!ConfigPathProvider.HasReadWriteAccess(ConfigPathProvider.LSLFolder))
        {
            var error = new UnauthorizedAccessException($"LSL does not have write access to config folder:{ConfigPathProvider.LSLFolder}");
            logger.LogCritical(error, "");
            return Result.Fail(error);
        }
        if (!ConfigPathProvider.HasReadWriteAccess(ConfigPathProvider.ServersFolder))
        {
            var error = new UnauthorizedAccessException($"LSL does not have write access to the servers folder:{ConfigPathProvider.ServersFolder}");
            logger.LogCritical(error, "");
            return Result.Fail(error);
        }
        // 确保LSL文件夹存在  
        Directory.CreateDirectory(ConfigPathProvider.LSLFolder);
        Directory.CreateDirectory(ConfigPathProvider.ServersFolder);
        if (!File.Exists(ConfigPathProvider.ConfigFilePath))
        {
            await File.WriteAllTextAsync(ConfigPathProvider.ConfigFilePath, "{}");
            var mainRes = await MainConfigManager.InitAsync();
            if (mainRes.Kind == ResultType.Error) return mainRes;
            logger.LogInformation("Config.json initialized.");
        }

        if (!File.Exists(ConfigPathProvider.ServerConfigPath))
        {
            await File.WriteAllTextAsync(ConfigPathProvider.ServerConfigPath, "{}");
            logger.LogInformation("ServerConfig.json initialized.");
        }

        if (!File.Exists(ConfigPathProvider.JavaListPath))
        {
            await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath, "{}");
            logger.LogInformation("JavaList.json initialized.");
        }
        return Result.Success();
    }

    #endregion
        
    #region 配置文件代理操作
    // 主配置文件
    public FrozenDictionary<string, object> MainConfigs => mcm.CurrentConfigs;
    public Task<Result<FrozenDictionary<string, object>>> ConfirmMainConfig(IDictionary<string, object> conf) => mcm.ConfirmConfig(conf);
    public Task<Result> ReadMainConfig() => mcm.LoadConfig();
    // 服务器配置
    public FrozenDictionary<int, IndexedServerConfig> ServerConfigs => scm.ServerConfigs;
    public Task<ServerConfigList> ReadServerConfig() => scm.ReadServerConfig();

    public Task<Result> RegisterServer(FormedServerConfig config) => scm.RegisterServer(config.ServerName,
        config.JavaPath, config.CorePath, uint.Parse(config.MinMem), uint.Parse(config.MaxMem), config.ExtJvm);
    public Task<Result> EditServer(int id, FormedServerConfig config) => scm.EditServer(id, config.ServerName, config.JavaPath, 
        uint.Parse(config.MinMem),
        uint.Parse(config.MaxMem), config.ExtJvm);
    public Task<Result> DeleteServer(int id) => scm.DeleteServer(id);

    public async Task<Result> AddExistedServer(FormedServerConfig config) => await scm.AddExistedServer(
        config.ServerName, config.JavaPath, config.CorePath, uint.Parse(config.MinMem), uint.Parse(config.MaxMem), config.ExtJvm);
    // Java配置
    public FrozenDictionary<int, JavaInfo> JavaConfigs => jcm.JavaDict;
    public Task<Result<Dictionary<int, JavaInfo>>> ReadJavaConfig(bool writeBack = false) => jcm.ReadJavaConfig(writeBack);
    public Task<Result> DetectJavaAsync() => jcm.DetectJavaAsync();

    #endregion
}

