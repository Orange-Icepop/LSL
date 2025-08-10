using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using LSL.Services;
using LSL.ViewModels;
using ReactiveUI;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace LSL.Views;

public partial class MainWindow : ReactiveWindow<ShellViewModel>
{
    public WindowNotificationManager? NotifyManager;

    public MainWindow()
    {
        InitializeComponent();
        this.Closing += MainWindow_Closing;// 重定向关闭窗口事件
        MessageBus.Current.Listen<ViewBroadcastArgs>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(args => args.Target == typeof(MainWindow))
            .Subscribe(args => BroadcastHandler(args.Message));
        MessageBus.Current.Listen<WindowOperationArgs>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(args => args.Body is not WindowOperationArgType.Raise)
            .Subscribe(args => CloseHandler(args.Body));
        this.WhenActivated(action =>
        {
            action(this.ViewModel!.ITAUnits.PopupITA.RegisterHandler(HandlePopup));
            action(this.ViewModel!.ITAUnits.NotifyITA.RegisterHandler(ShowNotification));
            action(this.ViewModel!.ITAUnits.FilePickerITA.RegisterHandler(OpenFileOperation));
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    await this.ViewModel!.InitializeMainWindow();
                }
                catch (Exception ex)
                {
                    await this.ViewModel!.ITAUnits.NotifyITA.Handle(new NotifyArgs(3, "主窗口初始化失败", ex.Message));
                    Environment.Exit(1);
                }
            });

        });
    }

    #region 生命周期
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
    private bool confirmClose = false;
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (confirmClose) e.Cancel = false;
        else
        {
            e.Cancel = true;
            MessageBus.Current.SendMessage(new WindowOperationArgs(WindowOperationArgType.Raise));
        }
        //TODO:add good force exit in initvm or sth else
    }

    private void CloseHandler(WindowOperationArgType cType)
    {
        switch (cType)
        {
            case WindowOperationArgType.Raise: return;
            case WindowOperationArgType.Confirm: 
            case WindowOperationArgType.ForceClose:
                confirmClose = true;
                this.Close();
                break;
            case WindowOperationArgType.Hide:
                this.Hide();
                break;
            default: return;
        }
    }
    #endregion

    private void BroadcastHandler(string arg)
    {
        switch (arg)
        {
            case "Show":
            {
                this.Show();
                break;
            }
            case "Close":
            {
                this.Close();
                break;
            }
            default: return;
        }
    }
    #region 显示通知
    private void ShowNotification(IInteractionContext<NotifyArgs, Unit> ITA)
    {
        var args = ITA.Input;
        var title = args.Title;
        var message = args.Message;
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
    #endregion

    #region 弹窗
    private async Task HandlePopup(IInteractionContext<InvokePopupArgs, PopupResult> interaction)
    {
        var args = interaction.Input;
        var dialog = new PopupWindow(args.PType, args.PTitle, args.PContent);
        var result = await dialog.ShowDialog<PopupResult>(this);
        interaction.SetOutput(result);
    }
    #endregion

    #region 文件选择
    private static FilePickerFileType CoreFileType { get; } = new("Minecraft服务器核心文件")
    {
        Patterns = ["*.jar"],
        MimeTypes = ["application/java-archive"]
    };
    private async Task OpenFileOperation(IInteractionContext<FilePickerType, string> ITA)
    {
        // 启动异步操作以打开对话框。
        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "打开Minecraft核心文件",
            AllowMultiple = false,
            FileTypeFilter = [CoreFileType],
        });

        if (files.Count >= 1)
        {
            var URI = files[0].Path;
            ITA.SetOutput(URI.LocalPath);
        }
        else ITA.SetOutput(string.Empty);
    }
    #endregion

}

public enum FilePickerType
{
    CoreFile,
    ZipFile,

}
