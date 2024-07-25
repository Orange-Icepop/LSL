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
            LeftChangedPublisher.Instance.LeftMessageReceived += HandleLeftChangeReceived;
            ReadServerList();
        }
        private void HandleLeftChangeReceived(string navigateTarget)
        {
            ChangeLeftColor(navigateTarget);
        }

        //����Left��ť��ʽ����
        private void ChangeLeftColor(string NowPage)
        {
            StatusButton.Classes.Remove("selLeft");
            TerminalButton.Classes.Remove("selLeft");
            ConfButton.Classes.Remove("selLeft");
            switch (NowPage)
            {
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
        #region �������б��ȡ���������ļ���ȡ��
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
