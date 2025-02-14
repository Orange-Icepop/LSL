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
        private static ServerHost _instance;
        public static ServerHost Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServerHost();
                }
                return _instance;
            }

        }

        // 注意：接受ServerId作为参数的方法采用的都是注册服务器的顺序，必须先在MainViewModel中将列表项解析为ServerId

        private ConcurrentDictionary<string, Process> _runningServers = [];// 存储正在运行的服务器实例

        #region 存储服务器进程实例LoadServer(string serverId, Process process)
        public void LoadServer(string serverId, Process process)
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
        public Process? GetServer(string serverId)
        {
            return _runningServers.TryGetValue(serverId, out Process? process) ? process : null;
        }
        #endregion

        #region 启动服务器RunServer(string serverId)
        public async Task RunServer(string serverId)
        {
            EnsureExited(serverId);
            //if (GetServer(serverId) != null||!GetServer(serverId).HasExited) return;
            ServerConfig config = ServerConfigManager.ServerConfigs[serverId];
            string serverPath = config.server_path;
            string configPath = Path.Combine(serverPath, "lslconfig.json");
            string corePath = Path.Combine(serverPath, config.core_name);
            string javaPath = config.using_java;
            string MinMem = config.min_memory.ToString();
            string MaxMem = config.max_memory.ToString();
            string arguments = $"-server -Xms{MinMem}M -Xmx{MaxMem}M -jar {corePath} nogui";

            ProcessStartInfo startInfo = new()// 提供服务器信息
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
            // 启动服务器
            Process? process = Process.Start(startInfo);
            if (process == null)
            {
                EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 服务器启动失败，请检查配置文件。" });
            }
            else
            {
                using (process)
                {
                    LoadServer(serverId, process);
                    EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 服务器正在启动，请稍后......" });
                    process.EnableRaisingEvents = true;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = e.Data });
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = e.Data });
                        }
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.Exited += (sender, e) =>
                    {
                        // 移除进程的实例
                        UnloadServer(serverId);
                        string exitCode = "Unknown，因为服务端进程以异常的方式结束了";
                        try
                        {
                            exitCode = process.ExitCode.ToString();
                        }
                        finally
                        {
                            string status = $"已关闭，进程退出码为{exitCode}";
                            EventBus.Instance.PublishAsync(new ServerStatusArgs { ServerId = serverId, Status = false });
                            EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 当前服务器" + status });
                        }
                    };

                    process.WaitForExit();
                }
            }
        }
        #endregion

        #region 发送命令SendCommand(string serverId, string command)
        public async Task SendCommand(string serverId, string command)
        {
            Process? server = GetServer(serverId);
            if (CheckProcess(server))
            {
                if (command == "stop")
                {
                    EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 关闭服务器命令已发出，请等待......" });
                }
                var writer = server.StandardInput;
                await writer.WriteLineAsync(command);
                await writer.FlushAsync();
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
            Process? serverProcess = GetServer(serverId);
            if (CheckProcess(serverProcess))
            {
                serverProcess.Kill();
                serverProcess.Dispose();
            }
            UnloadServer(serverId);
        }
        #endregion

        #region 终止所有服务器进程EndAllServers()
        public void EndAllServers()
        {
            foreach (var process in _runningServers.Values)
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                    process.Dispose();
                }
            }
            _runningServers.Clear();
        }
        #endregion

        #region 确保进程退出命令EnsureExited(string serverId)
        public void EnsureExited(string serverId)
        {
            Process? server = GetServer(serverId);
            if (server == null) { return; }
            else if (!server.HasExited)
            {
                server.Kill();
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
    public class ServerProcess : IDisposable
    {
        public ServerProcess(ServerConfig config)
        {
            string serverPath = config.server_path;
            string configPath = Path.Combine(serverPath, "lslconfig.json");
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
            SProcess = new Process { StartInfo = StartInfo };
            InStream = SProcess.StandardInput;
            SProcess.OutputDataReceived += (sender, args) => OutputReceived?.Invoke(sender, args);
            SProcess.ErrorDataReceived += (sender, args) => ErrorReceived?.Invoke(sender, args);
            SProcess.Exited += (sender, args) =>
            {
                ExitCode = SProcess.ExitCode;
                Exited?.Invoke(sender, args);
                SProcess.Dispose();
            };
        }
        private Process SProcess { get; set; }
        private ProcessStartInfo StartInfo { get; set; }
        private StreamWriter InStream { get; set; }
        public event DataReceivedEventHandler? OutputReceived;
        public event DataReceivedEventHandler? ErrorReceived;
        public event EventHandler? Exited;
        public bool IsRunning => !SProcess.HasExited;
        public int? ExitCode { get; private set; }
        public void Run()
        {
            if (!IsRunning)
            {
                SProcess.Start();
                if (!IsRunning) throw new InvalidOperationException("Failed to start server process.");
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
        public void ForceStop()
        {
            if (IsRunning)
            {
                try
                {
                    SProcess.Kill();
                }
                finally
                {
                    SProcess.Dispose();
                }
            }
        }
        public void Dispose()
        {
            ForceStop();
            GC.SuppressFinalize(this);
        }
    }

    public class OutputHandler// 服务端输出预处理
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
                    EventBus.Instance.PublishAsync(new ServerStatusArgs { ServerId = ServerId, Status = true });
                }

                if (MsgWithoutTime.StartsWith("Stopping the server"))
                {
                    EventBus.Instance.PublishAsync(new ServerStatusArgs { ServerId = ServerId, Status = false });
                }
            }
        }
    }
}
