using Avalonia.Controls;
using LSL.Views;
using LSL.Views.Download;
using LSL.Views.Download.ASViews;
using LSL.Views.Home;
using LSL.Views.Server;
using LSL.Views.Settings;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LSL.ViewModels;

//公共方法
#region ViewFactory穷举并创建所有视图
public static class ViewFactory
{
    public static UserControl CreateView(string viewName)
    {
        return viewName switch
        {
            //Bar
            "Bar" => new Bar(),
            "FSBar" => new FSBar(),

            //Top
            "HomeLeft" => new HomeLeft(),
            "ServerLeft" => new ServerLeft(),
            "DownloadLeft" => new DownloadLeft(),
            "SettingsLeft" => new SettingsLeft(),
            //Home
            "HomeRight" => new HomeRight(),
            //Server
            "ServerGeneral" => new ServerGeneral(),
            "ServerStat" => new ServerStat(),
            "ServerTerminal" => new ServerTerminal(),
            "ServerConf" => new ServerConf(),
            //Download
            "AutoDown" => new AutoDown(),
            "ManualDown" => new ManualDown(),
            "AddServer" => new AddServer(),
            "ModDown" => new ModDown(),
            //ASViews
            "AddCore" => new AddCore(),
            //Settings
            "Common" => new Common(),
            "DownloadSettings" => new DownloadSettings(),
            "PanelSettings" => new PanelSettings(),
            "StyleSettings" => new StyleSettings(),
            "About" => new About(),
            _ => throw new ArgumentException($"未找到视图: {viewName}，应用程序可能已经损坏。"),
        };
    }
}
#endregion

public class ViewModelBase : ReactiveObject
{
    /*
    // 实现INotifyPropertyChanged
    public new event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }*/
}
