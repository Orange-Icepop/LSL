using System;
using System.Diagnostics;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using LSL.Services;
using System.Collections.Generic;
using LSL.Views;

namespace LSL.ViewModels
{
    public partial class ShellViewModel
    {

        #region 导航相关
        //当前View
        public string CurrentLeftView { get; set; }
        public string CurrentRightView { get; set; }

        //创建切换触发方法
        public ICommand LeftViewCmd { get; }
        public ICommand RightViewCmd { get; }
        public ICommand FullViewCmd { get; set; }
        public ICommand FullViewBackCmd { get; set; }
        //这一部分是多参数导航按钮的部分，由于设置别的VM会导致堆栈溢出且暂时没找到替代方案，所以先摆了
        //还有就是本来希望可以创建一个方法来传递两个参数的，但是太麻烦了，还是先搁置了
        public ICommand PanelConfigCmd { get; }
        public ICommand DownloadConfigCmd { get; }
        public ICommand CommonConfigCmd { get; }
        #endregion

        #region 左视图切换命令
        public void INavigateLeft(string viewName) { NavigateLeftView(viewName); }
        public void NavigateLeftView(string viewName, bool dislink = false)
        {
            if (viewName != AppState.CurrentGeneralPage.ToString() + "Left")
            {
                if (viewName == "SettingsLeft")
                {
                    ConfigVM.GetConfig();
                }
                else if (AppState.CurrentGeneralPage == GeneralPageState.Settings)
                {
                    ConfigVM.ConfirmConfig();
                }
                GeneralPageState gps = new();
                switch (viewName)
                {
                    case "HomeLeft":
                        gps = GeneralPageState.Home;
                        if (!dislink)
                            NavigateRightView("HomeRight");
                        break;
                    case "ServerLeft":
                        gps = GeneralPageState.Server;
                        if (!dislink)
                            NavigateRightView("ServerStat");
                        break;
                    case "DownloadsLeft":
                        gps = GeneralPageState.Downloads;
                        if (!dislink)
                            NavigateRightView("AutoDown");
                        break;
                    case "SettingsLeft":
                        gps = GeneralPageState.Settings;
                        if (!dislink)
                            NavigateRightView("Common");
                        break;
                }
                MessageBus.Current.SendMessage(new NavigateArgs { LeftTarget = gps, RightTarget = RightPageState.Undefined });
                Debug.WriteLine("Left Page Switched:" + viewName);
            }
        }
        #endregion

        #region 右视图切换命令
        public void INavigateRight(string viewName) { NavigateRightView(viewName); }
        public void NavigateRightView(string viewName, bool force = false)
        {
            if (Enum.TryParse<RightPageState>(viewName, out var RV) && (viewName != AppState.CurrentRightPage.ToString() || force))
            {
                MessageBus.Current.SendMessage(new NavigateArgs { RightTarget = RV });
                if (AppState.CurrentGeneralPage.ToString() == "Settings") ConfigManager.ConfirmConfig(MainVM.ViewConfigs);//TODO
                Debug.WriteLine("Right Page Switched:" + viewName);
            }
        }
        #endregion

        #region 全屏视图切换命令
        public void NavigateFullScreenView(string viewName)
        {
            FullViewBackCmd = ReactiveCommand.Create(() => MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Common, LeftTarget = GeneralPageState.Undefined, RightTarget = RightPageState.Undefined }));
            if (!Enum.TryParse<RightPageState>(viewName, out var RV)) return;
            else if (RV == RightPageState.AddCore || RV == RightPageState.EditSC)
            {
                MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.FullScreen, LeftTarget = GeneralPageState.Empty, RightTarget = RV });
                if (RV == RightPageState.AddCore) MainVM.LoadNewServerConfig();//TODO
                if (RV == RightPageState.EditSC) MainVM.LoadCurrentServerConfig();
                Debug.WriteLine("Successfully navigated to " + viewName);
            }
            else Debug.WriteLine("This view is not a fullscreen view: " + viewName);
        }
        #endregion

        #region 右视图强制刷新命令
        public void RefreshRightView()
        {
            var original = AppState.CurrentRightPage;
            MessageBus.Current.SendMessage(new NavigateArgs { RightTarget = original });
            EventBus.Instance.Publish(new ViewBroadcastArgs { Target = "ServerTerminal.axaml.cs", Message = "ScrollToEnd" });
        }
        #endregion
    }
}