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

        private StringBuilder _serverTerminalText = new();// 服务器终端的文本
        public string ServerTerminalText// 公共字符串
        {
            get => _serverTerminalText.ToString();
            set
            {
                _serverTerminalText.Clear();
                _serverTerminalText.AppendLine(value);
                this.RaisePropertyChanged(nameof(ServerTerminalText));
            }
        }
        public void AddTerminalText(string text)// 添加服务器终端文本
        {
            _serverTerminalText.AppendLine(text);
            this.RaisePropertyChanged(nameof(ServerTerminalText));
        }
        public void ReceiveStdOutPut(TerminalOutputArgs e)
        {
            AddTerminalText(e.Output);
        }

        public ICommand StartServerCmd { get; set; }// 启动服务器命令

        public void StartServer()
        {
            string serverId = ServerIDs[SelectedServerIndex];
            NavigateRightView("ServerTerminal");
            Task RunServer = Task.Run(() => GameManager.StartServer(serverId));
        }
    }
}
