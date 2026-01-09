using System.Text.Json;
using LSL.Common.Utilities.Json;

namespace LSL.Common.Models.ServerConfigs;

public class ForgeConfigV1
{
    public int ConfigVersion { get; } = 1;
    public string UnixLibraryPath { get; set; } = string.Empty;
    public string WinLibraryPath { get; set; } = string.Empty;


    public static ServiceResult<ForgeConfigV1> Deserialize(JsonElement configRoot)
    {
        var result = new ForgeConfigV1();
        bool hasUnixLibrary = true;
        bool hasWinLibrary = true;
        JsonPropertyValidationHelper.FileHandler(onSuccess: s => result.UnixLibraryPath = s)
            .Invoke(configRoot, "unixLibraryPath", _ => hasUnixLibrary = false);
        JsonPropertyValidationHelper.FileHandler(onSuccess: s => result.WinLibraryPath = s)
            .Invoke(configRoot, "winLibraryPath", _ => hasWinLibrary = false);
        if (!hasUnixLibrary && hasWinLibrary)
        {
            result.UnixLibraryPath = result.WinLibraryPath.Replace("win_args.txt", "unix_args.txt");
            hasUnixLibrary = false;
        }

        if (!hasWinLibrary && hasUnixLibrary)
        {
            result.WinLibraryPath = result.UnixLibraryPath.Replace("unix_args.txt", "win_args.txt");
            hasWinLibrary = false;
        }

        if (hasWinLibrary && hasUnixLibrary) return ServiceResult.Success(result);
        return ServiceResult.Fail<ForgeConfigV1>("Neither win_args.txt nor unix_args.txt is set");
    }
}