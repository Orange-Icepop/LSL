using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LSL.Services
{
    // 配置文件管理  
    public class ConfigurationManager
    {

        // 配置文件的路径  
        private static readonly string _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LSL", "config.json");

        // 初始化配置文件的路径  
        static ConfigurationManager()
        {
            // 确保LSL文件夹存在  
            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath)!);
        }
        private ConfigurationManager()
        { 
            
        }

        // 获取配置文件的路径  
        public static string ConfigFilePath => _configFilePath;

        #region 初始化配置文件方法
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

                var inputOptions = new JsonSerializerOptions
                {
                    WriteIndented = true // 设置此属性为true以启用缩进  
                };
                // 序列化成JSON字符串并写入文件  
                string configString = JsonSerializer.Serialize(initialConfig, inputOptions);
                File.WriteAllText(_configFilePath, configString);

                Console.WriteLine("config.json initialized.");
            }
        }
        #endregion

        #region 修改键值

        public static void ModifyConfig(string key, object keyValue)
        {
            //读取
            string configString = File.ReadAllText(ConfigFilePath);
            JsonDocument doc = JsonDocument.Parse(configString);
            //缓冲区
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
                {
                    bool foundKey = false;

                    // 遍历原始JSON文档的每个元素  
                    writer.WriteStartObject();
                    foreach (var property in doc.RootElement.EnumerateObject())
                    {
                        if (property.Name == key)
                        {
                            // 写入新的值  
                            foundKey = true;
                            writer.WritePropertyName(key);
                            JsonSerializer.Serialize(writer, keyValue);
                        }
                        else
                        {
                            // 写入原始属性的值  
                            property.WriteTo(writer);
                        }
                    }

                    if (!foundKey)
                    {
                        throw new ArgumentException($"Key '{key}' not found in config.json when writing");
                    }

                    writer.WriteEndObject();
                }

                // 读取修改后的JSON字符串并写回文件  
                configString = Encoding.UTF8.GetString(memoryStream.ToArray());
                File.WriteAllText(ConfigFilePath, configString);
            }
            //反序列化整个配置文件到一个字典
            /*
            using (JsonDocument doc = JsonDocument.Parse(configString))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty(key, out JsonElement value))
                {
                    var configObject = JsonSerializer.Deserialize<Dictionary<string, object>>(configString);
                    if (configObject != null && configObject.ContainsKey(key))
                    {
                        configObject[key] = keyvalue; // 修改键值
                        configString = JsonSerializer.Serialize(configObject, new JsonSerializerOptions { WriteIndented = true }); // 重新序列化
                        File.WriteAllText(_configFilePath, configString); // 写回文件
                    }
                    else
                    {
                        throw new ArgumentException($"Key '{key}' not found in config.json when writing");
                    }
                }
                else
                {
                    throw new ArgumentException($"Key '{key}' not found in config.json when writing");
                }
            }*/
        }
        #endregion

        #region 读取键值
        public static object ReadConfig(string key)
        {
            //读取
            string configString = File.ReadAllText(ConfigFilePath);

            using (JsonDocument doc = JsonDocument.Parse(configString))
            {
                JsonElement root = doc.RootElement;
                switch (root.GetProperty(key).ValueKind)
                {
                    case JsonValueKind.String: return root.GetProperty(key).GetString();
                    case JsonValueKind.Number: return root.GetProperty(key).GetInt32();
                    case JsonValueKind.True: return true;
                    case JsonValueKind.False: return false;
                    default: throw new ArgumentException("Key is not a string, number or bool");
                }
            }
            /*
            // 反序列化JSON到一个字典  
            var configObject = JsonSerializer.Deserialize<Dictionary<string, object>>(configString);

            if (configObject != null && configObject.ContainsKey(key))
            {
                if (configObject.TryGetValue(key, out var value))
                {
                    if (value is bool)
                    {
                        return (bool)value;
                    }
                    else if (value is string)
                    {
                        return (string)value;
                    }
                    else if (value is int)
                    {
                        return (int)value; 
                    }
                    else
                    {
                        return value;
                        //throw new ArgumentException($"Key '{key}' is not an expected type in config.json when reading");
                    }
                }
                else
                {
                    throw new ArgumentException($"Key '{key}' not found in config.json when reading");
                }
                
            }
            else
            {
                throw new ArgumentException($"Key ' {key} ' not found in config.json when reading");
            }*/
        }
        #endregion
    }
}
