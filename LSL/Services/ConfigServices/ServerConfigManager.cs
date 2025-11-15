using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LSL.Common.Extensions;
using LSL.Common.Models;
using LSL.Common.Utilities;
using LSL.Common.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LSL.Services.ConfigServices;

/// <summary>
/// The config manager of LSL's servers, includes managing server registration/unregistration.
/// </summary>
/// <param name="logger">An ILogger that logs logs. （拜托，想个更好的双关语吧（彼得帕克音））</param> 
public class ServerConfigManager(MainConfigManager mcm, ILogger<ServerConfigManager> logger)
{
    private readonly ILogger<ServerConfigManager> _logger = logger;
    private readonly MainConfigManager _mainConfigManager = mcm;
    public FrozenDictionary<int, string> MainServerConfig { get; private set; } = FrozenDictionary<int, string>.Empty;

    public FrozenDictionary<int, ServerConfig> ServerConfigs { get; private set; } =
        FrozenDictionary<int, ServerConfig>.Empty;

    #region 读取各个服务器的LSL配置文件ReadServerConfig

    public async Task<ServerConfigReadResult> ReadServerConfig()
    {
        _logger.LogInformation("Start reading server config...");
        string mainPath = ConfigPathProvider.ServerConfigPath;
        // 读取服务器主配置文件
        var indexRes = await GetIndexConfigAsync(mainPath);
        if (!indexRes.HasResult)
        {
            _logger.LogError(indexRes.Error, "Error reading index config file of servers at {MainPath}", mainPath);
            return ServerConfigReadResult.Fail(indexRes.Error);
        }

        MainServerConfig = indexRes.Result;
        var detailRes = await GetServerDetailsAsync(MainServerConfig);
        if (!detailRes.HasResult)
        {
            _logger.LogError(detailRes.Error, "Error reading server config file of servers at {ServersFolder}",
                ConfigPathProvider.ServersFolder);
            return ServerConfigReadResult.Fail(detailRes.Error);
        }

        ServerConfigs = detailRes.Result;
        if (detailRes.IsFinishedWithWarning)
        {
            _logger.LogWarning(
                "Some servers failed to load.\rServer not found: {NotfoundServers}\rconfig errors(file nonexist, no permission or bad format): {ConfigErrorServers}",
                string.Join('\n', detailRes.NotFoundServers),
                string.Join('\n', detailRes.ConfigErrorServers));
            return detailRes;
        }

        _logger.LogInformation("Finished reading server config.");
        return ServerConfigReadResult.Success(detailRes.Result);
    }

    #endregion

    #region 刷新服务器主配置文件

    private async Task<ServiceResult<FrozenDictionary<int, string>>> GetIndexConfigAsync(string path)
    {
        if (!File.Exists(path))
        {
            var ex = new FileNotFoundException($"Server main config at {path} not found.");
            _logger.LogError(ex, "Server main config at {p} not found.", path);
            return ServiceResult.Fail<FrozenDictionary<int, string>>(ex);
        }

        string mainFile;
        try
        {
            mainFile = await File.ReadAllTextAsync(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ServiceResult.Fail<FrozenDictionary<int, string>>(
                new UnauthorizedAccessException($"Current user have no permission to read file {path}.", ex));
        }

        if (string.IsNullOrWhiteSpace(mainFile) || mainFile == "{}")
        {
            _logger.LogInformation("No server is registered in the main server config file.");
            return ServiceResult.Success(FrozenDictionary<int, string>.Empty);
        }

        try
        {
            var configs = JsonConvert.DeserializeObject<Dictionary<int, string>>(mainFile);
            return configs is null
                ? throw new JsonException("The Main Server Config file cannot be converted.")
                : ServiceResult.Success(configs.ToFrozenDictionary());
        }
        catch (JsonException ex)
        {
            var err = new JsonReaderException(
                $"The main server config file at {path} is not a valid LSL server config file.", ex);
            _logger.LogError(err, "");
            return ServiceResult.Fail<FrozenDictionary<int, string>>(err);
        }
    }

    #endregion

    #region 逐个获取服务器各自的配置文件

    private static async Task<ServerConfigReadResult> GetServerDetailsAsync(
        IDictionary<int, string> mainConfigs)
    {
        List<string> notfoundServers = [];
        List<string> configErrorServers = [];
        // 读取各个服务器的LSL配置文件
        Dictionary<int, ServerConfig> scCache = [];
        foreach (var (key, targetDir) in mainConfigs)
        {
            var result = await GetSingleServerConfigAsync(key, targetDir);
            switch (result.Status)
            {
                case ServerConfigParseResultType.ServerNotFound:
                {
                    notfoundServers.Add(targetDir);
                    break;
                }
                case ServerConfigParseResultType.NoReadAccess:
                case ServerConfigParseResultType.EmptyConfig:
                case ServerConfigParseResultType.Unparsable:
                case ServerConfigParseResultType.MissingKey:
                case ServerConfigParseResultType.ConfigFileNotFound:
                {
                    configErrorServers.Add(targetDir);
                    break;
                }
                case ServerConfigParseResultType.Success:
                {
                    scCache.Add(key, result.Config);
                    break;
                }
            }
        }

        // 检查错误
        if (notfoundServers.Count > 0 || configErrorServers.Count > 0)
        {
            return ServerConfigReadResult.PartialError(scCache.ToFrozenDictionary(), notfoundServers,
                configErrorServers);
        }

        return ServerConfigReadResult.Success(scCache.ToFrozenDictionary());
    }

    public static async Task<ServerConfigParseResult> GetSingleServerConfigAsync(int key, string targetDir)
    {
        if (!Directory.Exists(targetDir))
            return new ServerConfigParseResult(ServerConfigParseResultType.ServerNotFound);
        string targetConfig = Path.Combine(targetDir, "lslconfig.json");
        if (!File.Exists(targetConfig))
            return new ServerConfigParseResult(ServerConfigParseResultType.ConfigFileNotFound);
        string configFile;
        try
        {
            configFile = await File.ReadAllTextAsync(targetConfig);
        }
        catch (UnauthorizedAccessException)
        {
            return new ServerConfigParseResult(ServerConfigParseResultType.NoReadAccess);
        }

        if (string.IsNullOrWhiteSpace(configFile) || configFile == "{}")
            return new ServerConfigParseResult(ServerConfigParseResultType.EmptyConfig);
        try
        {
            var serverConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(configFile);
            if (serverConfig is null) return new ServerConfigParseResult(ServerConfigParseResultType.Unparsable);
            var vResult = CheckService.VerifyServerConfig(key, targetDir, serverConfig);
            if (!vResult.IsSuccess || vResult.Result is null)
                return new ServerConfigParseResult(ServerConfigParseResultType.MissingKey);
            return new ServerConfigParseResult(ServerConfigParseResultType.Success, vResult.Result);
        }
        catch (JsonException)
        {
            return new ServerConfigParseResult(ServerConfigParseResultType.Unparsable);
        }
    }

    #endregion

    // 注册与删除服务器均不会立刻更新字典

    #region 注册服务器方法RegisterServer

    public async Task<ServiceResult> RegisterServer(string serverName, string usingJava, string corePath, uint minMem,
        uint maxMem,
        string extJvm)
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
            string serializedConfig = JsonConvert.SerializeObject(initialConfig, Formatting.Indented);
            await File.WriteAllTextAsync(addedConfigPath, serializedConfig); // 写入服务器文件夹内的配置文件
            // 创建Eula文件
            if (!_mainConfigManager.CurrentConfigs.TryGetValue("auto_eula", out var rawEula) ||
                !bool.TryParse(rawEula.ToString(), out var eula)) eula = false;
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await File.WriteAllTextAsync(Path.Combine(addedServerPath, "eula.txt"),
                $"# Generated by LSL at {time}\r# For details of Mojang EULA, go to https://aka.ms/MinecraftEULA\reula={eula}");
            //找到空闲id
            int targetId = 0;
            while (MainServerConfig.ContainsKey(targetId))
            {
                ++targetId;
            }

            JsonHelper.AddJson(ConfigPathProvider.ServerConfigPath, targetId.ToString(),
                addedServerPath); // 在服务器列表文件中注册新服务器
            _logger.LogInformation("Server {ServerName} registered, config file path {AddedConfigPath}", serverName,
                addedConfigPath);
            return ServiceResult.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Server {ServerName} could not be registered", serverName);
            return ServiceResult.Fail(e);
        }
    }

    #endregion

    #region 修改服务器方法EditServer

    public async Task<ServiceResult> EditServer(int serverId, string serverName, string usingJava, uint minMem, uint maxMem,
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
                _logger.LogInformation("Server Edited:{id}", serverId);
                return ServiceResult.Success();
            }
            else
            {
                _logger.LogError("Server Not Found:{id}", serverId);
                return ServiceResult.Fail(new FileNotFoundException($"未找到服务器{serverId}：{serverName}"));
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Server {ServerId} failed to edit.", serverId);
            return ServiceResult.Fail(e);
        }
    }

    #endregion

    #region 删除服务器方法DeleteServer

    public async Task<ServiceResult> DeleteServer(int serverId)
    {
        if (!ServerConfigs.TryGetValue(serverId, out var config))
        {
            _logger.LogError("Server Not Found in Dictionary:{id}", serverId);
            return ServiceResult.Fail(new KeyNotFoundException($"在LSL配置文件中未找到id为{serverId}的服务器。"));
        }

        var serverPath = config.ServerPath;
        if (string.IsNullOrEmpty(serverPath) || !Directory.Exists(serverPath))
        {
            _logger.LogError("Server Directory Not Found:{id}", serverId);
            return ServiceResult.Fail(new FileNotFoundException($"编号为{serverId}的服务器的路径不存在。"));
        }

        try
        {
            await JsonHelper.DeleteJsonKeyAsync(ConfigPathProvider.ServerConfigPath, serverId.ToString()); // 在服务器列表文件中删除服务器
            await DirectoryExtensions.DeleteDirectoryAsync(serverPath); // 删除服务器文件夹
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Server {ServerId} could not be deleted", serverId);
            return ServiceResult.Fail(e);
        }
        
        _logger.LogInformation("Server deleted:{id}", serverId);
        return ServiceResult.Success();
    }

    #endregion

    #region 添加已有服务器方法AddExistedServer

    public async Task<ServiceResult> AddExistedServer(string serverName, string usingJava, string corePath, uint minMem,
        uint maxMem, string extJvm, IProgress<string>? progress = null)
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
        if (Directory.GetParent(originalServerDir)?.FullName != Path.GetFullPath(ConfigPathProvider.ServerConfigPath))
        {
            await DirectoryExtensions.CopyDirectoryAsync(originalServerDir, addedServerPath,
                DirectoryCopyMode.CopyContentsOnly, FileOverwriteMode.Overwrite,
                fileNameProgress: progress); // 复制核心文件到服务器文件夹内
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
            if (!_mainConfigManager.CurrentConfigs.TryGetValue("auto_eula", out var rawEula) ||
                !bool.TryParse(rawEula.ToString(), out var eula)) eula = false;
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await File.WriteAllTextAsync(Path.Combine(addedServerPath, "eula.txt"),
                $"# Generated by LSL at {time}\r# For details of Mojang EULA, go to https://aka.ms/MinecraftEULA\reula={eula}");
        }

        //找到空闲id
        int targetId = 0;
        while (MainServerConfig.ContainsKey(targetId))
        {
            ++targetId;
        }

        JsonHelper.AddJson(ConfigPathProvider.ServerConfigPath, targetId.ToString(),
            addedServerPath); // 在服务器列表文件中注册新服务器
        _logger.LogInformation("Server {serverName} registered, config file path {addedConfigPath}", serverName,
            addedConfigPath);
        return ServiceResult.Success();
    }

    #endregion
}