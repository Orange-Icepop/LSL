using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentResults;
using FluentResults.Extensions;
using LSL.Common.Extensions;
using LSL.Common.Models;
using LSL.Common.Models.Minecraft;
using LSL.Common.Options;
using LSL.Common.Utilities.Minecraft;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;

/// <summary>
///     The config manager of LSL's saved java information.
/// </summary>
/// <param name="logger">An ILogger that logs logs.</param>
public class JavaConfigManager(ILogger<JavaConfigManager> logger) //Java相关服务
{
    public ImmutableDictionary<int, JavaInfo> JavaDict { get; private set; } =
        ImmutableDictionary<int, JavaInfo>.Empty; // 目前读取的Java列表

    #region 获取系统中的Java

    public async Task<Result> DetectJavaAsync()
    {
        try
        {
            if (!File.Exists(ConfigPathProvider.JavaListPath))
                await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath, "{}");

            logger.LogInformation("Start detecting Java...");
            List<JavaInfo> javaList;
            try
            {
                javaList = await JavaFinder.GetInstalledJavaInfosAsync(); //调用JavaFinder查找Java
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error Detecting Java.");
                return Result.Fail(new ExceptionalError(e));
            }

            Dictionary<int, JavaInfo> javaDict = [];
            //遍历写入Java信息
            var id = 0;
            foreach (var javaInfo in javaList)
            {
                javaDict.Add(id, javaInfo);
                id++;
            }
            JavaDict = javaDict.ToImmutableDictionary();
            await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath,
                JsonSerializer.Serialize(javaDict, SnakeJsonOptions.Default.DictionaryInt32JavaInfo)); //写入配置文件
            logger.LogInformation("Java detection completed, found {count} javas.", javaDict.Count);
            return Result.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error Detecting Java.");
            return Result.Fail(new ExceptionalError(e));
        }
    }

    #endregion

    #region 读取Java列表

    public async Task<Result<ImmutableDictionary<int, JavaInfo>>> ReadJavaConfig(bool writeBack = false)
    {
        return await ReadConfig() //先读配置文件
            .Bind(async dict => // 验证
            {
                ConcurrentBag<string> errors = [];
                ConcurrentDictionary<int, JavaInfo> tmpDict = [];
                await Parallel.ForEachAsync(dict, ConcurrencyOptions.ConcurrencyLimit, async (kvp, _) =>
                {
                    if (!await kvp.Value.Validate())
                        errors.Add(kvp.Value.Path);
                    else
                        tmpDict.TryAdd(kvp.Key, kvp.Value);
                });
                return errors.IsEmpty
                    ? Result.Ok(tmpDict)
                    : Result.Ok(tmpDict).WithReasons(errors.Select(i => new WarningReason(i)));
            }).Bind(dict => Result.Ok(dict.ToImmutableDictionary()))
            .Handle(async dict =>
            {
                JavaDict = dict;
                if (writeBack)
                    await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath,
                        JsonSerializer.Serialize(dict, SnakeJsonOptions.Default.DictionaryInt32JavaInfo));
                logger.LogInformation("Read Java config completed.");
            }, async (dict, w) =>
            {
                JavaDict = dict;
                logger.LogWarning("Some javas are invalid when reading java config:\n{error}", w.GetMessages());
                if (writeBack)
                    await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath,
                        JsonSerializer.Serialize(dict, SnakeJsonOptions.Default.DictionaryInt32JavaInfo));
                logger.LogInformation("Read Java config completed.");
            }, e =>
            {
                logger.LogError("Something went wrong while reading java config.\n{ex}", e.GetMessages());
                return Task.CompletedTask;
            });
    }

    private static async Task<Result<Dictionary<int, JavaInfo>>> ReadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPathProvider.JavaListPath))
            {
                await File.WriteAllTextAsync(ConfigPathProvider.JavaListPath, "{}");
                return Result.Ok(new Dictionary<int, JavaInfo>());
            }

            var file = await File.ReadAllTextAsync(ConfigPathProvider.JavaListPath);
            var strDict = JsonSerializer.Deserialize(file, SnakeJsonOptions.Default.DictionaryStringJavaInfo) ??
                          new Dictionary<string, JavaInfo>();
            Dictionary<int, JavaInfo> tmpDict = [];
            List<string> error = [];
            foreach (var kvp in strDict)
                if (!int.TryParse(kvp.Key, out var id)) error.Add(kvp.Key);
                else tmpDict.Add(id, kvp.Value);

            return error.Count > 0
                ? Result.Ok(tmpDict).WithReason(new WarningReason(
                    new StringBuilder("Keys ").AppendJoin(',', error).Append(" are not valid").ToString()))
                : Result.Ok(tmpDict);
        }
        catch (Exception e)
        {
            return Result.Fail<Dictionary<int, JavaInfo>>(new ExceptionalError(e));
        }
    }

    #endregion
}