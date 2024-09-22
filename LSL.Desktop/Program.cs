using System;
using LSL.ViewModels;
using Avalonia;
using Avalonia.ReactiveUI;
using LSL.Services;
using ReactiveUI;
using System.Diagnostics.Tracing;
using System.Diagnostics;

namespace LSL.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        /*
        try
        {
        */
            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        /*
        }
        catch (Exception ex)
        {
            EventBus.Instance.Publish(new PopupMessageArgs { Type = "deadlyError", Message = ex.Message });
            Debug.WriteLine(ex.ToString());
        }*/
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
