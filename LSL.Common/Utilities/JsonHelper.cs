using System.Diagnostics;
using LSL.Common.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LSL.Common.Utilities;

/// <summary>
/// A static helper for easy single-point access to json file.
/// In order to enhance performance, please don't use this method unless really necessary.
/// </summary>
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
                _ => throw new ArgumentException(
                    $"Key '{keyPath}' is not a string, number, or bool.这有可能是因为一个配置文件损坏导致的，请备份并删除配置文件再试。"),
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
