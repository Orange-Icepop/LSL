using System.Text;
using LSL.Common.Utilities;
using LSL.Common.Validation;
using Newtonsoft.Json;

namespace LSL.Common.Models.AppConfigs;

public class WebConfig : IConfig<WebConfig>
{
    // Site
    public uint PanelPort { get; set; } = 25000;
    public bool UseSsl { get; set; } = false;
    public string CertPath { get; set; } = "LSL/ssl.crt";
    public string SiteUrl { get; set; } = string.Empty;
    // Performance
    public bool PerformanceMonitoring { get; set; } = true;
    public ulong PerformanceReportInterval { get; set; } = 1000;
    public ulong OutputReportInterval { get; set; } = 1000;
    // Styles
    public string SiteTitlePrefix { get; set; } = "LSL";
    public string SiteTitleSuffix { get; set; } = "服务器管理面板";
    public string SiteTitleSeparator { get; set; } = " | ";
    public string SiteThemeColor { get; set; } = "#33F3E5";
    public string SiteBackgroundColor { get; set; } = "#FFFFFF";
    public double SiteBackgroundOpacity { get; set; } = 1;
    [JsonIgnore] public static string ConfigFileName => "WebConfig.json";

    public ServiceResult Validate()
    {
        List<string> errors = [];
        if (!PanelPort.IsInRange<uint>(1, 65535))
        {
            PanelPort = 25000;
            errors.Add("PanelPort must be in range of [1,65535]");
        }

        if (!SiteUrl.IsValidUri())
        {
            SiteUrl = string.Empty;
            errors.Add("SiteUrl is invalid");
        }

        if (!PerformanceReportInterval.IsInRange<ulong>(100, 30000))
        {
            PerformanceReportInterval = 1000;
            errors.Add("PerformanceReportInterval must be in range of [100,30000]");
        } 
        if (!OutputReportInterval.IsInRange<ulong>(100, 30000))
        {
            OutputReportInterval = 1000;
            errors.Add("OutputReportInterval must be in range of [100,30000]");
        }

        if (!SiteThemeColor.IsValidRgbHex())
        {
            SiteThemeColor = "#33F3E5";
            errors.Add("SiteThemeColor is invalid");
        }
        if (!SiteBackgroundColor.IsValidRgbHex())
        {
            SiteBackgroundColor = "#FFFFFF";
            errors.Add("SiteThemeColor is invalid");
        }

        if (!SiteBackgroundOpacity.IsInRange(0, 1))
        {
            SiteBackgroundOpacity = 1;
            errors.Add("SiteBackgroundOpacity must be in range of [0,1]");
        }

        return errors.Count != 0
            ? ServiceResult.Fail(new StringBuilder().AppendJoin("\n", errors).ToString())
            : ServiceResult.Success();
    }

    public static ServiceResult<WebConfig> Deserialize(string json)
    {
        var result = JsonConvert.DeserializeObject<WebConfig>(json, NsJsonOptions.DefaultOptions);
        if (result is null)
            return ServiceResult.Fail<WebConfig>("The web panel config is not parsable");
        var validationResult = result.Validate();
        return validationResult.IsSuccess ? ServiceResult.Success(result) : ServiceResult.Warning(result, validationResult.Error);
    }
}