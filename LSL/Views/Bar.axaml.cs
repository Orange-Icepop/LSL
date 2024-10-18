using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using LSL.Views.Home;
using LSL.ViewModels;
using System;
using System.Diagnostics;
using System.Windows.Input;
using LSL.Services;

namespace LSL.Views
{
    public partial class Bar : UserControl
    {
        public Bar()
        {
            InitializeComponent();
            //这里不需要初始化最初的高亮按钮，因为MainViewModel初始化时会调用一次这个方法
            //以防万一，这里放一个备用的初始化方法
            //Home.Classes.Add("selected");
            EventBus.Instance.Subscribe<BarChangedEventArgs>(BarChangeHandler);
        }

        private void BarChangeHandler(BarChangedEventArgs args)
        {
            ChangeBarColor(args.NavigateTarget);
        }

        //设置Bar按钮样式方法
        private void ChangeBarColor(string NowPage)
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
