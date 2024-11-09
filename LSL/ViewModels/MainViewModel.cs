namespace LSL.ViewModels;

using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using LSL.Services;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using LSL.Views;

public partial class MainViewModel : ViewModelBase
{
    //初始化主窗口
    public void InitializeMainWindow()
    {
        NavigateLeftView("HomeLeft");
        NavigateRightView("HomeRight");
        LeftWidth = 350;
    }

    public MainViewModel()
    {
        // 初始化
        ResetPopup();// 重置弹出窗口
        ConfigManager.Initialize();// 初始化配置
        GetConfig();// 获取配置
        ReadServerList();// 读取服务器列表
        ReadJavaList();// 读取Java列表
        OutputHandler outputHandler = new();// 初始化输出处理

        LeftViewCmd = ReactiveCommand.Create<string>(NavigateLeftView);
        RightViewCmd = ReactiveCommand.Create<string>(INavigateRight);
        ShowMainWindowCmd = ReactiveCommand.Create(ShowMainWindow);
        QuitCmd = ReactiveCommand.Create(Quit);

        #region 多参数导航
        PanelConfigCmd = ReactiveCommand.Create(() =>
        {
            NavigateLeftView("SettingsLeft");
            NavigateRightView("PanelSettings");
        });
        DownloadConfigCmd = ReactiveCommand.Create(() =>
        {
            NavigateLeftView("SettingsLeft");
            NavigateRightView("DownloadSettings");
        });
        CommonConfigCmd = ReactiveCommand.Create(() =>
        {
            NavigateLeftView("SettingsLeft");
            NavigateRightView("Common");
        });
        #endregion

        #region 命令实现
        // 配置相关命令-start

        ConfirmAddServer = ReactiveCommand.Create(() =>
        {
            string JavaPath = JavaManager.MatchJavaList(JavaSelection.ToString());
            ConfigManager.RegisterServer(NewServerName, JavaPath, CorePath, MinMemory, MaxMemory, ExtJvm);
            ReadServerList();
            NavigateRightView("AddServer");
        });// 添加服务器命令-实现

        DeleteServer = ReactiveCommand.Create(async () =>
        {
            ConfigManager.DeleteServer(SelectedServerId);
            ReadServerList();
            await Task.Delay(100);
            SelectedServerIndex = 0;
        });// 删除服务器命令-实现

        SearchJava = ReactiveCommand.Create(() =>
        {
            JavaManager.DetectJava();
            ReadJavaList();
        });// 搜索Java命令-实现

        // 配置相关命令-end

        // 服务器相关命令-start

        StartServerCmd = ReactiveCommand.Create(StartServer);// 启动服务器命令-实现
        StopServerCmd = ReactiveCommand.Create(async () =>
        {
            string result = await ShowPopup(2, "警告", "确定关闭此服务器吗？你的存档将被保存。");
            if (result == "Yes")
            {
                SendServerCommand("stop");
            }
        });// 停止服务器命令-实现
        SaveServerCmd = ReactiveCommand.Create(() => SendServerCommand("save-all"));// 保存服务器命令-实现
        ShutServerCmd = ReactiveCommand.Create(() => ServerHost.Instance.EndServer(SelectedServerId));// 结束服务器进程命令-实现

        // 服务器相关命令-end

        // Popup相关命令-start
        // 正常情况下，这些命令被调用时PopupTcs不为null
        PopupConfirm = ReactiveCommand.Create(() => PopupTcs.TrySetResult("confirm"));
        PopupCancel = ReactiveCommand.Create(() => PopupTcs.TrySetResult("cancel"));
        PopupYes = ReactiveCommand.Create(() => PopupTcs.TrySetResult("yes"));
        PopupNo = ReactiveCommand.Create(() => PopupTcs.TrySetResult("no"));
        // Popup相关命令-end

        #endregion

        EventBus.Instance.Subscribe<TerminalOutputArgs>(ReceiveStdOutPut);
        EventBus.Instance.Subscribe<PlayerUpdateArgs>(ReceivePlayerUpdate);
        EventBus.Instance.Subscribe<PlayerMessageArgs>(ReceiveMessage);
        EventBus.Instance.Subscribe<ServerStatusArgs>(ReceiveServerStatus);
        EventBus.Instance.Subscribe<ClosingArgs>(QuitHandler);
    }

    public void QuitHandler(ClosingArgs args)// 退出事件处理
    {
        ConfigManager.ConfirmConfig(ViewConfigs);
    }

    public ICommand ShowMainWindowCmd { get; }// 显示主窗口命令
    public ICommand QuitCmd { get; }// 退出命令

    public static void ShowMainWindow()
    {
        EventBus.Instance.Publish(new ViewBroadcastArgs { Target = "MainWindow.axaml.cs", Message = "Show" });
    }
    public static void Quit() { Environment.Exit(0); }

}
