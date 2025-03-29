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
        private Channel<ColorOutputArgs> ServerOutputChannel = Channel.CreateUnbounded<ColorOutputArgs>();
        private CancellationTokenSource OutputCts = new();
        private void OnServerOutputLine(ColorOutputArgs args)
        {
            ServerOutputChannel.Writer.TryWrite(args);
        }
        private async Task HandleOutput(CancellationToken token)
        {
            await foreach (var args in ServerOutputChannel.Reader.ReadAllAsync(token))
            {
                await Dispatcher.UIThread.InvokeAsync(() => TerminalTexts[AppState.SelectedServerId].Add(new ColoredLines(args.Output, args.Color)));
            }
        }
        public ServerViewModel(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
        {
            AppState.WhenAnyValue(AS => AS.SelectedServerId)
                .Select(id => TerminalTexts.GetOrAdd(id, []))
                .ToPropertyEx(this, x => x.TerminalText);
            this.WhenAnyValue(t => t.TerminalTexts)
                .Select(t => t.TryGetValue(AppState.SelectedServerId, out var value) ? value : new())
                .Where(t => t != TerminalText)
                .ToPropertyEx(this, x => x.TerminalText);
            EventBus.Instance.Subscribe<ColorOutputArgs>(OnServerOutputLine);
            Task.Run(() => HandleOutput(OutputCts.Token));
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
                this.RaiseAndSetIfChanged(ref _inputText, endless.Length < value.Length ? "" : value);
            }
        }
        public void StartServer()//启动服务器方法
        {
            var result = VerifyServerConfigBeforeStart(AppState.SelectedServerId);
            if (result != null)
            {
                QuickHandler.ThrowError(result);
                return;
            }
            TerminalTexts.TryAdd(AppState.SelectedServerId, new());
            MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Server, RightTarget = RightPageState.ServerTerminal });
            Task RunServer = Task.Run(() => ServerHost.Instance.RunServer(AppState.SelectedServerId));
            //Notify(0, "服务器正在启动", "请稍候等待服务器启动完毕");
        }

        #endregion

        #region 启动前校验配置文件
        public static string? VerifyServerConfigBeforeStart(int serverId)
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


        private ConcurrentDictionary<int, ObservableCollection<ColoredLines>> TerminalTexts = new();
        public ObservableCollection<ColoredLines> TerminalText { [ObservableAsProperty] get; }
    }
    public class ColoredLines
    {
        public string Line { get; set; }
        public ISolidColorBrush LineColor { get; set; }
        public ColoredLines(string line, ISolidColorBrush lineColor)
        {
            Line = line;
            LineColor = lineColor;
        }
    }
}
