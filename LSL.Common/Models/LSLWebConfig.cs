namespace LSL.Common.Models;

public struct LSLWebConfig
{
    // Site
    public uint PanelPort;
    public bool UseSSL;
    public string SiteURL;
    // Performance
    public bool PanelMonitoring;
    // Styles
    public string SitePrefix;
    public string SiteSuffix;
    public string SiteNameSeparator;
    public string SiteThemeColor;
}