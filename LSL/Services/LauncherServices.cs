using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MinecraftLaunch.Components.Fetcher;
using MinecraftLaunch.Classes.Models.Game;
using LSL.ViewModels;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LSL.Views;
using Avalonia.Metadata;
using ReactiveUI;

namespace LSL.Services
{
    //JSON基本操作类
    public class JsonHelper
    {
        #region 修改键值基本方法ModifyJson
        public static void ModifyJson(string filePath, string keyPath, object keyValue)
        {
            //读取
            string jsonString = File.ReadAllText(filePath);
            JObject jsonObject = JObject.Parse(jsonString);
            //寻找指定的值
            JToken token = jsonObject.SelectToken(keyPath);
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
            JToken token = jsonObject.SelectToken(keyPath);

            if (token == null)
            {
                throw new ArgumentException($"{keyPath} not existed.这有可能是因为一个配置文件损坏导致的，请备份并删除配置文件再试。");
            }

            switch (token.Type)
            {
                case JTokenType.String:
                    return token.Value<string>();
                case JTokenType.Integer:
                    return token.Value<int>();
                case JTokenType.Boolean:
                    return token.Value<bool>();
                default:
                    throw new ArgumentException($"Key '{keyPath}' is not a string, number, or bool.这有可能是因为一个配置文件损坏导致的，请备份并删除配置文件再试。");
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

            JToken parentToken = parentPath.Length > 0 ? jObject.SelectToken(parentPath) : jObject;

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

        #region 清空JSON文件方法ClearJson
        // 这个方法会创建一个json文件以避免错误
        public static void ClearJson(string filePath)
        {
            // 检查文件路径是否有效  
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"{nameof(filePath)}文件不存在。下一次启动时LSL将自动修复这个错误。");
            }

            // 确保文件路径的目录存在  
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // 将文件内容替换为一个空的JSON对象  
            File.WriteAllText(filePath, "{}");
        }
        #endregion

    }
    // 配置文件管理  
    public class ConfigManager
    {
        public static void Init() 
        { 
            // 确保LSL文件夹存在  
            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(_serverConfigPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(_javaConfigPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(_serversPath)!);
            ConfigViewModel configViewModel = new ConfigViewModel();
            configViewModel.GetConfig();
        }
        // 配置文件的路径  
        private static readonly string _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LSL", "config.json");
        private static readonly string _serverConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LSL", "serverConfig.json");
        public static readonly string _javaConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LSL", "javaConfig.json");
        public static readonly string _serversPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Servers");

        // 初始化配置文件的路径  
        static ConfigManager()
        {
        }
        private ConfigManager()
        {

        }

        // 获取配置文件的路径  
        public static string ConfigFilePath => _configFilePath;
        public static string ServerConfigPath => _serverConfigPath;
        public static string JavaConfigPath => _javaConfigPath;
        public static string ServersPath => _serversPath;

        #region 初始化配置文件
        public static void WriteInitialConfig()
        {
            if (!File.Exists(_configFilePath))
            {
                // 定义初始配置数据（json格式）
                var initialConfig = new
                {
                    //Common
                    auto_eula = false,
                    app_priority = 1,
                    end_server_when_close = false,
                    daemon = true,
                    java_selection = 0,
                    auto_find_java = true,
                    output_encode = 0,
                    input_encode = 0,
                    coloring_terminal = true,
                    //Download
                    download_source = 0,
                    download_threads = 16,
                    download_limit = 0,
                    //Panel
                    panel_enable = true,
                    panel_port = 25000,
                    panel_monitor = true,
                    panel_terminal = true,
                    //Style:off
                    //About
                    auto_update = true,
                    beta_update = false
                };

                // 序列化成JSON字符串并写入文件  
                string configString = JsonConvert.SerializeObject(initialConfig, Formatting.Indented);
                File.WriteAllText(_configFilePath, configString);

                Debug.WriteLine("config.json initialized.");
            }
        }
        #endregion

        #region 注册服务器方法RegisterServer
        public static void RegisterServer(string serverName, string usingJava, string corePath, int minMem, int maxMem, string extJVM)
        {
             // 连接服务器路径
            string addedServerPath = Path.Combine(_serversPath, serverName);
            string addedConfigPath = Path.Combine(addedServerPath, "lslconfig.json");
            Directory.CreateDirectory(Path.GetDirectoryName(addedConfigPath)!);
            // 初始化服务器配置文件
            var initialConfig = new
            {
                name = serverName,
                using_java = usingJava,
                core_path = corePath,
                min_memory = minMem,
                max_memory = maxMem,
                ext_jvm = extJVM
            };

            //找到空闲id
            int targetId = 0;
            try
            {
                while (true)
                {
                    string idString = targetId.ToString();
                    var verifyObject = JsonHelper.ReadJson(ServerConfigPath, idString);
                    targetId++;
                }
            }
            catch (Exception)
            {
                JsonHelper.AddJson(ServerConfigPath, targetId.ToString(), initialConfig);
            }

            Debug.WriteLine($"Server {serverName} registered, config file path {addedConfigPath}");
        }
        #endregion

        // 修改配置键值

        public static void ModifyConfig(string key, object keyValue)
        {
            string keyPath = "$." + key;
            try
            {
                JsonHelper.ModifyJson(ConfigFilePath, keyPath, keyValue);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"{ConfigFilePath} 文件已损坏，请备份并删除该文件重试。");
            }
        }

        //读取配置键值
        public static object ReadConfig(string key)
        {
            string keyPath = "$." + key;
            try
            {
                return JsonHelper.ReadJson(ConfigFilePath, keyPath);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"{ConfigFilePath} 文件已损坏，请备份并删除该文件重试。");
            }
        }

    }

    public class GameManager//服务器相关服务
    {
        JavaFetcher javaFetcher = new JavaFetcher();

        #region 获取java列表
        public async void DetectJava()
        {
            var JavaList = await javaFetcher.FetchAsync();//调用MinecraftLaunch的API查找JAVA
            JsonHelper.ClearJson(ConfigManager.JavaConfigPath);//清空Java记录
            //遍历写入Java信息
            int id = 1;
            foreach (JavaEntry javalist in JavaList)
            {
                string writtenId = id.ToString();
                JsonHelper.AddJson(ConfigManager.JavaConfigPath, writtenId, new { version = javalist.JavaVersion, path = javalist.JavaPath });
                id++;
            }
        }
        #endregion

        #region 根据Java编号获取Java路径
        public static string MatchJavaList(int id)
        {
            string javaPath = (string)JsonHelper.ReadJson(ConfigManager.JavaConfigPath, "$." + id.ToString() + ".path");
            return javaPath;
        }
        #endregion
    }

    public class SharedValues : ReactiveObject
    {

    }
}
