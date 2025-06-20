﻿using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LSL.ViewModels;
using LSL.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Serilog;

namespace LSL;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logger = diServices.GetRequiredService<ILogger<App>>();
            logger.LogInformation("===== Starting App =====");
            var startupWindow = new StartupWindow
            {
                DataContext = startupVM,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                startupWindow.Show();
            });
            logger.LogInformation("Splash window loaded, loading main window");
            try
            {
                // 在后台线程初始化，不阻塞UI
                await Dispatcher.UIThread.InvokeAsync(() => shellVM = diServices.GetRequiredService<ShellViewModel>());
                await Task.WhenAll(
                    Task.Delay(3000),
                    startupVM.Initialize(shellVM)
                    );
                await Dispatcher.UIThread.InvokeAsync(() =>
                {                
                    // 创建并显示主窗口
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = shellVM,
                        ViewModel = shellVM
                    };
                    desktop.MainWindow.Show();
                });
                logger.LogInformation("===== App started =====");
            }
            catch (Exception ex)
            {
                // 处理初始化异常
                Debug.WriteLine(ex);
            }
            finally
            {
                // 确保启动窗口关闭
                startupWindow.Close();
            }
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
            singleViewPlatform.MainView = new MainView
            {
                DataContext = shellVM
            });
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ServicePointManager.DefaultConnectionLimit = 512;
            base.OnFrameworkInitializationCompleted();
        });
    }

    private ServiceCollection serviceDescriptors { get; }
    private ServiceProvider diServices { get; }
    private ShellViewModel shellVM { get; set; }
    private InitializationVM startupVM { get; }

    public App()
    {
        DI.InitSerilog();
        Log.Information("Building LSL DI Container");
        serviceDescriptors = new ServiceCollection();
        serviceDescriptors.AddLogging();
        serviceDescriptors.AddNetworking();
        serviceDescriptors.AddConfigManager();
        serviceDescriptors.AddServerHost();
        serviceDescriptors.AddStartUp();
        serviceDescriptors.AddViewModels();
        diServices = serviceDescriptors.BuildServiceProvider();
        Log.Information("DI Completed");
        startupVM = diServices.GetRequiredService<InitializationVM>();
        this.DataContext = startupVM;
    }
}

#region 全局常量

public class Constant
{
    public const string Version = "0.08.2";
}
#endregion
