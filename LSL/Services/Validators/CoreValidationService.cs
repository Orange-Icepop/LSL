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
            Illegal,
            Unknown,
            Client,
            ForgeInstaller,
            Forge,
            Server
        }
        public static CoreType Validate(string? filePath, out string ErrorMessage)
        {
            ErrorMessage = "";
            if (string.IsNullOrEmpty(filePath)) return CoreType.Illegal;
            if (!File.Exists(filePath)) return CoreType.Illegal;
            string? JarMainClass = GetMainClass(filePath);
            if (JarMainClass == null) return CoreType.Unknown;
            else if (JarMainClass.StartsWith("Access denied") || JarMainClass.StartsWith("Error"))
            {
                ErrorMessage = JarMainClass;
                return CoreType.Error;
            }
            else switch (JarMainClass)
                {
                    case "net.minecraft.server.MinecraftServer":
                        return CoreType.Server;
                    case "net.minecraft.client.Main":
                        return CoreType.Client;
                    case "net.minecraftforge.installer.SimpleInstaller":
                        return CoreType.ForgeInstaller;
                    default:
                        return CoreType.Unknown;
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
                if (manifestEntry != null)
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
