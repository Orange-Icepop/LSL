using Avalonia;
using Avalonia.Controls;
using System.ComponentModel;
using LSL.ViewModels;
using LSL.Services;
using System;
using System.Diagnostics;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
//using System.Reactive;
namespace LSL.Views;

public partial class MainWindow : Window
{
    public WindowNotificationManager NotifyManager;

    public MainWindow()
    {
        InitializeComponent();
        this.Closing += MainWindow_Closing;// 重定向关闭窗口事件
        this.Loaded += InitializeViews;
        EventBus.Instance.Subscribe<ViewBroadcastArgs>(BroadcastHandler);
        EventBus.Instance.Subscribe<NotifyArgs>(ShowNotification);
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        NotifyManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 5,
            FontSize = 12,
        };
    }
    private void InitializeViews(object sender, EventArgs e)
    {
        var mainViewModel = (MainViewModel)this.DataContext;
        mainViewModel.InitializeMainWindow();
    }
    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        EventBus.Instance.Publish(new ClosingArgs());
        bool EnableDaemon = (bool)ConfigManager.CurrentConfigs["daemon"];
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

    private void BroadcastHandler(ViewBroadcastArgs args)
    {
        if (args.Target == "MainWindow.axaml.cs")
        {
            switch (args.Message)
            {
                case "Show":
                    {
                        this.Show();
                        break;
                    }
                default: return;
            }
        }
    }

    private void ShowNotification(NotifyArgs args)
    {
        NotificationType type;
        switch (args.Type)
        {
            case 0:
                {
                    type = NotificationType.Information;
                    break;
                }
            case 1:
                {
                    type = NotificationType.Success;
                    break;
                }
            case 2:
                {
                    type = NotificationType.Warning;
                    break;
                }
            case 3:
                {
                    type = NotificationType.Error;
                    break;
                }
            default:
                return;
        }
        NotifyManager.Show(new Notification(args.Title, args.Message, type));
    }
}
