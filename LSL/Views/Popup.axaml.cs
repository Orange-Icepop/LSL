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
            PopupPublisher.Instance.PopupMessageReceived += HandlePopupMessageReceived;// ע����Ϣ�����¼�
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
                    Head.Text = "��Ϣ"; 
                    Intro.Text = null;
                    CloseIt.Content = "ȷ��";
                    msgtype = 1;
                    break;
                case "warn":
                    PopupTheme = new SolidColorBrush(Colors.Orange);
                    Head.Text = "����";
                    Intro.Text = null;
                    CloseIt.Content = "ȷ��";
                    msgtype = 2;
                    break;
                case "error":
                    PopupTheme = new SolidColorBrush(Colors.Red);
                    Head.Text = "����";
                    Intro.Text = "Lime Server Launcher������һ������";
                    CloseIt.Content = "ȷ��";
                    msgtype = 3;
                    break;
                case "deadlyError":
                    PopupTheme = new SolidColorBrush(Colors.Red);
                    Head.Text = "��������";
                    Intro.Text = "Lime Server Launcher������һ���������󣬼����رա�";
                    CloseIt.Content = "�ر�LSL";
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
