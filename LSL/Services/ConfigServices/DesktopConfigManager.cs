using System.IO;
using LSL.Common.Models.AppConfig;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;

public class DesktopConfigManager(ILogger<DesktopConfigManager> logger)
    : ConfigManagerComponentBase<DesktopConfigManager, DesktopConfig>(logger)
{
    protected override string ConfigPath => Path.Combine(ConfigPathProvider.BaseDir, "LSL.Config.toml");
}