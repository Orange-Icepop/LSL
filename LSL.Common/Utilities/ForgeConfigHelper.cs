using LSL.Common.Models;
using LSL.Common.Models.ServerConfigs;

namespace LSL.Common.Utilities;

public static class ForgeConfigHelper
{
    /// <summary>
    /// Find the library argument of the specified forge server.
    /// </summary>
    /// <param name="serverDir">The path of the forge server.</param>
    /// <returns>Pair (windowsArguments,unixArguments).</returns>
    public static ServiceResult<(string, string)> GetForgeConfig(string? serverDir)
    {
        if (string.IsNullOrWhiteSpace(serverDir) || !Directory.Exists(serverDir))
            return ServiceResult.Fail<(string, string)>("Server directory doesn't exist.");
        var tryArgs = FindLibraryArgsWithScript(Path.Combine(serverDir, "run.bat"), Path.Combine(serverDir, "run.sh"));
        return tryArgs.IsSuccess ? tryArgs : FindLibraryArgsWithContrast(serverDir);
    }

    /// <summary>
    /// Find the library argument of the specified forge server using the startup script.
    /// </summary>
    /// <param name="winScriptPath">The path of the run.bat file.</param>
    /// <param name="unixScriptPath">The path of the run.sh file.</param>
    /// <returns>Pair (windowsArguments,unixArguments).</returns>
    private static ServiceResult<(string, string)> FindLibraryArgsWithScript(string winScriptPath, string unixScriptPath)
    {
        string? winArgs = null, unixArgs = null;
        if (!string.IsNullOrWhiteSpace(winScriptPath) && File.Exists(winScriptPath)) winArgs = FindLibraryArgsPath(
            File.ReadAllLines(winScriptPath));
        if (!string.IsNullOrWhiteSpace(unixScriptPath) && File.Exists(unixScriptPath)) winArgs = FindLibraryArgsPath(
            File.ReadAllLines(unixScriptPath));
        if (winArgs is not null && unixArgs is null) return ServiceResult.Success((winArgs, winArgs.Replace("win_args.txt", "unix_args.txt")));
        if (winArgs is null && unixArgs is not null) return ServiceResult.Success((unixArgs.Replace("unix_args.txt", "win_args.txt"), unixArgs));
        return ServiceResult.Fail<(string,string)>("Cannot find any of the forge libraries through scripts.");
    }

    /// <summary>
    /// Find the library argument of the specified forge server using the so-observed contract of where these arguments are.
    /// </summary>
    /// <param name="serverPath">The path of the forge server.</param>
    /// <returns>Pair (windowsArguments,unixArguments).</returns>
    private static ServiceResult<(string, string)> FindLibraryArgsWithContrast(string serverPath)
    {
        if (string.IsNullOrWhiteSpace(serverPath) || !File.Exists(serverPath)) return ServiceResult.Fail<(string, string)>("Server directory doesn't exist.");
        var dir = Path.Combine(serverPath, "libraries/net/minecraftforge/forge");
        if (!Directory.Exists(dir)) return ServiceResult.Fail<(string, string)>("Server forge directory doesn't exist.");
        var subDirs = Directory.GetDirectories(dir);
        if (subDirs.Length != 1) return ServiceResult.Fail<(string, string)>("Cannot determine the directory of which library arguments existed");
        var subDir = Path.GetDirectoryName(subDirs[0]);
        return string.IsNullOrWhiteSpace(subDir)
            ? ServiceResult.Fail<(string, string)>("Cannot determine the directory of which library arguments existed")
            : ServiceResult.Success(($"libraries/net/minecraftforge/forge/{subDir}/win_args.txt",
                $"libraries/net/minecraftforge/forge/{subDir}/unix_args.txt"));
    }

    /// <summary>
    /// Fetch out the @libraries parameters in the provided lines of startup scripts.
    /// </summary>
    /// <param name="script">Lines of startup scripts.</param>
    /// <returns>The @libraries part in the script, trimmed @.</returns>
    private static string? FindLibraryArgsPath(string[] script)
    {
        return (from line in script
            where line.StartsWith("java")
            from part in line.Split(' ')
            where part.StartsWith("@libraries")
            select part.TrimStart('@')).FirstOrDefault();
    }
}