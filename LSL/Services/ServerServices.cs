using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LSL.Services;

namespace LSL.Services
{
    public interface IServerHost
    {

        Task RunServer(string serverId);
        void SendCommand(string serverId, string command);
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

        private Dictionary<string, Process> _runningServers = new();// 存储正在运行的服务器实例

        private readonly ReaderWriterLockSlim _lock = new();// 读写锁

        #region 存储服务器进程实例LoadServer(string serverId, Process process)
        public void LoadServer(string serverId, Process process)
        {
            _lock.EnterWriteLock();
            try
            {
                _runningServers.Add(serverId, process);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        #endregion

        #region 移除服务器进程实例UnloadServer(string serverId)
        public void UnloadServer(string serverId)
        {
            _lock.EnterWriteLock();
            try
            {
                _runningServers.Remove(serverId);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        #endregion

        #region 获取服务器进程实例GetServer(string serverId)
        public Process GetServer(string serverId)
        {
            _lock.EnterReadLock();
            try
            {
                return _runningServers[serverId];
            }
            catch { return null; }
            finally
            {
                _lock.ExitReadLock();
            }
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
                CreateNoWindow = true
            };
            // 启动服务器
            using Process process = Process.Start(startInfo);
            LoadServer(serverId, process);
            EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 服务器正在启动，请稍后......." });
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
                string status = $"已关闭，进程退出码为{process.ExitCode}";
                EventBus.Instance.PublishAsync(new ServerStatusArgs { ServerId = serverId, Status = false });
                EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 当前服务器" + status });
            };

            process.WaitForExit();
        }
        #endregion

        #region 发送命令SendCommand(string serverId, string command)
        public async void SendCommand(string serverId, string command)
        {
            Process server = GetServer(serverId);
            if (CheckProcess(server))
            {
                if (command == "stop")
                {
                    EventBus.Instance.PublishAsync(new TerminalOutputArgs { ServerId = serverId, Output = "[LSL 消息]: 关闭服务器命令已发出，请等待......" });
                }
                using (StreamWriter writer = server.StandardInput)
                {
                    await writer.WriteLineAsync(command);
                    await writer.FlushAsync();
                }
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
            Process serverProcess = GetServer(serverId);
            if (CheckProcess(serverProcess))
            {
                serverProcess.Kill();
                serverProcess.Dispose();
                UnloadServer(serverId);
            }
            else if (serverProcess.HasExited)
            {
                UnloadServer(serverId);
            }
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
            Process server = GetServer(serverId);
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
        public bool CheckProcess(Process process)
        {
            try
            {
                if (process != null && !process.HasExited) return true;
                else return false;
            }
            catch (InvalidOperationException) { return false; }
        }
        #endregion
    }

    public class OutputHandler
    {
        static OutputHandler()
        {
            EventBus.Instance.Subscribe<TerminalOutputArgs>(HandleOutput);
        }

        public static void HandleOutput(TerminalOutputArgs args)
        {
            Task.Run(() => OutputProcessor(args.ServerId, args.Output));
        }

        private static Dictionary<string, string> PlayerPool = new Dictionary<string, string>();

        private static async void OutputProcessor(string ServerId, string Output)
        {
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
                    MsgWithoutTime = Output.Substring(MsgWithoutTime.IndexOf(':') + 2).Trim();
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
