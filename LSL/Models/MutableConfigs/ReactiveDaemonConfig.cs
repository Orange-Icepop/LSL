using System.Collections.Generic;
using System.Runtime.Serialization;
using ReactiveUI.Validation.Helpers;

namespace LSL.Models.MutableConfigs;

public class ReactiveDaemonConfig : ReactiveValidationObject
{
    [IgnoreDataMember] public static readonly ReactiveDaemonConfig Default = new();
    public bool WebPanelAutoStart { get; set; } = false;
    public uint DownloadThreads { get; set; } = 4;
    public ulong DownloadLimitKBytes { get; set; } = 0;
    public bool EndServerOnClose { get; set; } = true;
    public bool AllowPanelShutdownDaemon { get; set; } = false;
    public bool AllowPanelEditDaemonConfig { get; set; } = false;
    // server setup
    public bool AutoEula { get; set; } = true;
    public List<string> UniversalJvmPrefix { get; set; } = [];
    
}