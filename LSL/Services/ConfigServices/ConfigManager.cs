using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using LSL.Common;
using LSL.Common.Models.AppConfig;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;
using LSL.Common.Results;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;

/// <summary>
/// The wrapper of detailed ConfigManagers.
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
            await ((List<Task<IResult>>)[
                ReadDaemonConfig().AsIResult(),
                ReadWebConfig().AsIResult(),
                ReadDesktopConfig().AsIResult(),
                Task.FromResult<IResult>(await ReadServerConfig()),
                ReadJavaConfig().AsIResult()
            ]).WhenAll();
            return Result.Success();
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
    public Task<Result<DaemonConfig>> SetDaemonConfig(DaemonConfig conf) => dcm.SetAndWriteAsync(conf);
    public Task<Result<DaemonConfig>> ReadDaemonConfig() => dcm.LoadAsync();
    // 网页面板配置
    public WebConfig WebConfigs => wcm.Config;
    public Task<Result<WebConfig>> SetWebConfig(WebConfig conf) => wcm.SetAndWriteAsync(conf);
    public Task<Result<WebConfig>> ReadWebConfig() => wcm.LoadAsync();
    // 桌面配置
    public DesktopConfig DesktopConfigs => dkcm.Config;
    public Task<Result<DesktopConfig>> SetDesktopConfig(DesktopConfig conf) => dkcm.SetAndWriteAsync(conf);
    public Task<Result<DesktopConfig>> ReadDesktopConfig() => dkcm.LoadAsync();
    // 服务器配置
    public Dictionary<int, IndexedServerConfig> CloneServerConfigs() => scm.CloneServerConfigs();
    public Task<ServerConfigList> ReadServerConfig() => scm.ReadServerConfig();
    public bool TryGetServerConfig(int serverId, [MaybeNullWhen(false)] out IndexedServerConfig serverConfig) =>
        scm.TryGetServerConfig(serverId, out serverConfig);
    public Task<Result<IDictionary<int, IndexedServerConfig>>> AddServerUsingCore(LocatedServerConfig config,
        string corePath, bool installForge, IProgress<string>? progress) =>
        scm.AddServerUsingCore(config, corePath, installForge, progress);
    public Task<Result<IDictionary<int, IndexedServerConfig>>> AddServerFolder(LocatedServerConfig config, IProgress<string>? progress) => scm.AddServerFolder(config, progress);
    public Task<Result<IDictionary<int, IndexedServerConfig>>> EditServer(IndexedServerConfig config) => scm.EditServer(config);
    public Task<Result<IDictionary<int, IndexedServerConfig>>> DeleteServer(int id) => scm.DeleteServer(id);
    // Java配置
    public ImmutableDictionary<int, JavaInfo> JavaConfigs => jcm.JavaDict;
    public Task<Result<ImmutableDictionary<int, JavaInfo>>> ReadJavaConfig(bool writeBack = false) => jcm.ReadJavaConfig(writeBack);
    public Task<Result> DetectJavaAsync() => jcm.DetectJavaAsync();
    #endregion
}

