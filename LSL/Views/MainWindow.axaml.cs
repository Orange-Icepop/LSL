using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using LSL.Common.Models;
using LSL.ViewModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace LSL.Views;

public partial class MainWindow : ReactiveWindow<InitializationViewModel>
{
    public WindowNotificationManager? NotifyManager;
    public ILogger<MainWindow>? Logger { private get; init; }

    public MainWindow()
    {
        InitializeComponent();
        this.Closing += MainWindow_Closing;// 重定向关闭窗口事件
        MessageBus.Current.Listen<WindowOperationArgs>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(args => args.Body is not WindowOperationArgType.CheckForClose)
            .Subscribe(args => CloseHandler(args.Body));
        this.WhenActivated(action =>
        {
            action(this.ViewModel!.AppState.InteractionUnits.NotifyInteraction.RegisterHandler(ShowNotification));
            action(this.ViewModel!.AppState.InteractionUnits.FilePickerInteraction.RegisterHandler(OpenFileOperation));
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
    private bool _confirmClose = false;
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_confirmClose) e.Cancel = false;
        else
        {
            e.Cancel = true;
            MessageBus.Current.SendMessage(new WindowOperationArgs(WindowOperationArgType.CheckForClose));
        }
    }

    private void CloseHandler(WindowOperationArgType cType)
    {
        switch (cType)
        {
            case WindowOperationArgType.CheckForClose: return;
            case WindowOperationArgType.ConfirmClose: 
            case WindowOperationArgType.ForceClose:
                _confirmClose = true;
                this.Close();
                break;
            case WindowOperationArgType.Hide:
                this.Hide();
                break;
            default: return;
        }
    }
    #endregion

    #region 显示通知
    private void ShowNotification(IInteractionContext<NotifyArgs, Unit> context)
    {
        var args = context.Input;
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
        context.SetOutput(Unit.Default);
    }
    #endregion
    
    #region 文件选择
    private static FilePickerFileType CoreFileType { get; } = new("Minecraft服务器核心文件")
    {
        Patterns = ["*.jar"],
        MimeTypes = ["application/java-archive"]
    };
    private async Task OpenFileOperation(IInteractionContext<FilePickerType, string> context)
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
            var uri = files[0].Path;
            context.SetOutput(uri.LocalPath);
        }
        else context.SetOutput(string.Empty);
    }
    #endregion

}