﻿using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LSL.Views;
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
        public MonitorViewModel MonitorVM { get; }
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
            MonitorViewModel monitorVM,
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
            MonitorVM = monitorVM;
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
                NavigateToPage(GeneralPageState.Settings, RightPageState.CommonSettings);
            });
            #endregion

            MessageBus.Current.Listen<WindowOperationArgs>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Where(args => args.Body is WindowOperationArgType.Raise)
                .Subscribe(async _ => await ExitHandler());
        }

        #region 生命周期
        //主窗口初始化完毕后的操作
        public void InitializeMainWindow()
        {
            MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Home, RightTarget = RightPageState.HomeRight });
            NavigateLeftView("HomeLeft");
            NavigateRightView("HomeRight");
            Task.Run(DoStartUp);
        }

        private async Task DoStartUp()
        {
            await Task.WhenAll(
                AutoCheckUpdates()
            );
        }
        private async Task AutoCheckUpdates()
        {
            if (AppState.CurrentConfigs.TryGetValue("auto_update", out var autoUpdate) && autoUpdate is bool update)
            {
                await ServeCon.CheckForUpdates();
            }
        }

        private async Task ExitHandler()
        {
            var result = await PreExitOperations();
            switch (result)
            {
                case 1: MessageBus.Current.SendMessage(new WindowOperationArgs(WindowOperationArgType.Hide)); break;
                case 0: MessageBus.Current.SendMessage(new WindowOperationArgs(WindowOperationArgType.Confirm)); break;
                default: return;
            }
        }
        private async Task<int> PreExitOperations()// 退出事件处理
        {
            await ServeCon.SaveConfig();
            bool daemon = (AppState.CurrentConfigs.TryGetValue("daemon", out var daemonObj) && bool.TryParse(daemonObj.ToString(), out var daemonConf)) && daemonConf;
            if (daemon) return 1;
            else
            {
                if (AppState.CurrentConfigs.TryGetValue("end_server_when_close", out var ESWCObj) &&
                    bool.TryParse(ESWCObj.ToString(), out var ESWC) && ESWC)
                {
                    ServeCon.EndAllServers();
                }
                else if (AppState.RunningServerCount > 0)
                {
                    var res = await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Warning_YesNoCancel,
                        "是否要关闭所有服务器？",
                        $"你正在尝试关闭LSL，但是有服务器正在运行。{Environment.NewLine}点击是将立刻关闭所有服务器；{Environment.NewLine}点击否将让这些服务器进程在后台运行，并且LSL不再管理它们；{Environment.NewLine}点击取消以取消关闭LSL的操作。"));
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
        }
        #endregion
    }
}
