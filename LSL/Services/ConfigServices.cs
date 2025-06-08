using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LSL.IPC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LSL.Services
{
    //JSON基本操作类
    public static class JsonHelper
    {
        // 注意：
        // 1、本类中所有方法均假设Json文件结构正确，否则将抛出异常
        // 2、除非必须要单独修改某个键值，否则不要使用ModifyJson，从而减小文件操作次数
        #region 修改键值基本方法ModifyJson
        public static void ModifyJson(string filePath, string keyPath, object keyValue)
        {
            //读取
            string jsonString = File.ReadAllText(filePath);
            JObject jsonObject = JObject.Parse(jsonString);
            //寻找指定的值
            JToken? token = jsonObject.SelectToken(keyPath);
            // 检查键是否存在  
            if (token != null && token.Type != JTokenType.Property)
            {
                // 修改键的值
                token.Replace(JToken.FromObject(keyValue));
            }
            else if (token is JProperty prop)
            {
                prop.Value = JToken.FromObject(keyValue);
            }
            else
            {
                throw new ArgumentException($"{keyPath} 不存在。请备份并删除配置文件重试。");
            }


            // 将修改后的JSON写回文件  
            string updatedString = jsonObject.ToString(Formatting.Indented);
            File.WriteAllText(filePath, updatedString);

            Debug.WriteLine($"{keyPath} changed successfully.");
        }
        #endregion

        #region 读取键值基本方法ReadJson
        public static object ReadJson(string filePath, string keyPath)
        {
            //读取
            string jsonString = File.ReadAllText(filePath);
            JObject jsonObject = JObject.Parse(jsonString);
            JToken? token = jsonObject.SelectToken(keyPath);

            if (token == null)
            {
                throw new Exception($"{keyPath} not existed.这有可能是因为一个配置文件损坏导致的，请备份并删除配置文件再试。");
            }
            else
            {
                return token.Type switch
                {
                    JTokenType.String => token.Value<string>()!,
                    JTokenType.Integer => token.Value<int>(),
                    JTokenType.Boolean => token.Value<bool>(),
                    _ => throw new ArgumentException($"Key '{keyPath}' is not a string, number, or bool.这有可能是因为一个配置文件损坏导致的，请备份并删除配置文件再试。"),
                };
            }
        }
        #endregion

        #region 增加键值基本方法AddJson
        public static void AddJson(string filePath, string keyPath, object keyValue)
        {
            string json = File.ReadAllText(filePath);
            JObject jObject = JObject.Parse(json);
            // 尝试找到路径的父对象或父数组  
            string[] pathParts = keyPath.Split('.');
            string lastPart = pathParts.Last();
            string parentPath = string.Join(".", pathParts.Take(pathParts.Length - 1));

            JToken? parentToken = parentPath.Length > 0 ? jObject.SelectToken(parentPath) : jObject;

            if (parentToken is not null)
            {
                if (parentToken is JArray jArray)
                {
                    // 如果是数组，添加新项（注意：这里假设jsonToAdd可以直接转换为JToken或简单类型）  
                    jArray.Add(JToken.FromObject(keyValue));
                }
                else if (parentToken is JObject jParentObject)
                {
                    // 如果是对象，添加新属性  
                    jParentObject.Add(lastPart, JToken.FromObject(keyValue));
                }
                else
                {
                    throw new ArgumentException("指定的JSON路径不是对象或数组");
                }

                // 将修改后的JObject转换回字符串并写回文件  
                string output = jObject.ToString();
                File.WriteAllText(filePath, output);

                Debug.WriteLine($"Key {keyPath} added successfully");
            }
            else
            {
                throw new ArgumentException("无法找到添加新值的JSON路径");
            }

        }
        #endregion

        #region 删除键值基本方法DeleteJsonKey
        public static void DeleteJsonKey(string filePath, string keyPath)
        {
            JObject jObject = JObject.Parse(File.ReadAllText(filePath));
            jObject.Remove(keyPath);
            File.WriteAllText(filePath, jObject.ToString());
        }
        #endregion

        #region 清空JSON文件方法ClearJson
        // 这个方法会创建一个json文件以避免错误
        public static void ClearJson(string filePath)
        {
            // 检查文件路径是否有效  
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new FatalException($"文件路径不能为空：{filePath}");
            }
            else
            {
                string? directoryPath = Path.GetDirectoryName(filePath);
                if (directoryPath != null && string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                // 将文件内容替换为一个空的JSON对象
                File.WriteAllText(filePath, "{}");
            }
        }
        #endregion

    }
    // 配置文件管理  
    public class ConfigManager
    {
        // 获取配置文件的路径  
        public static string ConfigFilePath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LSL", "Config.json");
        public static string ServerConfigPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LSL", "ServersConfig.json");
        public static string JavaListPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LSL", "JavaList.json");
        public static string CJavaListPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LSL", "CustomJavaList.json");
        public static string ServersPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Servers");

        static ConfigManager()
        {
            //初始化配置文件
            Initialize();
            //LoadConfig();
        }

        // 启动器配置模块
        #region 默认配置字典
        public static readonly Dictionary<string, object> DefaultConfigs = new()
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
        };
        #endregion

        #region 初始化配置文件
        private static void Initialize()
        {
            // 确保LSL文件夹存在  
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            Directory.CreateDirectory(ServersPath);
            if (!File.Exists(ConfigFilePath))
            {
                // 将初始配置字典序列化成JSON字符串并写入文件  
                string configString = JsonConvert.SerializeObject(DefaultConfigs, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, configString);
                Debug.WriteLine("Config.json initialized.");
            }
            if (!File.Exists(ServerConfigPath))
            {
                File.WriteAllText(ServerConfigPath, "{}");
                Debug.WriteLine("ServerConfig.json initialized.");
            }
            if (!File.Exists(JavaListPath))
            {
                File.WriteAllText(JavaListPath, "{}");
                Debug.WriteLine("JavaList.json initialized.");
            }
            if (!File.Exists(CJavaListPath))
            {
                File.WriteAllText(CJavaListPath, "{}");
                Debug.WriteLine("CustomJavaList.json initialized.");
            }
        }
        #endregion

        #region 使用的键的列表
        public static readonly List<string> ConfigKeys =
        [
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
        ];
        #endregion

        #region 集体修改配置方法 ConfirmConfig(Dictionary<string, object> confs)
        public static void ConfirmConfig(Dictionary<string, object> confs)
        {
            foreach (var conf in confs)
            {
                if (CheckService.VerifyConfig(conf.Key, conf.Value))
                {
                    CurrentConfigs[conf.Key] = conf.Value;
                }
            }
            File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(CurrentConfigs, Formatting.Indented));
        }
        #endregion

        #region 读取配置键值
        // 当前配置字典
        public static readonly Dictionary<string, object> CurrentConfigs = [];

        public static ServiceResult LoadConfig()
        {
            JObject configs;
            try
            {
                configs = JObject.Parse(File.ReadAllText(ConfigFilePath));
            }
            catch (FileNotFoundException ex)
            {
                return ServiceResult.Fail(new FileNotFoundException(
                    $"位于{ConfigFilePath}的LSL主配置文件不存在，请重启LSL。{Environment.NewLine}注意，这不是一个正常情况，因为LSL会在启动时自动创建主配置文件。若错误依旧，则LSL可能已经损坏，请重新下载。{Environment.NewLine}错误信息:{ex.Message}"));
            }
            catch (JsonReaderException ex)
            {
                return ServiceResult.Fail(new JsonReaderException(
                    $"位于{ConfigFilePath}的LSL主配置文件已损坏，请删除该文件并重启。{Environment.NewLine}具备能力的可以将其备份或者进行手动修复。{Environment.NewLine}错误信息:{ex.Message}"));
            }
            catch (Exception ex)
            {
                return ServiceResult.Fail(new Exception(
                    $"在读取位于{ConfigFilePath}的LSL主配置文件时出现未知错误。{Environment.NewLine}错误信息：{ex.Message}"));
            }
            List<string> keysNeedToRepair = [];// 需要修复的键
            CurrentConfigs.Clear();// 清空当前配置字典
            foreach (var key in ConfigKeys)
            {
                var config = configs[key];
                object? keyValue = config?.Type switch// 根据值类型读取
                {
                    JTokenType.Boolean => config.Value<bool>(),
                    JTokenType.Integer => config.Value<int>(),
                    JTokenType.String => config.Value<string>(),
                    _ => null,
                };
                if (keyValue == null || !CheckService.VerifyConfig(key, keyValue))
                {
                    CurrentConfigs.Add(key, DefaultConfigs[key]);
                    keysNeedToRepair.Add(key);
                }
                else
                {
                    CurrentConfigs.Add(key, keyValue);
                }
            }
            if (keysNeedToRepair.Count > 0)// 修复配置
            {
                foreach (var key in keysNeedToRepair)
                {
                    if (configs.ContainsKey(key))
                    {
                        object defaultConfig = DefaultConfigs[key];
                        Type type = defaultConfig.GetType();
                        if (type == typeof(bool))
                        {
                            configs[key] = (bool)defaultConfig;
                        }
                        else if (type == typeof(int))
                        {
                            configs[key] = (int)defaultConfig;
                        }
                        else if (type == typeof(string))
                        {
                            configs[key] = (string)defaultConfig;
                        }
                    }
                    else
                    {
                        configs.Add(key, JToken.FromObject(DefaultConfigs[key]));
                    }
                }
                File.WriteAllText(ConfigFilePath, configs.ToString());
                Debug.WriteLine("Config.json repaired.");
            }
            Debug.WriteLine("Config.json loaded.");
            return ServiceResult.Success;
        }
        #endregion

    }

    public class ServerConfigManager//服务器配置相关服务
    {
        public static Dictionary<int, string> MainServerConfig = [];

        public static Dictionary<int, ServerConfig> ServerConfigs = [];

        public static readonly List<string> ServerConfigKeys =
            [
                "name",
                "using_java",
                "core_name",
                "min_memory",
                "max_memory",
                "ext_jvm"
            ];

        #region 读取各个服务器的LSL配置文件LoadServerConfigs
        public static ServiceResult LoadServerConfigs()
        {
            ServerConfigs = [];
            List<string> NotfoundServers = [];
            List<string> ConfigErrorServers = [];
            // 读取服务器主配置文件
            string mainFile = "";
            try
            {
                mainFile = File.ReadAllText(ConfigManager.ServerConfigPath);
                if (string.IsNullOrEmpty(mainFile) || mainFile == "{}") return ServiceResult.Success;
            }
            catch (FileNotFoundException)
            {
                return ServiceResult.Fail(new FileNotFoundException(
                    $"位于{ConfigManager.ServerConfigPath}的服务器主配置文件不存在，请重启LSL。\r注意，这不是一个正常情况，因为LSL通常会在启动时创建该文件。若错误依旧，则LSL已经损坏，请重新下载。"));
            }
            try
            {
                var configs = JsonConvert.DeserializeObject<Dictionary<int, string>>(mainFile);
                if (configs is null) throw new JsonException();
                else MainServerConfig = configs;
            }
            catch (JsonException)
            {
                return ServiceResult.Fail(new JsonReaderException(
                    $"LSL读取到了服务器主配置文件，但是它是一个非法的Json文件。\r请确保{ConfigManager.ServerConfigPath}文件的格式正确。"));
            }
            // 读取各个服务器的LSL配置文件
            foreach (var config in MainServerConfig)
            {
                // 读取步骤
                string targetPath = Path.Combine(ConfigManager.ServersPath, config.Value, "lslconfig.json");
                string configFile = "";
                try
                {
                    configFile = File.ReadAllText(targetPath);
                    if (string.IsNullOrEmpty(configFile) || configFile == "{}") throw new FileNotFoundException();
                }
                catch (FileNotFoundException)
                {
                    NotfoundServers.Add(config.Value);
                    continue;
                }
                // 解析步骤
                try
                {
                    var serverConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(configFile);
                    if (serverConfig == null) throw new JsonException();
                    foreach (var item in ServerConfigKeys)
                    {
                        if (!serverConfig.ContainsKey(item)) throw new JsonException();
                    }
                    ServerConfigs.Add(config.Key, new ServerConfig(config.Key, config.Value, serverConfig["name"], serverConfig["using_java"], serverConfig["core_name"], uint.Parse(serverConfig["min_memory"]), uint.Parse(serverConfig["max_memory"]), serverConfig["ext_jvm"]));
                }
                catch (JsonException)
                {
                    ConfigErrorServers.Add(targetPath);
                    continue;
                }
            }
            // 检查错误
            if (NotfoundServers.Count > 0 || ConfigErrorServers.Count > 0)
            {
                string ErrorContext = "LSL读取了服务器主配置文件，但是它包含了一些";
                if (NotfoundServers.Count > 0 && ConfigErrorServers.Count > 0) ErrorContext += "不存在的服务器和格式错误的服务器配置文件。";
                else if (NotfoundServers.Count > 0) ErrorContext += "不存在的服务器。";
                else if (ConfigErrorServers.Count > 0) ErrorContext += "格式错误的服务器配置文件。";
                if (NotfoundServers.Count > 0) ErrorContext += "\r不存在的服务器：" + string.Join(", \r", NotfoundServers) + "\r请确保" + ConfigManager.ServerConfigPath + "文件中的服务器名称与实际服务器文件夹名称一致。";
                if (ConfigErrorServers.Count > 0) ErrorContext += "\r格式错误的服务器配置文件：" + string.Join(", \r", ConfigErrorServers) + "\r请确保这些配置文件的格式正确。";
                return new ServiceResult(ServiceResultType.FinishWithError, new Exception(ErrorContext));
            }
            return ServiceResult.Success;
        }
        #endregion

        #region 注册服务器方法RegisterServer
        public static void RegisterServer(string serverName, string usingJava, string corePath, uint maxMem, uint minMem, string extJVM)
        {
            if (!File.Exists(ConfigManager.ServerConfigPath))
            {
                File.WriteAllText(ConfigManager.ServerConfigPath, "{}");
            }
            // 连接服务器路径
            string addedServerPath = Path.Combine(ConfigManager.ServersPath, serverName);
            string addedConfigPath = Path.Combine(addedServerPath, "lslconfig.json");
            string trueCorePath = Path.Combine(addedServerPath, Path.GetFileName(corePath));
            string coreName = Path.GetFileName(corePath);
            Directory.CreateDirectory(addedServerPath);
            File.Copy(corePath, trueCorePath, true);// 复制核心文件到服务器文件夹内
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
            File.WriteAllText(addedConfigPath, serializedConfig);// 写入服务器文件夹内的配置文件
            // 创建Eula文件
            bool eula = (bool)ConfigManager.CurrentConfigs["auto_eula"];
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.WriteAllText(Path.Combine(addedServerPath, "eula.txt"), $"# For details of Mojang EULA, go to https://aka.ms/MinecraftEULA\r# Generated by LSL at {time}\reula={eula}");
            //找到空闲id
            int targetId = 0;
            while (MainServerConfig.ContainsKey(targetId))
            {
                ++targetId;
            }
            JsonHelper.AddJson(ConfigManager.ServerConfigPath, targetId.ToString(), addedServerPath);// 在服务器列表文件中注册新服务器
            Debug.WriteLine($"Server {serverName} registered, config file path {addedConfigPath}");
        }
        #endregion

        #region 修改服务器方法EditServer
        public static void EditServer(int serverId, string serverName, string usingJava, uint minMem, uint maxMem, string extJVM)
        {
            if (MainServerConfig.TryGetValue(serverId, out var serverPath) && Directory.Exists(serverPath))
            {
                string editedConfigPath = Path.Combine(serverPath, "lslconfig.json");
                string coreName = ServerConfigs[serverId].core_name;
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
                File.WriteAllText(editedConfigPath, serializedConfig);// 写入服务器文件夹内的配置文件
                Debug.WriteLine("Server Edited:" + serverId);
                LoadServerConfigs();
            }
            else
            {
                Debug.WriteLine("Server Not Found:" + serverId);
            }
        }
        #endregion

        #region 删除服务器方法DeleteServer
        public static void DeleteServer(int serverId)
        {
            string serverPath = ServerConfigs[serverId].server_path;
            if (serverPath != null && Directory.Exists(serverPath))
            {
                JsonHelper.DeleteJsonKey(ConfigManager.ServerConfigPath, serverId.ToString());// 在服务器列表文件中删除服务器
                Directory.Delete(serverPath, true);// 删除服务器文件夹
                Debug.WriteLine("Server Deleted:" + serverId);
            }
        }
        #endregion

    }

    public static class JavaManager//Java相关服务
    {

        public static Dictionary<int, JavaInfo> JavaDict = [];// 目前读取的Java列表

        #region 读取Java列表
        public static ServiceResult ReadJavaConfig()
        {
            try
            {
                // read
                var file = File.ReadAllText(ConfigManager.JavaListPath);
                var jsonObj = JObject.Parse(file);
                var tmpDict = new Dictionary<int, JavaInfo>();
                foreach (var item in jsonObj.Properties())//遍历配置文件中的所有Java
                {
                    var versionObject = item.Value["Version"];
                    var pathObject = item.Value["Path"];
                    var vendorObject = item.Value["Vendor"];
                    var archObject = item.Value["Architecture"];
                    if (versionObject is not null &&
                        pathObject is not null &&
                        vendorObject is not null &&
                        archObject is not null &&
                        versionObject.Type == JTokenType.String &&
                        pathObject.Type == JTokenType.String &&
                        vendorObject.Type == JTokenType.String &&
                        archObject.Type == JTokenType.String)
                    {
                        tmpDict.Add(int.Parse(item.Name), new JavaInfo(pathObject.ToString(), versionObject.ToString(), vendorObject.ToString(), archObject.ToString()));
                    }
                }
                // validate
                List<string> notFound = [];
                List<string> notJava = [];
                foreach (var item in tmpDict)
                {
                    var path = item.Value.Path;
                    if (!File.Exists(path))
                    {
                        notFound.Add(path);
                        tmpDict.Remove(item.Key);
                        continue;
                    }

                    if (JavaFinder.GetJavaInfo(path) is null)
                    {
                        notJava.Add(path);
                        tmpDict.Remove(item.Key);
                    }
                }
                // end
                JavaDict = tmpDict;
                if (notFound.Count > 0 || notJava.Count > 0)
                {
                    var error = "配置文件中的部分Java不存在。" + Environment.NewLine;
                    if (notFound.Count > 0)
                    {
                        error += "以下Java的路径不存在：";
                        foreach (var item in notFound)
                        {
                            error += item + Environment.NewLine;
                        }
                    }

                    if (notJava.Count > 0)
                    {
                        error += "以下文件不是Java：";
                        foreach (var item in notJava)
                        {
                            error += item + Environment.NewLine;
                        }
                    }

                    error += "这些错误一般可以通过重新搜索Java解决。";
                    return new ServiceResult(ServiceResultType.FinishWithError, new Exception(error));
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(ServiceResultType.Error, ex);
            }
            return ServiceResult.Success;
        }
        #endregion

        #region 获取系统中的Java
        public static async Task DetectJava()
        {
            if (!File.Exists(ConfigManager.JavaListPath))
            {
                await File.WriteAllTextAsync(ConfigManager.JavaListPath, "{}");
            }
            Debug.WriteLine("开始获取Java列表");
            var javaList = await Task.Run(JavaFinder.GetInstalledJavaInfosAsync);//调用JavaFinder查找JAVA
            Dictionary<string, JavaInfo> javaDict = [];
            //遍历写入Java信息
            int id = 0;
            foreach (JavaInfo javainfo in javaList)
            {
                string writtenId = id.ToString();
                javaDict.Add(writtenId, javainfo);
                id++;
            }
            await File.WriteAllTextAsync(ConfigManager.JavaListPath, JsonConvert.SerializeObject(javaDict, Formatting.Indented));//写入配置文件
        }
        #endregion

    }
}


