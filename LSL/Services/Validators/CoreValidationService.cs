using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.Services.Validators
{
    public class CoreValidationService
    {
        public enum CoreType
        {
            Error,
            Unknown,
            Client,
            ForgeInstaller,
            FabricInstaller,
            Forge,
            Fabric,
            Server
        }
        public static CoreType Validate(string? filePath, out string ErrorMessage)
        {
            ErrorMessage = "";
            if (string.IsNullOrEmpty(filePath))
            {
                ErrorMessage = "选定的路径为空";
                return CoreType.Error;

            }
            if (!File.Exists(filePath))
            {
                ErrorMessage = "选定的文件/路径不存在";
                return CoreType.Error;
            }
            string? JarMainClass = GetMainClass(filePath);
            if (JarMainClass == null) return CoreType.Unknown;
            else if (JarMainClass.StartsWith("Access denied") || JarMainClass.StartsWith("Error"))
            {
                ErrorMessage = JarMainClass;
                return CoreType.Error;
            }
            else
            {
                return JarMainClass switch
                {
                    "net.minecraft.server.MinecraftServer" => CoreType.Server,
                    "net.minecraft.bundler.Main" => CoreType.Server,
                    "net.minecraft.client.Main" => CoreType.Client,
                    "net.minecraftforge.installer.SimpleInstaller" => CoreType.ForgeInstaller,
                    "net.fabricmc.installer.Main" => CoreType.FabricInstaller,
                    "net.fabricmc.installer.ServerLauncher" => CoreType.Fabric,
                    _ => CoreType.Unknown,
                };
            }
        }
        // the following code is taken from https://github.com/Orange-Icepop/JavaMainClassFinder
        public static string? GetMainClass(string jarFilePath)
        {
            try
            {
                using FileStream stream = new FileStream(jarFilePath, FileMode.Open);
                using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);
                ZipArchiveEntry? manifestEntry = archive.Entries.FirstOrDefault(entry => entry.FullName == "META-INF/MANIFEST.MF");
                if (manifestEntry != null)// 对于较新版本的MC，MANIFEST.MF中应当包含Main-Class字段
                {
                    using StreamReader reader = new StreamReader(manifestEntry.Open());
                    string manifestContent = reader.ReadToEnd();
                    return FindMainClassLine(manifestContent);
                }
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine("Access denied: " + ex.Message);
                return "Access denied: " + ex.Message;
            }
            catch (IOException ex)
            {
                Console.WriteLine("IO error: " + ex.Message);
                return "Error reading file: " + ex.Message;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading jar file: " + ex.Message);
                return "Error reading jar file: " + ex.Message;
            }
        }

        public static string? FindMainClassLine(string manifestContent)
        {
            string[] lines = manifestContent.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (line.StartsWith("Main-Class:"))
                {
                    return line.Substring("Main-Class:".Length).Trim();
                }
            }
            return null;
        }
    }
}
