namespace LSL.Common.Models.AppConfigs;

public class WebConfig : IConfig<WebConfig>
{
    // Site
    public uint PanelPort { get; set; } = 25000;
    public bool UseSSL { get; set; } = false;
    public string SiteUrl { get; set; } = string.Empty;
    // Performance
    public bool PanelMonitoring { get; set; } = true;
    public ulong ReportInterval { get; set; } = 1000;
    // Styles
    public string SiteTitlePrefix { get; set; } = "LSL";
    public string SiteTitleSuffix { get; set; } = "服务器管理面板";
    public string SiteTitleSeparator { get; set; } = " | ";
    public string SiteThemeColor { get; set; } = "#33F3E5";
    public string SiteBackgroundColor { get; set; } = "#FFFFFF";
}