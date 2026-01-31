using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LSL.Common;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;
using LSL.Common.Options;
using LSL.Common.Utilities.FileSystem;
using LSL.Common.Utilities.Minecraft;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace LSL.Services.ConfigServices;

/// <summary>
/// The config manager of LSL's servers, includes managing server registration/unregistration.
/// </summary>
/// <param name="logger">An ILogger that logs logs.</param> 
public class ServerConfigManager(MainConfigManager mcm, ILogger<ServerConfigManager> logger)
{
    #region 服务器配置索引管理器

    private sealed class ServerConfigIndexManager
    {
        private readonly AsyncReaderWriterLock _syncLock = new();
        private Dictionary<int, IndexedServerConfig> _serverConfigs = [];

        public Dictionary<int, IndexedServerConfig> CloneServerConfigs()
        {
            using (_syncLock.ReaderLock())
            {
                return _serverConfigs.ToDictionary(config => config.Key,
                    config => new IndexedServerConfig(config.Value));
            }
        }

        public bool Contains(int serverId)
        {
            using (_syncLock.ReaderLock())
            {
                return _serverConfigs.ContainsKey(serverId);
            }
        }

        public bool TryGetServerConfig(int serverId, [MaybeNullWhen(false)] out IndexedServerConfig serverConfig)
        {
            using (_syncLock.ReaderLock())
            {
                return _serverConfigs.TryGetValue(serverId, out serverConfig);
            }
        }

        private async Task<Result> WriteServerConfigs()
        {
            try
            {
                Dictionary<int, string> idx;
                using (await _syncLock.ReaderLockAsync())
                {
                    idx = _serverConfigs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ServerPath);
                }

                await File.WriteAllTextAsync(ConfigPathProvider.ServerConfigPath,
                    JsonSerializer.Serialize(idx,
                        SnakeJsonOptions.Default.DictionaryInt32String));
                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Fail(e);
            }
        }

        #region 增/改/删服务器注册项操作

        public async Task<Result<Dictionary<int, IndexedServerConfig>>> AddServerToIndex(
            LocatedServerConfig config)
        {
            using (await _syncLock.WriterLockAsync())
            {
                var id = _serverConfigs.Keys.Max() + 1;
                if (_serverConfigs.Values.Select(x => x.ServerPath).Contains(config.ServerPath))
                    return Result.Fail<Dictionary<int, IndexedServerConfig>>(
                        new DuplicateNameException("The server path to add already exists in server index"));
                try
                {
                    if (!_serverConfigs.TryAdd(id, config.AsIndexed(id)))
                        return Result.Fail<Dictionary<int, IndexedServerConfig>>(
                            "Unable to add new server config to index server config");
                    await WriteServerConfigs().AsGeneric().GetValueOrThrow();
                    return Result.Success(_serverConfigs.ToDictionary(kvp => kvp.Key,
                        kvp => new IndexedServerConfig(kvp.Value)));
                }
                catch (Exception e)
                {
                    return Result.Fail<Dictionary<int, IndexedServerConfig>>(e);
                }
            }
        }

        public async Task<Result<Dictionary<int, IndexedServerConfig>>> ModifyServerInIndex(
            IndexedServerConfig config)
        {
            using (await _syncLock.WriterLockAsync())
            {
                try
                {
                    if (!_serverConfigs.ContainsKey(config.ServerId))
                        return Result.Fail<Dictionary<int, IndexedServerConfig>>(
                            new KeyNotFoundException("Index server config doesn't contain the selected server id"));
                    _serverConfigs[config.ServerId] = config;
                    await WriteServerConfigs().AsGeneric().GetValueOrThrow();
                    return Result.Success(_serverConfigs.ToDictionary(kvp => kvp.Key,
                        kvp => new IndexedServerConfig(kvp.Value)));
                }
                catch (Exception e)
                {
                    return Result.Fail<Dictionary<int, IndexedServerConfig>>(e);
                }
            }
        }

        public async Task<Result<Dictionary<int, IndexedServerConfig>>> DeleteServerFromIndex(int id)
        {
            using (await _syncLock.WriterLockAsync())
            {
                try
                {
                    _serverConfigs.Remove(id, out _);
                    (await WriteServerConfigs()).GetValueOrThrow();
                    return Result.Success(_serverConfigs.ToDictionary(kvp => kvp.Key,
                        kvp => new IndexedServerConfig(kvp.Value)));
                }
                catch (Exception e)
                {
                    return Result.Fail<Dictionary<int, IndexedServerConfig>>(e);
                }
            }
        }

        #endregion

        #region 读取LSL Server配置文件

        public async Task<ServerConfigList> ReadServerConfig()
        {
            try
            {
                // 读取服务器主配置文件
                var indexRes = await GetIndexConfigAsync();
                if (indexRes.IsFailed) return ServerConfigList.Fail(indexRes.Error);
                // 读取单个服务器配置文件
                var detailRes = await GetServerDetailsAsync(indexRes.Value);
                if (!detailRes.HasResult) return ServerConfigList.Fail(detailRes.Error);
                // 写入当前字典
                using (await _syncLock.WriterLockAsync())
                {
                    _serverConfigs = detailRes.Value.ToDictionary();
                }

                return detailRes;
            }
            catch (Exception e)
            {
                return ServerConfigList.Fail(e);
            }
        }

        #endregion

        #region 刷新服务器主配置文件

        private static async Task<Result<Dictionary<int, string>>> GetIndexConfigAsync()
        {
            // 测试目标文件存在性
            try
            {
                if (!File.Exists(ConfigPathProvider.ServerConfigPath))
                {
                    await File.WriteAllTextAsync(ConfigPathProvider.ServerConfigPath, "{}");
                    return Result.Success(new Dictionary<int, string>());
                }
            }
            catch
            {
                return Result.Fail<Dictionary<int, string>>(
                    "LSL has no permission to write server config file."); // 这不应该，哥们
            }

            // 读取
            string mainFile;
            try
            {
                mainFile = await File.ReadAllTextAsync(ConfigPathProvider.ServerConfigPath);
            }
            catch (Exception ex)
            {
                return Result.Fail<Dictionary<int, string>>(ex);
            }

            // 验空
            if (string.IsNullOrWhiteSpace(mainFile) || mainFile == "{}")
                return Result.Success(new Dictionary<int, string>());

            // 反序列化
            try
            {
                var configs = JsonSerializer.Deserialize(mainFile, SnakeJsonOptions.Default.DictionaryInt32String);
                return configs is null
                    ? throw new JsonException("The Main Server Config file cannot be converted.")
                    : Result.Success(configs);
            }
            catch (JsonException ex)
            {
                var err = new JsonException(
                    $"The main server config file at {ConfigPathProvider.ServerConfigPath} is not a valid LSL server config file.",
                    ex);
                return Result.Fail<Dictionary<int, string>>(err);
            }
            catch (Exception e)
            {
                return Result.Fail<Dictionary<int, string>>(e);
            }
        }

        #endregion

        #region 逐个获取服务器各自的配置文件

        private static async Task<ServerConfigList> GetServerDetailsAsync(IDictionary<int, string> mainConfigs)
        {
            ConcurrentBag<string> errors = [];
            ConcurrentBag<string> warnings = [];
            // 读取各个服务器的LSL配置文件
            ConcurrentDictionary<int, IndexedServerConfig> scCache = [];
            await Parallel.ForEachAsync(mainConfigs, ConcurrencyOptions.ConcurrencyLimit, async (pair, _) =>
            {
                var result = await GetSingleServerConfigAsync(pair.Key, pair.Value);
                if (result.IsFailed) errors.Add($"{pair.Value}: {result.Error.Message}");
                else if (result.IsWarning)
                {
                    warnings.Add($"{pair.Value}: {result.Error.Message}");
                    scCache.TryAdd(pair.Key, result.Value);
                }
                else scCache.TryAdd(pair.Key, result.Value);
            });

            if (!errors.IsEmpty || !warnings.IsEmpty)
            {
                return ServerConfigList.PartialError(scCache,
                    new StringBuilder().AppendJoin('\n', errors).ToString(),
                    new StringBuilder().AppendJoin('\n', warnings).ToString());
            }

            return ServerConfigList.Success(scCache);
        }

        public static Task<Result<IndexedServerConfig>> GetSingleServerConfigAsync(int id, string targetDir)
        {
            return ServerConfigHelper.ReadSingleConfigAsync(targetDir, true)
                .BindAsync(config => Task.FromResult(Result.Success(config.AsIndexed(id))));
        }

        #endregion
    }

    #endregion

    private readonly ServerConfigIndexManager _indexManager = new();

    public bool TryGetServerConfig(int serverId, [MaybeNullWhen(false)] out IndexedServerConfig serverConfig) =>
        _indexManager.TryGetServerConfig(serverId, out serverConfig);

    #region 注册服务器方法RegisterServer

    private async Task<Result<IDictionary<int, IndexedServerConfig>>> RegisterServer(
        LocatedServerConfig config)
    {
        try
        {
            if (!File.Exists(ConfigPathProvider.ServerConfigPath))
            {
                await File.WriteAllTextAsync(ConfigPathProvider.ServerConfigPath, "{}");
            }

            var writeResult = await config.ToLatestConfig()
                .BindAsync(conf => conf.WriteToFileAsync(config.ServerPath).AsGeneric());
            if (writeResult.IsFailed)
                return Result.Fail<IDictionary<int, IndexedServerConfig>>(writeResult.Error);
            // 创建Eula文件
            if (!mcm.CurrentConfigs.TryGetValue("auto_eula", out var rawEula) ||
                !bool.TryParse(rawEula.ToString(), out var eula)) eula = false; // TODO:在MainConfig改掉之后换过去
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await File.WriteAllTextAsync(Path.Combine(config.ServerPath, "eula.txt"),
                $"# Generated by LSL at {time}\n# For details of Mojang EULA, go to https://aka.ms/MinecraftEULA\neula={eula}");
            // 在服务器列表文件中注册新服务器
            var result = await _indexManager.AddServerToIndex(config);
            if (result.IsFailed) return result.Bind(Result.Success<IDictionary<int, IndexedServerConfig>>);
            logger.LogInformation("Server {ServerName} registered, config file path {AddedConfigPath}",
                config.ServerName,
                config.ServerPath);
            return result.Bind(Result.Success<IDictionary<int, IndexedServerConfig>>);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Server {ServerName} could not be registered", config.ServerName);
            return Result.Fail<IDictionary<int, IndexedServerConfig>>(e);
        }
    }

    #endregion

    #region 修改服务器方法EditServer

    public async Task<Result<IDictionary<int, IndexedServerConfig>>> EditServer(IndexedServerConfig config)
    {
        try
        {
            if (_indexManager.TryGetServerConfig(config.ServerId, out var serverConfig) &&
                serverConfig.ServerPath == config.ServerPath)
            {
                await config.LocatedConfig.ToLatestConfig()
                    .BindAsync(c => c.WriteToFileAsync(config.ServerPath).AsGeneric())
                    .GetValueOrThrow(); // 写入服务器文件夹内的配置文件
                return await _indexManager.ModifyServerInIndex(config)
                    .Match(
                        _ => logger.LogInformation("Server{id} \"{name}\" edited", config.ServerId, config.ServerName),
                        null,
                        ex => logger.LogWarning(ex, "Error editing server \"{serverName}\"", serverConfig.ServerName))
                    .Bind(Result.Success<IDictionary<int, IndexedServerConfig>>);
            }
            else
            {
                logger.LogError("Match Server Not Found:{id}", config.ServerId);
                return Result.Fail<IDictionary<int, IndexedServerConfig>>(
                    new KeyNotFoundException($"未找到匹配的服务器{config.ServerId}：{config.ServerName}"));
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Server {ServerId} failed to edit.", config.ServerId);
            return Result.Fail<IDictionary<int, IndexedServerConfig>>(e);
        }
    }

    #endregion

    #region 删除服务器方法DeleteServer

    public async Task<Result<IDictionary<int, IndexedServerConfig>>> DeleteServer(int serverId)
    {
        if (!_indexManager.TryGetServerConfig(serverId, out var config))
        {
            logger.LogError("Server Not Found in Dictionary:{id}", serverId);
            return Result.Fail<IDictionary<int, IndexedServerConfig>>(
                new KeyNotFoundException($"在LSL配置文件中未找到id为{serverId}的服务器。"));
        }

        var serverPath = config.ServerPath;

        try
        {
            return await _indexManager.DeleteServerFromIndex(serverId).MatchAsync( // 在服务器列表文件中删除服务器
                async _ =>
                {
                    await DirectoryExtensions.DeleteDirectoryAsync(serverPath); // 删除服务器文件夹
                    logger.LogInformation("Server deleted:{id}", serverId);
                }, null, ex =>
                {
                    logger.LogError(ex, "Server {ServerId} could not be deleted", serverId);
                    return Task.CompletedTask;
                }).Bind(Result.Success<IDictionary<int, IndexedServerConfig>>);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Server {ServerId} could not be deleted", serverId);
            return Result.Fail<IDictionary<int, IndexedServerConfig>>(e);
        }
    }

    #endregion

    #region 添加裸核服务器方法AddServerUsingCore

    public async Task<Result<IDictionary<int, IndexedServerConfig>>> AddServerUsingCore(LocatedServerConfig config,
        string corePath,
        bool installForge = false, IProgress<string>? progress = null)
    {
        config.ServerPath = Path.Combine(ConfigPathProvider.ServersFolder, config.ServerName);
        try
        {
            Directory.CreateDirectory(config.ServerPath);
            var coreFile = new FileInfo(corePath);
            if (!coreFile.Exists)
                return Result.Fail<IDictionary<int, IndexedServerConfig>>(
                    new FileNotFoundException($"File {corePath} not found"));
            progress?.Report("Start copying core file");
            await FileExtensions.CopyFileAsync(corePath, Path.Combine(config.ServerPath, coreFile.Name),
                FileOverwriteMode.Overwrite);
            progress?.Report("Core file copied");
        }
        catch (Exception e)
        {
            try
            {
                await DirectoryExtensions.DeleteDirectoryAsync(config.ServerPath);
            }
            catch (Exception ex)
            {
                return Result.Fail<IDictionary<int, IndexedServerConfig>>(ex);
            }

            return Result.Fail<IDictionary<int, IndexedServerConfig>>(e);
        }

        try
        {
            if (config.ServerType is ServerCoreType.ForgeInstaller && installForge)
            {
                var installResult = await ForgeInstaller.InstallForge(
                    Path.Combine(config.ServerPath, Path.GetFileName(config.ServerPath)), config.JavaPath, progress);
                if (installResult.IsFailed)
                    return Result.Fail<IDictionary<int, IndexedServerConfig>>(installResult.Error);
                var findResult = await ForgeConfigHelper.GetForgeConfig(config.ServerPath);
                if (findResult.IsFailed)
                {
                    var dirInfo = new DirectoryInfo(config.ServerPath);
                    var enumerations = dirInfo.EnumerateFiles("*universal.jar");
                    var fileInfos = enumerations as FileInfo[] ?? enumerations.ToArray();
                    if (fileInfos.Length > 1)
                        return Result.Warning<IDictionary<int, IndexedServerConfig>>(_indexManager.CloneServerConfigs(),
                            "Installation finished, but cannot ensure which is your Forge server jar file. Please re-add the server via \"Add server folder\"."); // TODO:本地化
                    config.CommonCoreInfo = new CommonCoreConfigV1() { JarName = fileInfos.First().Name };
                    config.ServerType = ServerCoreType.OldForge;
                }
                else
                {
                    config.ForgeCoreInfo = findResult.Value;
                    config.ServerType = ServerCoreType.Forge;
                }
            }
        }
        catch (Exception e)
        {
            return Result.Fail<IDictionary<int, IndexedServerConfig>>(e);
        }

        return await RegisterServer(config).Match(
            _ => logger.LogInformation("Server \"{serverName}\" registered at {serverPath}", config.ServerName,
                config.ServerPath),
            (_, ex) => logger.LogWarning(ex, "Server \"{serverName}\" registered at {serverPath} with warning",
                config.ServerName, config.ServerPath),
            ex => logger.LogError(ex, "Server \"{serverName}\" failed to register", config.ServerName));
    }

    #endregion

    #region 添加已有服务器方法AddExistedServer

    public async Task<Result<IDictionary<int, IndexedServerConfig>>> AddExistedServer(LocatedServerConfig config,
        IProgress<string>? progress = null)
    {
        try
        {
            var originalServerDir = new DirectoryInfo(config.ServerPath);
            if (!originalServerDir.Exists)
            {
                return Result.Fail<IDictionary<int, IndexedServerConfig>>(
                    new DirectoryNotFoundException($"Directory {config.ServerPath} not found"));
            }

            var registerServerPath = Path.Combine(ConfigPathProvider.ServersFolder, config.ServerName);

            if (originalServerDir.Parent!.FullName != ConfigPathProvider.ServersFolder)
            {
                await DirectoryExtensions.CopyDirectoryAsync(originalServerDir.FullName, registerServerPath, true, true,
                    DirectoryCopyMode.CopyContentsOnly, FileOverwriteMode.Overwrite,
                    fileInProgress: progress); // 复制原服务器文件到新服务器文件夹内
            }

            // 创建Eula文件
            if (!File.Exists(Path.Combine(registerServerPath, "eula.txt")))
            {
                if (!mcm.CurrentConfigs.TryGetValue("auto_eula", out var rawEula) ||
                    !bool.TryParse(rawEula.ToString(), out var eula)) eula = false;
                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                await File.WriteAllTextAsync(Path.Combine(registerServerPath, "eula.txt"),
                    $"# Generated by LSL at {time}\n# For details of Mojang EULA, go to https://aka.ms/MinecraftEULA\neula={eula}");
            }

            config.ServerPath = registerServerPath;
            return await RegisterServer(config).Match(
                _ => logger.LogInformation("Server \"{serverName}\" registered at {serverPath}", config.ServerName,
                    config.ServerPath),
                (_, ex) => logger.LogWarning(ex, "Server \"{serverName}\" registered at {serverPath} with warning",
                    config.ServerName, config.ServerPath),
                ex => logger.LogError(ex, "Server \"{serverName}\" failed to register", config.ServerName));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering server {serverName} at {corePath}", config.ServerName,
                config.ServerPath);
            try
            {
                await DirectoryExtensions.DeleteDirectoryAsync(config.ServerPath);
            }
            catch (Exception ex)
            {
                return Result.Fail<IDictionary<int, IndexedServerConfig>>(ex);
            }

            return Result.Fail<IDictionary<int, IndexedServerConfig>>(e);
        }
    }

    #endregion
}