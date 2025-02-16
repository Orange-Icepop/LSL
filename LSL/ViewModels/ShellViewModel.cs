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
        public MainViewModel MainVM { get; }
        public BarRegionVM BarVM { get; }
        public LeftRegionVM LeftVM { get; }
        public RightRegionVM RightVM { get; }
        public ShellViewModel()
        {
            AppState = new AppStateLayer();
            MainVM = new MainViewModel(AppState);
            BarVM = new BarRegionVM(AppState);
            LeftVM = new LeftRegionVM(AppState);
            RightVM = new RightRegionVM(AppState);
            EventBus.Instance.Subscribe<ClosingArgs>(QuitHandler);

            // 视图命令
            LeftViewCmd = ReactiveCommand.Create<string>(INavigateLeft);
            RightViewCmd = ReactiveCommand.Create<string>(INavigateRight);
            FullViewCmd = ReactiveCommand.Create<string>(NavigateFullScreenView);
            FullViewBackCmd = ReactiveCommand.Create(async () =>
            {
                await ShowPopup(4, "不应出现的命令错误", "当您看见该弹窗时，说明表单填充时用于返回的命令在未进入全屏表单时被触发了。如果您没有对LSL进行修改，这通常意味着LSL出现了一个Bug，请在LSL的源码仓库中提交一份关于该Bug的issue。");
            });
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
            BarView = new Bar();
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
