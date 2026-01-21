using LSL.Common.Utilities.Json;
using Newtonsoft.Json;

namespace LSL.Common.Models.AppConfigs;

public class DesktopConfig : IConfig<DesktopConfig>
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
    [JsonIgnore]
    public string ConfigFileName => "DesktopConfig.json";

    public ServiceResult Validate()
    {
        if (DownloadThreads > 256) return ServiceResult.Fail("DownloadThreads must be at most 256");
        return ServiceResult.Success();
    }

    public static ServiceResult<DesktopConfig> Deserialize(string json)
    {
        var result = JsonConvert.DeserializeObject<DesktopConfig>(json, ConfigSerializerOptions.DefaultOptions);
        if (result is null)
            return ServiceResult.Fail<DesktopConfig>("The daemon config is not parsable");
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