using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using FluentResults;
using LSL.Common.Validation;
using Mutty;
using Tomlyn;

namespace LSL.Common.Models.AppConfig;

[TomlModel]
[MutableGeneration]
public record DaemonConfig : AppConfigBase<DaemonConfig>, IConfig<DaemonConfig>
{
    [IgnoreDataMember] public static readonly DaemonConfig Default = new();
    public bool WebPanelAutoStart { get; init; } = false;
    public uint DownloadThreads { get; init; } = 4;
    public ulong DownloadLimitKBytes { get; init; } = 0;
    public bool EndServerOnClose { get; init; } = true;
    public bool AllowPanelShutdownDaemon { get; init; } = false;
    public bool AllowPanelEditDaemonConfig { get; init; } = false;
    // server setup
    public bool AutoEula { get; init; } = true;
    public List<string> UniversalJvmPrefix { get; init; } = [];

    public override Result Validate()
    {
        List<string> errors = [];
        if (!DownloadThreads.IsInRange<uint>(1,64)) errors.Add("DownloadThreads must be at most 64");
        if (UniversalJvmPrefix.Select(CheckComponents.ExtraJvmArg).Any(i=>!i.Passed)) errors.Add("One or multiple invalid JVM args exist");
        return errors.Count == 0 ? Result.Ok() : Result.Fail(new StringBuilder().AppendJoin('\n', errors).ToString());
    }

    public override Result<DaemonConfig> ValidateAndFix()
    {
        var tmp = this.CreateDraft();
        List<string> errors = [];
        if (!DownloadThreads.IsInRange<uint>(1, 64))
        {
            tmp.DownloadThreads = Default.DownloadThreads;
            errors.Add("DownloadThreads must be at most 64");
        }
        if (UniversalJvmPrefix.Select(CheckComponents.ExtraJvmArg).Any(i=>!i.Passed))
        {
            tmp.UniversalJvmPrefix = Default.UniversalJvmPrefix;
            errors.Add("One or multiple invalid JVM args exist");
        }

        return errors.Count == 0
            ? Result.Ok(this)
            : Result.Ok(tmp.FinishDraft())
                .WithReason(new WarningReason(new StringBuilder().AppendJoin('\n', errors).ToString()));
    }
}

public partial class MutableDaemonConfig : INotifyPropertyChanged;