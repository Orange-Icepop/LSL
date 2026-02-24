using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using FluentResults.Extensions;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;
using LSL.Services.ServerServices;

namespace LSL.Models.Server;

/// <summary>
///     The process instance of an LSL hosted Minecraft server.
/// </summary>
public partial class ServerProcess : IDisposable
{
    private readonly long _allocatedMemoryBytes;
    private DataReceivedEventHandler _outputReceivedHandler = null!;
    private DataReceivedEventHandler _errorReceivedHandler = null!;
    private EventHandler _exitedHandler = null!;
    
    private readonly Subject<string> _outputSubject = new();
    private readonly Subject<string> _errorSubject = new();
    private readonly Subject<ServerStatusInfo> _statusSubject = new();
    private readonly Subject<int> _exitedSubject = new();

    // 公开为只读 Observable
    public IObservable<string> OutputStream { get; }
    public IObservable<string> ErrorStream { get; }
    public IObservable<ProcessMetrics> MetricsStream { get; }
    public IObservable<ServerStatusInfo> StatusStream { get; }
    public IObservable<int> Exited { get; }

    private ServerProcess(int id, long allocatedMemoryBytes, ProcessStartInfo startInfo)
    {
        OutputStream = _outputSubject.AsObservable();
        ErrorStream = _errorSubject.AsObservable();
        MetricsStream = Observable.Interval(TimeSpan.FromSeconds(1))
            .TakeUntil(_exitedSubject)
            .Select(_ => GetCurrentMetrics())
            .Where(m => m != null)
            .Select(m => m!)  // 忽略已退出情况
            .Publish()
            .RefCount();
        StatusStream = _statusSubject.AsObservable();
        Exited = _exitedSubject.AsObservable();
        Id = id;
        _allocatedMemoryBytes = allocatedMemoryBytes;
        StartInfo = startInfo;
        InitProcessHandlers();
    }

    public int Id { get; }
    private Process? SProcess { get; set; }
    private ProcessStartInfo StartInfo { get; }
    private StreamWriter? InStream => SProcess is not null && !SProcess.HasExited ? SProcess.StandardInput : null;
    [MemberNotNullWhen(false, nameof(SProcess))]
    public bool HasExited => SProcess?.HasExited ?? true;

    public static Task<Result<ServerProcess>> Create(IndexedServerConfig config)
    {
        return config.LocatedConfig.GetStartInfo().Bind(r =>
            Result.Ok(new ServerProcess(config.ServerId, (long)config.MaxMemory * 1024 * 1024, r)));
    }

    #region 性能监控

    private readonly object _performanceLock = new();
    private TimeSpan _prevCpuTime;
    private DateTime _prevTime;

    private ProcessMetrics? GetCurrentMetrics()
    {
        lock (_performanceLock)
        {
            try
            {
                if (HasExited) return new ProcessMetrics(Id, 0, 0, 0, true);
                
                double cpuUsage = 0;
                SProcess.Refresh();
                // 计算CPU使用率
                var currentTime = DateTime.UtcNow;
                var currentCpuTime = SProcess.TotalProcessorTime;

                var cpuElapsed = (currentCpuTime - _prevCpuTime).TotalSeconds;
                var timeElapsed = (currentTime - _prevTime).TotalSeconds;

                _prevTime = currentTime;
                _prevCpuTime = currentCpuTime;

                if (timeElapsed > 0 && cpuElapsed > 0)
                {
                    cpuUsage = cpuElapsed / timeElapsed * 100;
                    cpuUsage /= Environment.ProcessorCount; // 多核百分比
                }

                // 计算内存使用量
                var processMemory = SProcess.PrivateMemorySize64;

                // 触发事件（即使进程已退出也通知）
                return new ProcessMetrics(
                    Id,
                    cpuUsage,
                    processMemory,
                    _allocatedMemoryBytes,
                    false
                );
            }
            catch (Exception ex)
            {
                // 触发包含错误信息的事件
                return new ProcessMetrics(Id, 0, 0, 0, true, ex.Message);
            }
        }
    }

    #endregion

    # region 状态获取
    public bool IsOnline { get; private set; }
    public bool IsRunning { get; private set; }

    private void UpdateStatus(bool isRunning, bool isOnline)
    {
        IsRunning = isRunning;
        IsOnline = isOnline;
        _statusSubject.OnNext(new ServerStatusInfo(Id, isRunning, isOnline));
    }

    #endregion

    #region 命令

    public void Start()
    {
        if (IsRunning) return;
        SProcess = Process.Start(StartInfo);
        if (SProcess is null || SProcess.HasExited)
            throw new InvalidOperationException("Failed to start server process.");
        SProcess.EnableRaisingEvents = true;
        AttachProcessHandlers();
        UpdateStatus(true, IsOnline);
        SProcess.BeginOutputReadLine();
        SProcess.BeginErrorReadLine();
    }

    public void Stop() => SendCommand("stop");

    public void SendCommand(string command)
    {
        InStream?.WriteLine(command);
        InStream?.Flush();
    }

    #endregion

    #region 句柄与生命周期

    private void InitProcessHandlers()
    {
        _outputReceivedHandler = (_, args) => OnOutputReceived(args.Data);
        _errorReceivedHandler = (_, args) => OnErrorReceived(args.Data);
        _exitedHandler = (_, _) => OnExited();
    }

    private void AttachProcessHandlers()
    {
        if (SProcess is null) return;
        SProcess.OutputDataReceived += _outputReceivedHandler;
        SProcess.ErrorDataReceived += _errorReceivedHandler;
        SProcess.Exited += _exitedHandler;
    }

    private void CleanupProcessHandlers()
    {
        if (SProcess is not null)
        {
            // 停止异步读取
            SProcess.OutputDataReceived -= _outputReceivedHandler;
            SProcess.ErrorDataReceived -= _errorReceivedHandler;
            SProcess.Exited -= _exitedHandler;
            SProcess.CancelOutputRead();
            SProcess.CancelErrorRead();
        }
    }

    private void OnExited()
    {
        UpdateStatus(false, false);
        _exitedSubject.OnNext(SProcess?.ExitCode ?? -1);
    }
    
    public async Task Kill()
    {
        if (SProcess is null) return;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            if (!SProcess.HasExited)
            {
                SProcess.Kill();
                await SProcess.WaitForExitAsync(cts.Token);
            }
        }
        catch (Exception) { }
        finally
        {
            CleanupProcessHandlers();
            SProcess?.Dispose();
            SProcess = null;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        SProcess?.Kill();
        SProcess?.Dispose();
        CleanupProcessHandlers();
        _outputSubject.OnCompleted();
        _errorSubject.OnCompleted();
        _statusSubject.OnCompleted();
        _exitedSubject.OnCompleted();
        _outputSubject.Dispose();
        _errorSubject.Dispose();
        _statusSubject.Dispose();
        _exitedSubject.Dispose();
    }

    #endregion

    #region 输出处理

    [GeneratedRegex(@"^\[.*\]:\s*Done\s*\(")]
    private static partial Regex GetDoneRegex();

    [GeneratedRegex(@"^\[.*\]:\s*Stopping\sthe\sserver")]
    private static partial Regex GetExitRegex();

    [GeneratedRegex(@"^\[.*\]:\s*\<(?<player>.*)\>\s*(?<message>.*)")]
    private static partial Regex GetMessageRegex();

    private static readonly Regex s_getDone = GetDoneRegex();
    private static readonly Regex s_getExit = GetExitRegex();
    private static readonly Regex s_getPlayerMessage = GetMessageRegex();

    private void HandleOutput(string? output)
    {
        if (string.IsNullOrEmpty(output) || s_getPlayerMessage.IsMatch(output)) return;
        if (s_getDone.IsMatch(output)) IsOnline = true;
        else if (s_getExit.IsMatch(output)) IsOnline = false;
    }

    
    private void OnOutputReceived(string? data)
    {
        if (!string.IsNullOrEmpty(data))
        {
            HandleOutput(data);
            _outputSubject.OnNext(data);
        }
    }

    private void OnErrorReceived(string? data)
    {
        if (!string.IsNullOrEmpty(data))
            _errorSubject.OnNext(data);
    }

    #endregion
}