using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentResults;
using LSL.Common.Models.AppConfig;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace LSL.Services.ConfigServices;

/// <summary>
///     The wrapper of detailed ConfigManagers.
/// </summary>
public class ConfigManager(
    DaemonConfigManager dcm,
    WebConfigManager wcm,
    DesktopConfigManager dkcm,
    ServerConfigManager scm,
    JavaConfigManager jcm,
    ILogger<ConfigManager> logger)
{
    #region 初始化配置文件

    public async Task<Result> Initialize()
    {
        try
        {
            await ((List<Task<IResult>>)
            [
                ReadDaemonConfig().AsIResult(),
                ReadWebConfig().AsIResult(),
                ReadDesktopConfig().AsIResult(),
                Task.FromResult<IResult>(await ReadServerConfig()),
                ReadJavaConfig().AsIResult()
            ]).WhenAll();
            return Result.Ok();
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Something critical error occured while loading config.");
            return Result.Fail(e);
        }
    }

    #endregion

    #region 配置文件代理操作

    // 守护进程配置
    public DaemonConfig DaemonConfigs => dcm.Config;

    public Task<Result<DaemonConfig>> SetDaemonConfig(DaemonConfig conf)
    {
        return dcm.SetAndWriteAsync(conf);
    }

    public Task<Result<DaemonConfig>> ReadDaemonConfig()
    {
        return dcm.LoadAsync();
    }

    // 网页面板配置
    public WebConfig WebConfigs => wcm.Config;

    public Task<Result<WebConfig>> SetWebConfig(WebConfig conf)
    {
        return wcm.SetAndWriteAsync(conf);
    }

    public Task<Result<WebConfig>> ReadWebConfig()
    {
        return wcm.LoadAsync();
    }

    // 桌面配置
    public DesktopConfig DesktopConfigs => dkcm.Config;

    public Task<Result<DesktopConfig>> SetDesktopConfig(DesktopConfig conf)
    {
        return dkcm.SetAndWriteAsync(conf);
    }

    public Task<Result<DesktopConfig>> ReadDesktopConfig()
    {
        return dkcm.LoadAsync();
    }

    // 服务器配置
    public Dictionary<int, IndexedServerConfig> CloneServerConfigs()
    {
        return scm.CloneServerConfigs();
    }

    public Task<ServerConfigList> ReadServerConfig()
    {
        return scm.ReadServerConfig();
    }

    public bool TryGetServerConfig(int serverId, [MaybeNullWhen(false)] out IndexedServerConfig serverConfig)
    {
        return scm.TryGetServerConfig(serverId, out serverConfig);
    }

    public Task<Result<IDictionary<int, IndexedServerConfig>>> AddServerUsingCore(LocatedServerConfig config,
        string corePath, bool installForge, IProgress<string>? progress)
    {
        return scm.AddServerUsingCore(config, corePath, installForge, progress);
    }

    public Task<Result<IDictionary<int, IndexedServerConfig>>> AddServerFolder(LocatedServerConfig config,
        IProgress<string>? progress)
    {
        return scm.AddServerFolder(config, progress);
    }

    public Task<Result<IDictionary<int, IndexedServerConfig>>> EditServer(IndexedServerConfig config)
    {
        return scm.EditServer(config);
    }

    public Task<Result<IDictionary<int, IndexedServerConfig>>> DeleteServer(int id)
    {
        return scm.DeleteServer(id);
    }

    // Java配置
    public ImmutableDictionary<int, JavaInfo> JavaConfigs => jcm.JavaDict;

    public Task<Result<ImmutableDictionary<int, JavaInfo>>> ReadJavaConfig(bool writeBack = false)
    {
        return jcm.ReadJavaConfig(writeBack);
    }

    public Task<Result> DetectJavaAsync()
    {
        return jcm.DetectJavaAsync();
    }

    #endregion
}