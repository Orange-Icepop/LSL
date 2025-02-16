using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using LSL.Services;
using LSL.Views;
using System.Diagnostics;
using System.Collections.Generic;
using LSL.Services.Validators;
using Avalonia.Threading;

namespace LSL.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // 不要忘了每次发布Release时更新版本号！！！
    public static string CurrentVersion { get; } = "v0.08.2";

    #region About页面的相关内容
    public ICommand OpenWebPageCmd { get; }
    public async void OpenWebPage(string url)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(url);
            //if (url.IndexOf("http://") != 1 && url.IndexOf("https://") != 1) throw new ArgumentException("URL格式错误");
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            Notify(1, "成功打开了网页！", url);
        }
        catch (System.ComponentModel.Win32Exception noBrowser)
        {
            if (noBrowser.ErrorCode == -2147467259)
                await ShowPopup(4, "打开网页失败", $"LSL未能成功打开网页{url}，请检查您的系统是否设置了默认浏览器。\r错误内容：{noBrowser.Message}");
        }
        catch (Exception ex)
        {
            await ShowPopup(4, "打开网页失败", $"LSL未能成功打开网页{url}，这是由于非浏览器配置错误造成的。\r如果这是在自定义主页中发生的，请检查您的自定义主页是否正确配置了网址；否则，这可能是一个Bug，请您提交一个issue反馈。\r错误内容：{ex.Message}");
        }
    }
    #endregion

    private AppStateLayer AppState { get; }

    public MainViewModel(AppStateLayer appState)
    {
        AppState = appState;

        // 事件订阅
        EventBus.Instance.Subscribe<PopupMessageArgs>(ReceivePopupMessage);
        EventBus.Instance.Subscribe<TerminalOutputArgs>(ReceiveStdOutPut);
        EventBus.Instance.Subscribe<PlayerUpdateArgs>(ReceivePlayerUpdate);
        EventBus.Instance.Subscribe<PlayerMessageArgs>(ReceiveMessage);
        EventBus.Instance.Subscribe<ServerStatusArgs>(ReceiveServerStatus);
        // 初始化
        ResetPopup();// 重置弹出窗口
        ConfigManager.Initialize();// 初始化配置
        GetConfig();// 获取配置
        ReadServerList();// 读取服务器列表
        ReadJavaList();// 读取Java列表
        var outputHandler = OutputHandler.Instance;// 初始化输出处理

        #region 配置相关命令实现
        ConfirmAddServer = ReactiveCommand.Create(async () => await AddNewServer());// 添加服务器命令-实现

        DeleteServer = ReactiveCommand.Create(async () =>
        {
            string confirmResult = await ShowPopup(3, "确定删除此服务器吗？", "该操作不可逆！");
            if (confirmResult == "Yes")
            {
                ServerConfigManager.DeleteServer(SelectedServerId);
                ReadServerList();
                Notify(1, null, "服务器已成功删除！");
            }
        });// 删除服务器命令-实现

        EditServer = ReactiveCommand.Create(async () => await EditCurrentServer());// 编辑服务器命令-实现

        SearchJava = ReactiveCommand.Create(async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => Notify(0, "正在搜索Java", "请耐心等待......"));
            await JavaManager.DetectJava();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ReadJavaList();
                Notify(1, "Java搜索完成！", $"搜索到了{JavaVersions.Count}个Java");
            });
        });// 搜索Java命令-实现
        #endregion

        #region 服务器相关命令实现
        StartServerCmd = ReactiveCommand.Create(StartServer);// 启动服务器命令-实现
        StopServerCmd = ReactiveCommand.Create(async () =>
        {
            string result = await ShowPopup(2, "确定关闭此服务器吗？", "你的存档将被保存。");
            if (result == "Yes")
            {
                await SendServerCommand("stop");
            }
        });// 停止服务器命令-实现
        SaveServerCmd = ReactiveCommand.Create(async () =>
        {
            await SendServerCommand("save-all");
        });// 保存服务器命令-实现
        ShutServerCmd = ReactiveCommand.Create(async () =>
        {
            string result = await ShowPopup(3, "确定强制关闭此服务器吗？", "这将直接结束服务器进程，你所做的最新操作可能不会被保存！");
            if (result == "Yes")
            {
                ServerHost.Instance.EndServer(SelectedServerId);
            }
        });// 结束服务器进程命令-实现
        SendServerCmd = ReactiveCommand.Create(async () =>
        {
            await SendServerCommand(ServerInputText);
            await Task.Run(ResetServerInputText);
        });// 发送服务器命令的命令-实现
        #endregion

        #region Popup相关命令实现
        // 正常情况下，这些命令被调用时PopupTcs不为null
        PopupConfirm = ReactiveCommand.Create(() => PopupTcs?.TrySetResult("Confirm"));
        PopupCancel = ReactiveCommand.Create(() => PopupTcs?.TrySetResult("Cancel"));
        PopupYes = ReactiveCommand.Create(() => PopupTcs?.TrySetResult("Yes"));
        PopupNo = ReactiveCommand.Create(() => PopupTcs?.TrySetResult("No"));
        #endregion

        #region 其它命令实现
        OpenWebPageCmd = ReactiveCommand.Create<string>(OpenWebPage);// 打开网页命令-实现
        NotifyCommand = ReactiveCommand.Create(() => EventBus.Instance.Publish(new ViewBroadcastArgs { Target = "MainWindow.axaml.cs", Message = "Notify" }));// 通知命令-实现
        #endregion

        StartUpProgress();
    }

    private void StartUpProgress()// 非必要启动项
    {
    }

}
