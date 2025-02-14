using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using ReactiveUI;
using LSL.Services;
using Avalonia.Threading;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        private int _selectedServerIndex;// 当前选中的服务器在列表中的位置，用于绑定到View
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

        #region 服务器控制
        public ICommand StartServerCmd { get; set; }// 启动服务器命令
        public ICommand StopServerCmd { get; set; }// 停止服务器命令
        public ICommand SaveServerCmd { get; set; }// 保存服务器命令
        public ICommand ShutServerCmd { get; set; }// 结束服务器进程命令
        public ICommand SendServerCmd { get; set; }// 发送服务器命令
        private string _serverInputText = "";// 服务器命令输入框文本
        public string ServerInputText // 服务器命令输入框文本访问器
        {
            get => _serverInputText;
            set
            {
                if (value.EndsWith('\n') || value.EndsWith('\r') || value.EndsWith("\r\n"))
                {
                    string sendedValue = value.TrimEnd('\n', '\r');
                    sendedValue = sendedValue.TrimEnd('\r', '\n');
                    Task.Run(() => SendServerCommand(sendedValue));
                    Task.Run(ResetServerInputText);
                }
                this.RaiseAndSetIfChanged(ref _serverInputText, value);
            }
        }
        private void ResetServerInputText() => Dispatcher.UIThread.Post(() => ServerInputText = "");
        /*别问为什么要把重置方法独立出来
        问就是这框架不知道抽了什么风
        在set方法里设置值的操作不会返回到View上*/
        public void StartServer()//启动服务器方法
        {
            var result = VerifyServerConfigBeforeStart(SelectedServerId);
            if (result != null)
            {
                QuickHandler.ThrowError(result);
                return;
            }
            TerminalTexts.TryAdd(SelectedServerId, new StringBuilder());
            NavigateLeftView("ServerLeft");
            NavigateRightView("ServerTerminal");
            Task RunServer = Task.Run(() => ServerHost.Instance.RunServer(SelectedServerId));
            Notify(0, "服务器正在启动", "请稍候等待服务器启动完毕");
        }

        public async Task SendServerCommand(string? message)// 发送服务器命令
        {
            if (!string.IsNullOrEmpty(message)) await Task.Run(() => ServerHost.Instance.SendCommand(SelectedServerId, message));
        }
        #endregion

        #region 启动前校验配置文件
        public static string? VerifyServerConfigBeforeStart(string serverId)
        {
            ServerConfig? config;
            try
            {
                config = ServerConfigManager.ServerConfigs[serverId];
            }
            catch
            {
                return "LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。";
            }
            if (config == null) return "LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。";
            else if (!File.Exists(config.using_java)) return "LSL无法启动选定的服务器，因为配置文件中指定的Java路径不存在。";
            else
            {
                string configPath = Path.Combine(config.server_path, config.core_name);
                if (!File.Exists(configPath)) return "LSL无法启动选定的服务器，因为配置文件中指定的核心文件不存在。";
            }
            return null;
        }
        #endregion

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
        }
        public void ReceiveStdOutPut(TerminalOutputArgs args)// 接收标准输出
        {
            AddTerminalText(args.ServerId, args.Output);
        }
        #endregion

        #region 服务器玩家列表与消息列表
        private ConcurrentDictionary<string, Dictionary<string, string>> ServerPlayers = new();// 服务器玩家列表
        public class UUID_Player
        {
            public string UUID { get; set; }
            public string Player { get; set; }
            public UUID_Player(string uuid, string player)
            {
                UUID = uuid;
                Player = player;
            }
        }
        private ConcurrentDictionary<string, StringBuilder> ServerMessages = new();// 服务器消息列表

        private void ReceivePlayerUpdate(PlayerUpdateArgs args)// 接收服务器玩家列表
        {
            if (args.Entering == true)
            {
                ServerPlayers.AddOrUpdate(args.ServerId,
                    new Dictionary<string, string> { { args.PlayerName, args.UUID } },
                    (key, existing) =>
                    {
                        existing.Add(args.UUID, args.PlayerName);
                        return existing; // 返回更新后的 ObservableCollection 实例
                    });
            }
            else
            {
                ServerPlayers.AddOrUpdate(args.ServerId,
                    new Dictionary<string, string>(),
                    (key, existing) =>
                    {
                        existing.Remove(args.PlayerName);
                        return existing;
                    });
            }
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
            this.RaisePropertyChanged(nameof(CurrentPlayerList));
            this.RaisePropertyChanged(nameof(CurrentPlayerMessage));
        }
        // 访问器
        public ObservableCollection<UUID_Player> CurrentPlayerList
        {
            get
            {
                var dictValue = ServerPlayers.GetOrAdd(SelectedServerId, new Dictionary<string, string>());
                ObservableCollection<UUID_Player> value = [];
                foreach (var item in dictValue)
                {
                    value.Add(new UUID_Player(item.Value, item.Key));
                }
                return value;
            }
        }
        public string CurrentPlayerMessage
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

    }
}
