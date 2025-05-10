using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Media;
using LSL.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels
{
    public class ServerViewModel : RegionalVMBase
    {
        public ServerViewModel(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
        {
            StartServerCmd = ReactiveCommand.Create(StartSelectedServer);
            StopServerCmd = ReactiveCommand.Create(Connector.StopSelectedServer);
            SaveServerCmd = ReactiveCommand.Create(Connector.SaveSelectedServer);
            EndServerCmd = ReactiveCommand.Create(Connector.EndSelectedServer);
            SendCommand = ReactiveCommand.Create(SendCommandToServer);

            // SelectedServerId的变化触发的属性通知
            var idChanged = AppState.WhenAnyValue(AS => AS.SelectedServerId);
            idChanged.Select(id => AppState.TerminalTexts.GetOrAdd(id, []))
                .ToPropertyEx(this, x => x.TerminalText);
            idChanged.Select(id => AppState.UserDict.GetOrAdd(id, []))
                .ToProperty(this, x => x.Users);
            idChanged.Select(id => AppState.MessageDict.GetOrAdd(id, []))
                .ToPropertyEx(this, x => x.CurrentUserMessage);
            // status更新
            var statusFlow = idChanged.Select(id => AppState.ServerStatuses.GetOrAdd(id, new ServerStatus()));
            statusFlow.Select(x => LBCEnabler(x))
                .ToPropertyEx(this, x => x.LBCEnabled);
            statusFlow.Select(x => LBCDicider(x))
                .ToPropertyEx(this, x => x.LaunchButtonCmd);
            statusFlow.Select(x => x.IsRunning ? "停止服务器" : "启动服务器")
                .ToPropertyEx(this, x => x.LaunchButtonContent);
            statusFlow.ToPropertyEx(this, x => x.CurrentStatus);

            AppState.WhenAnyValue(AS => AS.TerminalTexts)
                .Select(CD => CD.TryGetValue(AppState.SelectedServerId, out var value) ? value : new())
                .Where(t => t != TerminalText)
                .ToPropertyEx(this, x => x.TerminalText);
        }

        #region 控制
        public ICommand StartServerCmd { get; }// 启动服务器命令
        public ICommand StopServerCmd { get; }// 停止服务器命令
        public ICommand SaveServerCmd { get; }// 保存服务器命令
        public ICommand EndServerCmd { get; }// 结束服务器进程命令
        public ICommand SendCommand { get; }// 发送服务器命令
        private string _inputText = "";
        public string InputText
        {
            get => _inputText;
            set
            {
                string endless = value.TrimEnd('\r', '\n');
                if (endless.Length < value.Length)
                {
                    SendCommandToServer();
                    this.RaiseAndSetIfChanged(ref _inputText, "");
                }
                else this.RaiseAndSetIfChanged(ref _inputText, endless);
            }
        }
        public void StartSelectedServer()//启动服务器方法
        {
            MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Server, RightTarget = RightPageState.ServerTerminal });
            Connector.StartSelectedServer();
            AppState.ITAUnits.NotifyITA.Handle(new(0, "服务器正在启动", "请稍候等待服务器启动完毕")).Subscribe();
        }
        public void SendCommandToServer()//发送命令方法
        {
            if (string.IsNullOrEmpty(InputText))
            {
                AppState.ITAUnits.NotifyITA.Handle(new(0, "输入为空", "请输入要发送的命令")).Subscribe();
                return;
            }
            Connector.SendCommandToServer(InputText);
            InputText = "";
        }

        #endregion

        #region 服务器状态及其决定的操作
        public ICommand LaunchButtonCmd { [ObservableAsProperty] get; }// 绑定到快捷操作钮的命令
        private ICommand LBCDicider(ServerStatus param)
        {
            return param.IsRunning ? StopServerCmd : StartServerCmd;
        }
        private bool LBCEnabler(ServerStatus param)
        {
            if (param.IsRunning && !param.IsOnline) return false;
            else return true;
        }
        public bool LBCEnabled { [ObservableAsProperty] get; }
        public string LaunchButtonContent { [ObservableAsProperty] get; }
        public ServerStatus CurrentStatus { [ObservableAsProperty] get; }
        #endregion

        public ObservableCollection<ColoredLines> TerminalText { [ObservableAsProperty] get; }
        public ObservableCollection<UUID_User> Users { [ObservableAsProperty] get; }
        public ObservableCollection<string> CurrentUserMessage { [ObservableAsProperty] get; }
    }
    public record ColoredLines(string Line, ISolidColorBrush LineColor);
}
