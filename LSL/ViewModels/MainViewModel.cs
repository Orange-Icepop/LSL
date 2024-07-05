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
public class MainViewModel : ViewModelBase, INavigationService
{
    #region 定义类，穷举写得依托答辩
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
        set => this.RaiseAndSetIfChanged(ref _leftView, value);
    }
    public UserControl RightView 
    { 
        get => _rightView;
        set => this.RaiseAndSetIfChanged(ref _rightView, value);
    }
    //创建左栏宽度定义
    private double _leftWidth;
    public double LeftWidth
    {
        get => _leftWidth;
        set => this.RaiseAndSetIfChanged(ref _leftWidth, value);
    }
    #endregion

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
    //左视图
    public void NavigateLeftView(string viewName)
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
        Debug.WriteLine("Right Page Switched:" + viewName);
    }

    //导航部分结束
}
