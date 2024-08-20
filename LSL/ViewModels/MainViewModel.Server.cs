using Avalonia;
using LSL.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public Dictionary<string, StringBuilder> TerminalTexts = new();// 服务器终端输出
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
        public void AddTerminalText(string serverId, string text)// 添加服务器终端文本
        {
            TerminalTexts[serverId].AppendLine(text);
            this.RaisePropertyChanged(nameof(ServerTerminalText));
        }
        public void ReceiveStdOutPut(TerminalOutputArgs e)// 接收标准输出
        {
            AddTerminalText(e.ServerId, e.Output);
        }

        public ICommand StartServerCmd { get; set; }// 启动服务器命令

        public void StartServer()//启动服务器方法
        {
            string serverId = ServerIDs[SelectedServerIndex];
            TerminalTexts.Add(serverId, new StringBuilder());
            NavigateLeftView("ServerLeft");
            NavigateRightView("ServerTerminal");
            ServerHost SH = new();
            Task RunServer = Task.Run(() => SH.StartServer(serverId));
        }
    }
}
