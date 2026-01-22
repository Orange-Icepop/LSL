using System.Runtime.Serialization;
using System.Text;
using LSL.Common.Validation;
using Tomlyn;

namespace LSL.Common.Models.AppConfig;

public class DesktopConfig : IConfig<DesktopConfig>
{
    // system
    public bool EnableTray { get; set; } = true;
    public bool EnableDaemonKeepRunning { get; set; } = true;
    // server setup
    public bool AutoEula { get; set; } = true;
    public List<string> UniversalJvmPrefix { get; set; } = [];
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
    [IgnoreDataMember] public static string ConfigFileName => "DesktopConfig.toml";

    public ServiceResult Validate()
    {
        List<string> errors = [];
        if (!ThemeColor.IsValidRgbHex())
        {
            errors.Add("Theme color is invalid");
            ThemeColor = string.Empty;
        }
        if (!BackgroundColor.IsValidRgbHex())
        {
            errors.Add("Theme color is invalid");
            BackgroundColor = string.Empty;
        }
        if (!BackgroundOpacity.IsInRange(0.0, 1.0))
        {
            errors.Add("Background opacity is invalid");
            BackgroundOpacity = 1.0;
        }
        return errors.Count != 0 ? ServiceResult.Fail(new StringBuilder().AppendJoin('\n', errors).ToString()) : ServiceResult.Success();
    }

    public static ServiceResult<DesktopConfig> Deserialize(string content)
    {
        if (!Toml.TryToModel<DesktopConfig>(content, out var result, out var error))
            return ServiceResult.Fail<DesktopConfig>($"The desktop config is not parsable:\n{error}");
        var validationResult = result.Validate();
        return validationResult.IsSuccess ? ServiceResult.Success(result) : ServiceResult.Warning(result, validationResult.Error);
    }

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