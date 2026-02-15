using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using FluentResults;
using LSL.Common.Models;
using LSL.Common.Validation;
using Mutty;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace LSL.Models.MutableConfigs;

public class ReactiveWebConfig : ReactiveValidationObject
{
    [IgnoreDataMember] public static readonly ReactiveWebConfig Default = new();

    // Site
    public uint PanelPort { get; set; } = 25000;
    public bool UseSsl { get; set; } = false;
    public string CertPath { get; set; } = "LSL/ssl.crt";

    public string SiteUrl { get; set; } = string.Empty;

    // Performance
    public bool PerformanceMonitoring { get; set; } = true;
    public ulong PerformanceReportInterval { get; set; } = 1000;

    public ulong OutputReportInterval { get; set; } = 1000;

    // Styles
    public string SiteTitlePrefix { get; set; } = "LSL";
    public string SiteTitleSuffix { get; set; } = "服务器管理面板";
    public string SiteTitleSeparator { get; set; } = " | ";
    public string SiteThemeColor { get; set; } = "#33F3E5";
    public string SiteBackgroundColor { get; set; } = "#FFFFFF";
    public double SiteBackgroundOpacity { get; set; } = 1;
}