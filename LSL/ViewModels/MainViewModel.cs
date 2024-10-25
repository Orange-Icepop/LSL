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
        ConfigManager.Initialize();// 初始化配置
        GetConfig();// 获取配置
        ReadServerList();// 读取服务器列表
        ReadJavaList();// 读取Java列表
        RestorePopup();// 初始化弹窗
        OutputHandler outputHandler = new();// 初始化输出处理

        LeftViewCmd = ReactiveCommand.Create<string>(NavigateLeftView);
        RightViewCmd = ReactiveCommand.Create<string>(INavigateRight);

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

        StartServerCmd = ReactiveCommand.Create(StartServer);// 启动服务器命令-实现
        StopServerCmd = ReactiveCommand.Create(() =>
        {
            SendServerCommand("stop");
            AddTerminalText(SelectedServerId, "[LSL 消息]: 关闭服务器命令已发出，请等待");
        });// 停止服务器命令-实现
        SaveServerCmd = ReactiveCommand.Create(() => SendServerCommand("save-all"));// 保存服务器命令-实现
        ShutServerCmd = ReactiveCommand.Create(() => ServerHost.Instance.EndServer(SelectedServerId));// 结束服务器进程命令-实现

        PopupConfirm = ReactiveCommand.Create(() =>
        {
            PopupResponse = "true";
        });
        PopupCancel = ReactiveCommand.Create(() =>
        {
            PopupResponse = "false";
        });
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
}
