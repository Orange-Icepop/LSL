namespace LSL.Models;

public enum RightPageState
{
    HomeRight,

    //Server
    ServerGeneral,
    ServerStat,
    ServerTerminal,
    ServerConf,

    //Downloads
    AutoDown,
    ManualDown,
    AddServer,
    ModDown,

    //Settings
    CommonSettings,
    DownloadSettings,
    PanelSettings,
    StyleSettings,
    About,

    //FullScreen
    ServerConfEdit,
    AddCore,
    AddFolder,

    //Others
    Empty,
    Undefined,
    Hold
}