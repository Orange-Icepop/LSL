using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Text.Json;
using LSL.ViewModels;
using LSL.Views;
using static LSL.Controls.MyCard;
using LSL.Services;
using System.Threading;

namespace LSL;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //注册Daemon服务
            var cancellationTokenSource = new CancellationTokenSource();
            var daemonService = new DaemonService(cancellationTokenSource.Token);

            // 启动后台服务  
            _ = daemonService.StartAsync(); // 注意：我们使用 _ 来忽略 Task 的结果，因为我们不等待它完成


            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            /*
            // 订阅应用程序关闭事件，以便在关闭时取消后台任务  
            desktop.ApplicationStopping.Subscribe(() =>
            {
                cancellationTokenSource.Cancel();
                // 注意：我们在这里不等待后台服务完成，因为它可能会无限期地运行  
                // 如果你需要等待它完成（例如，进行清理），请确保以安全的方式处理它  
            });*/
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();

    }

}
