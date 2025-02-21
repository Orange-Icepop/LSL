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
        public AppStateLayer AppState { get; }
        public ServiceConnector ServeCon { get; }
        public MainViewModel MainVM { get; }
        public BarRegionVM BarVM { get; }
        public LeftRegionVM LeftVM { get; }
        public RightRegionVM RightVM { get; }
        public ShellViewModel()
        {
            AppState = new AppStateLayer();
            ServeCon = new ServiceConnector(AppState);
            MainVM = new MainViewModel(AppState);
            BarVM = new BarRegionVM(AppState, ServeCon);
            LeftVM = new LeftRegionVM(AppState, ServeCon);
            RightVM = new RightRegionVM(AppState, ServeCon);
            EventBus.Instance.Subscribe<ClosingArgs>(QuitHandler);

            // 视图命令
            LeftViewCmd = ReactiveCommand.Create<string>(INavigateLeft);
            RightViewCmd = ReactiveCommand.Create<string>(INavigateRight);
            FullViewCmd = ReactiveCommand.Create<string>(NavigateFullScreenView);
            FullViewBackCmd = ReactiveCommand.Create(() => MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common }));
            ShowMainWindowCmd = ReactiveCommand.Create(ShowMainWindow);
            QuitCmd = ReactiveCommand.Create(Quit);

            #region 多参数导航
            PanelConfigCmd = ReactiveCommand.Create(() =>
            {
                NavigateLeftView("SettingsLeft", true);
                NavigateRightView("PanelSettings");
            });
            DownloadConfigCmd = ReactiveCommand.Create(() =>
            {
                NavigateLeftView("SettingsLeft", true);
                NavigateRightView("DownloadSettings");
            });
            CommonConfigCmd = ReactiveCommand.Create(() =>
            {
                NavigateLeftView("SettingsLeft", true);
                NavigateRightView("Common");
            });
            #endregion
        }

        //初始化主窗口
        public void InitializeMainWindow()
        {
            MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Home, RightTarget = RightPageState.HomeRight });
            NavigateLeftView("HomeLeft");
            NavigateRightView("HomeRight");
            LeftWidth = 350;
        }


        public void QuitHandler(ClosingArgs args)// 退出事件处理
        {
            ConfigManager.ConfirmConfig(MainVM.ViewConfigs);
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
