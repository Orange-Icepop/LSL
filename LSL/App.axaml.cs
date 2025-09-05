using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LSL.ViewModels;
using LSL.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using NLog;

namespace LSL;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    #region 初始化窗口
    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logger = diServices.GetRequiredService<ILogger<App>>();
            logger.LogInformation("===== Starting App =====");
            desktop.MainWindow = new MainWindow
            {
                DataContext = startupVM,
                ViewModel = startupVM,
            };
            await startupVM.Initialize(diServices);
            logger.LogInformation("===== App started =====");
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            ShellViewModel? shellVM = null;
            await Dispatcher.UIThread.InvokeAsync(() => 
            singleViewPlatform.MainView = new SplashView()
            {
                DataContext = startupVM,
            });
            try
            {
                // 在后台线程初始化，不阻塞UI
                await Dispatcher.UIThread.InvokeAsync(() => shellVM = diServices.GetRequiredService<ShellViewModel>());
                if (shellVM is null) throw new Exception("ShellViewModel failed to initialize");
                await Task.WhenAll(
                    Task.Delay(3000),
                    shellVM.InitializeMainWindow(),
                    shellVM.ConfigVM.Init()
                );
                await Dispatcher.UIThread.InvokeAsync(() => 
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = shellVM,
                });
            }
            catch (Exception ex)
            {
                // 处理初始化异常
                Log.Error(ex);
                Environment.Exit(1);
            }
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ServicePointManager.DefaultConnectionLimit = 512;
            base.OnFrameworkInitializationCompleted();
        });
    }
    #endregion
    
    private ServiceCollection serviceDescriptors { get; }
    private ServiceProvider diServices { get; }
    private InitializationVM startupVM { get; }
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public App()
    {
        Log.Info("Building LSL DI Container");
        serviceDescriptors = new ServiceCollection();
        serviceDescriptors.AddLogging();
        serviceDescriptors.AddNetworking();
        serviceDescriptors.AddConfigManager();
        serviceDescriptors.AddServerHost();
        serviceDescriptors.AddStartUp();
        serviceDescriptors.AddViewModels();
        diServices = serviceDescriptors.BuildServiceProvider();
        Log.Info("DI Completed");
        startupVM = diServices.GetRequiredService<InitializationVM>();
        this.DataContext = startupVM;
    }
}

#region 全局常量

public static class DesktopConstant
{
    public const string AppName = "Orllow_LSL_Desktop";
    public const string Version = "0.09";
}
#endregion
