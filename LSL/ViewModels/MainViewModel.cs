namespace LSL.ViewModels;

using Avalonia.Controls;
using LSL.Views;
using LSL.Views.Home;
using LSL.Views.Server;
using LSL.Views.Download;
using LSL.Views.Download.ASViews;
using LSL.Views.Settings;
using ReactiveUI;
using System;
using System.Windows.Input;
using System.Reactive;
using System.Collections.Generic;
using Avalonia.Markup.Xaml.MarkupExtensions;
using System.Diagnostics;
using LSL.Services;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Interactivity;
using Newtonsoft.Json.Linq;
using System.IO;

//导航部分开始
public partial class MainViewModel : ViewModelBase, INavigationService
{
    //初始化主窗口
    public void InitializeMainWindow()
    {
        NavigateLeftView("HomeLeft");
        NavigateRightView("HomeRight");
        LeftWidth = 350;
        CurrentLeftView = "HomeLeft";
        CurrentRightView = "HomeRight";
    }

    #region 导航相关
    //原View
    private UserControl _leftView;
    private UserControl _rightView;

    //当前View
    public string CurrentLeftView { get; set; }
    public string CurrentRightView { get; set; }

    //创建两个可变动视图
    public UserControl LeftView
    {
        get => _leftView;
        set => this.RaiseAndSetIfChanged(ref _leftView, value);
    }
    public UserControl RightView
    {
        get => _rightView;
        set => this.RaiseAndSetIfChanged(ref _rightView, value);
    }

    //创建切换触发方法
    public ICommand LeftViewCmd { get; }
    public ICommand RightViewCmd { get; }
    //这一部分是多参数导航按钮的部分，由于设置别的VM会导致堆栈溢出且暂时没找到替代方案，所以先摆了
    //还有就是本来希望可以创建一个方法来传递两个参数的，但是太麻烦了，还是先搁置了
    public ReactiveCommand<Unit, Unit> PanelConfigCmd { get; }
    public ReactiveCommand<Unit, Unit> DownloadConfigCmd { get; }
    public ReactiveCommand<Unit, Unit> CommonConfigCmd { get; }
    #endregion

    #region 切换命令
    //左视图
    public void NavigateLeftView(string viewName)
    {
        UserControl newView = ViewFactory.CreateView(viewName);
        if (newView != null && viewName != CurrentLeftView)
        {
            LeftView = newView;
            switch (viewName)
            {
                case "HomeLeft":
                    NavigateRightView("HomeRight");
                    LeftWidth = 350;
                    break;
                case "ServerLeft":
                    NavigateRightView("ServerStat");
                    LeftWidth = 250;
                    break;
                case "DownloadLeft":
                    NavigateRightView("AutoDown");
                    LeftWidth = 150;
                    break;
                case "SettingsLeft":
                    NavigateRightView("Common");
                    LeftWidth = 150;
                    break;
            }
            CurrentLeftView = viewName;
            BarChangedPublisher.Instance.PublishMessage(viewName);//通知导航栏按钮样式更改
            Debug.WriteLine("Left Page Switched:" + viewName);
        }
    }
    //右视图
    public void NavigateRightView(string viewName)
    {
        UserControl newView = ViewFactory.CreateView(viewName);
        if (newView != null && viewName != CurrentRightView)
        {
            RightView = newView;
            CurrentRightView = viewName;
        }
        LeftChangedPublisher.Instance.LeftPublishMessage(viewName);
        Debug.WriteLine("Right Page Switched:" + viewName);
    }
    #endregion

    #region 左栏宽度定义
    private double _leftWidth;
    public double LeftWidth
    {
        get => _leftWidth;
        set => this.RaiseAndSetIfChanged(ref _leftWidth, value);
    }
    #endregion

    public MainViewModel()
    {
        LeftViewCmd = ReactiveCommand.Create<string>(NavigateLeftView);
        RightViewCmd = ReactiveCommand.Create<string>(NavigateRightView);

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

        ConfirmAddServer = ReactiveCommand.Create(() =>
        {
            string JavaPath = GameManager.MatchJavaList(JavaSelection.ToString());
            ConfigManager.RegisterServer(NewServerName, JavaPath, CorePath, MinMemory, MaxMemory, ExtJvm);
            ReadServerList();
            NavigateRightView("AddServer");
        });// 添加服务器命令-实现

        DeleteServer = ReactiveCommand.Create(() =>
        {
            string serverId = ServerIDs[SelectedServerIndex];
            ConfigManager.DeleteServer(serverId);
            ReadServerList();
            SelectedServerIndex = 0;
            Debug.WriteLine("Server Deleted:" + serverId);
        });// 删除服务器命令-实现

        SearchJava = ReactiveCommand.Create(() =>
        {
            GameManager.DetectJava();
            ReadJavaList();
        });// 搜索Java命令-实现

        StartServerCmd = ReactiveCommand.Create(StartServer);// 启动服务器命令-实现

        // 初始化
        ConfigManager.Initialize();
        GetConfig();
        ReadServerList();
        ReadJavaList();
        SelectedServerIndex = 0;

        #region 缓存验证
        if (appPriorityCache >= 0 && appPriorityCache <= 2)
            appPriority = appPriorityCache;
        else ConfigManager.ModifyConfig("app_priority", 1);

        if (javaSelectionCache >= 0)
            javaSelection = javaSelectionCache;
        else ConfigManager.ModifyConfig("java_selection", 0);

        if (outputEncodeCache >= 1 && outputEncodeCache <= 2)
            outputEncode = outputEncodeCache;
        else ConfigManager.ModifyConfig("output_encode", 1);

        if (inputEncodeCache >= 0 && inputEncodeCache <= 2)
            inputEncode = inputEncodeCache;
        else ConfigManager.ModifyConfig("input_encode", 0);

        if (downloadSourceCache >= 0 && downloadSourceCache <= 1)
            downloadSource = downloadSourceCache;
        else ConfigManager.ModifyConfig("download_source", 0);

        if (downloadThreadsCache >= 1 && downloadThreadsCache <= 128)
            downloadThreads = downloadThreadsCache;
        else ConfigManager.ModifyConfig("download_threads", 16);

        if (downloadLimitCache >= 0 && downloadLimitCache <= 1000)
            downloadLimit = downloadLimitCache;
        else ConfigManager.ModifyConfig("download_limit", 0);

        if (panelPortCache >= 8080 && panelPortCache <= 65535)
            panelPort = panelPortCache;
        else ConfigManager.ModifyConfig("panel_port", 25000);
        #endregion

    }
}
