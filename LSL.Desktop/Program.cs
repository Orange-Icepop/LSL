using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.ReactiveUI;

namespace LSL.Desktop;

class Program
{
        
    // 查重防多开
    private static Mutex desktopMutex { get; } = new(true, $"{DesktopConstant.AppName}_Mutex");

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (!desktopMutex.WaitOne(TimeSpan.Zero, true))
        {
            return;//TODO:使用IPC唤起窗口
        }
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            desktopMutex.ReleaseMutex();
            desktopMutex.Dispose();
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