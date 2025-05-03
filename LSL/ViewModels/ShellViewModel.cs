using LSL.Services;
using LSL.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSL.ViewModels
{
    public partial class ShellViewModel : ViewModelBase
    {
        private const string _version = "0.08.2";
        public static string Version => _version;
        public AppStateLayer AppState { get; }
        public ServiceConnector ServeCon { get; }
        public BarRegionVM BarVM { get; }
        public LeftRegionVM LeftVM { get; }
        public RightRegionVM RightVM { get; }
        public ConfigViewModel ConfigVM { get; }
        public ServerViewModel ServerVM { get; }
        public PublicCommand PublicCmd { get; }
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
            PublicCmd = publicCommand;
            ITAUnits = ITA;
            EventBus.Instance.Subscribe<ClosingArgs>(QuitHandler);

            // 视图命令
            LeftViewCmd = ReactiveCommand.Create<string>(param => NavigateLeftView(param, false));
            RightViewCmd = ReactiveCommand.Create<string>(param => NavigateRightView(param, false));
            FullViewCmd = ReactiveCommand.Create<string>(NavigateFullScreenView);
            FullViewBackCmd = ReactiveCommand.Create(() => MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Undefined, RightTarget = RightPageState.Undefined }));
            ShowMainWindowCmd = ReactiveCommand.Create(ShowMainWindow);
            QuitCmd = ReactiveCommand.Create(Quit);

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

        //初始化主窗口
        public void InitializeMainWindow()
        {
            MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Home, RightTarget = RightPageState.HomeRight });
            NavigateLeftView("HomeLeft");
            NavigateRightView("HomeRight");
        }


        public void QuitHandler(ClosingArgs args)// 退出事件处理
        {
            ServeCon.SaveConfig();
        }

        public ICommand ShowMainWindowCmd { get; }// 显示主窗口命令
        public ICommand QuitCmd { get; }// 退出命令

        public static void ShowMainWindow()
        {
            EventBus.Instance.Publish(new ViewBroadcastArgs { Target = "MainWindow.axaml.cs", Message = "Show" });
        }
        public static void Quit() { Environment.Exit(0); }


    }
}
