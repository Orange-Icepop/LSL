using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media;
using Avalonia.Threading;
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
            StopServerCmd = ReactiveCommand.Create(async () => await Connector.StopServer(AppState.SelectedServerId));
            SaveServerCmd = ReactiveCommand.Create(()=>Connector.SaveServer(AppState.SelectedServerId));
            EndServerCmd = ReactiveCommand.Create(async () => await Connector.EndServer(AppState.SelectedServerId));
            SendCommand = ReactiveCommand.Create(()=>
            {
                SendCommandToServer();
                InputText = "";
            });

            // SelectedServerId的变化触发的属性通知
            var idChanged = AppState.WhenAnyValue(AS => AS.SelectedServerId);
            idChanged.Select(id => AppState.TerminalTexts.GetOrAdd(id, []))
                .ToPropertyEx(this, x => x.TerminalText);
            idChanged.Select(id => new FlatTreeDataGridSource<UUID_User>(AppState.UserDict.GetOrAdd(id, []))
            {
                Columns =
                {
                    new TextColumn<UUID_User, string>("用户名", x => x.User),
                    new TextColumn<UUID_User, string>("UUID", x => x.UUID),
                }
            })
                .ToProperty(this, x => x.CurrentUsers);
            idChanged.Select(id => AppState.MessageDict.GetOrAdd(id, []))
                .ToPropertyEx(this, x => x.CurrentUserMessage);
            // status更新
            var statusFlow = idChanged.Select(id => AppState.ServerStatuses.GetOrAdd(id, new ServerStatus()));
            statusFlow.ToPropertyEx(this, x => x.CurrentStatus);
            // status连带更新
            var statusChanges = this.WhenAnyValue(x => x.CurrentStatus)
                .Where(status => status is not null)
                .SelectMany(status => status.WhenAnyValue(
                    s => s.IsRunning,
                    s => s.IsOnline,
                    (running, online) => (running, online))
                )
                .Publish()
                .RefCount();
            // 处理按钮状态
            statusChanges.Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(LBCEnabled));
                this.RaisePropertyChanged(nameof(LaunchButtonCmd));
                this.RaisePropertyChanged(nameof(LaunchButtonContent));
            });
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
                    _inputText = endless;
                    SendCommandToServer();
                    Dispatcher.UIThread.Post(() => this.RaiseAndSetIfChanged(ref _inputText, ""));// 避免编译时优化将赋值无效化
                }
                else 
                {
                    this.RaiseAndSetIfChanged(ref _inputText, endless); 
                }
            }
        }
        public void StartSelectedServer()//启动服务器方法
        {
            MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Server, RightTarget = RightPageState.ServerTerminal });
            Connector.StartServer(AppState.SelectedServerId);
            AppState.ITAUnits.NotifyITA.Handle(new NotifyArgs(0, "服务器正在启动", "请稍候等待服务器启动完毕")).Subscribe();
        }
        public void SendCommandToServer()//发送命令方法
        {
            if (string.IsNullOrEmpty(InputText))
            {
                AppState.ITAUnits.NotifyITA.Handle(new NotifyArgs(0, "输入为空", "请输入要发送的命令")).Subscribe();
                return;
            }
            Connector.SendCommandToServer(AppState.SelectedServerId, InputText);
        }

        #endregion

        #region 服务器状态及其决定的操作
        public bool LBCEnabled => CurrentStatus is not null && CurrentStatus is not { IsRunning: true, IsOnline: false };
        public ICommand LaunchButtonCmd => CurrentStatus?.IsRunning == true ? StopServerCmd : StartServerCmd;
        public string LaunchButtonContent => CurrentStatus?.IsRunning == true ? "停止服务器" : "启动服务器";
        public ServerStatus CurrentStatus { [ObservableAsProperty] get; }
        #endregion

        public ObservableCollection<ColoredLines> TerminalText { [ObservableAsProperty] get; }
        public FlatTreeDataGridSource<UUID_User> CurrentUsers { [ObservableAsProperty] get; }
        public ObservableCollection<UserMessageLine> CurrentUserMessage { [ObservableAsProperty] get; }
    }
    public class UserMessageLine(string msg)
    {
        public string Message { get; set; } = msg;
    }
    public class ColoredLines : ReactiveObject
    {
        [Reactive] string Line { get; init; }
        [Reactive] ISolidColorBrush LineColor { get; init; }
        public ColoredLines(string line, ISolidColorBrush lineColor)
        {
            Line = line;
            LineColor = lineColor;
        }
        public ColoredLines(string line, string ColorHex)
        {
            Line = line;
            LineColor = new SolidColorBrush(Color.Parse(ColorHex));
        }
    }
}
