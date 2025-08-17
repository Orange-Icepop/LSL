using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LSL.Common.Contracts;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ServerServices;

/// <summary>
/// A class used to preprocess the server output from ServerHost into different events for sorted storage and handlers.
/// </summary>
public partial class ServerOutputHandler : IDisposable
{
    private ILogger<ServerOutputHandler> _logger { get; }
    public ServerOutputHandler(ILogger<ServerOutputHandler> logger)
    {
        _disposed = false;
        _logger = logger;
        OutputChannel = Channel.CreateUnbounded<TerminalOutputArgs>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
        Task.Run(() => ProcessOutput(OutputCTS.Token));
        _logger.LogInformation("OutputHandler Launched");
    }

    #region 待处理队列
    public bool TrySendLine(TerminalOutputArgs args)
    {
        if (OutputChannel.Writer.TryWrite(args))
        {
            return true;
        }
        else
        {
            _logger.LogError("OutputChannel Writer is full");
            return false;
        }
    }
    private readonly Channel<TerminalOutputArgs> OutputChannel;
    private readonly CancellationTokenSource OutputCTS = new();
    private async Task ProcessOutput(CancellationToken ct)
    {
        try
        {
            await foreach (var args in OutputChannel.Reader.ReadAllAsync(ct))
            {
                try
                {
                    await OutputProcessor(args.ServerId, args.Output, args.ChannelType);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "An error occured while processing output.");
                }
            }
        }
        catch (OperationCanceledException) { }
    }
    #endregion

    #region 清理

    private bool _disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                OutputCTS.Cancel();
                OutputChannel.Writer.TryComplete();
                OutputCTS.Dispose();
            }
            _disposed = true;
        }
    }
    #endregion

    [GeneratedRegex(@"^\[(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2}).*(?<type>[A-Z]{4})\]\s*:\s*(?<context>.*)")] 
    private static partial Regex TimeStampRegex();
    [GeneratedRegex(@"^\<(?<player>.*)\>\s*(?<message>.*)")]
    private static partial Regex MessageRegex();
    [GeneratedRegex(@"^UUID\sof\splayer\s(?<player>.*)\sis\s(?<uuid>[\da-f-]*)")]
    private static partial Regex UUIDRegex();
    [GeneratedRegex(@"(?<player>.*)\sleft\sthe\sgame$")]
    private static partial Regex PlayerLeftRegex();

        
    private static readonly Regex GetTimeStamp = TimeStampRegex();
    private static readonly Regex GetPlayerMessage = MessageRegex();
    private static readonly Regex GetUUID = UUIDRegex();
    private static readonly Regex PlayerLeft = PlayerLeftRegex();

    #region 处理操作
    private static async Task OutputProcessor(int ServerId, string Output, OutputChannelType channel)
    {
        string colorBrush = "#000000";
        switch (channel)
        {
            // 检测消息是否带有时间戳
            case OutputChannelType.LSLInfo:
                colorBrush = "#019eff";
                break;
            case OutputChannelType.LSLError:
            case OutputChannelType.StdErr:
                colorBrush = "#ff0000";
                break;
            default:
            {
                if (GetTimeStamp.IsMatch(Output))
                {
                    var match = GetTimeStamp.Match(Output);
                    if (GetPlayerMessage.IsMatch(match.Groups["context"].Value))
                    {
                        await EventBus.Instance.PublishAsync<IStorageArgs>(new PlayerMessageArgs(ServerId, match.Groups["context"].Value));
                    }
                    else
                    {
                        string type = match.Groups["type"].Value;
                        colorBrush = type switch
                        {
                            "INFO" => "#019eff",// 还是这个颜色顺眼 (>v<)
                            "WARN" => "#ffc125",
                            "RROR" => "#ff0000",
                            "FATA" => "#ff0000",
                            _ => "#000000"
                        };
                        ProcessSystem(ServerId, match.Groups["context"].Value);
                    }
                }
                else
                {
                    colorBrush = "#019eff";
                }
                break;
            }
        }
        await EventBus.Instance.PublishAsync<IStorageArgs>(new ColorOutputArgs(ServerId, Output, colorBrush));
    }
    // 额外处理服务端自身输出所需要更新的操作
    private static void ProcessSystem(int ServerId, string Output)
    {
        if (GetUUID.IsMatch(Output))
        {
            var match = GetUUID.Match(Output);
            EventBus.Instance.Fire<IStorageArgs>(new PlayerUpdateArgs(ServerId, match.Groups["uuid"].Value, match.Groups["player"].Value, true));
        }

        if (PlayerLeft.IsMatch(Output))
        {
            EventBus.Instance.Fire<IStorageArgs>(new PlayerUpdateArgs(ServerId, "Unknown", GetUUID.Match(Output).Groups["player"].Value, false));
        }
    }

    #endregion
}
