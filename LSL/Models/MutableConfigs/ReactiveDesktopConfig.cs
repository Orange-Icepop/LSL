using System.Runtime.Serialization;
using System.Text;
using FluentResults;
using LSL.Common.Models.AppConfig;
using LSL.Common.Validation;
using Mutty;
using ReactiveUI.Validation.Helpers;

namespace LSL.Models.MutableConfigs;

public class ReactiveDesktopConfig : ReactiveValidationObject
{
    [IgnoreDataMember] public static readonly ReactiveDesktopConfig Default = new();

    // system
    public bool EnableTray { get; set; } = true;
    public bool EnableDaemonKeepRunning { get; set; } = true;
    // styles
    public string ThemeColor { get; set; } = "#33F3E5";
    public string BackgroundColor { get; set; } = "#C7FFEE";
    public BackgroundStretchOption BackgroundStretch { get; set; } = BackgroundStretchOption.UniformToFill;
    public double BackgroundOpacity { get; set; } = 1.0;
    public string CustomTitleText { get; set; } = "Lime Server Launcher";
    public CustomPageOption CustomPageType { get; set; } = CustomPageOption.None;
    public string CustomPageUrl { get; set; } = string.Empty;
    // about
    public bool AutoCheckUpdate { get; set; } = true;
    public bool BetaUpdate { get; set; } = false;
}