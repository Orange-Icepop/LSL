using LSL.Common.Models;

namespace LSL.Common.Utilities;

public static class ForgeConfigHelper
{
    /// <summary>
    /// Find the library argument of the specified forge server.
    /// </summary>
    /// <param name="serverDir">The path of the forge server.</param>
    /// <returns>Pair (unixArguments,windowsArguments).</returns>
    public static async Task<ServiceResult<(string unixArguments, string windowsArguments)>> GetForgeConfig(string? serverDir)
    {
        if (string.IsNullOrWhiteSpace(serverDir) || !Directory.Exists(serverDir))
            return ServiceResult.Fail<(string, string)>(new DirectoryNotFoundException("Server directory doesn't exist."));
        var tryArgs = await FindLibraryArgsWithScript(Path.Combine(serverDir, "run.bat"), Path.Combine(serverDir, "run.sh"));
        return tryArgs.IsSuccess ? tryArgs : FindLibraryArgsWithContrast(serverDir);
    }

    /// <summary>
    /// Find the library argument of the specified forge server using the startup script.
    /// </summary>
    /// <param name="winScriptPath">The path of the run.bat file.</param>
    /// <param name="unixScriptPath">The path of the run.sh file.</param>
    /// <returns>Pair (unixArguments,windowsArguments).</returns>
    private static async Task<ServiceResult<(string unixArguments, string windowsArguments)>> FindLibraryArgsWithScript(string unixScriptPath, string winScriptPath)
    {
        string? winArgs = null, unixArgs = null;
        if (!string.IsNullOrWhiteSpace(unixScriptPath) && File.Exists(unixScriptPath)) winArgs = FindLibraryArgsPath(
            await File.ReadAllLinesAsync(unixScriptPath));
        if (!string.IsNullOrWhiteSpace(winScriptPath) && File.Exists(winScriptPath)) winArgs = FindLibraryArgsPath(
            await File.ReadAllLinesAsync(winScriptPath));
        if (unixArgs is null && winArgs is not null) return ServiceResult.Success((winArgs.Replace("win_args.txt", "unix_args.txt"), winArgs));
        if (unixArgs is not null && winArgs is null) return ServiceResult.Success((unixArgs, unixArgs.Replace("unix_args.txt", "win_args.txt")));
        return ServiceResult.Fail<(string,string)>("Cannot find any of the forge libraries through scripts.");
    }

    /// <summary>
    /// Find the library argument of the specified forge server using the so-observed contract of where these arguments are.
    /// </summary>
    /// <param name="serverPath">The path of the forge server.</param>
    /// <returns>Pair (unixArguments,windowsArguments).</returns>
    private static ServiceResult<(string unixArguments, string windowsArguments)> FindLibraryArgsWithContrast(string serverPath)
    {
        if (string.IsNullOrWhiteSpace(serverPath) || !File.Exists(serverPath)) return ServiceResult.Fail<(string, string)>(
            new DirectoryNotFoundException("Server directory doesn't exist."));
        var dir = Path.Combine(serverPath, "libraries/net/minecraftforge/forge");
        if (!Directory.Exists(dir)) return ServiceResult.Fail<(string, string)>("Server forge directory doesn't exist.");
        var subDirs = Directory.GetDirectories(dir);
        if (subDirs.Length != 1) return ServiceResult.Fail<(string, string)>("Cannot determine the directory of which library arguments existed");
        var subDir = Path.GetDirectoryName(subDirs[0]);
        return string.IsNullOrWhiteSpace(subDir)
            ? ServiceResult.Fail<(string, string)>("Cannot determine the directory of which library arguments existed")
            : ServiceResult.Success(($"libraries/net/minecraftforge/forge/{subDir}/unix_args.txt",
                $"libraries/net/minecraftforge/forge/{subDir}/win_args.txt"));
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