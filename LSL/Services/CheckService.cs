﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LSL.Services
{
    public static class CheckService
    {
        #region 校验文件名-路径-合法Java

        public static bool IsValidFileName(string? fileName) //校验文件名
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            // 获取文件名中无效的字符
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // 检查文件名是否包含任何无效字符
            return !fileName.Any(c => invalidChars.Contains(c));
        }

        public static bool IsValidPath(string? path) //校验路径
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            // 获取路径中无效的字符
            char[] invalidChars = Path.GetInvalidPathChars();
            return !path.Any(c => invalidChars.Contains(c));
        }

        public static bool IsValidJavaPath(string? javaPath) //校验Java路径
        {
            if (!IsValidPath(javaPath)) return false;
            else if (!File.Exists(javaPath)) return false;
            else if (JavaFinder.GetJavaInfo == null) return false;
            else return true;
        }

        #endregion

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
                        if (value is not int || (int)value < 0) return false; break;
                    //Panel
                    case "panel_enable":
                        if (value is not bool) return false; break;
                    case "panel_port":
                        if (value is not int || (int)value < 0 || (int)value > 65535) return false; break;
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

        #region 服务器配置验证器 VerifyServerConfig(Dictionary<string, object> config)
        public static List<VerifyResult> VerifyServerConfig(Dictionary<string, string> config)
        {
            var result = new List<VerifyResult>();
            foreach (var item in config)
            {
                VerifyResult resCache;
                switch (item.Key)
                {
                    case "ServerName": resCache = ServerName(item.Value); break;
                    case "JavaPath": resCache = JavaPath(item.Value); break;
                    case "CorePath": resCache = JavaPath(item.Value); break;
                    case "MinMem": resCache = MinMem(item.Value); break;
                    case "MaxMem": resCache = MinMem(item.Value); break;
                    case "ExtJvm": resCache = ExtJvm(item.Value); break;
                    default: resCache = new VerifyResult(item.Key, false, "未知配置项"); break;
                }
                result.Add(resCache);
            }
            return result;
        }

        #region 验证器组件
        // 为了组件化做的，让复用性提升
        public static VerifyResult ServerName(string? name)
        {
            if (!IsValidFileName(name)) return new VerifyResult("ServerName", false, "服务器名称不可为空或包含特殊字符");
            else return new VerifyResult("ServerName", true, null);
        }

        public static VerifyResult JavaPath(string? path)
        {
            if (!IsValidPath(path)) return new VerifyResult("JavaPath", false, "Java路径不可为空或为非法路径");
            else if (!File.Exists(path)) return new VerifyResult("JavaPath", false, "选定的Java不存在");
            else if (!IsValidJavaPath(path)) return new VerifyResult("JavaPath", false, "选定的文件不是有效的Java");
            else return new VerifyResult("JavaPath", true, null);
        }

        public static VerifyResult CorePath(string? path)
        {
            if (!IsValidPath(path)) return new VerifyResult("CorePath", false, "核心路径不可为空或为非法路径");
            else if (!File.Exists(path)) return new VerifyResult("CorePath", false, "选定的核心不存在");
            //else if (!IsValidCorePath(path)) return new VerifyResult( "CorePath",false,"选定的文件不是有效的核心");//TODO:对不同种类的核心进行验证
            else return new VerifyResult("CorePath", true, null);
        }

        public static VerifyResult MinMem(string? num)
        {
            if (num == null || num.ToString() == "")
            {
                return new VerifyResult("MinMem", false, "最小内存不可为空");
            }
            else
            {
                string pattern = @"^\d+$";
                if (!Regex.IsMatch(num, pattern))
                {
                    return new VerifyResult("MinMem", false, "必须是正整数");
                }
                else
                {
                    var result = uint.Parse(num);
                    if (result < 1) return new VerifyResult("MinMem", false, "最小内存不可小于1MB");
                    else if (result > 2097152) return new VerifyResult("MinMem", false, "最小内存不可大于2TB");
                    else return new VerifyResult("MinMem", true, null);
                }
            }
        }

        public static VerifyResult MaxMem(string? num, string minMem = "1")
        {
            if (num == null || num.ToString() == "")
            {
                return new VerifyResult("MaxMem", false, "最大内存不可为空");
            }
            else
            {
                string pattern = @"^\d+$";
                if (!Regex.IsMatch(num, pattern))
                {
                    return new VerifyResult("MaxMem", false, "必须是正整数");
                }
                else
                {
                    var result = uint.Parse(num);
                    if (result < 1) return new VerifyResult("MaxMem", false, "最大内存不可小于1MB");
                    else if (result < uint.Parse(minMem)) return new VerifyResult("MaxMem", false, "最大内存不可小于最小内存");
                    else if (result > 2097152) return new VerifyResult("MaxMem", false, "最大内存不可大于2TB");
                    else return new VerifyResult("MaxMem", true, null);
                }
            }
        }

        public static VerifyResult ExtJvm(string? num)
        {
            if (num != null && num.ToString() != "")
            {
                var group = num.Split(' ');
                foreach (var item in group)
                {
                    if (item.StartsWith('-') && !item.StartsWith("--") && !item.EndsWith('-')) continue;
                    else return new VerifyResult("ExtJvm", false, "扩展参数格式错误");
                }
            }
            return new VerifyResult("ExtJvm", true, null);
        }
        #endregion

        #endregion
    }

    // 表单验证器

    #region 服务器配置验证器
    public class ServerNameValidator : ValidationAttribute // 服务器名称验证器
    {
        protected override ValidationResult IsValid(object? value, ValidationContext context)
        {
            var result = CheckService.ServerName(value.ToString());
            if (result.Passed)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(result.Reason);
            }
        }
    }

    public class ServerCorePathValidator : ValidationAttribute // 服务器核心路径验证器
    {
        protected override ValidationResult IsValid(object? value, ValidationContext context)
        {
            var result = CheckService.CorePath(value.ToString());
            if (result.Passed)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(result.Reason);
            }
        }
    }

    public class MinMemValidator : ValidationAttribute // 最小内存验证器
    {
        protected override ValidationResult IsValid(object? value, ValidationContext context)
        {
            var result = CheckService.MinMem(value.ToString());
            if (result.Passed)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(result.Reason);
            }
        }
    }

    public class MaxMemValidator : ValidationAttribute // 最大内存验证器
    {
        protected override ValidationResult IsValid(object? value, ValidationContext context)
        {
            var result = CheckService.MaxMem(value.ToString());
            if (result.Passed)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(result.Reason);
            }
        }
    }

    public class ExtJvmValidator : ValidationAttribute // 扩展参数验证器
    {
        protected override ValidationResult IsValid(object? value, ValidationContext context)
        {
            var result = CheckService.ExtJvm(value.ToString());
            if (result.Passed)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(result.Reason);
            }
        }
    }
    #endregion


    public record VerifyResult(string Key, bool Passed, string? Reason);
}
