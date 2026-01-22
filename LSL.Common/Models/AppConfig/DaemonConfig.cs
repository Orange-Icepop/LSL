using System.Runtime.Serialization;
using Tomlyn;

namespace LSL.Common.Models.AppConfig;

public class DaemonConfig : IConfig<DaemonConfig>
{
    public bool WebPanelAutoStart { get; set; } = false;
    public uint DownloadThreads { get; set; } = 4;
    public ulong DownloadLimitKBytes { get; set; } = 0;
    public bool EndServerOnClose { get; set; } = true;
    public bool AllowPanelShutdownDaemon { get; set; } = false;
    public bool AllowPanelEditDaemonConfig { get; set; } = false;
    [IgnoreDataMember] public static string ConfigFileName => "DaemonConfig.toml";

    public ServiceResult Validate()
    {
        if (DownloadThreads > 256) return ServiceResult.Fail("DownloadThreads must be at most 256");
        return ServiceResult.Success();
    }

    public static ServiceResult<DaemonConfig> Deserialize(string content)
    {
        if (!Toml.TryToModel<DaemonConfig>(content, out var result, out var error))
            return ServiceResult.Fail<DaemonConfig>($"The daemon config is not parsable:\n{error}");
        var validationResult = result.Validate();
        return validationResult.IsSuccess ? ServiceResult.Success(result) : ServiceResult.Warning(result, validationResult.Error);
    }
}