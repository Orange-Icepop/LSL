using System;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LSL.ViewModels;
using LSL.Views;
using Microsoft.Extensions.DependencyInjection;
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
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logger = _diServices.GetRequiredService<ILogger<App>>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = _startupViewModel,
                ViewModel = _startupViewModel,
            };
            desktop.MainWindow.Show();
            _ = _startupViewModel.Initialize(_diServices).ContinueWith(t=>
            {
                if (!t.IsFaulted) return;
                logger.LogError(t.Exception, "An error occured while initializing the application.");
                Environment.Exit(1);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            ShellViewModel? shellVM = null;
            Dispatcher.UIThread.Invoke(() =>
                singleViewPlatform.MainView = new SplashView());
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                shellVM = _diServices.GetRequiredService<ShellViewModel>();
                return shellVM;
            }).GetTask().ContinueWith(prevTask =>
            {
                if (prevTask.IsFaulted || prevTask.Result == null)
                {
                    throw new Exception("ShellViewModel failed to initialize");
                }

                shellVM = prevTask.Result;
                return Task.WhenAll(
                    Task.Delay(3000),
                    shellVM.InitializeMainWindow(),
                    shellVM.ConfigVM.Init()
                );
            }).Unwrap().ContinueWith(prevTask =>
            {
                if (prevTask.IsFaulted)
                {
                    throw prevTask.Exception;
                }

                Dispatcher.UIThread.Invoke(() =>
                {
                    singleViewPlatform.MainView = new MainView
                    {
                        DataContext = shellVM,
                    };
                });
            }, TaskScheduler.FromCurrentSynchronizationContext()).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // 处理初始化异常
                    s_logger.Error(task.Exception);
                    Environment.Exit(1);
                }
            });
        }
        ServicePointManager.DefaultConnectionLimit = 512;
        base.OnFrameworkInitializationCompleted();
    }
    #endregion

    private readonly ServiceProvider _diServices;
    private readonly InitializationViewModel _startupViewModel;
    private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();
    public App()
    {
        s_logger.Info("Building LSL DI Container");
        var serviceDescriptors = new ServiceCollection();
        serviceDescriptors.AddLogging();
        serviceDescriptors.AddNetworking();
        serviceDescriptors.AddConfigManager();
        serviceDescriptors.AddServerHost();
        serviceDescriptors.AddStartUp();
        serviceDescriptors.AddViewModels();
        _diServices = serviceDescriptors.BuildServiceProvider();
        s_logger.Info("DI Completed");
        _startupViewModel = _diServices.GetRequiredService<InitializationViewModel>();
        this.DataContext = _startupViewModel;
    }
}

#region 全局常量

public static class DesktopConstant
{
    public const string AppName = "Orllow_LSL_Desktop";
    public const string Version = "0.09";
}
#endregion
