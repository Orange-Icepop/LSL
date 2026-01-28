using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LSL.Common.Models;
using LSL.Common.Models.ServerConfig;
using LSL.Common.Options;
using LSL.Common.Utilities.FileSystem;
using LSL.Common.Utilities.Minecraft;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;

/// <summary>
/// The config manager of LSL's servers, includes managing server registration/unregistration.
/// </summary>
/// <param name="logger">An ILogger that logs logs.</param> 
public class ServerConfigManager(MainConfigManager mcm, ILogger<ServerConfigManager> logger)
{
    public FrozenDictionary<int, string> MainServerConfig { get; private set; } = FrozenDictionary<int, string>.Empty;

    public FrozenDictionary<int, IndexedServerConfig> ServerConfigs { get; private set; } = FrozenDictionary<int, IndexedServerConfig>.Empty;

    #region 读取各个服务器的LSL配置文件ReadServerConfig

    public async Task<ServerConfigList> ReadServerConfig()
    {
        try
        {
            logger.LogInformation("Start reading server config...");
            var mainPath = ConfigPathProvider.ServerConfigPath;
            // 读取服务器主配置文件
            var indexRes = await GetIndexConfigAsync(mainPath);
            if (indexRes.IsError)
            {
                logger.LogError(indexRes.Error, "Error reading index config file of servers at {MainPath}", mainPath);
                return ServerConfigList.Fail(indexRes.Error);
            }

            MainServerConfig = indexRes.Result.ToFrozenDictionary();
            var detailRes = await GetServerDetailsAsync(MainServerConfig);
            if (!detailRes.HasResult)
            {
                logger.LogError(detailRes.Error, "Error reading server config file of servers at {ServersFolder}",
                    ConfigPathProvider.ServersFolder);
                return ServerConfigList.Fail(detailRes.Error);
            }

            ServerConfigs = detailRes.Result.ToFrozenDictionary();
            if (detailRes.IsWarning)
            {
                logger.LogWarning(
                    "Some errors and warnings occured when reading server config.\nThe following servers are not loaded due to uncorrectable errors:\n{ErrorMessages}\nWarnings:\n{WarningMessages}",
                    string.Join('\n', detailRes.ErrorMessages),
                    string.Join('\n', detailRes.WarningMessages));
                return detailRes;
            }

            logger.LogInformation("Finished reading server config.");
            return ServerConfigList.Success(detailRes.Result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error reading server config.");
            return ServerConfigList.Fail(e);
        }
    }

    #endregion

    #region 刷新服务器主配置文件

    private async Task<ServiceResult<Dictionary<int, string>>> GetIndexConfigAsync(string path)
    {
        if (!File.Exists(path))
        {
            logger.LogError("Server main config at {path} not found.", path);
            return ServiceResult.Fail<Dictionary<int, string>>(new FileNotFoundException($"Server main config at {path} not found."));
        }

        string mainFile;
        try
        {
            mainFile = await File.ReadAllTextAsync(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ServiceResult.Fail<Dictionary<int, string>>(ex);
        }

        if (string.IsNullOrWhiteSpace(mainFile) || mainFile == "{}")
        {
            logger.LogInformation("No server is registered in the main server config file.");
            return ServiceResult.Success(new Dictionary<int, string>());
        }

        try
        {
            var configs = JsonSerializer.Deserialize(mainFile, SnakeJsonOptions.Default.DictionaryInt32String);
            return configs is null
                ? throw new JsonException("The Main Server Config file cannot be converted.")
                : ServiceResult.Success(configs);
        }
        catch (JsonException ex)
        {
            var err = new JsonException($"The main server config file at {path} is not a valid LSL server config file.", ex);
            logger.LogError(err, "");
            return ServiceResult.Fail<Dictionary<int, string>>(err);
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
            if (result.IsError) errors.Add($"{pair.Value}: {result.Error.Message}");
            else if (result.IsWarning)
            {
                warnings.Add($"{pair.Value}: {result.Error.Message}");
                scCache.TryAdd(pair.Key, result.Result);
            }
            else scCache.TryAdd(pair.Key, result.Result);
        });
        
        if (!errors.IsEmpty || !warnings.IsEmpty)
        {
            return ServerConfigList.PartialError(scCache,
                new StringBuilder().AppendJoin('\n', errors).ToString(),
                new StringBuilder().AppendJoin('\n', warnings).ToString());
        }
        return ServerConfigList.Success(scCache);
    }

    public static Task<ServiceResult<IndexedServerConfig>> GetSingleServerConfigAsync(int id, string targetDir)
    {
        return ServerConfigHelper.ReadSingleConfigAsync(targetDir, true)
            .BindAsync(config => Task.FromResult(ServiceResult.Success(config.AsIndexed(id))));
    }

    #endregion

    // 注册与删除服务器均不会立刻更新字典

    #region 注册服务器方法RegisterServer

    public async Task<ServiceResult> RegisterServer(LocatedServerConfig config)
    {
        try
        {
            if (!File.Exists(ConfigPathProvider.ServerConfigPath))
            {
                await File.WriteAllTextAsync(ConfigPathProvider.ServerConfigPath, "{}");
            }

            // 连接服务器路径
            string addedServerPath = Path.Combine(ConfigPathProvider.ServersFolder, serverName);
            string addedConfigPath = Path.Combine(addedServerPath, "lslconfig.json");
            string trueCorePath = Path.Combine(addedServerPath, Path.GetFileName(corePath));
            string coreName = Path.GetFileName(corePath);
            Directory.CreateDirectory(addedServerPath);
            File.Copy(corePath, trueCorePath, true); // 复制核心文件到服务器文件夹内
            // 初始化服务器配置文件
            var initialConfig = new
            {
                name = serverName,
                using_java = usingJava,
                core_name = coreName,
                min_memory = minMem,
                max_memory = maxMem,
                ext_jvm = extJvm
            };
            string serializedConfig = JsonSerializer.Serialize(initialConfig, DefaultOptions);
            await File.WriteAllTextAsync(addedConfigPath, serializedConfig); // 写入服务器文件夹内的配置文件
            // 创建Eula文件
            if (!mcm.CurrentConfigs.TryGetValue("auto_eula", out var rawEula) ||
                !bool.TryParse(rawEula.ToString(), out var eula)) eula = false;
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await File.WriteAllTextAsync(Path.Combine(addedServerPath, "eula.txt"),
                $"# Generated by LSL at {time}\n# For details of Mojang EULA, go to https://aka.ms/MinecraftEULA\neula={eula}");
            //找到空闲id
            int targetId = 0;
            while (MainServerConfig.ContainsKey(targetId))
            {
                ++targetId;
            }

            JsonHelper.AddJson(ConfigPathProvider.ServerConfigPath, targetId.ToString(),
                addedServerPath); // 在服务器列表文件中注册新服务器
            logger.LogInformation("Server {ServerName} registered, config file path {AddedConfigPath}", serverName,
                addedConfigPath);
            return ServiceResult.Success();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Server {ServerName} could not be registered", serverName);
            return ServiceResult.Fail(e);
        }
    }

    #endregion

    #region 修改服务器方法EditServer

    public async Task<ServiceResult> EditServer(int serverId, string serverName, string usingJava, uint minMem,
        uint maxMem,
        string extJvm)
    {
        try
        {
            if (MainServerConfig.TryGetValue(serverId, out var serverPath) && Directory.Exists(serverPath) &&
                ServerConfigs.TryGetValue(serverId, out var config))
            {
                string editedConfigPath = Path.Combine(serverPath, "lslconfig.json");
                string coreName = config.CoreName;
                var newConfig = new
                {
                    name = serverName,
                    using_java = usingJava,
                    core_name = coreName,
                    min_memory = minMem,
                    max_memory = maxMem,
                    ext_jvm = extJvm
                };
                string serializedConfig = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
                await File.WriteAllTextAsync(editedConfigPath, serializedConfig); // 写入服务器文件夹内的配置文件
                logger.LogInformation("Server Edited:{id}", serverId);
                return ServiceResult.Success();
            }
            else
            {
                logger.LogError("Server Not Found:{id}", serverId);
                return ServiceResult.Fail(new FileNotFoundException($"未找到服务器{serverId}：{serverName}"));
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Server {ServerId} failed to edit.", serverId);
            return ServiceResult.Fail(e);
        }
    }

    #endregion

    #region 删除服务器方法DeleteServer

    public async Task<ServiceResult> DeleteServer(int serverId)
    {
        if (!ServerConfigs.TryGetValue(serverId, out var config))
        {
            logger.LogError("Server Not Found in Dictionary:{id}", serverId);
            return ServiceResult.Fail(new KeyNotFoundException($"在LSL配置文件中未找到id为{serverId}的服务器。"));
        }

        var serverPath = config.ServerPath;
        if (string.IsNullOrEmpty(serverPath) || !Directory.Exists(serverPath))
        {
            logger.LogError("Server Directory Not Found:{id}", serverId);
            return ServiceResult.Fail(new FileNotFoundException($"编号为{serverId}的服务器的路径不存在。"));
        }

        try
        {
            await JsonHelper.DeleteJsonKeyAsync(ConfigPathProvider.ServerConfigPath,
                serverId.ToString()); // 在服务器列表文件中删除服务器
            await DirectoryExtensions.DeleteDirectoryAsync(serverPath); // 删除服务器文件夹
        }
        catch (Exception e)
        {
            logger.LogError(e, "Server {ServerId} could not be deleted", serverId);
            return ServiceResult.Fail(e);
        }

        logger.LogInformation("Server deleted:{id}", serverId);
        return ServiceResult.Success();
    }

    #endregion

    #region 添加已有服务器方法AddExistedServer

    public async Task<ServiceResult> AddExistedServer(string serverName, string usingJava, string corePath, uint minMem,
        uint maxMem, string extJvm, IProgress<string>? progress = null)
    {
        try
        {
            if (!File.Exists(ConfigPathProvider.ServerConfigPath))
            {
                await File.WriteAllTextAsync(ConfigPathProvider.ServerConfigPath, "{}");
            }

            // 连接服务器路径
            var originalServerDir = Path.GetDirectoryName(corePath);
            if (originalServerDir is null)
                return ServiceResult.Fail(new ArgumentException("Cannot get the root folder of the server",
                    nameof(corePath)));
            string addedServerPath = Path.Combine(ConfigPathProvider.ServersFolder, serverName);
            string addedConfigPath = Path.Combine(addedServerPath, "lslconfig.json");
            string coreName = Path.GetFileName(corePath);
            Directory.CreateDirectory(addedServerPath);
            if (Directory.GetParent(originalServerDir)?.FullName !=
                Path.GetFullPath(ConfigPathProvider.ServerConfigPath))
            {
                await DirectoryExtensions.CopyDirectoryAsync(originalServerDir, addedServerPath,
                    DirectoryCopyMode.CopyContentsOnly, FileOverwriteMode.Overwrite,
                    fileInProgress: progress); // 复制核心文件到服务器文件夹内
            }

            // 初始化服务器配置文件
            var initialConfig = new
            {
                name = serverName,
                using_java = usingJava,
                core_name = coreName,
                min_memory = minMem,
                max_memory = maxMem,
                ext_jvm = extJvm
            };
            string serializedConfig = JsonConvert.SerializeObject(initialConfig, Formatting.Indented);
            await File.WriteAllTextAsync(addedConfigPath, serializedConfig); // 写入服务器文件夹内的配置文件
            // 创建Eula文件
            if (!File.Exists(Path.Combine(addedServerPath, "eula.txt")))
            {
                if (!mcm.CurrentConfigs.TryGetValue("auto_eula", out var rawEula) ||
                    !bool.TryParse(rawEula.ToString(), out var eula)) eula = false;
                string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                await File.WriteAllTextAsync(Path.Combine(addedServerPath, "eula.txt"),
                    $"# Generated by LSL at {time}\n# For details of Mojang EULA, go to https://aka.ms/MinecraftEULA\neula={eula}");
            }

            //找到空闲id
            int targetId = 0;
            while (MainServerConfig.ContainsKey(targetId))
            {
                ++targetId;
            }

            JsonHelper.AddJson(ConfigPathProvider.ServerConfigPath, targetId.ToString(),
                addedServerPath); // 在服务器列表文件中注册新服务器
            logger.LogInformation("Server {serverName} registered, config file path {addedConfigPath}", serverName,
                addedConfigPath);
            return ServiceResult.Success();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering server {serverName} at {corePath}", serverName, corePath);
            return ServiceResult.Fail(e);
        }
    }

    #endregion
}