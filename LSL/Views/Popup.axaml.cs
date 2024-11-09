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
            // ��ʼ������Ϊ���ɼ�
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

        private TaskCompletionSource<string> tcs;// ����һ��TaskCompletionSource����������ĵĶ��������ܹ��ȴ��û�����

        // ������ʾ
        public async Task<string> Show(int type = 0, string title = "�����ˣ�", string message = "����һ���յĵ�����")
        {
            // ���ð�ť
            Confirm.IsVisible = false;
            Cancel.IsVisible = false;
            No.IsVisible = false;
            Yes.IsVisible = false;
            tcs = new TaskCompletionSource<string>();// ����tcs
            // type���壺0-��ʾ��ֻ��ȷ�ϣ���1-���棨��/��/ȡ������2-���棨��/�񣩣�3-����ȷ�ϣ�������Ϣ��
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
            Debug.WriteLine("�����ѱ�����");
            EventBus.Instance.Publish(new ViewBroadcastArgs() { Target = "MainWindow.axaml.cs", Message = "ShowPopup" });
            // �ȴ��û�����
            var result = await tcs.Task;
            this.IsVisible = false;
            this.Opacity = 1;
            EventBus.Instance.Publish(new ViewBroadcastArgs() { Target = "MainWindow.axaml.cs", Message = "HidePopup" });
            Debug.WriteLine("���������");
            return result;
        }

        // ��ť���
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
