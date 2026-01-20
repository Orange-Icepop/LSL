namespace LSL.Common.Models.ServerConfigs;

public class ForgeCoreConfigV1
{
    public int ConfigVersion => 1;
    public string UnixLibraryArgsPath { get; set; } = string.Empty;
    public string WinLibraryArgsPath { get; set; } = string.Empty;
    
    public static ForgeCoreConfigV1 FromTuple((string unix, string win) tuple) => new()
        { UnixLibraryArgsPath = tuple.unix, WinLibraryArgsPath = tuple.win };
}