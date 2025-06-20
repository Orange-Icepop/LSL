using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LSL.Common.Contracts;
using Microsoft.Extensions.Logging;

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
        private ServerOutputHandler OutputHandler { get; }
        private ServerOutputStorage OutputStorage { get; }
        private ServerConfigManager serverConfigManager { get; }
        private ILogger<ServerHost> _logger { get; }
        public ServerHost(ServerOutputHandler outputHandler, ServerOutputStorage outputStorage, ServerConfigManager scm, ILogger<ServerHost> logger)
        {
            OutputStorage = outputStorage;
            OutputHandler = outputHandler;
            serverConfigManager = scm;
            _logger = logger;
            _logger.LogInformation("ServerHost Launched");
        }
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
                _logger.LogInformation("Server with id {id} unloaded successfully", serverId);
            }
            else
            {
                _logger.LogError("Server with id {id} not found", serverId);
            }
        }
        #endregion

        #region 获取服务器进程实例GetServer(int serverId)
        private ServerProcess? GetServer(int serverId)
        {
            return _runningServers.TryGetValue(serverId, out var process) ? process : null;
        }
        #endregion

        #region 启动服务器RunServer(int serverId)
        public bool RunServer(int serverId)
        {
            if (GetServer(serverId) is not null)
            {
                OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 服务器已经在运行中。"));
                return false;
            }
            ServerConfig config = serverConfigManager.ServerConfigs[serverId];
            var SP = new ServerProcess(config);
            SP.StatusEventHandler += (sender, args) => EventBus.Instance.PublishAsync<IStorageArgs>(new ServerStatusArgs(serverId, args.Item1, args.Item2));
            // 启动服务器
            try
            {
                SP.Start();
            }
            catch (InvalidOperationException)
            {
                OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 服务器启动失败，请检查配置文件。"));
                SP.Dispose();
                return false;
            }
            LoadServer(serverId, SP);
            OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 服务器正在启动，请稍后......"));
            SP.OutputReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, e.Data));
                }
            };

            SP.ErrorReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, e.Data));
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
                    OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 当前服务器" + status));
                    SP.Dispose();
                }
            };
            SP.StatusEventHandler += (sender, e) =>
            {
                if (e.Item2)
                {
                    OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 服务器启动成功!"));
                }
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
                OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 关闭服务器命令已发出，请等待......"));
            }
            else
            {
                OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 错误]: 服务器未启动，消息无法发送"));
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
                    OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 消息]: 关闭服务器命令已发出，请等待......"));
                }
                server.SendCommand(command);
                return true;
            }
            else
            {
                OutputHandler.TrySendLine(new TerminalOutputArgs(serverId, "[LSL 错误]: 服务器未启动，消息无法发送"));
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
            _logger.LogInformation("Ended all servers.");
        }
        #endregion

        #region 确保进程退出命令EnsureExited(int serverId)
        private void EnsureExited(int serverId)
        {
            var server = GetServer(serverId);
            server?.Dispose();
            UnloadServer(serverId);
        }
        #endregion

        // 释放资源
        public void Dispose()
        {
            EndAllServers();
            GC.SuppressFinalize(this);
        }
    }

    public record TerminalOutputArgs(int ServerId, string Output);// 终端输出事件
    public record ColorOutputLine(string Line, string ColorHex);// 着色输出行

    // 服务端输出预处理
    public partial class ServerOutputHandler : IDisposable
    {
        private ILogger<ServerOutputHandler> _logger { get; }
        public ServerOutputHandler(ILogger<ServerOutputHandler> logger)
        {
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
        private async Task OutputProcessor(int ServerId, string Output)
        {
            string colorBrush = "#000000";
            string final = Output;
            // 检测消息是否带有时间戳
            if (Output.StartsWith("[LSL"))
            {
                colorBrush = "#019eff";
            }
            else if (GetTimeStamp.IsMatch(Output))
            {
                var match = GetTimeStamp.Match(Output);
                if (GetPlayerMessage.IsMatch(match.Groups["context"].Value))
                {
                    EventBus.Instance.PublishAsync<IStorageArgs>(new PlayerMessageArgs(ServerId, match.Groups["context"].Value));
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
                colorBrush = "#ff0000";
            }
            EventBus.Instance.PublishAsync<IStorageArgs>(new ColorOutputArgs(ServerId, final, colorBrush));
        }
        // 额外处理服务端自身输出所需要更新的操作
        private static void ProcessSystem(int ServerId, string Output)
        {
            if (GetUUID.IsMatch(Output))
            {
                var match = GetUUID.Match(Output);
                EventBus.Instance.PublishAsync<IStorageArgs>(new PlayerUpdateArgs(ServerId, match.Groups["uuid"].Value, match.Groups["player"].Value, true));
            }

            if (PlayerLeft.IsMatch(Output))
            {
                EventBus.Instance.PublishAsync<IStorageArgs>(new PlayerUpdateArgs(ServerId, "Unknown", GetUUID.Match(Output).Groups["player"].Value, false));
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
        public readonly ConcurrentDictionary<int, ObservableCollection<string>> MessageDict = new();
        private readonly Channel<IStorageArgs> StorageQueue;
        private readonly CancellationTokenSource StorageCTS = new();
        private ILogger<ServerOutputStorage> _logger { get; }
        public ServerOutputStorage(ILogger<ServerOutputStorage> logger)
        {
            _logger = logger;
            StorageQueue = Channel.CreateUnbounded<IStorageArgs>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
            Task.Run(() => ProcessStorage(StorageCTS.Token));
            EventBus.Instance.Subscribe<IStorageArgs>(arg=>TrySendLine(arg));
            _logger.LogInformation("ServerOutputStorage Launched");
        }

        #region 排队处理
        private bool TrySendLine(IStorageArgs args)
        {
            if (StorageQueue.Writer.TryWrite(args))
            {
                return true;
            }
            else
            {
                _logger.LogError("StorageQueue Writer is full");
                return false;
            }
        }
        private async Task ProcessStorage(CancellationToken ct)
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
        private async Task StorageProcessor(IStorageArgs args)
        {
            switch (args)
            {
                case ColorOutputArgs COA:
                    OutputDict.AddOrUpdate(COA.ServerId, [new ColorOutputLine(COA.Output, COA.ColorHex)], (key, value) =>
                    {
                        value.Add(new ColorOutputLine(COA.Output, COA.ColorHex));
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
                case PlayerMessageArgs PMA:
                    MessageDict.AddOrUpdate(PMA.ServerId, _ => [PMA.Message], (key, value) =>
                    {
                        value.Add(PMA.Message);
                        return value;
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
