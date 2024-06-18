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


public class MainViewModel : ViewModelBase
{

    //创建两个可变动视图
    public UserControl LeftView { get; private set; }
    public UserControl RightView { get; private set; }
    //创建切换触发方法
    public ICommand LeftViewCmd { get; }
    // 使用字典来缓存已创建的视图  
    private Dictionary<string, UserControl> _leftViews = new Dictionary<string, UserControl>();
    // 字典缓存逻辑
    private void SetLeftView(string viewName)
    {
        if (_leftViews.TryGetValue(viewName, out UserControl view))
        {
            // 如果视图已存在，则直接使用它  
            LeftView = view;
        }
        else
        {
            // 否则，创建新视图并缓存它  
            view = CreateLeftView(viewName);
            _leftViews[viewName] = view;
            LeftView = view;
        }
    }
    public MainViewModel()
    {
        SetLeftView("Home");//设置默认视图
        LeftViewCmd = ReactiveCommand.Create<string>(viewName => LoadLeftView(viewName));
    }

    private UserControl CreateLeftView(string viewName)
    {
        switch (viewName)
        {
            case "Home":
                return new HomeLeft();
            case "Server":
                return new ServerLeft();
            case "Download":
                return new DownloadLeft();
            case "Settings":
                return new SettingsLeft();
            default:
                throw new ArgumentException("Invalid view name", nameof(viewName));
        }
    }
    // 直接使用 SetLeftView 来设置 LeftView  
    private void LoadLeftView(string viewName)
    {
        SetLeftView(viewName);
    }
}
