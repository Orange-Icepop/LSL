using System.Text.Json;
using LSL.Common.Extensions;
using LSL.Common.Utilities.Json;

namespace LSL.Common.Models.ServerConfigs;

public class ForgeConfigV1
{
    public int ConfigVersion { get; } = 1;
    public string UnixLibraryArgsPath { get; set; } = string.Empty;
    public string WinLibraryArgsPath { get; set; } = string.Empty;


    public static ServiceResult<ForgeConfigV1> Deserialize(JsonElement configRoot)
    {
        var result = new ForgeConfigV1();
        bool hasUnixLibrary = true;
        bool hasWinLibrary = true;
        configRoot.ParseFileProperty("unixLibraryPath", s => result.UnixLibraryArgsPath = s,_ => hasUnixLibrary = false);
        configRoot.ParseFileProperty("winLibraryPath", s => result.WinLibraryArgsPath = s, _ => hasWinLibrary = false);
        if (!hasUnixLibrary && hasWinLibrary)
        {
            result.UnixLibraryArgsPath = result.WinLibraryArgsPath.Replace("win_args.txt", "unix_args.txt");
            hasUnixLibrary = false;
        }

        if (!hasWinLibrary && hasUnixLibrary)
        {
            result.WinLibraryArgsPath = result.UnixLibraryArgsPath.Replace("unix_args.txt", "win_args.txt");
            hasWinLibrary = false;
        }

        if (hasWinLibrary && hasUnixLibrary) return ServiceResult.Success(result);
        return ServiceResult.Fail<ForgeConfigV1>("Neither win_args.txt nor unix_args.txt is set");
    }

    public static ForgeConfigV1 FromTuple((string unix, string win) tuple) => new()
        { UnixLibraryArgsPath = tuple.unix, WinLibraryArgsPath = tuple.win };
}