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
            case "Launcher":
                return new Launcher();
            case "DownloadSettings":
                return new DownloadSettings();
            case "About":
                return new About();
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
    //创建切换触发方法
    public ICommand LeftViewCmd { get; }
    public ICommand RightViewCmd { get; }
    public MainViewModel()
    {
        LeftViewCmd = ReactiveCommand.Create<string>(NavigateLeftView);
        RightViewCmd = ReactiveCommand.Create<string>(NavigateRightView);
        LeftView = new HomeLeft();
        RightView = new HomeRight();
    }
    //切换命令
    private void NavigateLeftView(string viewName)
    {
        UserControl newView = ViewFactory.CreateView(viewName);
        if (newView != null)
        {
            LeftView = newView;
            switch (viewName)
            {
                case "HomeLeft":
                    RightView = new HomeRight();
                    break;
                case "ServerLeft":
                    RightView = new ServerConf();
                    break;
                case "DownloadLeft":
                    RightView = new ManualDown();
                    break;
                case "SettingsLeft":
                    RightView = new Common();
                    break;
            }
        }
    }
    private void NavigateRightView(string viewName)
    {
        UserControl newView = ViewFactory.CreateView(viewName);
        if (newView != null)
        {
            RightView = newView;
        }
    }


}
