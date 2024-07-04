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
using System.Reactive.Linq;
using System.Reactive.Subjects;

public class BarViewModel : ReactiveObject
{
    public BarViewModel()
    {

    }
    //按钮样式更改事件订阅 开始
    public readonly Subject<string> _changeBarColor = new Subject<string>();

    // 公开一个IObservable<string>供外部订阅  
    public IObservable<string> ChangeBarColor => _changeBarColor.AsObservable();

    // 一个方法用于发布消息到事件流  
    public void PublishChangeBarColor(string message)
    {
        _changeBarColor.OnNext(message);
    }
    //按钮样式更改事件订阅 结束

}
