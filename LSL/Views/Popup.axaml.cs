using Avalonia.Controls;
using Avalonia.Animation.Easings;
using Avalonia.Interactivity;
using Avalonia.Media;
using LSL.ViewModels;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using LSL.Services;

namespace LSL.Views
{
    public partial class Popup : UserControl
    {
        public Popup()
        {
            InitializeComponent();
            // 初始化弹窗为不可见
            //this.IsVisible = false;
            this.IsVisible = true;
            this.Opacity = 1;
        }
        private static Popup _instance;

        public static Popup Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Popup();
                }
                return _instance;
            }
        }

        private TaskCompletionSource<string> tcs;// 创建一个TaskCompletionSource，这是最核心的东西，它能够等待用户操作

        // 弹窗显示
        public async Task<string> Show(int type = 0, string title = "嗨嗨嗨！", string message = "我是一个空的弹窗！")
        {
            // 重置按钮
            Confirm.IsVisible = false;
            Cancel.IsVisible = false;
            No.IsVisible = false;
            Yes.IsVisible = false;
            tcs = new TaskCompletionSource<string>();// 重置tcs
            // type定义：0-提示（只有确认），1-警告（是/否/取消），2-警告（是/否），3-错误（确认，复制消息）
            switch (type)
            {
                case 0:
                    Cover.Background = new SolidColorBrush(Color.Parse("#33e0e5"));
                    PopupBorder.Background = new SolidColorBrush(Color.Parse("#33e0e5"));
                    Confirm.IsVisible = true;
                    break;
                case 1:
                    Cover.Background = new SolidColorBrush(Colors.Yellow);
                    PopupBorder.Background = new SolidColorBrush(Colors.Yellow);
                    Yes.IsVisible = true;
                    No.IsVisible = true;
                    Cancel.IsVisible = true;
                    break;
                case 2:
                    Cover.Background = new SolidColorBrush(Colors.Yellow);
                    PopupBorder.Background = new SolidColorBrush(Colors.Yellow);
                    Yes.IsVisible = true;
                    No.IsVisible = true;
                    break;
                case 3:
                    Cover.Background = new SolidColorBrush(Colors.Red);
                    PopupBorder.Background = new SolidColorBrush(Colors.Red);
                    Confirm.IsVisible = true;
                    break;
            }
            Head.Text = title;
            Message.Text = message;
            this.IsVisible = true;
            this.Opacity = 0;
            Debug.WriteLine("弹窗已被触发");
            EventBus.Instance.Publish(new ViewBroadcastArgs() { Target = "MainWindow.axaml.cs", Message = "ShowPopup" });
            // 等待用户操作
            var result = await tcs.Task;
            this.IsVisible = false;
            this.Opacity = 1;
            EventBus.Instance.Publish(new ViewBroadcastArgs() { Target = "MainWindow.axaml.cs", Message = "HidePopup" });
            Debug.WriteLine("弹窗已完成");
            return result;
        }

        // 按钮完成
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            tcs.TrySetResult("confirm");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            tcs.TrySetResult("cancel");
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            tcs.TrySetResult("no");
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            tcs.TrySetResult("yes");
        }


    }
}
