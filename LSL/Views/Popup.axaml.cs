using Avalonia.Controls;
using Avalonia.Animation.Easings;
using Avalonia.Interactivity;
using Avalonia.Media;
using LSL.ViewModels;
using System;
using System.Threading.Tasks;

namespace LSL.Views
{
    public partial class Popup : UserControl
    {
        public Popup()
        {
            InitializeComponent();
            this.IsVisible = false;
            this.Opacity = 1;
            PopupPublisher.Instance.PopupMessageReceived += HandlePopupMessageReceived;// 注册消息接收事件
            Confirm.Click += CloseIt_Click;
        }
        private void ShowPopup()
        { 
            this.IsVisible = true;
            this.Opacity = 0;
        }


        private void HandlePopupMessageReceived(string type, string message)
        {
            ShowPopup();
        }

        private async void CloseIt_Click(object? sender, RoutedEventArgs e)
        {
            this.Opacity = 0;
            await Task.Delay(200);
            this.IsVisible = false;
        }
    }
}
