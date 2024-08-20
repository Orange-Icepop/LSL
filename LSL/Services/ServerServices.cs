﻿using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LSL.Services
{
    public interface IServerHost
    {
        Task StartServer(string serverId);
        Task SendCommand(string serverId, string command);
        void EndServer(string serverId);
        void EndAllServers();
    }


    public class ServerHost : IServerHost
    {
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
            finally
            {
                _lock.ExitReadLock();
            }
        }
        #endregion

        #region 启动服务器StartServer(string serverId)
        public async Task StartServer(string serverId)
        {
            string serverPath = (string)JsonHelper.ReadJson(ConfigManager.ServerConfigPath, serverId);
            string configPath = Path.Combine(serverPath, "lslconfig.json");
            string corePath = Path.Combine(serverPath, (string)JsonHelper.ReadJson(configPath, "$.core_name"));
            string javaPath = (string)JsonHelper.ReadJson(configPath, "$.using_java");
            string MinMem = JsonHelper.ReadJson(configPath, "$.min_memory").ToString();
            string MaxMem = JsonHelper.ReadJson(configPath, "$.max_memory").ToString();
            string arguments = $"-server -XX:+UseG1GC -Xms{MinMem}M -Xmx{MaxMem}M -jar {corePath} nogui";

            ProcessStartInfo startInfo = new()// 提供服务器信息
            {
                FileName = javaPath,
                Arguments = arguments,
                WorkingDirectory = serverPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            // 启动服务器
            using (Process process = Process.Start(startInfo))
            {
                LoadServer(serverId, process);
                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) =>
                {
                    // 移除进程的实例
                    UnloadServer(serverId);
                };
                // 读取Java程序的输出  
                using (StreamReader reader = process.StandardOutput)
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        EventBus.Instance.Publish(new TerminalOutputArgs { ServerId = serverId, Output = line });
                    }
                }
            }
        }
        #endregion

        #region 发送命令SendCommand(string serverId, string command)
        public async Task SendCommand(string serverId, string command)
        {
            Process server = GetServer(serverId);
            if (server != null && !server.HasExited)
            {
                using (StreamWriter writer = server.StandardInput)
                {
                    await writer.WriteLineAsync(command);
                    await writer.FlushAsync();
                }
            }
        }
        #endregion

        #region 强制结束服务器进程EndServer(string serverId)
        public void EndServer(string serverId)
        {
            Process serverProcess = GetServer(serverId);
            if (serverProcess != null && !serverProcess.HasExited)
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
    }
}