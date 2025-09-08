namespace LSL.Common.Models;

public struct LSLAppConfig
{
    // system
    public bool Daemon;
    // server setup
    public bool AutoEula;
    public string UniversalJvmPrefix;
    // styles
    public double ThemeOpacity;
    public string ThemeColor;
    public uint BackgroundStretchOption;// 0 Fill:fill without reserving the aspect ratio, 1 Uniform:inscribed, 2 UniformToFill:external attach
    public double BackgroundOpacity;
    public bool EnableTitleText;
    public string TitleText;
    public uint CustomPageOption;// 0:Disable, 1:Use custom.xaml under the LSL folder, 2:Use the xaml provider online
    public uint CustomPageUrl;
    // about
    public bool AutoCheckUpdate;
    public bool BetaUpdate;
}