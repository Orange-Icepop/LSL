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
public class MainViewModel : ViewModelBase, INavigationService
{
    ConfigViewModel configViewModel = new ConfigViewModel();

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


    #region 存储文件路径方法
    public void SaveFilePath(string path, string targetValue)
    {
        switch(targetValue)
        {
            case "CorePath":
                CorePath = path;
                break;
        }
    }
    #endregion

    #region 新增服务器数据
    private string _corePath = string.Empty;// 核心文件路径
    public string CorePath { get => _corePath; set => this.RaiseAndSetIfChanged(ref _corePath, value); }

    private string _newServerName = string.Empty;// 新增服务器名称
    public string NewServerName { get => _newServerName; set => this.RaiseAndSetIfChanged(ref _newServerName, value); }

    private int _minMemory = 0;// 新增服务器最小内存
    public int MinMemory { get => _minMemory; set => this.RaiseAndSetIfChanged(ref _minMemory, value); }

    private int _maxMemory = 0;// 新增服务器最大内存
    public int MaxMemory { get => _maxMemory; set => this.RaiseAndSetIfChanged(ref _maxMemory, value); }

    private int _javaId = 0;//Java编号
    public int JavaId { get => _javaId; set => this.RaiseAndSetIfChanged(ref _javaId, value); }

    private string _extJvm = string.Empty;
    public string ExtJvm { get => _extJvm; set => this.RaiseAndSetIfChanged(ref _extJvm, value); }
    #endregion

    public ICommand SearchForJava { get; }
    public ICommand ConfirmAddServer { get; }

    #region 全局获取服务器列表
    //持久化服务器映射列表
    private ObservableCollection<string> _serverIDs = new ObservableCollection<string>();
    private ObservableCollection<string> _servernames = new ObservableCollection<string>();
    public ObservableCollection<string> ServerIDs => _serverIDs;
    public ObservableCollection<string> ServerNames => _servernames;
    // 服务器列表读取（从配置文件读取）
    public void ReadServerList()
    {
        string jsonContent = File.ReadAllText(ConfigManager.ServerConfigPath);
        JObject jsonObj = JObject.Parse(jsonContent);
        //遍历配置文件中的所有服务器ID
        foreach (var item in jsonObj.Properties())
        {
            ServerIDs.Add(item.Name);
        }
        //根据服务器ID读取每个服务器的配置文件
        foreach (var ServerID in ServerIDs)
        {
            string TargetedServerPath = (string)JsonHelper.ReadJson(ConfigManager.ServerConfigPath, $"$.{ServerID}");
            string TargetedConfigPath = Path.Combine(TargetedServerPath, "lslconfig.json");
            string KeyPath = "$.name";
            ServerNames.Add((string)JsonHelper.ReadJson(TargetedConfigPath, KeyPath));
        }
    }

    #endregion

    #region 全局获取Java列表
    //持久化服务器映射列表
    private ObservableCollection<string> _javas = new ObservableCollection<string>();
    public ObservableCollection<string> Javas => _serverIDs;
    // 服务器列表读取（从配置文件读取）
    public void ReadJavaList()
    {
        string jsonContent = File.ReadAllText(ConfigManager.JavaConfigPath);
        JObject jsonObj = JObject.Parse(jsonContent);
        //遍历配置文件中的所有Java
        foreach (var item in jsonObj.Properties())
        {
            Javas.Add(item.Value.ToString());
        }
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

        SearchForJava = ReactiveCommand.Create(GameManager.DetectJava);
        ConfirmAddServer = ReactiveCommand.Create(() =>
        {
            string JavaPath = GameManager.MatchJavaList(JavaId);
            ConfigManager.RegisterServer(NewServerName, JavaPath, CorePath, MinMemory, MaxMemory, ExtJvm);
            NavigateRightView("AddServer");
        });

        ReadServerList();
    }
}
