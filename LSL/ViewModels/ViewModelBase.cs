using Avalonia.Controls;
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
        switch (viewName)
        {
            //Bar
            case "HomeLeft":
                return new HomeLeft();
            case "ServerLeft":
                return new ServerLeft();
            case "DownloadLeft":
                return new DownloadLeft();
            case "SettingsLeft":
                return new SettingsLeft();
            //Home
            case "HomeRight":
                return new HomeRight();
            //Server
            case "ServerStat":
                return new ServerStat();
            case "ServerTerminal":
                return new ServerTerminal();
            case "ServerConf":
                return new ServerConf();
            //Download
            case "AutoDown":
                return new AutoDown();
            case "ManualDown":
                return new ManualDown();
            case "AddServer":
                return new AddServer();
            case "ModDown":
                return new ModDown();
            //ASViews
            case "AddCore":
                return new AddCore();
            //Settings
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
                throw new ArgumentException($"未找到视图: {viewName}，应用程序可能已经损坏。");
        }
    }
}
#endregion

#region 定义导航栏改变事件发布
public class BarChangedPublisher
{
    private static BarChangedPublisher _instance;

    private BarChangedPublisher() { }

    public static BarChangedPublisher Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new BarChangedPublisher();
            }
            return _instance;
        }
    }
    // 定义事件委托  
    public delegate void BarChangedEventHandler(string navigateTarget);

    // 定义事件  
    public event BarChangedEventHandler BarMessageReceived;

    // 触发事件的方法  
    protected virtual void BarChangedReceived(string navigateTarget)
    {
        // 检查是否有任何订阅者  
        BarChangedEventHandler handler = BarMessageReceived;
        if (handler != null)
        {
            // 如果有，则调用它们  
            handler(navigateTarget);
        }
    }

    // 一个公共方法，用于从类的外部请求触发事件  
    public void PublishMessage(string navigateTarget)
    {
        // 这里，我们直接调用受保护的方法来触发事件  
        BarChangedReceived(navigateTarget);
    }
}
#endregion

#region 定义左栏改变事件发布
public class LeftChangedPublisher
{
    private static LeftChangedPublisher _instance;

    private LeftChangedPublisher() { }

    public static LeftChangedPublisher Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new LeftChangedPublisher();
            }
            return _instance;
        }
    }
    // 定义事件委托  
    public delegate void LeftChangedEventHandler(string navigateLeftTarget);

    // 定义事件  
    public event LeftChangedEventHandler LeftMessageReceived;

    // 触发事件的方法  
    protected virtual void LeftChangedReceived(string navigateLeftTarget)
    {
        // 检查是否有任何订阅者  
        LeftChangedEventHandler handler = LeftMessageReceived;
        if (handler != null)
        {
            // 如果有，则调用它们  
            handler(navigateLeftTarget);
        }
    }

    // 一个公共方法，用于从类的外部请求触发事件  
    public void LeftPublishMessage(string navigateLeftTarget)
    {
        // 这里，我们直接调用受保护的方法来触发事件  
        LeftChangedReceived(navigateLeftTarget);
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

#region INavigationService 接口定义  
public interface INavigationService
{
    void NavigateLeftView(string viewName);
    void NavigateRightView(string viewName);
}
#endregion

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
