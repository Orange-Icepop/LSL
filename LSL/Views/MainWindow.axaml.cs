using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
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
                .Where(args => args.Target == "MainWindow.axaml.cs")
                .Subscribe(args => BroadcastHandler(args.Message));
        this.WhenActivated(action =>
        {
            action(this.ViewModel!.ITAUnits.PopupITA.RegisterHandler(HandlePopup));
            action(this.ViewModel!.ITAUnits.NotifyITA.RegisterHandler(ShowNotification));
            action(this.ViewModel!.ITAUnits.FilePickerITA.RegisterHandler(OpenFileOperation));
            this.ViewModel!.InitializeMainWindow();
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
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (this.ViewModel!.CheckForExiting())
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            e.Cancel = false;
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
            default: return;
        }
    }
    #region 显示通知
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
        Patterns = new[] { "*.jar" },
        MimeTypes = new[] { "application/java-archive" }
    };
    public async Task OpenFileOperation(IInteractionContext<FilePickerType, string> ITA)
    {
        // 启动异步操作以打开对话框。
        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "打开Minecraft核心文件",
            AllowMultiple = false,
            FileTypeFilter = new[] { CoreFileType },
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
