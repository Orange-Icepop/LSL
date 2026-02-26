using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentResults;
using FluentResults.Extensions;
using LSL.Common.DTOs;
using LSL.Common.Models.Minecraft;
using LSL.Common.Models.ServerConfig;

namespace LSL.Models.Server;

/// <summary>
///     The process instance of an LSL hosted Minecraft server.
/// </summary>
public partial class ServerProcess : IDisposable
{
    private readonly Process _process;
    public int Id { get; }
    public readonly long AllocatedMemoryBytes;
    private readonly CompositeDisposable _subscription; // 用于管理内部订阅
    private readonly BehaviorSubject<ServerStatusArgs> _statusSubject;

    public IObservable<string> OutputStream { get; }
    public IObservable<string> ErrorStream { get; }
    public IObservable<int> Exited { get; }
    public IObservable<ProcessMetrics> MetricsStream { get; }
    public IObservable<ServerStatusArgs> StatusStream { get; }

    private ServerProcess(int id, Process process, long allocatedMemoryBytes)
    {
        _process = process;
        Id = id;
        AllocatedMemoryBytes = allocatedMemoryBytes;

        // 将事件转换为 Observable
        var output = Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>(
                h => _process.OutputDataReceived += h,
                h => _process.OutputDataReceived -= h)
            .Select(x => x.EventArgs.Data)
            .Where(data => data != null)
            .Select(s => s!)
            .Finally(() => _process.CancelOutputRead());

        var error = Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>(
                h => _process.ErrorDataReceived += h,
                h => _process.ErrorDataReceived -= h)
            .Select(x => x.EventArgs.Data)
            .Where(data => data != null)
            .Select(s => s!)
            .Finally(() => _process.CancelErrorRead());

        var exit = Observable.FromEventPattern<EventHandler, EventArgs>(
                h => _process.Exited += h,
                h => _process.Exited -= h)
            .Select(_ => _process.ExitCode)
            .Take(1)
            .Finally(() => _process.Dispose());

        // 输出和错误流在进程退出时自动完成
        OutputStream = output.TakeUntil(exit).Publish().RefCount();
        ErrorStream = error.TakeUntil(exit).Publish().RefCount();
        Exited = exit.Publish().RefCount();

        // 性能监控：每秒采样，直到进程退出
        MetricsStream = Observable.Interval(TimeSpan.FromSeconds(1))
            .TakeUntil(Exited)
            .Select(_ => GetCurrentMetrics())
            .Where(m => m != null)
            .Select(m => m!)
            .Publish()
            .RefCount();

        // 初始化状态
        IsRunning = true;
        IsOnline = false;
        _statusSubject = new BehaviorSubject<ServerStatusArgs>(
            new ServerStatusArgs(Id, IsRunning, IsOnline));
        StatusStream = _statusSubject.AsObservable();

        // 启动异步读取
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // 可选：将内部订阅组合为一个，便于整体释放
        // 订阅输出流和退出事件以更新状态
        var disposables = new CompositeDisposable
        {
            OutputStream.Subscribe(HandleOutput),
            Exited.Subscribe(_ => SetExited())
        };
        _subscription = disposables;
    }

    public bool HasExited => _process.HasExited;

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
                _process.Refresh();
                // 计算CPU使用率
                var currentTime = DateTime.UtcNow;
                var currentCpuTime = _process.TotalProcessorTime;

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
                var processMemory = _process.PrivateMemorySize64;

                // 触发事件（即使进程已退出也通知）
                return new ProcessMetrics(
                    Id,
                    cpuUsage,
                    processMemory,
                    AllocatedMemoryBytes,
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

    private readonly object _statusLock = new();
    public bool IsOnline { get; private set; }
    public bool IsRunning { get; private set; }

    private void SetOnline(bool online)
    {
        lock (_statusLock)
        {
            if (IsOnline == online) return;
            IsOnline = online;
            _statusSubject.OnNext(new ServerStatusArgs(Id, IsRunning, IsOnline));
        }
    }

    private void SetExited()
    {
        lock (_statusLock)
        {
            IsRunning = false;
            IsOnline = false;
            _statusSubject.OnNext(new ServerStatusArgs(Id, IsRunning, IsOnline));
        }
    }

    #endregion

    #region 命令

    public static Task<Result<ServerProcess>> Create(IndexedServerConfig config)
    {
        return config.LocatedConfig.GetStartInfo()
            .Bind(startInfo =>
            {
                var process = Process.Start(startInfo);
                if (process == null || process.HasExited) return Result.Fail<Process>("Failed to start process.");
                process.EnableRaisingEvents = true;
                return Result.Ok(process);
            })
            .Bind(process =>
                Result.Ok(new ServerProcess(config.ServerId, process, (long)config.MaxMemory * 1024 * 1024)));
    }

    public void SendCommand(string command) => _process.StandardInput.WriteLine(command);

    #endregion

    #region 句柄与生命周期

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (!_process.HasExited)
        {
            _process.Kill();
            _process.WaitForExit(5000);
        }

        _subscription?.Dispose();
        _process.Dispose();
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
        if (string.IsNullOrEmpty(output) || s_getPlayerMessage.IsMatch(output))
            return;

        if (s_getDone.IsMatch(output))
            SetOnline(true);
        else if (s_getExit.IsMatch(output))
            SetOnline(false);
    }

    #endregion
}