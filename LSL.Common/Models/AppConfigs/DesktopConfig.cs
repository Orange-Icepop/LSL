namespace LSL.Common.Models.AppConfigs;

public record DesktopConfig : IConfig<DesktopConfig>
{
    // system
    public bool EnableTray { get; set; } = true;
    public bool EnableDaemonKeepRunning { get; set; } = true;
    // server setup
    public bool AutoEula { get; set; } = true;
    public string[] UniversalJvmPrefix { get; set; } = [];
    // styles
    public string ThemeColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public BackgroundStretchOption BackgroundStretch { get; set; } = BackgroundStretchOption.UniformToFill;
    public double BackgroundOpacity { get; set; } = 1.0;
    public string CustomTitleText { get; set; } = string.Empty;
    public CustomPageOption CustomPageType { get; set; } = CustomPageOption.None;
    public string CustomPageUrl { get; set; } = string.Empty;
    // about
    public bool AutoCheckUpdate { get; set; } = true;
    public bool BetaUpdate { get; set; } = false;
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