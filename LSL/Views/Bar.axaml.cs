using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using LSL.Views.Home;
using LSL.ViewModels;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace LSL.Views
{
    public partial class Bar : UserControl
    {
        public Bar()
        {
            InitializeComponent();
            //���ﲻ��Ҫ��ʼ������ĸ�����ť����ΪMainViewModel��ʼ��ʱ�����һ���������
            //�Է���һ�������һ�����õĳ�ʼ������
            //Home.Classes.Add("selected");
            BarChangedPublisher.Instance.MessageReceived += HandleBarChangeReceived;
        }
        private void HandleBarChangeReceived(string navigateTarget)
        {
            ChangeBarColor(navigateTarget);
        }

        //����Bar��ť��ʽ����
        public void ChangeBarColor(string NowPage)
        {
            Home.Classes.Remove("selected");
            Server.Classes.Remove("selected");
            Download.Classes.Remove("selected");
            Settings.Classes.Remove("selected");
            switch (NowPage)
            {
                case "HomeLeft":
                    Home.Classes.Add("selected");
                    break;
                case "ServerLeft":
                    Server.Classes.Add("selected");
                    break;
                case "DownloadLeft":
                    Download.Classes.Add("selected");
                    break;
                case "SettingsLeft":
                    Settings.Classes.Add("selected");
                    break;

            }
            Debug.WriteLine("Bar Color Switched:" + NowPage);

        }
    }

}
