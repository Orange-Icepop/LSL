using System;
using Avalonia;
using Avalonia.ReactiveUI;

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
            EventBus.Instance.PublishAsync(new PopupMessageArgs { Type = "deadlyError", Message = ex.Message });
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
