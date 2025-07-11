﻿using System.Text.RegularExpressions;
using LSL.Common.Models;
using LSL.Common.Utilities;

namespace LSL.Common.Validation;

/// <summary>
/// A class that contains static methods for systematic validation.
/// </summary>
public static class CheckComponents
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

    public static bool IsValidJava(string? javaPath) //校验Java路径并确认可执行
    {
        if (!IsValidPath(javaPath)) return false;
        else if (!File.Exists(javaPath)) return false;
        else if (JavaFinder.GetJavaInfo == null) return false;
        else return true;
    }

    #endregion

    #region 添加/修改服务器验证器组件

    public static VerifyResult ServerName(string? name)
    {
        if (!IsValidFileName(name)) return new VerifyResult("ServerName", false, "服务器名称不可为空或包含特殊字符");
        else return new VerifyResult("ServerName", true, null);
    }

    public static VerifyResult JavaPath(string? path)
    {
        if (!IsValidPath(path)) return new VerifyResult("JavaPath", false, "Java路径不可为空或为非法路径");
        else if (!File.Exists(path)) return new VerifyResult("JavaPath", false, "选定的Java不存在");
        else return new VerifyResult("JavaPath", true, null);
    }

    public static VerifyResult CorePath(string? path)
    {
        if (!IsValidPath(path)) return new VerifyResult("CorePath", false, "核心路径不可为空或为非法路径");
        else if (!File.Exists(path)) return new VerifyResult("CorePath", false, "选定的核心不存在");
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
                return new VerifyResult("MinMem", false, "最小内存必须是正整数");
            }
            else if (num.Length > 7) return new VerifyResult("MinMem", false, "最小内存不可大于2TB");
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
                return new VerifyResult("MaxMem", false, "最小内存必须是正整数");
            }
            else if (num.Length > 7) return new VerifyResult("MaxMem", false, "最大内存不可大于2TB");
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

    public static VerifyResult ServerPath(string serverPath)
    {
        if (!IsValidPath(serverPath)) return new VerifyResult("ServerPath", false, "指定的服务器路径不存在");
        else return new VerifyResult("ServerPath", true, null);
    }

    #region LSL核心配置验证器组件

    public static VerifyResult DownloadLimit(string? value)
    {
        if (value == null || value.ToString() == "") return new VerifyResult("DownloadLimit", false, "下载限速不可为空");
        else
        {
            string pattern = @"^\d+$";
            if (!Regex.IsMatch(value, pattern))
            {
                return new VerifyResult("DownloadLimit", false, "下载限速必须是整数");
            }
            else if (value.Length > 8) return new VerifyResult("DownloadLimit", false, "下载限速不可大于100Gbps（有这个带宽的还要限速干嘛）");
            else if (int.Parse(value) == 0) return new VerifyResult("DownloadLimit", true, null);
            else if (value.ToString().StartsWith('0') && value.ToString().Length > 1)
                return new VerifyResult("DownloadLimit", false, "下载限速数字格式错误");
            else
            {
                var result = uint.Parse(value);
                if (result < 1) return new VerifyResult("DownloadLimit", false, "下载限速不可小于1KB/s");
                else if (result > 13107200)
                    return new VerifyResult("DownloadLimit", false, "下载限速不可大于100Gbps（有这个带宽的还要限速干嘛）");
                else return new VerifyResult("DownloadLimit", true, null);
            }
        }
    }

    public static VerifyResult PanelPort(string? value)
    {
        if (value == null || value.ToString() == "") return new VerifyResult("PanelPort", false, "面板端口不可为空");
        else
        {
            string pattern = @"^\d+$";
            if (!Regex.IsMatch(value, pattern))
            {
                return new VerifyResult("PanelPort", false, "面板端口必须是正整数");
            }
            else if (value.Length > 5) return new VerifyResult("PanelPort", false, "面板端口不可大于65535");
            else
            {
                var result = uint.Parse(value);
                if (result < 30) return new VerifyResult("PanelPort", false, "为了安全考虑，面板端口不可小于30");
                else if (result > 65535) return new VerifyResult("PanelPort", false, "面板端口不可大于65535");
                else return new VerifyResult("PanelPort", true, null);
            }
        }
    }

    #endregion
}