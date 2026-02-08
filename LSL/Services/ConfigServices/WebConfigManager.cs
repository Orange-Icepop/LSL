using System.IO;
using LSL.Common.Models.AppConfig;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ConfigServices;

public class WebConfigManager(ILogger<WebConfigManager> logger)
    : ConfigManagerComponentBase<WebConfigManager, WebConfig>(logger)
{
    protected override string ConfigPath => Path.Combine(ConfigPathProvider.LSLFolder, "LSL.Config.Desktop.toml");
}