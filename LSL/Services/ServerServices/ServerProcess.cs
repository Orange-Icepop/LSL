using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using LSL.Common.Contracts;
using LSL.Common.Models;

namespace LSL.Services.ServerServices;

    // 服务器进程类ServerProcess
    public partial class ServerProcess : IDisposable
    {
        public int ID { get; }
        public ServerProcess(ServerConfig config)
        {
            ID = config.server_id;
            string serverPath = config.server_path;
            string corePath = Path.Combine(serverPath, config.core_name);
            string javaPath = config.using_java;
            string MinMem = config.min_memory.ToString();
            string MaxMem = config.max_memory.ToString();
            string arguments = $"-server -Xms{MinMem}M -Xmx{MaxMem}M -jar {corePath} nogui";
            allocatedMemoryBytes = ((long)config.max_memory) * 1024 * 1024;
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
                StandardErrorEncoding = null//神奇的是，把这三个设置为null就能解决乱码问题
            };
        }
        private Process? SProcess { get; set; }
        private readonly long allocatedMemoryBytes;
        private ProcessStartInfo StartInfo { get; set; }
        private StreamWriter InStream { get; set; }
        public event DataReceivedEventHandler? OutputReceived;
        public event DataReceivedEventHandler? ErrorReceived;
        public event EventHandler? Exited;
        private DataReceivedEventHandler OutputReceivedHandler;
        private DataReceivedEventHandler ErrorReceivedHandler;
        private EventHandler ExitedHandler;
        public bool HasExited => SProcess?.HasExited ?? false;
        
        #region 性能监控
        private ProcessMetricsMonitor? _monitor;
        public event EventHandler<ProcessMetricsEventArgs>? MetricsReceived;
        private EventHandler<ProcessMetricsEventArgs> _metricsHandler;
        // Monitor is used in AttachProcessHandlers method.
        #endregion

        # region 状态获取
        public event EventHandler<(bool, bool)>? StatusEventHandler;// IsRunning, IsOnline
        private bool isOnline;
        public bool IsOnline
        {
            get => isOnline;
            private set
            {
                if (isOnline == value) return;
                else
                {
                    isOnline = value;
                    StatusEventHandler?.Invoke(this, (IsRunning, value));
                }
            }
        }
        private bool isRunning;
        public bool IsRunning
        {
            get => isRunning;
            private set
            {
                if (isRunning == value) return;
                else
                {
                    isRunning = value;
                    StatusEventHandler?.Invoke(this, (value, IsOnline));
                }
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
            else
            {
                InStream = SProcess.StandardInput;
                SProcess.EnableRaisingEvents = true;
                InitProcessHandlers();
                AttachProcessHandlers();
                IsRunning = true;
            }
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
            if (SProcess is not null && !SProcess.HasExited)
            {
                SProcess.StandardInput.WriteLine("stop");
                SProcess.StandardInput.Flush();
            }
        }
        public void SendCommand(string command)
        {
            if (IsRunning)
            {
                InStream.WriteLine(command);
                InStream.FlushAsync();
            }
        }
        #endregion

        #region 句柄与生命周期
        private void InitProcessHandlers()
        {
            OutputReceivedHandler = (sender, args) => OutputReceived?.Invoke(sender, args);
            OutputReceivedHandler += (sender, args) => HandleOutput(args.Data);
            ErrorReceivedHandler = (sender, args) => ErrorReceived?.Invoke(sender, args);
            ExitedHandler = (sender, args) =>
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
            SProcess.OutputDataReceived += OutputReceivedHandler;
            SProcess.ErrorDataReceived += ErrorReceivedHandler;
            SProcess.Exited += ExitedHandler;
            _monitor = new ProcessMetricsMonitor(SProcess, ID, allocatedMemoryBytes);
            _monitor.MetricsUpdated += _metricsHandler;
        }
        private void CleanupProcessHandlers()
        {
            if (SProcess is not null)
            {
                // 停止异步读取
                SProcess.OutputDataReceived -= OutputReceivedHandler;
                SProcess.ErrorDataReceived -= ErrorReceivedHandler;
                SProcess.Exited -= ExitedHandler;
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
        private static readonly Regex GetDone = GetDoneRegex();
        private static readonly Regex GetExit = GetExitRegex();
        private static readonly Regex GetPlayerMessage = GetMessageRegex();
        private void HandleOutput(string? Output)
        {
            if (string.IsNullOrEmpty(Output) || GetPlayerMessage.IsMatch(Output)) return;
            if (GetDone.IsMatch(Output)) IsOnline = true;
            else if (GetExit.IsMatch(Output)) IsOnline = false;
        }
        #endregion

    }

