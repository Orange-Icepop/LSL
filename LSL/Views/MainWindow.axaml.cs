using Avalonia;
using Avalonia.Controls;
using System.ComponentModel;
using LSL.ViewModels;
using LSL.Services;
namespace LSL.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainViewModel(); // 设置DataContext为MainViewModel的实例
        this.Closing += MainWindow_Closing;// 重定向关闭窗口事件
        PopupPublisher.Instance.PopupMessageReceived += HandlePopupMessageReceived;// 注册消息接收事件
        PopupClosePublisher.Instance.PopupCloseOccurred += PopupClosing;// 注册弹窗关闭事件
        //初始化配置文件，虽然放在这里有些不应该，但是App里面没有初始化相关代码，只能这么干了
        ConfigManager.WriteInitialConfig();
    }
    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        bool EnableDaemon = (bool)ConfigManager.ReadConfig("daemon");
        if (EnableDaemon == true)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            e.Cancel = false;
        }
    }

    private void HandlePopupMessageReceived(string type, string message)
    {
        Popup.IsVisible = true;
    }

    private void PopupClosing()
    {
        Popup.IsVisible = false;
    }
}
