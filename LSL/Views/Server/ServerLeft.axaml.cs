using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Windows.Input;
using System.Diagnostics;
using LSL.ViewModels;
using LSL.Services;
using LSL.Views.Download;

namespace LSL.Views.Server
{
    public partial class ServerLeft : UserControl
    {
        public ServerLeft()
        {
            InitializeComponent();
            EventBus.Instance.Subscribe<LeftChangedEventArgs>(LeftChangeHandler);
            ReadServerList();
        }
        private void LeftChangeHandler(LeftChangedEventArgs args)
        {
            ChangeLeftColor(args.LeftTarget);
        }

        //设置Left按钮样式方法
        private void ChangeLeftColor(string NowPage)
        {
            GeneralButton.Classes.Remove("selLeft");
            StatusButton.Classes.Remove("selLeft");
            TerminalButton.Classes.Remove("selLeft");
            ConfButton.Classes.Remove("selLeft");
            switch (NowPage)
            {
                case "ServerGeneral":
                    GeneralButton.Classes.Add("selLeft");
                    break;
                case "ServerStat":
                    StatusButton.Classes.Add("selLeft");
                    break;
                case "ServerTerminal":
                    TerminalButton.Classes.Add("selLeft");
                    break;
                case "ServerConf":
                    ConfButton.Classes.Add("selLeft");
                    break;
            }
            Debug.WriteLine("Left Color Switched:" + NowPage);
        }
        #region 服务器列表读取（从配置文件读取）
        public void ReadServerList()
        {
            int count = 0;
            ServerList.Items.Clear();
            try
            {
                while (true)
                {
                    string KeyPath = $"$.{count}.name";
                    ServerList.Items.Add(JsonHelper.ReadJson(ConfigManager.ServerConfigPath, KeyPath));
                    count++;
                }
            }
            catch { }
            ServerList.SelectedIndex = 0;
        }
        #endregion
    }
}
