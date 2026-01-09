using System.Text.Json;
using LSL.Common.Utilities.Json;

namespace LSL.Common.Models.ServerConfigs;

public class ForgeConfigV1
{
    public int ConfigVersion { get; } = 1;
    public string UnixLibraryPath { get; set; } = string.Empty;
    public string WinLibraryPath { get; set; } = string.Empty;
    

    public static bool Deserialize(JsonElement configRoot, out ForgeConfigV1 config)
    {
        var result = new ForgeConfigV1();
        bool hasUnixLibrary = true;
        bool hasWinLibrary = true;
        JsonPropertyValidationHelper.FileHandler(onSuccess: s => result.UnixLibraryPath = s)
            .Invoke(configRoot, "unixLibraryPath", _ => hasUnixLibrary = false);
        JsonPropertyValidationHelper.FileHandler(onSuccess: s => result.WinLibraryPath = s)
            .Invoke(configRoot, "winLibraryPath", _ => hasWinLibrary = false);
        config = result;
        if (!hasUnixLibrary && hasWinLibrary)
        {
            result.UnixLibraryPath = result.WinLibraryPath.Replace("win_args", "unix_args");
            hasUnixLibrary = false;
        }

        if (!hasWinLibrary && hasUnixLibrary)
        {
            result.WinLibraryPath = result.UnixLibraryPath.Replace("unix_args", "win_args");
            hasWinLibrary = false;
        }

        return hasWinLibrary && hasUnixLibrary;
    }
}