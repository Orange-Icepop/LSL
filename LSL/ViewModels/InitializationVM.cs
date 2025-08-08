using System;
using System.Threading.Tasks;
using System.Windows.Input;
using LSL.Views;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace LSL.ViewModels;

public class InitializationVM : ViewModelBase
{
    private ILogger<InitializationVM> _logger { get; }
    public AppStateLayer AppState { get; }
    public ShellViewModel? Shell { get; set; }

    public InitializationVM(ILogger<InitializationVM> logger, AppStateLayer appState)
    {
        _logger = logger;
        AppState = appState;
        ShowMainWindowCmd = ReactiveCommand.Create(ShowMainWindow);
        QuitCmd = ReactiveCommand.Create(Quit);
        _logger.LogInformation("Initialization VM ctor complete.");
    }

    public async Task Initialize(ShellViewModel shell)
    {
        Shell = shell;
        await shell.ConfigVM.Init();
    }
    
    public ICommand ShowMainWindowCmd { get; }// 显示主窗口命令
    public ICommand QuitCmd { get; }// 退出命令

    public static void ShowMainWindow()
    {
        MessageBus.Current.SendMessage(new ViewBroadcastArgs(typeof(MainWindow), "Show"));
    }
    public static void Quit() { Environment.Exit(0); }
}