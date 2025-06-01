using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LSL.IPC;
using LSL.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace LSL.ViewModels;

public class InitializationVM : ViewModelBase
{
    private ILogger<InitializationVM> _logger { get; }
    private ServerHost daemonHost { get; }
    private ServerOutputStorage outputStorage { get; }
    public AppStateLayer AppState { get; }
    public ShellViewModel Shell { get; set; }

    public InitializationVM(ILogger<InitializationVM> logger, ServerHost daemon, ServerOutputStorage optStorage, AppStateLayer appState)
    {
        _logger = logger;
        daemonHost = daemon;
        outputStorage = optStorage;
        AppState = appState;
        ShowMainWindowCmd = ReactiveCommand.Create(ShowMainWindow);
        QuitCmd = ReactiveCommand.Create(Quit);
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
        MessageBus.Current.SendMessage(new ViewBroadcastArgs { Target = "MainWindow.axaml.cs", Message = "Show" });
    }
    public static void Quit() { Environment.Exit(0); }
}