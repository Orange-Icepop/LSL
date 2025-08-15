using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using LSL.Common.Models;

namespace LSL.Common.Validation;

/// <summary>
/// A class of systematic validators.
/// </summary>
public static class CheckService
{
    private static readonly ImmutableArray<string> ServerConfigKeys =
    [
        "name",
        "using_java",
        "core_name",
        "min_memory",
        "max_memory",
        "ext_jvm"
    ];

    #region 校验LSL核心配置方法 VerifyConfig(string key, object value)
    public static bool VerifyConfig(string key, object value)
    {
        try
        {
            switch (key)
            {
                //Common
                case "auto_eula":
                    if (value is not bool) return false; break;
                case "app_priority":
                    if (value is not int || (int)value > 2 || (int)value < 0) return false; break;
                case "end_server_when_close":
                    if (value is not bool) return false; break;
                case "daemon":
                    if (value is not bool) return false; break;
                case "auto_find_java":
                    if (value is not bool) return false; break;
                case "output_encode":
                    if (value is not int || (int)value > 1 || (int)value < 0) return false; break;
                case "input_encode":
                    if (value is not int || (int)value > 2 || (int)value < 0) return false; break;
                case "coloring_terminal":
                    if (value is not bool) return false; break;
                //Download
                case "download_source":// TODO: 开发多源下载
                    if (value is not int) return false; break;
                case "download_threads":// TODO: 开发多线程下载（不知道有没有必要）
                    if (value is not int || (int)value > 128 || (int)value < 1) return false; break;
                case "download_limit":// TODO: 开发下载限速
                    if (CheckComponents.DownloadLimit(value.ToString()).Passed == false) return false; break;
                //Panel
                case "panel_enable":
                    if (value is not bool) return false; break;
                case "panel_port":
                    if (CheckComponents.PanelPort(value.ToString()).Passed == false) return false; break;
                case "panel_monitor":
                    if (value is not bool) return false; break;
                case "panel_terminal":
                    if (value is not bool) return false; break;
                //Style:off
                //About
                case "auto_update":
                    if (value is not bool) return false; break;
                case "beta_update":
                    if (value is not bool) return false; break;
                default:
                    return false;
            }
        }
        catch (InvalidCastException)
        {
            return false;
        }
        return true;
    }
    #endregion

    #region 准服务器配置验证方法 VerifyFormedServerConfig
    public static List<VerifyResult> VerifyFormedServerConfig(FormedServerConfig config, bool skipCP = false)
    {
        var result = new List<VerifyResult>
        {
            CheckComponents.ServerName(config.ServerName),
            CheckComponents.JavaPath(config.JavaPath)
        };
        if(!skipCP) result.Add(CheckComponents.CorePath(config.CorePath));
        result.Add(CheckComponents.MaxMem(config.MaxMem, config.MinMem));
        result.Add(CheckComponents.MinMem(config.MinMem));
        result.Add(CheckComponents.ExtJvm(config.ExtJvm));
        return result;
    }

    #endregion
        
    #region 服务器配置验证方法
        
    public static ServiceResult<ServerConfig> VerifyServerConfig(int id, string path, IDictionary<string, string> config)
    {
        if (id < 0) return ServiceResult.Fail<ServerConfig>(new ArgumentException($"Server id of {id} is not valid."));
        ServerConfig cache = ServerConfig.None;
        var pResult = CheckComponents.ServerPath(path);
        if (!pResult.Passed) return ServiceResult.Fail<ServerConfig>(new ValidationException($"Error validating server config with id {id} because of nonexistent server path."));
        cache.server_id = id;
        cache.server_path = path;
        foreach (var item in ServerConfigKeys)
        {
            if (!config.TryGetValue(item, out var value))
                return ServiceResult.Fail<ServerConfig>(
                    new KeyNotFoundException($"key {item} not found in server with id {id}."));
            VerifyResult vResult;
            switch(item)
            {
                case "name":
                {
                    vResult = CheckComponents.ServerName(value);
                    cache.name = value;
                    break;
                }
                case "using_java":
                {
                    vResult = CheckComponents.JavaPath(value);
                    cache.using_java = value;
                    break;
                }
                case "core_name":
                {
                    vResult = VerifyResult.Success("core_name");
                    cache.core_name = value;
                    break;
                }
                case "min_memory":
                {
                    var tmp1 = CheckComponents.MinMem(value);
                    if (!tmp1.Passed) vResult = tmp1;
                    else if (!uint.TryParse(value, out var minmem)) vResult = VerifyResult.Fail(item, "min_memory's value is not an unsigned integer.");
                    else
                    {
                        cache.min_memory = minmem;
                        vResult = VerifyResult.Success("min_memory");
                    }
                    break;
                }
                case "max_memory":
                {
                    var tmp1 = CheckComponents.MaxMem(value);
                    if (!tmp1.Passed) vResult = tmp1;
                    else if (!uint.TryParse(value, out var maxmem)) vResult = VerifyResult.Fail(item, "max_memory's value is not an unsigned integer.");
                    else
                    {
                        cache.max_memory = maxmem;
                        vResult = VerifyResult.Success("max_memory");
                    }
                    break;
                }
                case "ext_jvm":
                {
                    vResult = CheckComponents.ExtJvm(value);
                    cache.ext_jvm = value;
                    break;
                }
                default:
                {
                    vResult = VerifyResult.Fail(string.IsNullOrEmpty(value) ? "string.Empty" : value,
                        "what fucking key it is?");
                    break;
                }
            }

            if (!vResult.Passed) return ServiceResult.Fail<ServerConfig>(
                new ValidationException($"Error validating server config with id {id} at key {vResult.Key}:{Environment.NewLine}{vResult.Reason}"));
        }
        return ServiceResult.Success(cache);
    }
    #endregion
}