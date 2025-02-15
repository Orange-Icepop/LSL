using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LSL.Services
{
    public interface IServerHost
    {

        Task RunServer(string serverId);
        Task SendCommand(string serverId, string command);
        void EndServer(string serverId);
        void EndAllServers();
    }


    public class ServerHost : IServerHost
    {
        private ServerHost() { }
        private static readonly Lazy<ServerHost> _lazyInstance = new(() => new ServerHost());
        public static ServerHost Instance => _lazyInstance.Value;

        // 注意：接受ServerId作为参数的方法采用的都是注册服务器的顺序，必须先在MainViewModel中将列表项解析为ServerId

        private ConcurrentDictionary<string, ServerProcess> _runningServers = [];// 存储正在运行的服务器实例

        #region 存储服务器进程实例LoadServer(string serverId, Process process)
        public void LoadServer(string serverId, ServerProcess process)
        {
            _runningServers.AddOrUpdate(serverId, process, (key, value) => process);
        }
        #endregion

        #region 移除服务器进程实例UnloadServer(string serverId)
        public void UnloadServer(string serverId)
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

        #region 获取服务器进程实例GetServer(string serverId)
        public ServerProcess? GetServer(string serverId)
        {
            return _runningServers.TryGetValue(serverId, out ServerProcess? process) ? process : null;
        }
        #endregion

        #region 启动服务器RunServer(string serverId)
        public async Task RunServer(string serverId)
        {
            EnsureExited(serverId);
            //if (GetServer(serverId) != null||!GetServer(serverId).HasExited) return;
            ServerConfig config = ServerConfigManager.ServerConfigs[serverId];
            var SP = new ServerProcess(config);
            // 启动服务器
            try
            {
                SP.Start();
            }
            catch (InvalidOperationException)
            {
                EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 服务器启动失败，请检查配置文件。" });
                SP.Dispose();
                return;
            }
            LoadServer(serverId, SP);
            EventBus.Instance.PublishAsync(new ServerStatusArgs { ServerId = serverId, Running = true, Online = false });
            EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 服务器正在启动，请稍后......" });
            SP.OutputReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = e.Data });
                }
            };

            SP.ErrorReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = e.Data });
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
                    EventBus.Instance.PublishAsync(new ServerStatusArgs { ServerId = serverId, Running = false, Online = false });
                    EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 当前服务器" + status });
                    SP.Dispose();
                }
            };
        }
        #endregion

        #region 发送命令SendCommand(string serverId, string command)
        public async Task SendCommand(string serverId, string command)
        {
            ServerProcess? server = GetServer(serverId);
            if (server != null && server.IsRunning)
            {
                if (command == "stop")
                {
                    EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 关闭服务器命令已发出，请等待......" });
                }
                server.SendCommand(command);
            }
            else
            {
                EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 错误]: 服务器未启动，消息无法发送" });
            }
        }
        #endregion

        #region 强制结束服务器进程EndServer(string serverId)
        public void EndServer(string serverId)
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
                process.Dispose();
            }
            _runningServers.Clear();
        }
        #endregion

        #region 确保进程退出命令EnsureExited(string serverId)
        public void EnsureExited(string serverId)
        {
            ServerProcess? server = GetServer(serverId);
            if (server == null) return;
            else
            {
                server.Dispose();
            }
            if (server != null)
            {
                UnloadServer(serverId);
            }
        }
        #endregion

        #region 检查服务器进程是否存在CheckProcess(string serverId)
        public static bool CheckProcess(Process? process)
        {
            try
            {
                if (process == null) return false;
                else if (process.HasExited) return false;
                else return true;
            }
            catch (InvalidOperationException) { return false; }
        }
        #endregion

    }

    #region 服务器进程类ServerProcess
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

        public bool IsRunning => SProcess != null && !SProcess.HasExited;
        public int? ExitCode { get; private set; }
        public void Start()
        {
            if (!IsRunning)
            {
                SProcess = Process.Start(StartInfo);
                if (!IsRunning) throw new InvalidOperationException("Failed to start server process.");
                InStream = SProcess.StandardInput;
                SProcess.EnableRaisingEvents = true;
                OutputReceivedHandler = (sender, args) => OutputReceived?.Invoke(sender, args);
                ErrorReceivedHandler = (sender, args) => ErrorReceived?.Invoke(sender, args);
                ExitedHandler = (sender, args) =>
                {
                    SProcess.CancelOutputRead();
                    SProcess.CancelErrorRead();
                    ExitCode = SProcess.ExitCode;
                    Exited?.Invoke(sender, args);
                    SProcess?.Dispose();
                };
                SProcess.OutputDataReceived += OutputReceivedHandler;
                SProcess.ErrorDataReceived += ErrorReceivedHandler;
                SProcess.Exited += ExitedHandler;
            }
        }
        public void BeginRead()
        {
            SProcess.BeginOutputReadLine();
            SProcess.BeginErrorReadLine();
        }
        public void SendCommand(string command)
        {
            if (IsRunning)
            {
                InStream.WriteLine(command);
                InStream.FlushAsync();
            }
        }
        private void CleanupProcessHandlers()
        {
            if (SProcess != null)
            {
                SProcess.OutputDataReceived -= OutputReceivedHandler;
                SProcess.ErrorDataReceived -= ErrorReceivedHandler;
                SProcess.Exited -= ExitedHandler;
            }
        }
        public void Kill()
        {
            if (SProcess != null)
            {
                try
                {
                    // 停止异步读取
                    SProcess.CancelOutputRead();
                    SProcess.CancelErrorRead();
                    SProcess.Kill();
                    SProcess.WaitForExit(5000);
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
    }
    #endregion

    #region 服务端输出预处理
    public class OutputHandler
    {

        private static readonly Lazy<OutputHandler> _instance = new(() => new OutputHandler());

        public static OutputHandler Instance => _instance.Value;

        private OutputHandler()
        {
            EventBus.Instance.Subscribe<TerminalOutputArgs>(HandleOutput);
        }

        public void HandleOutput(TerminalOutputArgs args)
        {
            Task.Run(() => OutputProcessor(args.ServerId, args.Output));
        }

        private Dictionary<string, string> PlayerPool = [];

        private async void OutputProcessor(string ServerId, string Output)
        {
            if (Output.Substring(1, 3) == "LSL") return;
            bool isMsgWithTime;
            string MsgWithoutTime;
            // 检测消息是否带有时间戳
            if (Output.StartsWith('['))
            {
                isMsgWithTime = true;
                MsgWithoutTime = Output.Substring(Output.IndexOf(']') + 2).Trim();
            }
            else
            {
                isMsgWithTime = false;
                MsgWithoutTime = Output;
            }
            // 检测消息是否为用户消息
            if (isMsgWithTime && MsgWithoutTime.StartsWith('<'))
            {
                EventBus.Instance.PublishAsync(new PlayerMessageArgs { ServerId = ServerId, Message = MsgWithoutTime });
            }
            else
            {
                if (MsgWithoutTime.StartsWith('['))
                {
                    MsgWithoutTime = MsgWithoutTime.Substring(MsgWithoutTime.IndexOf(':') + 1).Trim();
                }
                var MessagePieces = MsgWithoutTime.Split(' ');
                if (MsgWithoutTime.Contains("UUID of player"))
                {
                    string PlayerName = MessagePieces[4];
                    string uuid = MessagePieces[6];
                    EventBus.Instance.PublishAsync(new PlayerUpdateArgs { ServerId = ServerId, UUID = uuid, PlayerName = PlayerName, Entering = true });
                }

                if (MsgWithoutTime.Contains("left the game"))
                {
                    string PlayerName = MessagePieces[0];
                    EventBus.Instance.PublishAsync(new PlayerUpdateArgs { ServerId = ServerId, UUID = "lefting", PlayerName = PlayerName, Entering = false });
                }

                if (MsgWithoutTime.StartsWith("Done"))
                {
                    EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = ServerId, Output = "[LSL 消息]: 服务器启动成功！" });
                    EventBus.Instance.PublishAsync(new ServerStatusArgs { ServerId = ServerId, Running = true, Online = true });
                    ServerHost.Instance.SendCommand(ServerId, "hello");
                    // 现在的情况是这个服务器输入处理方式不知道抽了什么风，第一个发送的命令会被修改，Buffer啥的也都排除掉了，所以这里加了一个hello命令，防止第一个命令被修改影响实际操作
                }

                if (MsgWithoutTime.StartsWith("Stopping the server"))
                {
                    EventBus.Instance.PublishAsync(new ServerStatusArgs { ServerId = ServerId, Running = true, Online = false });
                }
            }
        }
    }
    #endregion
}
