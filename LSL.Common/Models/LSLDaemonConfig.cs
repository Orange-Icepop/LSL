namespace LSL.Common.Models;

public struct LSLDaemonConfig
{
    public bool WebPanelAutoStart;
    public uint DownloadThreads;
    public ulong DownloadLimit;
    public bool EndServerOnClose;
}