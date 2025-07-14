using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using LSL.Common.Models;
using LSL.Common.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LSL.Services.ConfigServices;
/// <summary>
/// The config manager of LSL's saved java information.
/// </summary>
/// <param name="logger">An ILogger that logs logs. （拜托，想个更好的双关语吧（彼得帕克音））</param> 
public class JavaConfigManager(ILogger<JavaConfigManager> logger) //Java相关服务
{
    private ILogger<JavaConfigManager> _logger { get; } = logger;
    public ConcurrentDictionary<int, JavaInfo> JavaDict { get; private set; } = []; // 目前读取的Java列表

    #region 读取Java列表
    public ServiceResult ReadJavaConfig()
    {
        try
        {
            // read
            var file = File.ReadAllText(ConfigPathProvider.JavaListPath);
            var jsonObj = JObject.Parse(file);
            ConcurrentDictionary<int, JavaInfo> tmpDict = [];
            foreach (var item in jsonObj.Properties()) //遍历配置文件中的所有Java
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
                    var res = new JavaInfo(pathObject.ToString(), versionObject.ToString(), vendorObject.ToString(),
                        archObject.ToString());
                    tmpDict.AddOrUpdate(int.Parse(item.Name), k => res, (k, v) => res);
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
                    tmpDict.Remove(item.Key, out _);
                    continue;
                }

                if (JavaFinder.GetJavaInfo(path) is null)
                {
                    notJava.Add(path);
                    tmpDict.Remove(item.Key, out _);
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
                return new ServiceResult(ServiceResultType.FinishWithWarning, new Exception(error));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Reading JavaConfig");
            return new ServiceResult(ServiceResultType.Error, ex);
        }

        return ServiceResult.Success();
    }

    #endregion

    #region 获取系统中的Java

    public async Task<ServiceResult> DetectJava()
    {
        if (!File.Exists(ConfigPathProvider.JavaListPath))
        {
            await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath, "{}");
        }

        Debug.WriteLine("开始获取Java列表");
        List<JavaInfo> javaList = [];
        try
        {
            javaList = await Task.Run(JavaFinder.GetInstalledJavaInfosAsync); //调用JavaFinder查找JAVA
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error Detecting Java");
            return ServiceResult.Fail(new KeyNotFoundException($"在搜索Java时出现错误：{e.Message}"));
        }
        Dictionary<string, JavaInfo> javaDict = [];
        //遍历写入Java信息
        int id = 0;
        foreach (var javainfo in javaList)
        {
            string writtenId = id.ToString();
            javaDict.Add(writtenId, javainfo);
            id++;
        }

        await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath,
            JsonConvert.SerializeObject(javaDict, Formatting.Indented)); //写入配置文件
        return ServiceResult.Success();
    }

    #endregion
}