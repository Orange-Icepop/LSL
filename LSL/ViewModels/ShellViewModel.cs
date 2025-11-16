using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace LSL.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    public static string Version => DesktopConstant.Version;
    public AppStateLayer AppState { get; }
    public ServiceConnector ServeCon { get; }
    public BarRegionViewModel BarViewModel { get; }
    public LeftRegionViewModel LeftViewModel { get; }
    public RightRegionViewModel RightViewModel { get; }
    public ConfigViewModel ConfigVM { get; }
    public MonitorViewModel MonitorVM { get; }
    public ServerViewModel ServerVM { get; }
    public FormPageViewModel FormViewModel { get; }
    public PublicCommand PublicCmd { get; }
        
    // 弹窗交互，这玩意儿和上面的东西没关系
    public InteractionUnits InteractionSocket { get; }

    public ShellViewModel(
        AppStateLayer appState,
        ServiceConnector connector,
        BarRegionViewModel barViewModel,
        LeftRegionViewModel leftViewModel,
        RightRegionViewModel rightViewModel,
        ConfigViewModel configVM,
        MonitorViewModel monitorVM,
        ServerViewModel serverVM,
        FormPageViewModel formViewModel,
        PublicCommand publicCommand,
        InteractionUnits ita
    ) : base(appState.LoggerFactory.CreateLogger<ShellViewModel>())
    {
        AppState = appState;
        ServeCon = connector;
        BarViewModel = barViewModel;
        LeftViewModel = leftViewModel;
        RightViewModel = rightViewModel;
        ConfigVM = configVM;
        MonitorVM = monitorVM;
        ServerVM = serverVM;
        FormViewModel = formViewModel;
        PublicCmd = publicCommand;
        InteractionSocket = ita;

        // 视图命令
        LeftViewCmd = ReactiveCommand.CreateFromTask<string>(async param => await NavigateLeftView(param));
        RightViewCmd = ReactiveCommand.CreateFromTask<string>(async param => await NavigateRightView(param));
        FullViewCmd = ReactiveCommand.Create<string>(NavigateFullScreenView);
        FullViewBackCmd = ReactiveCommand.Create(() => MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Undefined, RightTarget = RightPageState.Undefined }));

        #region 多参数导航

        ServerConfigCmd = ReactiveCommand.CreateFromTask(async () => await NavigateToPage(GeneralPageState.Server, RightPageState.ServerConf));
        PanelConfigCmd = ReactiveCommand.CreateFromTask(async () => await NavigateToPage(GeneralPageState.Settings, RightPageState.PanelSettings));
        DownloadConfigCmd = ReactiveCommand.CreateFromTask(async () => await NavigateToPage(GeneralPageState.Settings, RightPageState.DownloadSettings));
        CommonConfigCmd = ReactiveCommand.CreateFromTask(async () => await NavigateToPage(GeneralPageState.Settings, RightPageState.CommonSettings));
        #endregion

        MessageBus.Current.Listen<WindowOperationArgs>()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(args => args.Body is WindowOperationArgType.CheckForClose)
            .Select(_ => Observable.FromAsync(PreExitOperations))
            .Switch()
            .Subscribe(
                onNext: result =>
                {
                    switch (result)
                    {
                        case 1: MessageBus.Current.SendMessage(new WindowOperationArgs(WindowOperationArgType.Hide)); break;
                        case 0: MessageBus.Current.SendMessage(new WindowOperationArgs(WindowOperationArgType.ConfirmClose)); break;
                    }
                },
                onError: ex =>
                {
                    Logger.LogError(ex, "An error occured while processing window exit operation.");
                    InteractionSocket.PopupInteraction.Handle(new InvokePopupArgs(PopupType.ErrorConfirm, "窗口退出处理错误", $"LSL在处理退出操作时出现了错误。\n{ex}"));
                }
            );
    }

    #region 生命周期
    //主窗口初始化完毕后的操作
    public async Task InitializeMainWindow()
    {
        MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Home, RightTarget = RightPageState.HomeRight });
        await DoStartUp();
    }

    private async Task DoStartUp()
    {
        await Task.WhenAll(
            AutoCheckUpdates()
        );
    }
    private async Task AutoCheckUpdates()
    {
        if (AppState.CurrentConfigs.TryGetValue("auto_update", out var autoUpdate) && autoUpdate is true)
        {
            await ServeCon.CheckForUpdates();
        }
    }
    private async Task<int> PreExitOperations()// 退出事件处理
    {
        await ServeCon.SaveConfig();
        bool daemon = (AppState.CurrentConfigs.TryGetValue("daemon", out var daemonObj) && bool.TryParse(daemonObj.ToString(), out var daemonConf)) && daemonConf;
        if (daemon) return 1;
        if (AppState.CurrentConfigs.TryGetValue("end_server_when_close", out var eswc) && eswc is true)
        {
            ServeCon.EndAllServers();
        }
        else if (AppState.RunningServerCount > 0)
        {
            var res = await AppState.InteractionUnits.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningYesNoCancel,
                "是否要关闭所有服务器？",
                "你正在尝试关闭LSL，但是有服务器正在运行。\n点击是将立刻关闭所有服务器；\n点击否将让这些服务器进程在后台运行，并且LSL不再管理它们；\n点击取消以取消关闭LSL的操作。"));
            switch (res)
            {
                case PopupResult.Yes:
                {
                    ServeCon.EndAllServers();
                    return 0;
                }
                case PopupResult.No: return 0;
                case PopupResult.Cancel:
                default: return -1;
            }
        }
        return 0;
    }
    #endregion
}