using System;
using System.Collections.Generic;
using Avalonia.Controls;
using System.Diagnostics;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;
using System.Threading.Tasks;
using LSL.Services;

namespace LSL.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        #region 导航相关
        //原View
        private UserControl _leftView;
        private UserControl _rightView;

        //当前View
        public string CurrentLeftView { get; set; }
        public string CurrentRightView { get; set; }

        //创建两个可变动视图
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

        //创建切换触发方法
        public ICommand LeftViewCmd { get; }
        public ICommand RightViewCmd { get; }
        //这一部分是多参数导航按钮的部分，由于设置别的VM会导致堆栈溢出且暂时没找到替代方案，所以先摆了
        //还有就是本来希望可以创建一个方法来传递两个参数的，但是太麻烦了，还是先搁置了
        public ReactiveCommand<Unit, Unit> PanelConfigCmd { get; }
        public ReactiveCommand<Unit, Unit> DownloadConfigCmd { get; }
        public ReactiveCommand<Unit, Unit> CommonConfigCmd { get; }
        #endregion

        #region 切换命令
        //左视图
        public void NavigateLeftView(string viewName)
        {
            UserControl newView = ViewFactory.CreateView(viewName);
            if (newView != null && viewName != CurrentLeftView)
            {
                if (viewName == "SettingsLeft") GetConfig();
                LeftView = newView;
                switch (viewName)
                {
                    case "HomeLeft":
                        NavigateRightView("HomeRight");
                        LeftWidth = 350;
                        break;
                    case "ServerLeft":
                        NavigateRightView("ServerStat");
                        LeftWidth = 250;
                        break;
                    case "DownloadLeft":
                        NavigateRightView("AutoDown");
                        LeftWidth = 150;
                        break;
                    case "SettingsLeft":
                        NavigateRightView("Common");
                        LeftWidth = 150;
                        break;
                }
                CurrentLeftView = viewName;
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
                EventBus.Instance.Publish(new LeftChangedEventArgs { LeftTarget = viewName });
                Debug.WriteLine("Right Page Switched:" + viewName);
            }
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