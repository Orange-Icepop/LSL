using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LSL.Common.Models;
using LSL.Common.Options;
using LSL.Common.Utilities;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;
/// <summary>
/// The config manager of LSL's saved java information.
/// </summary>
/// <param name="logger">An ILogger that logs logs.</param> 
public class JavaConfigManager(ILogger<JavaConfigManager> logger) //Java相关服务
{
    public FrozenDictionary<int, JavaInfo> JavaDict { get; private set; } = FrozenDictionary<int, JavaInfo>.Empty; // 目前读取的Java列表

    #region 读取Java列表
    public async Task<ServiceResult<Dictionary<int, JavaInfo>>> ReadJavaConfig(bool writeBack = false)
    {
        var res = await ReadConfig()//先读配置文件
            .BindAsync(async dict =>// 验证
        {
            ConcurrentBag<string> errors = [];
            await Parallel.ForEachAsync(dict, ConcurrencyOptions.ConcurrencyLimit, async (kvp, _) =>
            {
                if (!await kvp.Value.Validate()) errors.Add(kvp.Value.Path);
            });
            return errors.IsEmpty
                ? ServiceResult.Success(dict)
                : ServiceResult.Warning(dict, new StringBuilder().AppendJoin('\n', errors).ToString());
        });
        if (res.IsError)
        {
            logger.LogError(res.Error, "Something went wrong while reading java config.");
            return res;
        }

        if (res.IsWarning) logger.LogWarning("Some javas are invalid when reading java config:{error}", res.Error.ToString());
        if (writeBack)
        {
            await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath, JsonSerializer.Serialize(res.Result, SnakeJsonOptions.Default.DictionaryInt32JavaInfo));
        }
        logger.LogInformation("Read Java config completed.");
        return res;
    }

    private static async Task<ServiceResult<Dictionary<int, JavaInfo>>> ReadConfig()
    {
        try
        {
            var file = await File.ReadAllTextAsync(ConfigPathProvider.JavaListPath);
            var strDict = JsonSerializer.Deserialize(file, SnakeJsonOptions.Default.DictionaryStringJavaInfo) ??
                          new Dictionary<string, JavaInfo>();
            Dictionary<int, JavaInfo> tmpDict = [];
            List<string> error = [];
            foreach (var kvp in strDict)
            {
                if (!int.TryParse(kvp.Key, out var id)) error.Add(kvp.Key);
                else tmpDict.Add(id, kvp.Value);
            }

            return error.Count > 0
                ? ServiceResult.Warning(tmpDict,
                    new StringBuilder("Keys ").AppendJoin(',', error).Append(" are not valid").ToString())
                : ServiceResult.Success(tmpDict);
        }
        catch (Exception e)
        {
            return ServiceResult.Fail<Dictionary<int, JavaInfo>>(e);
        }
    }

    #endregion

    #region 获取系统中的Java

    public async Task<ServiceResult> DetectJavaAsync()
    {
        try
        {
            if (!File.Exists(ConfigPathProvider.JavaListPath))
            {
                await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath, "");
            }

            logger.LogInformation("Start detecting Java...");
            List<JavaInfo> javaList;
            try
            {
                javaList = await Task.Run(JavaFinder.GetInstalledJavaInfosAsync); //调用JavaFinder查找Java
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
                JsonSerializer.Serialize(javaDict, SnakeJsonOptions.Default.DictionaryInt32JavaInfo)); //写入配置文件
            logger.LogInformation("Java detection completed, found {count} javas.", javaDict.Count);
            return ServiceResult.Success();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error Detecting Java.");
            return ServiceResult.Fail(e);
        }
    }

    #endregion
}