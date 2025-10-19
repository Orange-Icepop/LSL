using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using LSL.Common.Models;

namespace LSL.Services.ServerServices;

    // 服务器进程类ServerProcess
    public partial class ServerProcess : IDisposable
    {
        public int Id { get; }
        public ServerProcess(ServerConfig config)
        {
            Id = config.ServerId;
            var serverPath = config.ServerPath;
            var corePath = Path.Combine(serverPath, config.CoreName);
            var javaPath = config.UsingJava;
            var minMem = config.MinMemory.ToString();
            var maxMem = config.MaxMemory.ToString();
            var arguments = $"-server -Xms{minMem}M -Xmx{maxMem}M -jar {corePath} nogui";
            _allocatedMemoryBytes = ((long)config.MaxMemory) * 1024 * 1024;
            StartInfo = new ProcessStartInfo()// 提供服务器信息
            {
                FileName = javaPath,
                Arguments = arguments,
                WorkingDirectory = serverPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = null,
                StandardOutputEncoding = null,
                StandardErrorEncoding = null// 神奇的是，把这三个设置为null就能解决乱码问题
            };
        }
        private Process? SProcess { get; set; }
        private readonly long _allocatedMemoryBytes;
        private ProcessStartInfo StartInfo { get; }
        private StreamWriter? InStream => SProcess is not null && !SProcess.HasExited ? SProcess.StandardInput : null;
        public event DataReceivedEventHandler? OutputReceived;
        public event DataReceivedEventHandler? ErrorReceived;
        public event EventHandler? Exited;
        private DataReceivedEventHandler _outputReceivedHandler = null!;
        private DataReceivedEventHandler _errorReceivedHandler = null!;
        private EventHandler _exitedHandler = null!;
        public bool HasExited => SProcess?.HasExited ?? false;
        
        #region 性能监控
        private ProcessMetricsMonitor? _monitor;
        public event EventHandler<ProcessMetricsEventArgs>? MetricsReceived;
        private EventHandler<ProcessMetricsEventArgs> _metricsHandler = null!;
        // Monitor is used in AttachProcessHandlers method.
        #endregion

        # region 状态获取
        public event EventHandler<(bool, bool)>? StatusEventHandler;// IsRunning, IsOnline
        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            private set
            {
                if (_isOnline == value) return;
                _isOnline = value;
                StatusEventHandler?.Invoke(this, (IsRunning, value));
            }
        }
        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning == value) return;
                _isRunning = value;
                StatusEventHandler?.Invoke(this, (value, IsOnline));
            }
        }
        #endregion

        public int? ExitCode { get; private set; }

        #region 命令
        public void Start()
        {
            if (IsRunning) return;
            SProcess = Process.Start(StartInfo);
            if (SProcess is null || SProcess.HasExited) throw new InvalidOperationException("Failed to start server process.");
            SProcess.EnableRaisingEvents = true;
            InitProcessHandlers();
            AttachProcessHandlers();
            IsRunning = true;
        }
        public void BeginRead()
        {
            if (SProcess is not null && !SProcess.HasExited)
            {
                SProcess.BeginOutputReadLine();
                SProcess.BeginErrorReadLine();
            }
        }
        public void Stop()
        {

            InStream?.WriteLine("stop");
            InStream?.FlushAsync();
        }
        public void SendCommand(string command)
        {
            InStream?.WriteLine(command);
            InStream?.FlushAsync();
        }
        #endregion

        #region 句柄与生命周期
        private void InitProcessHandlers()
        {
            _outputReceivedHandler = (sender, args) => OutputReceived?.Invoke(sender, args);
            _outputReceivedHandler += (_, args) => HandleOutput(args.Data);
            _errorReceivedHandler = (sender, args) => ErrorReceived?.Invoke(sender, args);
            _exitedHandler = (sender, args) =>
            {
                IsOnline = false;
                IsRunning = false;
                CleanupProcessHandlers();
                ExitCode = SProcess?.ExitCode;
                Exited?.Invoke(sender, args);
                SProcess?.Dispose();
            };
            _metricsHandler = (sender, args) => MetricsReceived?.Invoke(sender, args);
        }
        private void AttachProcessHandlers()
        {
            if (SProcess is null) return;
            SProcess.OutputDataReceived += _outputReceivedHandler;
            SProcess.ErrorDataReceived += _errorReceivedHandler;
            SProcess.Exited += _exitedHandler;
            _monitor = new ProcessMetricsMonitor(SProcess, Id, _allocatedMemoryBytes);
            _monitor.MetricsUpdated += _metricsHandler;
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
            StatusEventHandler = null;
        }
        public void Kill()
        {
            if (SProcess is not null)
            {
                try
                {
                    if (!SProcess.HasExited)
                    {
                        SProcess.Kill();
                        SProcess.WaitForExit(5000);
                    }
                }
                finally
                {
                    CleanupProcessHandlers();
                    SProcess?.Dispose();
                    SProcess = null;
                }
            }
        }
        public void Dispose()
        {
            _monitor?.Dispose();
            Kill();
            GC.SuppressFinalize(this);
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
        #endregion

    }

