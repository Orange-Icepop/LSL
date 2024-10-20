﻿using Avalonia.Controls;
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
            //Blank
            "blank" => new UserControl(),
            _ => throw new ArgumentException($"未找到视图: {viewName}，应用程序可能已经损坏。"),
        };
    }
}
#endregion

#region 定义弹窗事件发布
public class PopupPublisher
{
    private static PopupPublisher _instance;

    private PopupPublisher() { }

    public static PopupPublisher Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new PopupPublisher();
            }
            return _instance;
        }
    }
    // 定义事件委托  
    public delegate void PopupEventHandler(string Type, string PopupInfo);

    // 定义事件  
    public event PopupEventHandler PopupMessageReceived;

    // 触发事件的方法  
    protected virtual void PopupReceived(string Type, string PopupInfo)
    {
        // 检查是否有任何订阅者  
        PopupEventHandler handler = PopupMessageReceived;
        if (handler != null)
        {
            // 如果有，则调用它们  
            handler(Type, PopupInfo);
        }
    }

    // 一个公共方法，用于从类的外部请求触发事件  
    public void PopupMessage(string Type, string PopupInfo)
    {
        // 这里，我们直接调用受保护的方法来触发事件  
        PopupReceived(Type, PopupInfo);
    }
}
#endregion

#region 定义弹窗关闭事件发布  
public class PopupClosePublisher
{
    private static PopupClosePublisher _instance;

    // 私有构造函数，实现单例模式  
    private PopupClosePublisher() { }

    // 单例访问器  
    public static PopupClosePublisher Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new PopupClosePublisher();
            }
            return _instance;
        }
    }

    // 定义事件委托  
    public delegate void PopupCloseHandler();

    // 定义无参数事件  
    public event PopupCloseHandler PopupCloseOccurred;

    // 触发事件的方法  
    protected virtual void OnPopupCloseOccurred()
    {
        // 检查是否有任何订阅者  
        PopupCloseHandler handler = PopupCloseOccurred;
        if (handler != null)
        {
            // 如果有，则调用它们  
            handler();
        }
    }

    // 公共方法，用于从类的外部请求触发事件  
    public void ClosePopup()
    {
        // 直接调用受保护的方法来触发事件  
        OnPopupCloseOccurred();
    }
}
#endregion

// 示例用法
/*
class Program
{
    static void Main(string[] args)
    {
        // 订阅事件  
        PopupClosePublisher.Instance.PopupCloseOccurred += Instance_PopupCloseEventOccurred;

        // 触发事件  
        PopupClosePublisher.Instance.ClosePopup();

        // 取消订阅（在实际应用中，这可能在某个地方根据需要进行）  
        // PopupClosePublisher.Instance.PopupCloseEventOccurred -= Instance_PopupCloseEventOccurred;  
    }

    // 事件处理方法  
    private static void Instance_PopupCloseEventOccurred()
    {
        Console.WriteLine("PopupCloseEventOccurred 事件被触发！");
    }
}*/

public class ViewModelBase : ReactiveObject
{
    /*
    // 实现INotifyPropertyChanged
    // 虽然使用了CommunityToolkit，但是有一部分必须依赖ReactiveObject
    public new event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }*/
}
