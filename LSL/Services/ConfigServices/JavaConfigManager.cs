using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    public FrozenDictionary<int, JavaInfo> JavaDict { get; private set; } = FrozenDictionary<int, JavaInfo>.Empty; // 目前读取的Java列表

    #region 读取Java列表
    public async Task<ServiceResult<JavaConfigReadResult>> ReadJavaConfig()
    {
        logger.LogInformation("Start reading JavaConfig...");
        try
        {
            // read
            var file = await File.ReadAllTextAsync(ConfigPathProvider.JavaListPath);
            var jsonObj = JObject.Parse(file);
            Dictionary<int, JavaInfo> tmpDict = [];
            foreach (var item in jsonObj.Properties()) //遍历配置文件中的所有Java
            {
                var versionObject = item.Value["Version"];
                var pathObject = item.Value["Path"];
                var vendorObject = item.Value["Vendor"];
                var archObject = item.Value["Architecture"];
                if (versionObject is null ||
                    pathObject is null ||
                    vendorObject is null ||
                    archObject is null ||
                    versionObject.Type != JTokenType.String ||
                    pathObject.Type != JTokenType.String ||
                    vendorObject.Type != JTokenType.String ||
                    archObject.Type != JTokenType.String) continue;
                var res = new JavaInfo(pathObject.ToString(), versionObject.ToString(), vendorObject.ToString(),
                    archObject.ToString());
                tmpDict.Add(int.Parse(item.Name), res);
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
            JavaDict = tmpDict.ToFrozenDictionary();
            logger.LogDebug("Read JavaConfig complete, Parsing...");
            if (notFound.Count > 0 || notJava.Count > 0)
            {
                var error = new StringBuilder("Some nonfatal error occured when reading java config:");
                error.AppendLine();
                if (notFound.Count > 0)
                {
                    error.AppendLine("The following items cannot be found:");
                    error.AppendJoin(Environment.NewLine, notFound);
                }

                if (notJava.Count > 0)
                {
                    error.AppendLine("The following items are not an executable java file:");
                    error.AppendJoin(Environment.NewLine, notJava);
                }
                error.AppendLine();
                error.Append("You may need to re-detect java to solve this problem.");
                logger.LogWarning("{}", error.ToString());
                return ServiceResult.FinishWithWarning(new JavaConfigReadResult(notFound, notJava), new Exception(error.ToString()));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured when reading java config.");
            return ServiceResult.Fail<JavaConfigReadResult>(ex);
        }
        return ServiceResult.Success(new JavaConfigReadResult());
    }

    #endregion

    #region 获取系统中的Java

    public async Task<ServiceResult> DetectJavaAsync()
    {
        if (!File.Exists(ConfigPathProvider.JavaListPath))
        {
            await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath, "{}");
        }

        logger.LogInformation("Start detecting Java...");
        List<JavaInfo> javaList;
        try
        {
            javaList = await Task.Run(JavaFinder.GetInstalledJavaInfosAsync); //调用JavaFinder查找JAVA
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error Detecting Java.");
            return ServiceResult.Fail(new KeyNotFoundException(e.Message));
        }
        Dictionary<string, JavaInfo> javaDict = [];
        //遍历写入Java信息
        int id = 0;
        foreach (var javaInfo in javaList)
        {
            string writtenId = id.ToString();
            javaDict.Add(writtenId, javaInfo);
            id++;
        }

        await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath,
            JsonConvert.SerializeObject(javaDict, Formatting.Indented)); //写入配置文件
        logger.LogInformation("Java detection completed, found {count} javas.", javaDict.Count);
        return ServiceResult.Success();
    }

    #endregion
}