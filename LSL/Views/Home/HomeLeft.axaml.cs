using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LSL.Services;
using LSL.ViewModels;
using LSL.Views.Server;
using System.Reactive;

namespace LSL.Views.Home
{
    public partial class HomeLeft : UserControl
    {

        public HomeLeft()
        {
            InitializeComponent();
            ReadServerList();
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

