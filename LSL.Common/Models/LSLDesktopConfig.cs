namespace LSL.Common.Models;

public struct LSLDesktopConfig
{
    // system
    public bool EnableTray;
    public bool EnableDaemonKeepRunning;
    // server setup
    public bool AutoEula;
    public string UniversalJvmPrefix;
    // styles
    public string ThemeColor;
    public string BackgroundColor;
    public uint BackgroundStretchOption;// 0 Fill:fill without reserving the aspect ratio, 1 Uniform:inscribed, 2 UniformToFill:external attach
    public double BackgroundOpacity;
    public string CustomTitleText;
    public uint CustomPageOption;// 0:Disable, 1:Use custom.xaml under the LSL folder, 2:Use the xaml provider online
    public string CustomPageUrl;
    // about
    public bool AutoCheckUpdate;
    public bool BetaUpdate;
}