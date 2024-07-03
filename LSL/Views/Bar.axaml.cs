using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using LSL.Views.Home;
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
            Home.Click += (sender, e) => ChangeBarColor("HomeLeft");
            Server.Click += (sender, e) => ChangeBarColor("ServerLeft");
            Download.Click += (sender, e) => ChangeBarColor("DownloadLeft");
            Settings.Click += (sender, e) => ChangeBarColor("SettingsLeft");
            Home.Classes.Add("selected");
        }
        //设置Bar按钮样式
        private void ChangeBarColor(string NowPage)
        {
            Debug.WriteLine("Bar Button State Switched:"+NowPage);
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


        }
    }

}
