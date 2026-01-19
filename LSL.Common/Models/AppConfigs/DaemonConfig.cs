using System.Text.Json;
using System.Text.Json.Serialization;

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
    
    public 

    public static ServiceResult<DaemonConfig> Deserialize(JsonElement configRoot)
    {
        
    }
}