using System;
using System.ComponentModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.ReactiveUI;
using LSL.Services;
using LSL.ViewModels;
using ReactiveUI;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace LSL.Views;

public partial class MainWindow : ReactiveWindow<ShellViewModel>
{
    public WindowNotificationManager? NotifyManager;
    public static MainWindow Instance { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        this.Closing += MainWindow_Closing;// 重定向关闭窗口事件
        this.Loaded += InitializeViews;
        EventBus.Instance.Subscribe<ViewBroadcastArgs>(BroadcastHandler);
        this.WhenActivated(action =>
        {
            action(this.ViewModel!.ITAUnits.PopupITA.RegisterHandler(HandlePopup));
            action(this.ViewModel!.ITAUnits.NotifyITA.RegisterHandler(ShowNotification));
        });
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        NotifyManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.TopRight,
            MaxItems = 5,
            FontSize = 12,
        };
    }
    private void InitializeViews(object? sender, EventArgs e)
    {
        var shellViewModel = this.ViewModel!;
        shellViewModel.InitializeMainWindow();
    }
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
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

    private void ShowNotification(IInteractionContext<NotifyArgs, Unit> ITA)
    {
        var args = ITA.Input;
        var title = args.Title;
        var message = args.Message;
        title ??= "通知";
        message ??= "未知消息";
        NotificationType type;
        switch (args.Type)
        {
            case 0:
                {
                    type = NotificationType.Information;
                    title ??= "消息";
                    break;
                }
            case 1:
                {
                    type = NotificationType.Success;
                    title ??= "成功";
                    break;
                }
            case 2:
                {
                    type = NotificationType.Warning;
                    title ??= "警告";
                    break;
                }
            case 3:
                {
                    type = NotificationType.Error;
                    title ??= "错误";
                    break;
                }
            default:
                return;
        }
        NotifyManager?.Show(new Notification(title, message, type));
        ITA.SetOutput(Unit.Default);
    }
    private async Task HandlePopup(IInteractionContext<InvokePopupArgs, PopupResult> interaction)
    {
        var args = interaction.Input;
        var dialog = new PopupWindow(args.PType, args.PTitle, args.PContent);
        var result = await dialog.ShowDialog<PopupResult>(this);
        interaction.SetOutput(result);
    }
}
