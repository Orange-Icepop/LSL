using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using LSL.Services;
using LSL.Views.Server;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        private ServerHost SH = new();
        private int _selectedServerIndex;// 当前选中的服务器
        public int SelectedServerIndex
        {
            get => _selectedServerIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedServerIndex, value);
                RefreshRightView();
                ReadProperties();
            }
        }
        public string SelectedServerId => ServerIDs[SelectedServerIndex];
        public ICommand StartServerCmd { get; set; }// 启动服务器命令
        public ICommand StopServerCmd { get; set; }// 停止服务器命令
        public ICommand SaveServerCmd { get; set; }// 保存服务器命令
        public ICommand ShutServerCmd { get; set; }// 结束服务器进程命令
        public void StartServer()//启动服务器方法
        {
            //string serverId = ServerIDs[SelectedServerIndex];
            TerminalTexts.TryAdd(SelectedServerId, new StringBuilder());
            NavigateLeftView("ServerLeft");
            NavigateRightView("ServerTerminal");
            Task RunServer = Task.Run(() => SH.RunServer(SelectedServerId));
        }
        public async void SendServerCommand(string message)// 发送服务器命令
        {
            await Task.Run(() => SH.SendCommand(SelectedServerId, message));
        }

        #region 终端信息
        private ConcurrentDictionary<string, StringBuilder> TerminalTexts = new();// 服务器终端输出
        public string ServerTerminalText// 终端文本访问器
        {
            get
            {
                return TerminalTexts.GetOrAdd(SelectedServerId, new StringBuilder()).ToString();
            }
            set
            {
                TerminalTexts[SelectedServerId].Clear();
                TerminalTexts[SelectedServerId].AppendLine(value);
                this.RaisePropertyChanged(nameof(ServerTerminalText));
            }
        }

        private readonly Subject<Unit> _scrollTerminal = new();// 触发终端文本滚动到底部
        public IObservable<Unit> ScrollTerminal => _scrollTerminal.AsObservable();// 触发终端文本滚动到底部

        public void AddTerminalText(string serverId, string text)// 添加服务器终端文本
        {
            //if (text == null || text == "") return;
            //EventBus.Instance.PublishAsync(new UpdateTerminalArgs { Type = "get" });
            TerminalTexts.AddOrUpdate(serverId,
                new StringBuilder(text),
                (key, existing) =>
                {
                    // AppendLine不是线程安全的！
                    existing.AppendLine(text);
                    return existing; // 返回更新后的 StringBuilder 实例  
                });
            this.RaisePropertyChanged(nameof(ServerTerminalText));
            _scrollTerminal.OnNext(Unit.Default);
            //EventBus.Instance.PublishAsync(new UpdateTerminalArgs { Type = "set" });
        }
        public void ReceiveStdOutPut(TerminalOutputArgs args)// 接收标准输出
        {
            AddTerminalText(args.ServerId, args.Output);
        }
        #endregion

        #region 服务器玩家列表与消息列表
        private ConcurrentDictionary<string, List<string>> ServerPlayers = new();// 服务器玩家列表
        private ConcurrentDictionary<string, StringBuilder> ServerMessages = new();// 服务器消息列表

        private void ReceivePlayerUpdate(PlayerUpdateArgs args)// 接收服务器玩家列表
        {
            ServerPlayers.AddOrUpdate(args.ServerId,
                new List<string> { args.PlayerName },
                (key, existing) =>
                {
                    existing.Add(args.PlayerName + "\r\n");
                    return existing; // 返回更新后的 List 实例
                });
        }

        private void ReceiveMessage(PlayerMessageArgs args)// 接收服务器消息
        {
            ServerMessages.AddOrUpdate(args.ServerId,
                new StringBuilder(args.Message),
                (key, existing) =>
                {
                    existing.AppendLine(args.Message);
                    return existing;
                });
            this.RaisePropertyChanged(nameof(PlayerList));
            this.RaisePropertyChanged(nameof(PlayerMessage));
        }
        // 访问器
        public List<string> PlayerList
        {
            get
            {
                return ServerPlayers.GetOrAdd(SelectedServerId, new List<string>());
            }
        }
        public string PlayerMessage
        {
            get
            {
                return ServerMessages.GetOrAdd(SelectedServerId, new StringBuilder()).ToString();
            }
        }

        #endregion

        #region 服务器状态
        public ConcurrentDictionary<string, bool> ServerStatus = new();// 服务器状态
        public void ReceiveServerStatus(ServerStatusArgs args)// 接收服务器状态
        {
            if (args.Status)
            {
                ServerStatus.AddOrUpdate(args.ServerId, true, (key, existing) => true);
            }
            else
            {
                ServerStatus.AddOrUpdate(args.ServerId, false, (key, existing) => false);
            }
            this.RaisePropertyChanged(nameof(EnableOperation));
        }
        // 访问器
        public bool ServerStatusValue(string serverId)
        {
            return ServerStatus.GetOrAdd(serverId, false);
        }

        public bool EnableOperation
        {
            get => ServerStatusValue(SelectedServerId);
        }

        #endregion

        #region 当前服务器配置文件字典
        public Dictionary<string, object> CurrentServerProperty = new();// 当前服务器server.properties字典
        public bool ReadProperties()
        {
            try
            {
                var text = File.ReadAllLines(Path.Combine((string)JsonHelper.ReadJson(ConfigManager.ConfigFilePath, SelectedServerId), "server.properties"));
                CurrentServerProperty.Clear();
                foreach (var line in text)
                {
                    if (line.StartsWith("#")) continue;
                    var keyValue = line.Split('=');
                    if (keyValue.Length == 2)
                    {
                        CurrentServerProperty.Add(keyValue[0], keyValue[1]);
                    }
                }
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }
        #endregion
    }
}
