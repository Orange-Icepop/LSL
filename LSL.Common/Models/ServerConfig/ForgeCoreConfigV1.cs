using Mutty;

namespace LSL.Common.Models.ServerConfig;

[MutableGeneration]
public record ForgeCoreConfigV1
{
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

    public int ConfigVersion => 1;
    public string UnixLibraryArgsPath { get; init; }
    public string WinLibraryArgsPath { get; init; }
}