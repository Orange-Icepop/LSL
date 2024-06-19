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
        //RightViewCmd = ReactiveCommand.Create<string>(NavigateRightView);
        LeftView = new HomeLeft();
        RightView = new HomeRight();
    }
    //切换命令
    private void NavigateLeftView(string viewName)
    {
        switch (viewName)
        {
            case "Home":
                LeftView = new HomeLeft();
                //RightView = new HomeRight();
                break;
            case "Server":
                LeftView = new ServerLeft();
                //RightView = new ServerRight();
                break;
            case "Download":
                LeftView = new DownloadLeft();
                //RightView = new DownloadRight();
                break;
            case "Settings":
                LeftView = new SettingsLeft();
                //RightView = new SettingsRight();
                break;

        }
    }


}
