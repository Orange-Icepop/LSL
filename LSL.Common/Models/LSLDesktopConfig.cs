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
    public BackgroundStretchOption BackgroundStretch;
    public double BackgroundOpacity;
    public string CustomTitleText;
    public CustomPageOption CustomPageType;
    public string CustomPageUrl;
    // about
    public bool AutoCheckUpdate;
    public bool BetaUpdate;
}

public enum BackgroundStretchOption
{
    Fill,// fill without reserving the aspect ratio
    Uniform,// inscribed
    UniformToFill// external attach
}

public enum CustomPageOption
{
    None,// disable
    File,// Use custom.xaml under the LSL folder
    Url// Use the xaml provider online
}