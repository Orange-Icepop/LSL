using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace LSL.ViewModels
{
    public partial class ShellViewModel : ViewModelBase
    {
        public static string Version => Constant.Version;
        public AppStateLayer AppState { get; }
        public ServiceConnector ServeCon { get; }
        public BarRegionVM BarVM { get; }
        public LeftRegionVM LeftVM { get; }
        public RightRegionVM RightVM { get; }
        public ConfigViewModel ConfigVM { get; }
        public ServerViewModel ServerVM { get; }
        public FormPageVM FormVM { get; }
        public PublicCommand PublicCmd { get; }
        //Logger
        private ILogger<ShellViewModel> _logger { get; }
        // 弹窗交互，这玩意儿和上面的东西没关系
        public InteractionUnits ITAUnits { get; }

        public ShellViewModel(
            AppStateLayer appState,
            ServiceConnector connector,
            BarRegionVM barVM,
            LeftRegionVM leftVM,
            RightRegionVM rightVM,
            ConfigViewModel configVM,
            ServerViewModel serverVM,
            FormPageVM formVM,
            PublicCommand publicCommand,
            InteractionUnits ITA
            )
        {
            AppState = appState;
            ServeCon = connector;
            BarVM = barVM;
            LeftVM = leftVM;
            RightVM = rightVM;
            ConfigVM = configVM;
            ServerVM = serverVM;
            FormVM = formVM;
            PublicCmd = publicCommand;
            ITAUnits = ITA;
            
            _logger = appState.LoggerFactory.CreateLogger<ShellViewModel>();

            // 视图命令
            LeftViewCmd = ReactiveCommand.Create<string>(param => NavigateLeftView(param, false));
            RightViewCmd = ReactiveCommand.Create<string>(param => NavigateRightView(param, false));
            FullViewCmd = ReactiveCommand.Create<string>(NavigateFullScreenView);
            FullViewBackCmd = ReactiveCommand.Create(() => MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Undefined, RightTarget = RightPageState.Undefined }));

            #region 多参数导航
            PanelConfigCmd = ReactiveCommand.Create(() =>
            {
                NavigateToPage(GeneralPageState.Settings, RightPageState.PanelSettings);
            });
            DownloadConfigCmd = ReactiveCommand.Create(() =>
            {
                NavigateToPage(GeneralPageState.Settings, RightPageState.DownloadSettings);
            });
            CommonConfigCmd = ReactiveCommand.Create(() =>
            {
                NavigateToPage(GeneralPageState.Settings, RightPageState.Common);
            });
            #endregion
        }

        //主窗口初始化完毕后的操作
        public void InitializeMainWindow()
        {
            MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Home, RightTarget = RightPageState.HomeRight });
            NavigateLeftView("HomeLeft");
            NavigateRightView("HomeRight");
            Task.Run(AutoCheckUpdates);
        }

        private async Task AutoCheckUpdates()
        {
            if (AppState.CurrentConfigs.TryGetValue("auto_update", out var autoUpdate) && autoUpdate is bool update)
            {
                await ServeCon.CheckForUpdates();
            }
        }

        public bool CheckForExiting()// 退出事件处理
        {
            ServeCon.SaveConfig();
            if(AppState.CurrentConfigs.TryGetValue("daemon", out var value) && bool.TryParse(value.ToString(), out var res))
            {
                return res;
            }
            else return false;
        }
    }
}
