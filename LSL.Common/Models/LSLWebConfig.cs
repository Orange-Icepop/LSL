namespace LSL.Common.Models;

public struct LSLWebConfig
{
    // Site
    public uint PanelPort;
    public bool UseSSL;
    public string SiteUrl;
    // Performance
    public bool PanelMonitoring;
    // Styles
    public string SiteTitlePrefix;
    public string SiteTitleSuffix;
    public string SiteTitleSeparator;
    public string SiteThemeColor;
    public string SiteBackgroundColor;
}