using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using ReactiveUI;
using LSL.Services;
using System.Collections.ObjectModel;
using DynamicData.Binding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LSL.Services.Validators;

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
        public string SelectedServerId => ServerIDs[SelectedServerIndex];// 当前选中的服务器ID
        public ICommand StartServerCmd { get; set; }// 启动服务器命令
        public ICommand StopServerCmd { get; set; }// 停止服务器命令
        public ICommand SaveServerCmd { get; set; }// 保存服务器命令
        public ICommand ShutServerCmd { get; set; }// 结束服务器进程命令
        public void StartServer()//启动服务器方法
        {
            var result = VerifyServerConfigBeforeStart(SelectedServerId);
            if (result != null)
            {
                ErrorMessage.ThrowError(result);
            }
            TerminalTexts.TryAdd(SelectedServerId, new StringBuilder());
            NavigateLeftView("ServerLeft");
            NavigateRightView("ServerTerminal");
            Task RunServer = Task.Run(() => ServerHost.Instance.RunServer(SelectedServerId));
        }

        public async void SendServerCommand(string message)// 发送服务器命令
        {
            await Task.Run(() => ServerHost.Instance.SendCommand(SelectedServerId, message));
        }

        #region 启动前校验配置文件
        public static string? VerifyServerConfigBeforeStart(string serverId)
        {
            var config = ServerConfigManager.ServerConfigs[serverId];
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
                ObservableCollection<UUID_Player> value = new();
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

        #region 当前服务器配置文件
        public string CurrentServerName { get => CurrentServerConfig.name; }
        public string CurrentServerPath { get => CurrentServerConfig.server_path; }
        public string CurrentServerJava { get => CurrentServerConfig.using_java; }

        public Dictionary<string, object> CurrentServerProperty = new();// 当前服务器server.properties字典
        public bool ReadProperties()// 读取当前服务器server.properties
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
