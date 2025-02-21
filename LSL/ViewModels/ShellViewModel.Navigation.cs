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
            if (viewName != CurrentLeftView)
            {
                CurrentLeftView = viewName;
                if (viewName == "SettingsLeft") MainVM.GetConfig();
                GeneralPageState gps = new();
                switch (viewName)
                {
                    case "HomeLeft":
                        gps = GeneralPageState.Home;
                        if (!dislink)
                            NavigateRightView("HomeRight");
                        LeftWidth = 350;
                        break;
                    case "ServerLeft":
                        gps = GeneralPageState.Server;
                        if (!dislink)
                            NavigateRightView("ServerStat");
                        LeftWidth = 250;
                        break;
                    case "DownloadLeft":
                        gps = GeneralPageState.Downloads;
                        if (!dislink)
                            NavigateRightView("AutoDown");
                        LeftWidth = 150;
                        break;
                    case "SettingsLeft":
                        gps = GeneralPageState.Settings;
                        if (!dislink)
                            NavigateRightView("Common");
                        LeftWidth = 150;
                        break;
                }
                MessageBus.Current.SendMessage(new NavigateArgs { LeftTarget = gps, RightTarget = RightPageState.Undefined});
                EventBus.Instance.Publish(new BarChangedEventArgs { NavigateTarget = viewName });//通知主要视图更改
                Debug.WriteLine("Left Page Switched:" + viewName);
            }
        }
        #endregion

        #region 右视图切换命令
        public void INavigateRight(string viewName) { NavigateRightView(viewName); }
        public void NavigateRightView(string viewName, bool force = false)
        {
            UserControl newView = ViewFactory.CreateView(viewName);
            if (newView != null && (viewName != CurrentRightView || force))
            {
                MessageBus.Current.SendMessage(new NavigateArgs { RightTarget = RightPageState.Undefined });//TODO:增加变成Enum的方法
                if (CurrentLeftView == "SettingsLeft") ConfigManager.ConfirmConfig(MainVM.ViewConfigs);
                CurrentRightView = viewName;
                EventBus.Instance.Publish(new LeftChangedEventArgs { LeftView = CurrentLeftView, LeftTarget = viewName });
                Debug.WriteLine("Right Page Switched:" + viewName);
            }
        }
        #endregion

        #region 全屏视图切换命令
        public void NavigateFullScreenView(string viewName)
        {
            double originalLeftWidth = LeftWidth;
            string originalLeftView = new string(CurrentLeftView);
            string originalRightView = new string(CurrentRightView);
            Dictionary<string, string> TitleMatcher = new()
            {
                { "AddCore", "从核心添加服务器" },
                { "EditSC", "修改服务器配置" },
            };
            AppState.FullScreenTitle = TitleMatcher.TryGetValue(viewName, out string? value) ? value : viewName;
            FullViewBackCmd = ReactiveCommand.Create(() =>
            {
                MessageBus.Current.SendMessage(new NavigateArgs {BarTarget = BarState.Common, LeftTarget = GeneralPageState.Undefined, RightTarget = RightPageState.Undefined });
                LeftWidth = originalLeftWidth;
                NavigateLeftView(originalLeftView);
                NavigateRightView(originalRightView);
                EventBus.Instance.Publish(new BarChangedEventArgs { NavigateTarget = originalLeftView });
            });
            LeftWidth = 0;
            MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.FullScreen, LeftTarget = GeneralPageState.Empty, RightTarget = RightPageState.Empty });
            if (viewName == "AddCore") MainVM.LoadNewServerConfig();
            if (viewName == "EditSC") MainVM.LoadCurrentServerConfig();
            NavigateRightView(viewName);
        }
        #endregion

        #region 右视图强制刷新命令
        public void RefreshRightView()
        {
            string original = CurrentRightView;
            NavigateRightView(original, true);
            EventBus.Instance.Publish(new ViewBroadcastArgs { Target = "ServerTerminal.axaml.cs", Message = "ScrollToEnd" });
        }
        #endregion

        #region 左栏宽度定义
        private double _leftWidth;
        public double LeftWidth
        {
            get => _leftWidth;
            set => this.RaiseAndSetIfChanged(ref _leftWidth, value);
        }
        #endregion

    }
}