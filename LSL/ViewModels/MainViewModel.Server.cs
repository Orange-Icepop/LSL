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

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        private int _selectedServerIndex;// 当前选中的服务器
        public int SelectedServerIndex
        {
            get => _selectedServerIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedServerIndex, value);
        }

        #region 终端信息
        public ConcurrentDictionary<string, StringBuilder> TerminalTexts = new();// 服务器终端输出
        public string ServerTerminalText// 终端文本
        {
            get
            {
                string serverId = ServerIDs[SelectedServerIndex];
                return TerminalTexts[serverId].ToString();
            }
            set
            {
                string serverId = ServerIDs[SelectedServerIndex];
                TerminalTexts[serverId].Clear();
                TerminalTexts[serverId].AppendLine(value);
                this.RaisePropertyChanged(nameof(ServerTerminalText));
            }
        }

        private readonly Subject<Unit> _scrollTerminal = new();// 触发终端文本滚动到底部
        public IObservable<Unit> ScrollTerminal => _scrollTerminal.AsObservable();// 触发终端文本滚动到底部

        public void AddTerminalText(string serverId, string text)// 添加服务器终端文本
        {
            if (text == null || text == "") return;
            //EventBus.Instance.Publish(new UpdateTerminalArgs { Type = "get" });
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
            //EventBus.Instance.Publish(new UpdateTerminalArgs { Type = "set" });
        }
        public void ReceiveStdOutPut(TerminalOutputArgs e)// 接收标准输出
        {
            AddTerminalText(e.ServerId, e.Output);
        }
        #endregion

        public ICommand StartServerCmd { get; set; }// 启动服务器命令
        public void StartServer()//启动服务器方法
        {
            string serverId = ServerIDs[SelectedServerIndex];
            TerminalTexts.TryAdd(serverId, new StringBuilder());
            NavigateLeftView("ServerLeft");
            NavigateRightView("ServerTerminal");
            ServerHost SH = new();
            Task RunServer = Task.Run(() => SH.RunServer(serverId));
        }
    }
}
