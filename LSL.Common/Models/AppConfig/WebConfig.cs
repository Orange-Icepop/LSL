using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using FluentResults;
using LSL.Common.Validation;
using Mutty;
using Tomlyn;

namespace LSL.Common.Models.AppConfig;

[TomlModel]
[MutableGeneration]
public record WebConfig : AppConfigBase<WebConfig>, IConfig<WebConfig>
{
    [IgnoreDataMember] public static readonly WebConfig Default = new();

    // Site
    public uint PanelPort { get; init; } = 25000;
    public bool UseSsl { get; init; } = false;
    public string CertPath { get; init; } = "LSL/ssl.crt";

    public string SiteUrl { get; init; } = string.Empty;

    // Performance
    public bool PerformanceMonitoring { get; init; } = true;
    public ulong PerformanceReportInterval { get; init; } = 1000;
    public ulong OutputReportInterval { get; init; } = 1000;

    // Styles
    public string SiteTitlePrefix { get; init; } = "LSL";
    public string SiteTitleSuffix { get; init; } = "服务器管理面板";
    public string SiteTitleSeparator { get; init; } = " | ";
    public string SiteThemeColor { get; init; } = "#33F3E5";
    public string SiteBackgroundColor { get; init; } = "#FFFFFF";
    public double SiteBackgroundOpacity { get; init; } = 1;

    public override Result Validate()
    {
        List<string> errors = [];
        if (!PanelPort.IsInRange<uint>(1, 65535)) errors.Add("PanelPort must be in range of [1,65535]");
        if (!SiteUrl.IsValidUri(true)) errors.Add("SiteUrl is invalid");
        if (!PerformanceReportInterval.IsInRange<ulong>(100, 30000))
            errors.Add("PerformanceReportInterval must be in range of [100,30000]");
        if (!OutputReportInterval.IsInRange<ulong>(100, 30000))
            errors.Add("OutputReportInterval must be in range of [100,30000]");
        if (!SiteThemeColor.IsValidRgbHex()) errors.Add("SiteThemeColor is invalid");
        if (!SiteBackgroundColor.IsValidRgbHex()) errors.Add("SiteThemeColor is invalid");
        if (!SiteBackgroundOpacity.IsInRange(0, 1)) errors.Add("SiteBackgroundOpacity must be in range of [0,1]");
        return errors.Count != 0
            ? Result.Fail(new StringBuilder().AppendJoin("\n", errors).ToString())
            : Result.Ok();
    }

    public override Result<WebConfig> ValidateAndFix()
    {
        var tmp = this.CreateDraft();
        List<string> errors = [];
        if (!PanelPort.IsInRange<uint>(1, 65535))
        {
            tmp.PanelPort = Default.PanelPort;
            errors.Add("PanelPort must be in range of [1,65535]");
        }

        if (!SiteUrl.IsValidUri(true))
        {
            tmp.SiteUrl = Default.SiteUrl;
            errors.Add("SiteUrl is invalid");
        }

        if (!PerformanceReportInterval.IsInRange<ulong>(100, 30000))
        {
            tmp.PerformanceReportInterval = Default.PerformanceReportInterval;
            errors.Add("PerformanceReportInterval must be in range of [100,30000]");
        }

        if (!OutputReportInterval.IsInRange<ulong>(100, 30000))
        {
            tmp.OutputReportInterval = Default.OutputReportInterval;
            errors.Add("OutputReportInterval must be in range of [100,30000]");
        }

        if (!SiteThemeColor.IsValidRgbHex())
        {
            tmp.SiteThemeColor = Default.SiteThemeColor;
            errors.Add("SiteThemeColor is invalid");
        }

        if (!SiteBackgroundColor.IsValidRgbHex())
        {
            tmp.SiteBackgroundColor = Default.SiteBackgroundColor;
            errors.Add("SiteThemeColor is invalid");
        }

        if (!SiteBackgroundOpacity.IsInRange(0, 1))
        {
            tmp.SiteBackgroundOpacity = Default.SiteBackgroundOpacity;
            errors.Add("SiteBackgroundOpacity must be in range of [0,1]");
        }

        return errors.Count != 0
            ? Result.Ok(tmp.FinishDraft()).WithReasons(errors.Select(s => new WarningReason(s)))
            : Result.Ok(this);
    }
}

public partial class MutableWebConfig : INotifyPropertyChanged;