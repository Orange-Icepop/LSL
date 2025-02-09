using System;
using System.Diagnostics;
using System.Reactive;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using LSL.Services;
using System.Collections.Generic;
using LSL.Views;
using System.Threading.Tasks;
using System.Threading;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {

        #region 导航相关
        //View原件
        private UserControl _leftView;
        private UserControl _rightView;
        private UserControl _barView;

        //当前View
        public string CurrentLeftView { get; set; }
        public string CurrentRightView { get; set; }

        //View访问器
        public UserControl LeftView
        {
            get => _leftView;
            set => this.RaiseAndSetIfChanged(ref _leftView, value);
        }
        public UserControl RightView
        {
            get => _rightView;
            set => this.RaiseAndSetIfChanged(ref _rightView, value);
        }
        public UserControl BarView
        {
            get => _barView;
            set => this.RaiseAndSetIfChanged(ref _barView, value);
        }

        //创建切换触发方法
        public ICommand LeftViewCmd { get; }
        public ICommand RightViewCmd { get; }
        public ICommand FullViewCmd { get; set; }
        public ICommand FullViewBackCmd { get; set; }
        //这一部分是多参数导航按钮的部分，由于设置别的VM会导致堆栈溢出且暂时没找到替代方案，所以先摆了
        //还有就是本来希望可以创建一个方法来传递两个参数的，但是太麻烦了，还是先搁置了
        public ReactiveCommand<Unit, Unit> PanelConfigCmd { get; }
        public ReactiveCommand<Unit, Unit> DownloadConfigCmd { get; }
        public ReactiveCommand<Unit, Unit> CommonConfigCmd { get; }
        #endregion

        #region 切换命令
        //左视图
        public void INavigateLeft(string viewName) { NavigateLeftView(viewName); }
        public void NavigateLeftView(string viewName, bool dislink = false)
        {
            UserControl newView = ViewFactory.CreateView(viewName);
            if (newView != null && viewName != CurrentLeftView)
            {
                CurrentLeftView = viewName;
                if (viewName == "SettingsLeft") GetConfig();
                LeftView = newView;
                switch (viewName)
                {
                    case "HomeLeft":
                        if(!dislink)
                            NavigateRightView("HomeRight");
                        LeftWidth = 350;
                        break;
                    case "ServerLeft":
                        if(!dislink)
                            NavigateRightView("ServerStat");
                        LeftWidth = 250;
                        break;
                    case "DownloadLeft":
                        if(!dislink)
                            NavigateRightView("AutoDown");
                        LeftWidth = 150;
                        break;
                    case "SettingsLeft":
                        if(!dislink)
                            NavigateRightView("Common");
                        LeftWidth = 150;
                        break;
                }
                EventBus.Instance.Publish(new BarChangedEventArgs { NavigateTarget = viewName });//通知主要视图更改
                Debug.WriteLine("Left Page Switched:" + viewName);
            }
        }
        //右视图
        public void INavigateRight(string viewName) { NavigateRightView(viewName); }
        public void NavigateRightView(string viewName, bool force = false)
        {
            UserControl newView = ViewFactory.CreateView(viewName);
            if (newView != null && (viewName != CurrentRightView || force))
            {
                RightView = newView;
                if (CurrentLeftView == "SettingsLeft") ConfigManager.ConfirmConfig(ViewConfigs);
                CurrentRightView = viewName;
                EventBus.Instance.Publish(new LeftChangedEventArgs { LeftView = CurrentLeftView, LeftTarget = viewName });
                Debug.WriteLine("Right Page Switched:" + viewName);
            }
        }
        // 全屏视图
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
            FSTitle = TitleMatcher.TryGetValue(viewName, out string? value) ? value : viewName;
            FullViewBackCmd = ReactiveCommand.Create(() =>
            {
                BarView = ViewFactory.CreateView("Bar");
                LeftWidth = originalLeftWidth;
                NavigateLeftView(originalLeftView);
                NavigateRightView(originalRightView);
                EventBus.Instance.Publish(new BarChangedEventArgs { NavigateTarget = originalLeftView });
            });
            LeftWidth = 0;
            BarView = new FSBar();
            if (viewName == "AddCore") LoadNewServerConfig();
            if (viewName == "EditSC") LoadCurrentServerConfig();
            NavigateRightView(viewName);
        }
        // 强制刷新
        public void RefreshRightView()
        {
            string original = CurrentRightView;
            NavigateRightView(original, true);
            EventBus.Instance.Publish(new ViewBroadcastArgs{ Target = "ServerTerminal.axaml.cs", Message = "ScrollToEnd" });
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