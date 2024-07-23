using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using LSL.ViewModels;
using System;

namespace LSL.Views
{
    public partial class Popup : UserControl
    {
        public Popup()
        {
            InitializeComponent();
            PopupPublisher.Instance.PopupMessageReceived += HandlePopupMessageReceived;// 注册消息接收事件
            CloseIt.Click += CloseIt_Click;
        }

        private int msgtype = 0;
        private void HandlePopupMessageReceived(string type, string message)
        {
            Message.Text = message;
            SolidColorBrush PopupTheme;
            switch (type)
            {
                case "info":
                    PopupTheme = new SolidColorBrush(Colors.Green); 
                    Head.Text = "信息"; 
                    Intro.Text = null;
                    CloseIt.Content = "确定";
                    msgtype = 1;
                    break;
                case "warn":
                    PopupTheme = new SolidColorBrush(Colors.Orange);
                    Head.Text = "警告";
                    Intro.Text = null;
                    CloseIt.Content = "确定";
                    msgtype = 2;
                    break;
                case "error":
                    PopupTheme = new SolidColorBrush(Colors.Red);
                    Head.Text = "错误";
                    Intro.Text = "Lime Server Launcher发生了一个错误。";
                    CloseIt.Content = "确定";
                    msgtype = 3;
                    break;
                case "deadlyError":
                    PopupTheme = new SolidColorBrush(Colors.Red);
                    Head.Text = "致命错误";
                    Intro.Text = "Lime Server Launcher发生了一个致命错误，即将关闭。";
                    CloseIt.Content = "关闭LSL";
                    msgtype = 4;
                    break;
            }
        }

        private void CloseIt_Click(object? sender, RoutedEventArgs e)
        {
            switch (msgtype)
            {
                case 1: case 2: case 3:
                    PopupClosePublisher.Instance.ClosePopup();
                    break;
                case 4: 
                    Environment.Exit(1); 
                    break;
            }
        }
    }
}
