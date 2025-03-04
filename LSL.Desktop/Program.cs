﻿using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.ReactiveUI;
using LSL.Components;
using LSL.Services;

namespace LSL.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }
        catch (NonfatalException ex)
        {
            EventBus.Instance.PublishAsync(new PopupMessageArgs { Type = 4, Title = "非致命错误", Message = ex.Message });
            Debug.WriteLine(ex.ToString());
        }
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
