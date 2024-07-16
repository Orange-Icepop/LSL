﻿namespace LSL.ViewModels;

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
    //这一部分是多参数导航按钮的部分，由于设置别的VM会导致堆栈溢出且暂时没找到替代方案，所以先摆了
    //还有就是本来希望可以创建一个方法来传递两个参数的，但是太麻烦了，还是先搁置了
    public ReactiveCommand<Unit, Unit> PanelConfigCmd { get; }
    public ReactiveCommand<Unit, Unit> DownloadConfigCmd { get; }
    public ReactiveCommand<Unit, Unit> CommonConfigCmd { get; }
    //结束

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

        //多参数导航
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

    }


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
    //导航部分结束
}
