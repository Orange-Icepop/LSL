using LSL.Common.Results;
using Mutty;

namespace LSL.Common.Models.AppConfig;

[MutableGeneration]
public record DaemonConfig : AppConfigBase<DaemonConfig>, IConfig<DaemonConfig>
{
    public bool WebPanelAutoStart { get; init; } = false;
    public uint DownloadThreads { get; init; } = 4;
    public ulong DownloadLimitKBytes { get; init; } = 0;
    public bool EndServerOnClose { get; init; } = true;
    public bool AllowPanelShutdownDaemon { get; init; } = false;
    public bool AllowPanelEditDaemonConfig { get; init; } = false;

    public override Result Validate()
    {
        if (DownloadThreads > 64) return Result.Fail("DownloadThreads must be at most 64");
        return Result.Success();
    }

    public override Result<DaemonConfig> ValidateAndFix()
    {
        if (DownloadThreads > 64)
        {
            return Result.Warning(this with { DownloadThreads = 4 }, "DownloadThreads must be at most 64");
        }

        return Result.Success(this);
    }
}