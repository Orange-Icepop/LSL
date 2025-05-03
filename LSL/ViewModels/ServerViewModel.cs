using Avalonia.Media;
using Avalonia.Threading;
using LSL.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSL.ViewModels
{
    public class ServerViewModel : RegionalVMBase
    {
        public ServerViewModel(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
        {
            AppState.WhenAnyValue(AS => AS.SelectedServerId)
                .Select(id => AppState.TerminalTexts.GetOrAdd(id, []))
                .ToPropertyEx(this, x => x.TerminalText);
            AppState.WhenAnyValue(AS => AS.TerminalTexts)
                .Select(CD => CD.TryGetValue(AppState.SelectedServerId, out var value) ? value : new())
                .Where(t => t != TerminalText)
                .ToPropertyEx(this, x => x.TerminalText);
            StartServerCmd = ReactiveCommand.Create(StartSelectedServer);
            StopServerCmd = ReactiveCommand.Create(Connector.StopSelectedServer);
            SaveServerCmd = ReactiveCommand.Create(Connector.SaveSelectedServer);
            EndServerCmd = ReactiveCommand.Create(Connector.EndSelectedServer);
            SendCommand = ReactiveCommand.Create(SendCommandToServer);
        }

        #region 控制
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
                if(endless.Length < value.Length)
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
            AppState.ITAUnits.NotifyITA.Handle(new(0, "服务器正在启动", "请稍候等待服务器启动完毕"));
        }
        public void SendCommandToServer()//发送命令方法
        {
            if (string.IsNullOrEmpty(InputText))
            {
                AppState.ITAUnits.NotifyITA.Handle(new(0, "输入为空", "请输入要发送的命令"));
                return;
            }
            Connector.SendCommandToServer(InputText);
            InputText = "";
        }

        #endregion

        public ObservableCollection<ColoredLines> TerminalText { [ObservableAsProperty] get; }
    }
    public record ColoredLines(string Line, ISolidColorBrush LineColor);
}
