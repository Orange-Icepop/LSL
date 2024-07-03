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
//ViewFactory 创建视图，穷举所有视图
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
            case "Common":
                return new Common();
            case "StyleSettings":
                return new StyleSettings();
            case "DownloadSettings":
                return new DownloadSettings();
            case "About":
                return new About();
            case "AutoDown":
                return new AutoDown();
            case "ManualDown":
                return new ManualDown();
            case "ModDown":
                return new ModDown();
            default:
                throw new ArgumentException($"No view found for name: {viewName}");
        }
    }
}
public class MainViewModel : ViewModelBase
{
    private UserControl _leftView;
    private UserControl _rightView;
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
        LeftView = new HomeLeft();
        RightView = new HomeRight();
        LeftWidth = 350;
    }
    
    
    //切换命令
    private void NavigateLeftView(string viewName)
    {
        Debug.WriteLine("Left Page Switched:" + viewName);
        UserControl newView = ViewFactory.CreateView(viewName);
        if (newView != null)
        {
            LeftView = newView;
            switch (viewName)
            {
                case "HomeLeft":
                    RightView = new HomeRight();
                    LeftWidth = 350;
                    break;
                case "ServerLeft":
                    RightView = new ServerConf();
                    LeftWidth = 250;
                    break;
                case "DownloadLeft":
                    RightView = new ManualDown();
                    LeftWidth = 150;
                    break;
                case "SettingsLeft":
                    RightView = new Common();
                    LeftWidth = 150;
                    break;
            }
        }
    }
    private void NavigateRightView(string viewName)
    {
        Debug.WriteLine("Right Page Switched:" + viewName);
        UserControl newView = ViewFactory.CreateView(viewName);
        if (newView != null)
        {
            RightView = newView;
        }
    }
    //导航部分结束
}
