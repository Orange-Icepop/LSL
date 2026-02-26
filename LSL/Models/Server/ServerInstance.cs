using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentResults;
using FluentResults.Extensions;
using LSL.Common.DTOs;
using LSL.Common.Models.ServerConfig;
using Microsoft.Extensions.Logging;

namespace LSL.Models.Server;

public partial class ServerInstance : IDisposable
{
    private readonly ServerProcess _process;
    private readonly int _serverId;
    private readonly long _allocatedMemoryBytes;
    private readonly CompositeDisposable _subscriptions = new();
    private readonly ILogger _logger;

    #region 带数据缓存的ReplaySubjects

    private readonly ReplaySubject<ColorOutputArgs> _colorOutput;
    private readonly ReplaySubject<PlayerMessageArgs> _playerMessage;
    private readonly ReplaySubject<PlayerUpdateArgs> _playerUpdate;
    private readonly ReplaySubject<SecondlyMetricsReport> _secondlyMetrics; // 缓存最近60秒
    private readonly ReplaySubject<MinutelyMetricsReport> _minutelyMetrics; // 缓存最近30分钟
    private readonly BehaviorSubject<ServerStatusArgs> _status; // 只缓存最新状态

    #endregion

    private ServerInstance(ServerProcess process, ILogger<ServerInstance> logger)
    {
        _process = process;
        _serverId = process.Id;
        _allocatedMemoryBytes = process.AllocatedMemoryBytes;
        _colorOutput = new ReplaySubject<ColorOutputArgs>(1000);
        _playerMessage = new ReplaySubject<PlayerMessageArgs>(1000);
        _playerUpdate = new ReplaySubject<PlayerUpdateArgs>(1000);
        _secondlyMetrics = new ReplaySubject<SecondlyMetricsReport>(60);
        _minutelyMetrics = new ReplaySubject<MinutelyMetricsReport>(30);
        _status = new BehaviorSubject<ServerStatusArgs>(new ServerStatusArgs(_serverId, true, false));
        _logger = logger;

        // 订阅原始流并处理
        _subscriptions.Add(process.OutputStream.Subscribe(OnOutput, ex => _logger.LogError(ex, "OutputStream error")));
        _subscriptions.Add(process.ErrorStream.Subscribe(OnError, ex => _logger.LogError(ex, "ErrorStream error")));
        _subscriptions.Add(process.MetricsStream.Subscribe(OnMetrics, ex => _logger.LogError(ex, "MetricsStream error")));
        _subscriptions.Add(process.StatusStream.Subscribe(OnStatus, ex => _logger.LogError(ex, "StatusStream error")));
        _subscriptions.Add(process.Exited.Subscribe(OnExited, ex => _logger.LogError(ex, "Exited error")));
        // 推送平均值报告（如果需要峰值，可再推送一个）
        _subscriptions.Add(
            _secondlyMetrics
                .Buffer(TimeSpan.FromMinutes(1))
                .Where(buffer => buffer.Count > 0)
                .Subscribe(buffer =>
                {
                    var cpuAvg = buffer.Average(m => m.CpuUsage);
                    var memAvg = buffer.Average(m => m.MemBytes);
                    var memPercentAvg = memAvg / _allocatedMemoryBytes * 100;
                    
                    _minutelyMetrics.OnNext(new MinutelyMetricsReport(_serverId, cpuAvg, (long)memAvg, memPercentAvg));
                }));

        _colorOutput.OnNext(new ColorOutputArgs(_serverId, "[LSL|Info] Server is launching, please wait......",
            "#019eff"));
        _logger.LogInformation("ServerInstance of server {id} created", _serverId);
    }

    public static Task<Result<ServerInstance>> Create(IndexedServerConfig config, ILogger<ServerInstance> logger)
    {
        return ServerProcess.Create(config)
            .Bind(p => Result.Ok(new ServerInstance(p, logger)));
    }

    #region 代理操作

    public void Stop()
    {
        _logger.LogInformation("Stopping server instance {id}", _serverId);
        _colorOutput.OnNext(new ColorOutputArgs(_serverId, "[LSL|Info] Server is stopping, please wait......",
            "#019eff"));
        SendCommand("stop");
    }

    public void SendCommand(string command) => _process.SendCommand(command);

    #endregion

    #region 处理后的流

    public IObservable<ColorOutputArgs> ColorOutput => _colorOutput.AsObservable();
    public IObservable<PlayerMessageArgs> PlayerMessage => _playerMessage.AsObservable();
    public IObservable<PlayerUpdateArgs> PlayerUpdate => _playerUpdate.AsObservable();
    public IObservable<SecondlyMetricsReport> SecondlyMetrics => _secondlyMetrics.AsObservable();
    public IObservable<MinutelyMetricsReport> MinutelyMetrics => _minutelyMetrics.AsObservable();
    public IObservable<ServerStatusArgs> Status => _status.AsObservable();

    // 统一流
    public IObservable<IServerMessage> AllEvents => Observable.Merge<IServerMessage>(
        _colorOutput,
        _playerMessage,
        _playerUpdate,
        _secondlyMetrics,
        _minutelyMetrics,
        _status
    );

    #endregion

    #region 处理器

    private void OnOutput(string line)
    {
        var colorBrush = "#000000";

        if (s_getTimeStamp.IsMatch(line))
        {
            var match = s_getTimeStamp.Match(line);
            if (s_getPlayerMessage.IsMatch(match.Groups["context"].Value))
            {
                _playerMessage.OnNext(
                    new PlayerMessageArgs(_serverId, match.Groups["player"].Value, match.Groups["context"].Value));
            }
            else
            {
                var type = match.Groups["type"].Value;
                colorBrush = type switch
                {
                    "INFO" => "#019eff", // 还是这个颜色顺眼 (>v<)
                    "WARN" => "#ffc125",
                    "RROR" => "#ff0000",
                    "FATA" => "#ff0000",
                    _ => "#000000"
                };
                ProcessSystem(_serverId, match.Groups["context"].Value);
            }
        }
        else
        {
            colorBrush = "#019eff";
        }

        _colorOutput.OnNext(new ColorOutputArgs(_serverId, line, colorBrush));
    }

    // 额外处理服务端自身输出所需要更新的操作
    private void ProcessSystem(int serverId, string output)
    {
        if (s_getUUID.IsMatch(output))
        {
            var match = s_getUUID.Match(output);
            _playerUpdate.OnNext(new PlayerUpdateArgs(serverId, match.Groups["uuid"].Value,
                match.Groups["player"].Value, true));
        }

        if (s_playerLeft.IsMatch(output))
            _playerUpdate.OnNext(new PlayerUpdateArgs(serverId, "Unknown",
                s_getUUID.Match(output).Groups["player"].Value, false));
    }


    private void OnError(string line)
    {
        // 直接红色输出
        _colorOutput.OnNext(new ColorOutputArgs(_serverId, line, "#ff0000"));
    }

    private void OnStatus(ServerStatusArgs status)
    {
        if (status.IsOnline) _logger.LogInformation("Server with id {id} is online now", _serverId);
        _status.OnNext(status);
    }

    private void OnMetrics(ProcessMetrics metrics)
    {
        _secondlyMetrics.OnNext(new SecondlyMetricsReport(_serverId, metrics.CpuUsagePercent, metrics.MemoryUsageBytes,
            metrics.MemoryUsagePercent));
    }

    private void OnExited(int exitCode)
    {
        // 退出时发送最终状态并完成所有 Subject
        _status.OnNext(new ServerStatusArgs(_serverId, false, false));
        _colorOutput.OnNext(new ColorOutputArgs(_serverId, $"[LSL|Info] Server exited，code {exitCode}", "#ff0000"));
        _colorOutput.OnCompleted();
        _playerMessage.OnCompleted();
        _playerUpdate.OnCompleted();
        _secondlyMetrics.OnCompleted();
        _minutelyMetrics.OnCompleted();
        _status.OnCompleted();
        _logger.LogInformation("Server with id {id} is stopped.", _serverId);
    }

    #endregion

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _subscriptions.Dispose();
        _colorOutput.Dispose();
        _playerMessage.Dispose();
        _playerUpdate.Dispose();
        _secondlyMetrics.Dispose();
        _minutelyMetrics.Dispose();
        _status.Dispose();
    }

    #region 正则

    [GeneratedRegex(@"^\[(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2}).*(?<type>[A-Z]{4})\]\s*:\s*(?<context>.*)")]
    private static partial Regex TimeStampRegex();

    [GeneratedRegex(@"^\<(?<player>.*)\>\s*(?<message>.*)")]
    private static partial Regex MessageRegex();

    [GeneratedRegex(@"^UUID\sof\splayer\s(?<player>.*)\sis\s(?<uuid>[\da-f-]*)")]
    private static partial Regex UUIDRegex();

    [GeneratedRegex(@"(?<player>.*)\sleft\sthe\sgame$")]
    private static partial Regex PlayerLeftRegex();

    private static readonly Regex s_getTimeStamp = TimeStampRegex();
    private static readonly Regex s_getPlayerMessage = MessageRegex();
    private static readonly Regex s_getUUID = UUIDRegex();
    private static readonly Regex s_playerLeft = PlayerLeftRegex();

    #endregion
}