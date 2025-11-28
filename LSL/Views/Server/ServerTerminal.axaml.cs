using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using LSL.ViewModels;
using ReactiveUI;

namespace LSL.Views.Server;

public partial class ServerTerminal : UserControl
{
    public ServerTerminal()
    {
        InitializeComponent();
        MessageBus.Current.Listen<ViewBroadcastArgs>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ForceScroll);
    }
    public void ForceScroll(ViewBroadcastArgs args)
    {
        if (args.Target == typeof(ServerTerminal) && args.Message == "ScrollToEnd")
        {
            Terminal.ForceScrollToBottom();
        }
    }
}