using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using LSL.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace LSL.Views.Server;

public partial class ServerTerminal : UserControl
{
    public ServerTerminal()
    {
        InitializeComponent();
        MessageBus.Current.Listen<ViewBroadcastArgs>()
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(ForceScroll);
    }

    public void ForceScroll(ViewBroadcastArgs args)
    {
        if (args.Target == typeof(ServerTerminal) && args.Message == "ScrollToEnd") Terminal.ForceScrollToBottom();
    }
}