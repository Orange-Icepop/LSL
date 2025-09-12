namespace LSL.Common.Models;

public struct LSLDaemonReportedConfig
{
    public bool WebPanelAutoStart;
    public uint DownloadThreads;
    public ulong DownloadLimit;
    public bool EndServerOnClose;
}