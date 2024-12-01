﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using LSL.Services;
using LSL.Views;

namespace LSL.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // 不要忘了每次发布Release时更新版本号！！！
    public static string CurrentVersion = "v0.06";

    //初始化主窗口
    public void InitializeMainWindow()
    {
        BarView = new Bar();
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

        LeftViewCmd = ReactiveCommand.Create<string>(INavigateLeft);
        RightViewCmd = ReactiveCommand.Create<string>(INavigateRight);
        FullViewCmd = ReactiveCommand.Create<string>(NavigateFullScreenView);
        FullViewBackCmd = ReactiveCommand.Create(async() =>
        {
            await ShowPopup(4, "不应出现的命令错误", "当您看见该弹窗时，说明表单填充时用于返回的命令在未进入全屏表单时被触发了。如果您没有对LSL进行修改，这通常意味着LSL出现了一个Bug，请在LSL的源码仓库中提交一份关于该Bug的issue。");
        });
        ShowMainWindowCmd = ReactiveCommand.Create(ShowMainWindow);
        QuitCmd = ReactiveCommand.Create(Quit);

        #region 多参数导航
        PanelConfigCmd = ReactiveCommand.Create(() =>
        {
            NavigateLeftView("SettingsLeft", true);
            NavigateRightView("PanelSettings");
        });
        DownloadConfigCmd = ReactiveCommand.Create(() =>
        {
            NavigateLeftView("SettingsLeft", true);
            NavigateRightView("DownloadSettings");
        });
        CommonConfigCmd = ReactiveCommand.Create(() =>
        {
            NavigateLeftView("SettingsLeft", true);
            NavigateRightView("Common");
        });
        #endregion

        #region 命令实现
        // 配置相关命令-start

        ConfirmAddServer = ReactiveCommand.Create(async () =>
        {
            string JavaPath = JavaManager.MatchJavaList(JavaId.ToString());
            string confirmResult = await ShowPopup(2, "确定添加此服务器吗？", $"服务器信息：\r名称：{NewServerName}\rJava路径：{JavaPath}\r核心文件路径：{CorePath}\r内存范围：{MinMemory} ~ {MaxMemory}\r附加JVM参数：{ExtJvm}（默认为空）");
            if (confirmResult == "Yes")
                ConfigManager.RegisterServer(NewServerName, JavaPath, CorePath, MinMemory, MaxMemory, ExtJvm);
            ReadServerList();
            //NavigateRightView("AddServer");
            FullViewBackCmd.Execute(null);
        });// 添加服务器命令-实现

        DeleteServer = ReactiveCommand.Create(async() =>
        {
            string confirmResult = await ShowPopup(2, "确定删除此服务器吗？", "该操作不可逆！");
            if (confirmResult == "Yes")
            {
                ConfigManager.DeleteServer(SelectedServerId);
                ReadServerList();
            }
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
            string result = await ShowPopup(2, "确定关闭此服务器吗？", "你的存档将被保存。");
            if (result == "Yes")
            {
                SendServerCommand("stop");
            }
        });// 停止服务器命令-实现
        SaveServerCmd = ReactiveCommand.Create(async() =>
        {
            SendServerCommand("save-all");
        });// 保存服务器命令-实现
        ShutServerCmd = ReactiveCommand.Create(async () =>
        {
            string result = await ShowPopup(3, "确定强制关闭此服务器吗？", "这将直接结束服务器进程，你所做的最新操作可能不会被保存！");
            if (result == "Yes")
            {
                ServerHost.Instance.EndServer(SelectedServerId);
            }
        });// 结束服务器进程命令-实现

        // 服务器相关命令-end

        // Popup相关命令-start
        // 正常情况下，这些命令被调用时PopupTcs不为null
        PopupConfirm = ReactiveCommand.Create(() => PopupTcs.TrySetResult("Confirm"));
        PopupCancel = ReactiveCommand.Create(() => PopupTcs.TrySetResult("Cancel"));
        PopupYes = ReactiveCommand.Create(() => PopupTcs.TrySetResult("Yes"));
        PopupNo = ReactiveCommand.Create(() => PopupTcs.TrySetResult("No"));
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
