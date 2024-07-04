namespace LSL.ViewModels;

using Avalonia.Controls;
using LSL.Views;
using LSL.Views.Home;
using LSL.Views.Server;
using LSL.Views.Download;
using LSL.Views.Settings;
using ReactiveUI;
using System;
using System.Windows.Input;
using System.Reactive;
using System.Collections.Generic;
using Avalonia.Markup.Xaml.MarkupExtensions;
using System.Diagnostics;

//导航部分开始
#region ViewFactory穷举并创建所有视图
public static class ViewFactory
{
    public static UserControl CreateView(string viewName)
    {
        switch (viewName)
        {
            case "HomeLeft":
                return new HomeLeft();
            case "ServerLeft":
                return new ServerLeft();
            case "DownloadLeft":
                return new DownloadLeft();
            case "SettingsLeft":
                return new SettingsLeft();
            case "HomeRight":
                return new HomeRight();
            case "ServerConf":
                return new ServerConf();
            case "AutoDown":
                return new AutoDown();
            case "ManualDown":
                return new ManualDown();
            case "ModDown":
                return new ModDown();
            case "Common":
                return new Common();
            case "DownloadSettings":
                return new DownloadSettings();
            case "PanelSettings":
                return new PanelSettings();
            case "StyleSettings":
                return new StyleSettings();
            case "About":
                return new About();
            default:
                throw new ArgumentException($"No view found for name: {viewName}");
        }
    }
}
#endregion
public class MainViewModel : ViewModelBase
{
    //定义类，穷举写得依托答辩
    //原View
    private UserControl _leftView;
    private UserControl _rightView;
    //当前View
    public string CurrentLeftView { get; set; }
    public string CurrentRightView { get; set; }
    //按钮样式类
    public string HomeButtonClass { get; set; }
    public string ServerButtonClass { get; set; }
    public string DownloadButtonClass { get; set; }
    public string SettingsButtonClass { get; set; }
    //创建两个可变动视图
    public UserControl LeftView {
        get => _leftView;
        set
        {
            _leftView = value;
            OnPropertyChanged(nameof(LeftView));
        } 
    }
    public UserControl RightView 
    { 
        get => _rightView;
        set
        {
            _rightView = value;
            OnPropertyChanged(nameof(RightView));
        }
    }
    //创建左栏宽度定义
    private double _leftWidth;
    public double LeftWidth
    {
        get => _leftWidth;
        set
        {
            _leftWidth = value;
            OnPropertyChanged(nameof(LeftWidth));
        }
    }

    //创建切换触发方法
    public ICommand LeftViewCmd { get; }
    public ICommand RightViewCmd { get; }
    public MainViewModel()
    {
        LeftViewCmd = ReactiveCommand.Create<string>(NavigateLeftView);
        RightViewCmd = ReactiveCommand.Create<string>(NavigateRightView);
        //初始化
        NavigateLeftView("HomeLeft");
        NavigateRightView("HomeRight");
        LeftWidth = 350;
        CurrentLeftView = "HomeLeft";
        CurrentRightView = "HomeRight";
        HomeButtonClass = "selected";
        ServerButtonClass = "bar";
        DownloadButtonClass = "bar";
        SettingsButtonClass = "bar";
    }
    
    
    //切换命令
    private void NavigateLeftView(string viewName)
    {
        UserControl newView = ViewFactory.CreateView(viewName);
        if (newView != null && viewName != CurrentLeftView)
        {
            LeftView = newView;
            HomeButtonClass = "bar";
            ServerButtonClass = "bar";
            DownloadButtonClass = "bar";
            SettingsButtonClass = "bar";
            switch (viewName)
            {
                case "HomeLeft":
                    NavigateRightView("HomeRight");
                    LeftWidth = 350;
                    HomeButtonClass = "selected";
                    break;
                case "ServerLeft":
                    NavigateRightView("ServerConf");
                    LeftWidth = 250;
                    ServerButtonClass = "selected";
                    break;
                case "DownloadLeft":
                    NavigateRightView("AutoDown");
                    LeftWidth = 150;
                    DownloadButtonClass = "selected";
                    break;
                case "SettingsLeft":
                    NavigateRightView("Common");
                    LeftWidth = 150;
                    SettingsButtonClass = "selected";
                    break;
            }
            CurrentLeftView = viewName;
            Debug.WriteLine("Left Page Switched:" + viewName);
        }
    }
    private void NavigateRightView(string viewName)
    {
        UserControl newView = ViewFactory.CreateView(viewName);
        if (newView != null && viewName != CurrentRightView)
        {
            RightView = newView;
            CurrentRightView = viewName;
        }
        Debug.WriteLine("Right Page Switched:" + viewName);
    }
    //导航部分结束
}
