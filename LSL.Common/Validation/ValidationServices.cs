using System.ComponentModel.DataAnnotations;

namespace LSL.Common.Validation
{
    // 表单验证器
    #region 服务器配置验证器
    public class ServerNameValidator : ValidationAttribute // 服务器名称验证器
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var result = CheckComponents.ServerName(value as string);
            return result.Passed ? ValidationResult.Success : new ValidationResult(result.Reason);
        }
    }

    public class ServerCorePathValidator : ValidationAttribute // 服务器核心路径验证器
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var result = CheckComponents.CorePath(value as string);
            return result.Passed ? ValidationResult.Success : new ValidationResult(result.Reason);
        }
    }

    public class MinMemValidator : ValidationAttribute // 最小内存验证器
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var result = CheckComponents.MinMem(value as string);
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
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var result = CheckComponents.MaxMem(value as string);
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

    public class JavaPathValidator : ValidationAttribute // Java路径验证器
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var result = CheckComponents.JavaPath(value as string);
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
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var result = CheckComponents.ExtJvm(value as string);
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

    #region 配置文件验证器
    public class DownloadLimitValidator : ValidationAttribute // 下载限速验证器
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var result = CheckComponents.DownloadLimit(value?.ToString());
            return result.Passed ? ValidationResult.Success : new ValidationResult(result.Reason);
        }
    }

    public class PanelPortValidator : ValidationAttribute // 控制面板端口验证器
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var result = CheckComponents.PanelPort(value?.ToString());
            return result.Passed ? ValidationResult.Success : new ValidationResult(result.Reason);
        }
    }
    #endregion
}
