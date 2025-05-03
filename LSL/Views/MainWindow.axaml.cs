using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.ReactiveUI;
using LSL.Services;
using LSL.ViewModels;
using ReactiveUI;

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
        EventBus.Instance.Subscribe<NotifyArgs>(ShowNotification);
        this.WhenActivated(action => action(this.ViewModel!.PopupITA.RegisterHandler(HandlePopup)));
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

    private void ShowNotification(NotifyArgs args)
    {
        var title = args.Title;
        var message = args.Message;
        bool IsTitleEmpty = title == null;
        title ??= "通知";
        message ??= "未知消息";
        NotificationType type;
        switch (args.Type)
        {
            case 0:
                {
                    type = NotificationType.Information;
                    if (IsTitleEmpty) title = "消息";
                    break;
                }
            case 1:
                {
                    type = NotificationType.Success;
                    if (IsTitleEmpty) title = "成功";
                    break;
                }
            case 2:
                {
                    type = NotificationType.Warning;
                    if (IsTitleEmpty) title = "警告";
                    break;
                }
            case 3:
                {
                    type = NotificationType.Error;
                    if (IsTitleEmpty) title = "错误";
                    break;
                }
            default:
                return;
        }
        NotifyManager?.Show(new Notification(title, message, type));
    }
    private async Task HandlePopup(IInteractionContext<InvokePopupArgs, PopupResult> interaction)
    {
        var args = interaction.Input;
        var dialog = new PopupWindow(args.type, args.title, args.content);
        var result = await dialog.ShowDialog<PopupResult>(this);
        interaction.SetOutput(result);
    }
}
