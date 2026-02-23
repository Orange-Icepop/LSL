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
public partial record DesktopConfig : AppConfigBase<DesktopConfig>, IConfig<DesktopConfig>
{
    [IgnoreDataMember] public static readonly DesktopConfig Default = new();

    // system
    [TomlPropertyName("enable_tray")] public bool EnableTray { get; init; } = true;
    public bool EnableDaemonKeepRunning { get; init; } = true;

    // styles
    public string ThemeColor { get; init; } = "#33F3E5";
    public string BackgroundColor { get; init; } = "#C7FFEE";
    public BackgroundStretchOption BackgroundStretch { get; init; } = BackgroundStretchOption.UniformToFill;
    public double BackgroundOpacity { get; init; } = 1.0;
    public string CustomTitleText { get; init; } = "Lime Server Launcher";
    public CustomPageOption CustomPageType { get; init; } = CustomPageOption.None;
    public string CustomPageUrl { get; init; } = string.Empty;

    // about
    public bool AutoCheckUpdate { get; init; } = true;
    public bool BetaUpdate { get; init; } = false;

    public override Result Validate()
    {
        List<string> errors = [];
        if (!ThemeColor.IsValidRgbHex()) errors.Add("Theme color is invalid");
        if (!BackgroundColor.IsValidRgbHex()) errors.Add("Theme color is invalid");
        if (!BackgroundOpacity.IsInRange(0.0, 1.0)) errors.Add("Background opacity is invalid");
        return errors.Count != 0
            ? Result.Fail(new StringBuilder().AppendJoin('\n', errors).ToString())
            : Result.Ok();
    }

    public override Result<DesktopConfig> ValidateAndFix()
    {
        var tmp = this.CreateDraft();
        List<string> errors = [];
        if (!ThemeColor.IsValidRgbHex())
        {
            tmp.ThemeColor = Default.ThemeColor;
            errors.Add("Theme color is invalid");
        }

        if (!BackgroundColor.IsValidRgbHex())
        {
            tmp.BackgroundColor = Default.BackgroundColor;
            errors.Add("Background color is invalid");
        }

        if (!BackgroundOpacity.IsInRange(0.0, 1.0))
        {
            tmp.BackgroundOpacity = Default.BackgroundOpacity;
            errors.Add("Background opacity is invalid");
        }

        return errors.Count != 0
            ? Result.Ok(tmp.FinishDraft()).WithReasons(errors.Select(s=>new WarningReason(s)))
            : Result.Ok(this);
    }
}

public partial class MutableDesktopConfig : INotifyPropertyChanged;

public enum BackgroundStretchOption
{
    Fill, // fill without reserving the aspect ratio
    Uniform, // inscribed
    UniformToFill // external attach
}

public enum CustomPageOption
{
    None, // disable
    File, // Use custom.xaml under the LSL folder
    Url // Use the xaml provider online
}