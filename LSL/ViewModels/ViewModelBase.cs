using Avalonia.Controls;
using LSL.Views.Download;
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
    public event BarChangedEventHandler MessageReceived;

    // 触发事件的方法  
    protected virtual void BarChangedReceived(string navigateTarget)
    {
        // 检查是否有任何订阅者  
        BarChangedEventHandler handler = MessageReceived;
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

#region INavigationService 接口定义  
public interface INavigationService
{
    void NavigateLeftView(string viewName);
    void NavigateRightView(string viewName);
}
#endregion

public class ViewModelBase : ReactiveObject
{
    //这一部分是用来实现INotifyPropertyChanged的，但是ReactiveUI已经实现了，所以可以不用，保险起见还是留着
    /*public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }*/
}
