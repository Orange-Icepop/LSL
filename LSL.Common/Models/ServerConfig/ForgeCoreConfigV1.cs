namespace LSL.Common.Models.ServerConfig;

public class ForgeCoreConfigV1
{
    public int ConfigVersion => 1;
    public string UnixLibraryArgsPath { get; set; }
    public string WinLibraryArgsPath { get; set; }

    public ForgeCoreConfigV1()
    {
        UnixLibraryArgsPath = string.Empty;
        WinLibraryArgsPath = string.Empty;
    }
    public ForgeCoreConfigV1(string unixLibraryArgs, string winLibraryArgs)
    {
        UnixLibraryArgsPath = unixLibraryArgs;
        WinLibraryArgsPath = winLibraryArgs;
    }
}