using System.Text;
using LSL.Common.Utilities.Json;
using LSL.Common.Validation;
using Newtonsoft.Json;

namespace LSL.Common.Models.AppConfigs;

public class DaemonConfig : IConfig<DaemonConfig>
{
    public bool WebPanelAutoStart { get; set; } = false;
    public uint DownloadThreads { get; set; } = 4;
    public ulong DownloadLimitKBytes { get; set; } = 0;
    public bool EndServerOnClose { get; set; } = true;
    public bool AllowPanelShutdownDaemon { get; set; } = false;
    public bool AllowPanelEditDaemonConfig { get; set; } = false;
    [JsonIgnore]
    public string ConfigFileName => "DaemonConfig.json";

    public ServiceResult Validate()
    {
        if (DownloadThreads > 256) return ServiceResult.Fail("DownloadThreads must be at most 256");
        return ServiceResult.Success();
    }

    public static ServiceResult<DaemonConfig> Deserialize(string json)
    {
        var result = JsonConvert.DeserializeObject<DaemonConfig>(json, ConfigSerializerOptions.DefaultOptions);
        if (result is null)
            return ServiceResult.Fail<DaemonConfig>("The daemon config is not parsable");
        var validationResult = result.Validate();
        return validationResult.IsSuccess ? ServiceResult.Success(result) : ServiceResult.Warning(result, validationResult.Error);
    }
}