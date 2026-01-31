using System.Diagnostics;

namespace LSL.Common.Utilities.Minecraft;

public static class ForgeInstaller
{
    public static async Task<Result> InstallForge(string installerPath, string javaPath, IProgress<string>? progress = null)
    {
        if (!File.Exists(installerPath)) return Result.Fail(new FileNotFoundException($"The file {installerPath} was not found."));
        if (!File.Exists(javaPath)) return Result.Fail(new FileNotFoundException($"The file {javaPath} was not found."));
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = javaPath,
                Arguments = $"-jar \"{installerPath}\" installServer",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
            };
            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data is not null) progress?.Report(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data is not null) progress?.Report(e.Data);
            };
            if (!process.Start()) return Result.Fail($"Unable to start the java runner to install forge.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                progress?.Report("Failed to install forge server");
                return Result.Fail($"Failed to install forge server: {process.ExitCode}");
            }

            progress?.Report("Forge installation complete");
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Fail(e);
        }
    }
}