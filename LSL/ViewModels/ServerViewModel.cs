using LSL.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSL.ViewModels
{
    public class ServerViewModel : RegionalVMBase
    {
        public ServerViewModel(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
        {
            AppState.WhenAnyValue(AS => AS.CurrentServerConfigs)
                .Select(s => new ObservableCollection<string>(s.Keys))
                .ToPropertyEx(this, x => x.ServerIDs);
            AppState.WhenAnyValue(AS => AS.CurrentServerConfigs)
                .Select(s => new ObservableCollection<string>(s.Values.Select(v => v.name)))
                .ToPropertyEx(this, x => x.ServerNames);
        }
        #region 控制
        private int _selectedServerIndex;// 当前选中的服务器在列表中的位置，用于绑定到View
        public int SelectedServerIndex
        {
            get => _selectedServerIndex;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedServerIndex, value);
                MessageBus.Current.SendMessage(new NavigateCommand { Type = NavigateCommandType.Refresh });
            }
        }
        public ICommand StartServerCmd { get; set; }// 启动服务器命令
        public ICommand StopServerCmd { get; set; }// 停止服务器命令
        public ICommand SaveServerCmd { get; set; }// 保存服务器命令
        public ICommand EndServerCmd { get; set; }// 结束服务器进程命令
        public ICommand SendCommand { get; set; }// 发送服务器命令
        private string _inputText = "";
        public string InputText
        {
            get => _inputText;
            set
            {
                string endless = value.TrimEnd('\r', '\n');
                this.RaiseAndSetIfChanged(ref _inputText, endless.Length < value.Length ? "" : value);
            }
        }
        public void StartServer()//启动服务器方法
        {
            var result = VerifyServerConfigBeforeStart(AppState.SelectedServerId.ToString());
            if (result != null)
            {
                QuickHandler.ThrowError(result);
                return;
            }
            TerminalTexts.TryAdd(SelectedServerId, new StringBuilder());
            MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Server, RightTarget = RightPageState.ServerTerminal });
            Task RunServer = Task.Run(() => ServerHost.Instance.RunServer(SelectedServerId));
            Notify(0, "服务器正在启动", "请稍候等待服务器启动完毕");
        }

        #endregion

        #region 启动前校验配置文件
        public static string? VerifyServerConfigBeforeStart(string serverId)
        {
            if (ServerConfigManager.ServerConfigs.TryGetValue(serverId, out var config)) return "LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。";
            else if (config == null) return "LSL无法启动选定的服务器，因为它不存在能够被读取到的配置文件。";
            else if (!File.Exists(config.using_java)) return "LSL无法启动选定的服务器，因为配置文件中指定的Java路径不存在。";
            else
            {
                string configPath = Path.Combine(config.server_path, config.core_name);
                if (!File.Exists(configPath)) return "LSL无法启动选定的服务器，因为配置文件中指定的核心文件不存在。";
            }
            return null;
        }
        #endregion

        #region 服务器配置
        public ObservableCollection<string> ServerIDs { [ObservableAsProperty] get; }
        public ObservableCollection<string> ServerNames { [ObservableAsProperty] get; }
        #endregion

    }

}
