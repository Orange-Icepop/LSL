using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LSL.Common.Core;
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
    private ILogger<ServerConfigManager> _logger { get; } = logger;
    private MainConfigManager _mainConfigManager { get; } = mcm;
    public FrozenDictionary<int, string> MainServerConfig { get; private set; } = FrozenDictionary<int, string>.Empty;

    public FrozenDictionary<int, ServerConfig> ServerConfigs { get; private set; } =
        FrozenDictionary<int, ServerConfig>.Empty;

    #region 读取各个服务器的LSL配置文件ReadServerConfig

    public ServiceResult ReadServerConfig()
    {
        _logger.LogInformation("Start reading server config...");
        string mainPath = ConfigPathProvider.ServerConfigPath;
        // 读取服务器主配置文件
        var indexRes = GetIndexConfig(mainPath);
        if (indexRes.HasError || indexRes.Result is null)
            return ServiceResult.Fail(indexRes.Error ?? new Exception($"Error reading main server config {mainPath}."));
        MainServerConfig = indexRes.Result;
        var detailRes = GetServerDetails(MainServerConfig);
        if (detailRes.HasError || detailRes.Result is null)
            return ServiceResult.Fail(detailRes.Error ??
                                      new Exception($"Error reading main server config {mainPath}."));
        ServerConfigs = detailRes.Result;
        _logger.LogInformation("Finished reading server config.");
        return ServiceResult.Success();
    }

    #endregion

    #region 刷新服务器主配置文件

    private ServiceResult<FrozenDictionary<int, string>> GetIndexConfig(string path)
    {
        if (!File.Exists(path))
        {
            var ex = new FileNotFoundException($"Server main config at {path} not found.");
            _logger.LogError(ex, "");
            return ServiceResult.Fail<FrozenDictionary<int, string>>(ex);
        }

        string mainFile = File.ReadAllText(path);
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
        catch (JsonException)
        {
            var err = new JsonReaderException(
                $"The main server config file at {path} is not a valid LSL server config file.");
            _logger.LogError(err, "");
            return ServiceResult.Fail<FrozenDictionary<int, string>>(err);
        }
    }

    #endregion

    #region 逐个获取服务器各自的配置文件

    private static ServiceResult<FrozenDictionary<int, ServerConfig>> GetServerDetails(
        IDictionary<int, string> mainConfigs)
    {
        List<string> NotfoundServers = [];
        List<string> ConfigErrorServers = [];
        // 读取各个服务器的LSL配置文件
        Dictionary<int, ServerConfig> scCache = [];
        foreach (var (key, targetDir) in mainConfigs)
        {
            // 读取步骤
            string targetConfig = Path.Combine(targetDir, "lslconfig.json");
            if (!File.Exists(targetConfig))
            {
                NotfoundServers.Add(targetDir);
                continue;
            }

            var configFile = File.ReadAllText(targetConfig);
            if (string.IsNullOrWhiteSpace(configFile) || configFile == "{}")
            {
                NotfoundServers.Add(targetDir);
                continue;
            }

            // 解析步骤
            try
            {
                var serverConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(configFile) ??
                                   throw new FormatException("Error parsing server config to dictionary.");
                var vResult = CheckService.VerifyServerConfig(key, targetDir, serverConfig);
                if (!vResult.IsFullSuccess || vResult.Result is null) ConfigErrorServers.Add(targetConfig);
                else scCache.Add(key, vResult.Result);
            }
            catch (JsonException)
            {
                ConfigErrorServers.Add(targetConfig);
            }
        }

        // 检查错误
        if (NotfoundServers.Count > 0 || ConfigErrorServers.Count > 0)
        {
            StringBuilder error = new("LSL在读取服务器配置文件时发现了以下错误：");
            if (NotfoundServers.Count > 0)
            {
                error.AppendLine()
                    .AppendLine($"有{NotfoundServers.Count}个已注册的服务器不存在：")
                    .AppendJoin(Environment.NewLine, NotfoundServers);
            }

            if (ConfigErrorServers.Count > 0)
            {
                error.AppendLine()
                    .AppendLine($"有{ConfigErrorServers.Count}个服务器的配置文件格式不正确：")
                    .AppendJoin(Environment.NewLine, ConfigErrorServers);
            }

            return ServiceResult.FinishWithWarning(scCache.ToFrozenDictionary(), new Exception(error.ToString()));
        }

        return ServiceResult.Success(scCache.ToFrozenDictionary());
    }

    #endregion

    // 注册与删除服务器均不会立刻更新字典

    #region 注册服务器方法RegisterServer

    public ServiceResult RegisterServer(string serverName, string usingJava, string corePath, uint minMem, uint maxMem,
        string extJVM)
    {
        if (!File.Exists(ConfigPathProvider.ServerConfigPath))
        {
            File.WriteAllText(ConfigPathProvider.ServerConfigPath, "{}");
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
            ext_jvm = extJVM
        };
        string serializedConfig = JsonConvert.SerializeObject(initialConfig, Formatting.Indented);
        File.WriteAllText(addedConfigPath, serializedConfig); // 写入服务器文件夹内的配置文件
        // 创建Eula文件
        if (!_mainConfigManager.CurrentConfigs.TryGetValue("auto_eula", out var rawEula) ||
            !bool.TryParse(rawEula.ToString(), out var eula)) eula = false;
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        File.WriteAllText(Path.Combine(addedServerPath, "eula.txt"),
            $"# Generated by LSL at {time}\r# For details of Mojang EULA, go to https://aka.ms/MinecraftEULA\reula={eula}");
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

    #region 修改服务器方法EditServer

    public ServiceResult EditServer(int serverId, string serverName, string usingJava, uint minMem, uint maxMem,
        string extJVM)
    {
        try
        {
            if (MainServerConfig.TryGetValue(serverId, out var serverPath) && Directory.Exists(serverPath) &&
                ServerConfigs.TryGetValue(serverId, out var config))
            {
                string editedConfigPath = Path.Combine(serverPath, "lslconfig.json");
                string coreName = config.core_name;
                var newConfig = new
                {
                    name = serverName,
                    using_java = usingJava,
                    core_name = coreName,
                    min_memory = minMem,
                    max_memory = maxMem,
                    ext_jvm = extJVM
                };
                string serializedConfig = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
                File.WriteAllText(editedConfigPath, serializedConfig); // 写入服务器文件夹内的配置文件
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
            _logger.LogError("{e}", e.Message);
            return ServiceResult.Fail(e);
        }
    }

    #endregion

    #region 删除服务器方法DeleteServer

    public ServiceResult DeleteServer(int serverId)
    {
        if (!ServerConfigs.TryGetValue(serverId, out var config))
        {
            _logger.LogError("Server Not Found in Dictionary:{id}", serverId);
            return ServiceResult.Fail(new KeyNotFoundException($"在LSL配置文件中未找到id为{serverId}的服务器。"));
        }

        var serverPath = config.server_path;
        if (string.IsNullOrEmpty(serverPath) || !Directory.Exists(serverPath))
        {
            _logger.LogError("Server Directory Not Found:{id}", serverId);
            return ServiceResult.Fail(new FileNotFoundException($"编号为{serverId}的服务器的路径不存在。"));
        }

        JsonHelper.DeleteJsonKey(ConfigPathProvider.ServerConfigPath, serverId.ToString()); // 在服务器列表文件中删除服务器
        Directory.Delete(serverPath, true); // 删除服务器文件夹
        _logger.LogInformation("Server Deleted:{id}", serverId);
        return ServiceResult.Success();
    }

    #endregion

    #region 添加已有服务器方法AddExistedServer

    public async Task<ServiceResult> AddExistedServer(string serverName, string usingJava, string corePath, uint minMem,
        uint maxMem, string extJVM, IProgress<string>? progress = null)
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
        await DirectoryExtensions.CopyDirectoryAsync(originalServerDir, addedServerPath,
            DirectoryCopyMode.CopyContentsOnly, FileOverwriteMode.Overwrite,
            fileNameProgress: progress); // 复制核心文件到服务器文件夹内
        // 初始化服务器配置文件
        var initialConfig = new
        {
            name = serverName,
            using_java = usingJava,
            core_name = coreName,
            min_memory = minMem,
            max_memory = maxMem,
            ext_jvm = extJVM
        };
        string serializedConfig = JsonConvert.SerializeObject(initialConfig, Formatting.Indented);
        await File.WriteAllTextAsync(addedConfigPath, serializedConfig); // 写入服务器文件夹内的配置文件
        // 创建Eula文件
        if(!File.Exists(Path.Combine(addedServerPath, "eula.txt")))
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