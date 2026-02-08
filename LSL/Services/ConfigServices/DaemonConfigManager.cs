using System.IO;
using LSL.Common.Models.AppConfig;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;

public class DaemonConfigManager(ILogger<DaemonConfigManager> logger)
    : ConfigManagerComponentBase<DaemonConfigManager, DaemonConfig>(logger)
{
    protected override string ConfigPath => Path.Combine(ConfigPathProvider.LSLFolder, "LSL.Config.Daemon.toml");
}