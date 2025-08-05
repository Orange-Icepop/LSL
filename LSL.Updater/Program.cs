using System.Diagnostics;

namespace LSL.Updater;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("LSL Updater for version 0.9");
        var isValid = ArgumentMatcher.IsValid(args, out var result);
        if (isValid is not null)
        {
            Console.WriteLine(isValid);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
        }

        if (!result.StartReplacement || result.BaseDirectory == Constant.BaseDirectory)
        {
            Console.WriteLine("The update program is at the root of LSL. Moving to temp directory.");
            string finPath;
            try
            {
                var name = File.Exists("LSL.Updater") ? "LSL.Updater" : "LSL.Updater.exe";
                finPath = FileHelper.Move(result.BaseDirectory, result.TempDirectory, name);
                FileHelper.Move(finPath, result.TempDirectory, "LSL.Updater.dll");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while moving the updater to the temp directory:");
                Console.WriteLine(ex);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
                return;
            }
            Console.WriteLine("The updater is copied to the temp directory. Press any key to continue updating.");
            Console.ReadKey();
            Process.Start(finPath,
            [
                $"--BaseDirectory={result.BaseDirectory}",
                $"--TempDirectory={result.TempDirectory}",
                $"--ZipFilePath={result.ZipFilePath}",
                "--StartReplacement=true"
            ]);
            Environment.Exit(0);
            return;
        }

        var currentDir = Path.GetFullPath(result.BaseDirectory);
        Console.WriteLine($"Called base directory: {currentDir}");
        Console.WriteLine($"Program Location directory: {Constant.BaseDirectory}");
        if (currentDir == Constant.BaseDirectory)
        {
            Console.WriteLine("Error:The updater is at the BASE directory! This could cause IOException during Update. Do you still want to update it? Press y to continue.");
            var res = Console.ReadKey();
            if (res.Key != ConsoleKey.Y)
            {
                Environment.Exit(1);
                return;
            }
        }
        Console.WriteLine("Starting to unzip update file into the temp directory.");
        try
        {
            FileHelper.UnZip(result.ZipFilePath, result.TempDirectory);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occured while unzipping file:");
            Console.WriteLine(ex);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
            return;
        }
        Console.WriteLine($"The update program is unzipped into {result.TempDirectory}.");
        Console.WriteLine("Do you want to update now? (y/n)");
        ConsoleKeyInfo key = Console.ReadKey();
        if (key.Key != ConsoleKey.Y)
        {
            Console.WriteLine("Exiting...");
            Environment.Exit(1);
            return;
        }
        Console.WriteLine("Start overwriting file. Do not close the window!");
        try
        {
            FileHelper.UpdateFile(result.BaseDirectory, Path.Combine(result.TempDirectory, "LSL"));
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occured while updating file:");
            Console.WriteLine(ex);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
            return;
        }
        Console.WriteLine("Update complete!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        Environment.Exit(0);
    }
}

internal static class Constant
{
    internal static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}