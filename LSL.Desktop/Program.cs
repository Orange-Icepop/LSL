using System;
using System.Threading;
using Avalonia;
using ReactiveUI.Avalonia;

namespace LSL.Desktop;

internal class Program
{
    // 查重防多开
    private static readonly Mutex s_desktopMutex = new(true, $"{DesktopConstant.AppName}_Mutex");

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (!s_desktopMutex.WaitOne(TimeSpan.Zero, true)) return; //TODO:使用IPC唤起窗口

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            s_desktopMutex.ReleaseMutex();
            s_desktopMutex.Dispose();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
    }
}