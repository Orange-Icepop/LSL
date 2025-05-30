using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LSL.ViewModels;
using LSL.Views;
using Microsoft.Extensions.DependencyInjection;
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
                DataContext = shellVM
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = shellVM
            };
        }
        ServicePointManager.DefaultConnectionLimit = 512;
        base.OnFrameworkInitializationCompleted();
    }

    private ServiceCollection serviceDescriptors;
    private ShellViewModel shellVM;

    public App()
    {
        serviceDescriptors = new ServiceCollection();
        serviceDescriptors.AddLogging();
        serviceDescriptors.AddNetworking();
        serviceDescriptors.AddService();
        serviceDescriptors.AddViewModels();
        var services = serviceDescriptors.BuildServiceProvider();
        shellVM = services.GetRequiredService<ShellViewModel>();
        this.DataContext = shellVM;
    }

}
