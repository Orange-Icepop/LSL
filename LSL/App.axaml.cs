using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Text.Json;
using System.Net;
using LSL.ViewModels;
using LSL.Views;
using static LSL.Components.MyCard;
using LSL.Services;
using System.Threading;
using System.Windows.Input;
using ReactiveUI;

namespace LSL;
public partial class App : Application
{
    private Window mainWindow;
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
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }
        ServicePointManager.DefaultConnectionLimit = 512;
        base.OnFrameworkInitializationCompleted();
    }

    public App()
    {
        this.DataContext = new MainViewModel();
    }

}
