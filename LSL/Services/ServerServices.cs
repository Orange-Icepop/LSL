using Avalonia.Media;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LSL.Services
{
    public interface IServerHost
    {
        bool RunServer(int serverId);
        void StopServer(int serverId);
        bool SendCommand(int serverId, string command);
        void EndServer(int serverId);
        void EndAllServers();
    }
    public class ServerHost : IServerHost, IDisposable
    {
        // 启动输出处理器
        private readonly OutputHandler outputHandler;
        private readonly ServerOutputStorage outputStorage;
        private ServerHost()
        {
            outputStorage = new();
            outputHandler = new(outputStorage);
            Debug.WriteLine("ServerHost Launched");
        }
        private static readonly Lazy<ServerHost> _lazyInstance = new(() => new ServerHost());
        public static ServerHost Instance => _lazyInstance.Value;

        // 注意：接受ServerId作为参数的方法采用的都是注册服务器的顺序，必须先在ViewModel中将列表项解析为ServerId

        private readonly ConcurrentDictionary<int, ServerProcess> _runningServers = [];// 存储正在运行的服务器实例

        #region 存储服务器进程实例LoadServer(int serverId, Process process)
        private void LoadServer(int serverId, ServerProcess process)
        {
            _runningServers.AddOrUpdate(serverId, process, (key, value) => process);
        }
        #endregion

        #region 移除服务器进程实例UnloadServer(int serverId)
        private void UnloadServer(int serverId)
        {
            if (_runningServers.TryRemove(serverId, out _))
            {
                Debug.WriteLine($"服务器{serverId}已成功卸载");
            }
            else
            {
                Debug.WriteLine($"服务器{serverId}未找到，无法卸载");
            }
        }
        #endregion

        #region 获取服务器进程实例GetServer(int serverId)
        private ServerProcess? GetServer(int serverId)
        {
            return _runningServers.TryGetValue(serverId, out ServerProcess? process) ? process : null;
        }
        #endregion

        #region 启动服务器RunServer(int serverId)
        public bool RunServer(int serverId)
        {
            EnsureExited(serverId);
            //if (GetServer(serverId) != null||!GetServer(serverId).HasExited) return;
            ServerConfig config = ServerConfigManager.ServerConfigs[serverId];
            var SP = new ServerProcess(config);
            SP.StatusEventHandler += async (sender, args) => await EventBus.Instance.PublishAsync(new ServerStatusArgs(serverId, args.Item1, args.Item2));
            // 启动服务器
            try
            {
                SP.Start();
            }
            catch (InvalidOperationException)
            {
                outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 服务器启动失败，请检查配置文件。"));
                SP.Dispose();
                return false;
            }
            LoadServer(serverId, SP);
            outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 服务器正在启动，请稍后......"));
            SP.OutputReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputHandler.TrySendLine(new TerminalOutputArgs(serverId, e.Data));
                }
            };

            SP.ErrorReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputHandler.TrySendLine(new TerminalOutputArgs(serverId, e.Data));
                }
            };
            SP.BeginRead();
            SP.Exited += (sender, e) =>
            {
                // 移除进程的实例
                UnloadServer(serverId);
                string exitCode = "Unknown，因为服务端进程以异常的方式结束了";
                try
                {
                    int SPExitCode = SP.ExitCode ?? -1;
                    exitCode = SPExitCode == -1 ? "Unknown，因为服务端进程以异常的方式结束了" : SPExitCode.ToString();
                }
                finally
                {
                    string status = $"已关闭，进程退出码为{exitCode}";
                    outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 当前服务器" + status));
                    SP.Dispose();
                }
            };
            SP.StatusEventHandler += (sender, e) =>
            {
                if (e.Item2)
                {
                    outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 服务器启动成功!"));
                    SendCommand(serverId, "hello");// 防止某些奇怪的情况导致第一个命令被修改
                }
                // storage.TrySendStatus
            };
            return true;
        }
        #endregion

        #region 关闭服务器StopServer(int serverId)
        public void StopServer(int serverId)
        {
            ServerProcess? server = GetServer(serverId);
            if (server is not null && server.IsRunning)
            {
                server.Stop();
                outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 关闭服务器命令已发出，请等待......"));
            }
            else
            {
                outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 错误]: 服务器未启动，消息无法发送"));
            }
        }
        #endregion

        #region 发送命令SendCommand(int serverId, string command)
        public bool SendCommand(int serverId, string command)
        {
            ServerProcess? server = GetServer(serverId);
            if (server is not null && server.IsRunning)
            {
                if (command == "stop")
                {
                    outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 关闭服务器命令已发出，请等待......"));
                }
                server.SendCommand(command);
                return true;
            }
            else
            {
                outputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 错误]: 服务器未启动，消息无法发送"));
                return false;
            }
        }
        #endregion

        #region 强制结束服务器进程EndServer(int serverId)
        public void EndServer(int serverId)
        {
            ServerProcess? server = GetServer(serverId);
            server?.Kill();
        }
        #endregion

        #region 终止所有服务器进程EndAllServers()
        public void EndAllServers()
        {
            foreach (var process in _runningServers.Values)
            {
                process?.Dispose();
            }
            _runningServers.Clear();
        }
        #endregion

        #region 确保进程退出命令EnsureExited(int serverId)
        private void EnsureExited(int serverId)
        {
            ServerProcess? server = GetServer(serverId);
            server?.Dispose();
            UnloadServer(serverId);
        }
        #endregion

        // 释放资源
        public void Dispose()
        {
            EndAllServers();
            outputHandler.Dispose();
            outputStorage.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    // 服务器进程类ServerProcess
    public class ServerProcess : IDisposable
    {
        public ServerProcess(ServerConfig config)
        {
            string serverPath = config.server_path;
            string corePath = Path.Combine(serverPath, config.core_name);
            string javaPath = config.using_java;
            string MinMem = config.min_memory.ToString();
            string MaxMem = config.max_memory.ToString();
            string arguments = $"-server -Xms{MinMem}M -Xmx{MaxMem}M -jar {corePath} nogui";
            StartInfo = new()// 提供服务器信息
            {
                FileName = javaPath,
                Arguments = arguments,
                WorkingDirectory = serverPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
        }
        private Process? SProcess { get; set; }
        private ProcessStartInfo StartInfo { get; set; }
        private StreamWriter InStream { get; set; }
        public event DataReceivedEventHandler? OutputReceived;
        public event DataReceivedEventHandler? ErrorReceived;
        public event EventHandler? Exited;
        private DataReceivedEventHandler OutputReceivedHandler;
        private DataReceivedEventHandler ErrorReceivedHandler;
        private EventHandler ExitedHandler;

        # region 状态获取
        public EventHandler<(bool, bool)>? StatusEventHandler;// IsRunning, IsOnline
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
        /*
        private async Task MonitorState()
        {
            while (true)
            {
                if(SProcess is null || SProcess.HasExited)
                {
                    IsOnline = false;
                    IsRunning = false;
                }
                await Task.Delay(1000);
            }
        }
        */
        #endregion

        public int? ExitCode { get; private set; }

        #region 命令
        public void Start()
        {
            if (!IsRunning)
            {
                SProcess = Process.Start(StartInfo);
                if (SProcess is null || SProcess.HasExited) throw new InvalidOperationException("Failed to start server process.");
                else
                {
                    InStream = SProcess.StandardInput;
                    SProcess.EnableRaisingEvents = true;
                    OutputReceivedHandler = (sender, args) => OutputReceived?.Invoke(sender, args);
                    OutputReceivedHandler += (sender, args) => HandleOutput(args.Data);
                    ErrorReceivedHandler = (sender, args) => ErrorReceived?.Invoke(sender, args);
                    ExitedHandler = (sender, args) =>
                    {
                        IsOnline = false;
                        IsRunning = false;
                        CleanupProcessHandlers();
                        ExitCode = SProcess.ExitCode;
                        Exited?.Invoke(sender, args);
                        SProcess?.Dispose();
                    };
                    AttachProcessHandlers();
                    IsRunning = true;
                }
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
        private void AttachProcessHandlers()
        {
            if (SProcess is not null)
            {
                SProcess.OutputDataReceived += OutputReceivedHandler;
                SProcess.ErrorDataReceived += ErrorReceivedHandler;
                SProcess.Exited += ExitedHandler;
            }
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
                    if(!SProcess.HasExited)
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
            Kill();
            GC.SuppressFinalize(this);
        }
        #endregion

        #region 输出处理
        private static readonly Regex GetDone = new(@"^\[.*\]\s*Done", RegexOptions.Compiled);
        private static readonly Regex GetExit = new(@"^\[.*\]\s*Stopping\sthe\sserver", RegexOptions.Compiled);
        private static readonly Regex GetPlayerMessage = new(@"^\<(?<player>.*)\>\s*(?<message>.*)", RegexOptions.Compiled);
        private void HandleOutput(string? Output)
        {
            if (!string.IsNullOrEmpty(Output) && !GetPlayerMessage.IsMatch(Output))
            {
                if (GetDone.IsMatch(Output)) IsOnline = true;
                else if (GetExit.IsMatch(Output)) IsOnline = false;
            }
        }
        #endregion

    }

    #region 存储类
    public interface IStorageArgs;
    public record TerminalOutputArgs(int ServerId, string Output);// 终端输出事件
    public record ColorOutputLine(string Line, ISolidColorBrush Color);// 着色输出行
    public record ColorOutputArgs(int ServerId, string Output, ISolidColorBrush ColorBrush) : IStorageArgs;// 彩色终端输出事件
    public record ServerStatusArgs(int ServerId, bool IsRunning, bool IsOnline) : IStorageArgs;// 服务器状态更新事件
    public record PlayerUpdateArgs(int ServerId, string UUID, string PlayerName, bool Entering) : IStorageArgs;// 玩家列表更新事件
    public record PlayerMessageArgs(int ServerId, string Message) : IStorageArgs;// 服务器消息事件
    #endregion

    // 服务端输出预处理
    public class OutputHandler : IDisposable
    {
        private readonly ServerOutputStorage OutputStorage;
        public OutputHandler(ServerOutputStorage outputStorage)
        {
            OutputStorage = outputStorage;
            OutputChannel = Channel.CreateUnbounded<TerminalOutputArgs>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
            Task.Run(() => ProcessOutput(OutputCTS.Token));
            Debug.WriteLine("OutputHandler Launched");
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
                Debug.WriteLine("OutputChannel Writer is full");
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
                    try { await OutputProcessor(args.ServerId, args.Output); }
                    catch { }
                }
            }
            catch (OperationCanceledException) { }
        }
        #endregion

        #region 清理
        private bool _disposed = false;
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

        private static readonly Regex GetTimeStamp = new(@"^\[(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})\s(?<type>[A-Z]{4})\]\s*:\s*(?<context>.*)", RegexOptions.Compiled);
        private static readonly Regex GetPlayerMessage = new(@"^\<(?<player>.*)\>\s*(?<message>.*)", RegexOptions.Compiled);
        private static readonly Regex GetUUID = new(@"^UUID\sof\splayer\s(?<player>.*)\sis\s(?<uuid>[\da-f-]*)", RegexOptions.Compiled);
        private static readonly Regex PlayerLeft = new(@"(?<player>.*)\sleft\sthe\sgame$", RegexOptions.Compiled);

        #region 处理操作
        private async Task OutputProcessor(int ServerId, string Output)
        {
            ISolidColorBrush colorBrush = new SolidColorBrush(Colors.Black);
            string final = Output;
            // 检测消息是否带有时间戳
            if (Output.StartsWith("[LSL")) { }
            else if (GetTimeStamp.IsMatch(Output))
            {
                var match = GetTimeStamp.Match(Output);
                if (GetPlayerMessage.IsMatch(match.Groups["context"].Value))
                {
                    EventBus.Instance.PublishAsync(new PlayerMessageArgs(ServerId, match.Groups["context"].Value));
                }
                else
                {
                    string type = match.Groups["type"].Value;
                    colorBrush = type switch
                    {
                        "INFO" => new SolidColorBrush(Colors.Blue),
                        "WARN" => new SolidColorBrush(Colors.Orange),
                        "ERRO" => new SolidColorBrush(Colors.Red),
                        "FATA" => new SolidColorBrush(Colors.Red),
                        _ => new SolidColorBrush(Colors.Black)
                    };
                    ProcessSystem(ServerId, match.Groups["context"].Value);
                }
            }
            else
            {
                colorBrush = new SolidColorBrush(Colors.Red);
            }
            EventBus.Instance.PublishAsync(new ColorOutputArgs(ServerId, final, colorBrush));
        }
        // 额外处理服务端自身输出所需要更新的操作
        private void ProcessSystem(int ServerId, string Output)
        {
            if (GetUUID.IsMatch(Output))
            {
                var match = GetUUID.Match(Output);
                EventBus.Instance.PublishAsync(new PlayerUpdateArgs(ServerId, match.Groups["uuid"].Value, match.Groups["player"].Value, true));
            }

            if (PlayerLeft.IsMatch(Output))
            {
                EventBus.Instance.PublishAsync(new PlayerUpdateArgs(ServerId, "Unknown", GetUUID.Match(Output).Groups["player"].Value, false));
            }
        }
        #endregion
    }

    // 服务端输出存储
    public class ServerOutputStorage : IDisposable
    {
        public readonly ConcurrentDictionary<int, ObservableCollection<ColorOutputLine>> OutputDict = new();
        public readonly ConcurrentDictionary<int, (bool IsRunning, bool IsOnline)> StatusDict = new();
        public readonly ConcurrentDictionary<(int ServerId, string PlayerName), string> PlayerDict = new();
        private readonly Channel<IStorageArgs> StorageQueue;
        private readonly CancellationTokenSource StorageCTS = new();
        public ServerOutputStorage()
        {
            StorageQueue = Channel.CreateUnbounded<IStorageArgs>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
            Debug.WriteLine("ServerOutputStorage Launched");
        }

        #region 排队处理
        public bool TrySendLine(IStorageArgs args)//TODO:增加泛型处理机制
        {
            if (StorageQueue.Writer.TryWrite(args))
            {
                return true;
            }
            else
            {
                Debug.WriteLine("StorageQueue Writer is full");
                return false;
            }
        }
        public async Task ProcessStorage(CancellationToken ct)
        {
            try
            {
                await foreach (var args in StorageQueue.Reader.ReadAllAsync(ct))
                {
                    try { await StorageProcessor(args); }
                    catch { }
                }
            }
            catch (OperationCanceledException) { }
        }
        public async Task StorageProcessor(IStorageArgs args)
        {
            switch (args)
            {
                case ColorOutputArgs COA:
                    OutputDict.AddOrUpdate(COA.ServerId, new ObservableCollection<ColorOutputLine> { new ColorOutputLine(COA.Output, COA.ColorBrush) }, (key, value) =>
                    {
                        value.Add(new ColorOutputLine(COA.Output, COA.ColorBrush));
                        return value;
                    });
                    break;
                case ServerStatusArgs SSA:
                    StatusDict.AddOrUpdate(SSA.ServerId, (SSA.IsRunning, SSA.IsOnline), (key, value) =>
                    {
                        value.IsRunning = SSA.IsRunning;
                        value.IsOnline = SSA.IsOnline;
                        return value;
                    });
                    break;
                case PlayerUpdateArgs PUA:
                    PlayerDict.AddOrUpdate((PUA.ServerId, PUA.PlayerName), PUA.UUID, (key, value) =>
                    {
                        if (PUA.Entering)
                        {
                            return PUA.UUID;
                        }
                        else
                        {
                            PlayerDict.TryRemove(key, out _);
                            return string.Empty;
                        }
                    });
                    break;
                default:
                    break;
            }
        }
        public void Dispose()
        {
            StorageCTS.Cancel();
            StorageQueue.Writer.TryComplete();
            StorageCTS.Dispose();
            GC.SuppressFinalize(this);
        }
    #endregion

    }
}
