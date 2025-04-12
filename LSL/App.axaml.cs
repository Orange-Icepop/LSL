using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LSL.ViewModels;
using LSL.Views;
using System.Net;

namespace LSL;
public partial class App : Application
{
    //private Window mainWindow;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = (ShellViewModel)this.DataContext
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = (ShellViewModel)this.DataContext
            };
        }
        ServicePointManager.DefaultConnectionLimit = 512;
        base.OnFrameworkInitializationCompleted();
    }

    public App()
    {
        this.DataContext = new ShellViewModel();
    }

}
