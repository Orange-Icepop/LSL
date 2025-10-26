using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using LSL.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class InitializationViewModel : ViewModelBase
{

    [Reactive] public UserControl MainWindowView { get; private set; }

    private readonly ILogger<InitializationViewModel> _logger;
    public AppStateLayer AppState { get; }
    public ShellViewModel? Shell { get; set; }

    public InitializationViewModel(ILogger<InitializationViewModel> logger, AppStateLayer appState)
    {
        _logger = logger;
        AppState = appState;
        ShowMainWindowCmd = ReactiveCommand.Create(ShowMainWindow);
        //QuitCmd = ReactiveCommand.Create(Quit);
        MainWindowView = new SplashView();
        _logger.LogInformation("Initialization VM ctor complete.");
    }

    public async Task Initialize(IServiceProvider provider)
    {
        _logger.LogInformation("===== Starting App =====");
        Shell = provider.GetRequiredService<ShellViewModel>();
        if (Shell is null) throw new Exception("ShellViewModel failed to initialize");
        await Task.Run(async () =>
        {
            await Shell.InitializeMainWindow();
            await Shell.ConfigVM.Init();
        });
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            MainWindowView = new MainView() { DataContext = Shell, ViewModel = Shell };
        });
        _logger.LogInformation("===== App started =====");
    }
    
    public ICommand ShowMainWindowCmd { get; }// 显示主窗口命令
    public ICommand QuitCmd { get; } = ReactiveCommand.Create(Quit);// 退出命令

    public static void ShowMainWindow()
    {
        MessageBus.Current.SendMessage(new ViewBroadcastArgs(typeof(MainWindow), "Show"));
    }
    public static void Quit() { Environment.Exit(0); }
}