using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using LSL.Services.ConfigServices;
using LSL.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class InitializationViewModel : ViewModelBase
{
    [Reactive] public UserControl MainWindowView { get; private set; }

    public AppStateLayer AppState { get; }
    public ShellViewModel? Shell { get; private set; }
    public DialogViewModel DialogModel { get; }

    public InitializationViewModel(ILogger<InitializationViewModel> logger, AppStateLayer appState,
        DialogViewModel dialogModel) : base(logger)
    {
        MessageBus.Current.Listen<ViewModelFatalError>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ex => _ = OnFatalErrorReceived(ex));
        AppState = appState;
        DialogModel = dialogModel;
        ShowMainWindowCmd = ReactiveCommand.Create(ShowMainWindow);
        TrayQuitCmd = ReactiveCommand.Create(TrayCalledQuit);
        MainWindowView = new SplashView();
        Logger.LogInformation("Initialization VM ctor complete.");
    }

    public async Task Initialize(IServiceProvider provider)
    {
        Logger.LogInformation("===== Starting App =====");
        var configMgr = provider.GetRequiredService<ConfigManager>(); 
        var res = await configMgr.Initialize();
        if (res.IsError)
        {
            Logger.LogCritical(res.Error, "Config initialization failed.");
            throw new Exception("Config initialization failed.", res.Error);
        }
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
        Logger.LogInformation("===== App started =====");
    }

    public ICommand ShowMainWindowCmd { get; } // 显示主窗口命令
    public ICommand TrayQuitCmd { get; } // 退出命令

    public static void ShowMainWindow()
    {
        MessageBus.Current.SendMessage(new WindowOperationArgs(WindowOperationArgType.Show));
    }

    private static void TrayCalledQuit()
    {
        MessageBus.Current.SendMessage(new WindowOperationArgs(WindowOperationArgType.RequestClose));
    }

    private async Task OnFatalErrorReceived(ViewModelFatalError error)
    {
        try
        {
            Logger.LogCritical(error.Ex, "A fatal error occured when running LSL, resulting a crash.");
            ShowMainWindow();
            await AppState.InteractionUnits.PopupInteraction.Handle(
                new InvokePopupArgs(PopupType.ErrorConfirm, "致命错误",
                    $"LSL Desktop在运行时发生了致命错误。请您确认并复制该报错信息以方便排查和上报Bug，随后LSL Desktop将自行关闭。\n{error.PopupMessage ?? error.Message}\n{error.Ex}"));
        }
        finally
        {
            Environment.Exit(1);
        }
    }
}

public record ViewModelFatalError(Exception Ex, string Message, string? PopupMessage);