using LSL.Services;
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
        public int SelectedServerIndex { get; set; }

        public ICommand StartServerCmd { get; set; }// 启动服务器命令

        public void StartServer()
        {
            string serverId = ServerIDs[SelectedServerIndex];
            GameManager.StartServer(serverId);
        }
    }
}
