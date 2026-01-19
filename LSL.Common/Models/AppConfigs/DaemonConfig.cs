using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LSL.Common.Extensions;

namespace LSL.Common.Models.AppConfigs;

public record DaemonConfig : IConfig<DaemonConfig>
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
        
    }

    public static ServiceResult<DaemonConfig> Deserialize(JsonElement configRoot)
    {
        var result = new DaemonConfig();
        List<string> errors = [];
        Action<string> onError = error => errors.Add(error);
        configRoot.ParseBoolProperty(nameof(WebPanelAutoStart), b => result.WebPanelAutoStart = b, onError);
        configRoot.ParseUIntProperty(nameof(DownloadThreads), b => result.DownloadThreads = b, onError);
        configRoot.ParseULongProperty(nameof(DownloadLimitKBytes), b => result.DownloadLimitKBytes = b, onError);
        configRoot.ParseBoolProperty(nameof(EndServerOnClose), b => result.EndServerOnClose = b, onError);
        configRoot.ParseBoolProperty(nameof(AllowPanelShutdownDaemon), b => result.AllowPanelShutdownDaemon = b, onError);
        configRoot.ParseBoolProperty(nameof(AllowPanelEditDaemonConfig), b => result.AllowPanelEditDaemonConfig = b, onError);
        return errors.Count != 0
            ? ServiceResult.Warning(result, new StringBuilder().AppendJoin('\n', errors).ToString())
            : ServiceResult.Success(result);
    }
}