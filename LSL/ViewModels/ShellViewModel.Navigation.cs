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

        #region �������
        //Viewԭ��
        private UserControl _leftView;
        private UserControl _rightView;
        private UserControl _barView;

        //��ǰView
        public string CurrentLeftView { get; set; }
        public string CurrentRightView { get; set; }

        //View������
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

        //�����л���������
        public ICommand LeftViewCmd { get; }
        public ICommand RightViewCmd { get; }
        public ICommand FullViewCmd { get; set; }
        public ICommand FullViewBackCmd { get; set; }
        //��һ�����Ƕ����������ť�Ĳ��֣��������ñ��VM�ᵼ�¶�ջ�������ʱû�ҵ���������������Ȱ���
        //���о��Ǳ���ϣ�����Դ���һ���������������������ģ�����̫�鷳�ˣ������ȸ�����
        public ICommand PanelConfigCmd { get; }
        public ICommand DownloadConfigCmd { get; }
        public ICommand CommonConfigCmd { get; }
        #endregion

        #region ����ͼ�л�����
        public void INavigateLeft(string viewName) { NavigateLeftView(viewName); }
        public void NavigateLeftView(string viewName, bool dislink = false)
        {
            UserControl newView = ViewFactory.CreateView(viewName);
            if (newView != null && viewName != CurrentLeftView)
            {
                CurrentLeftView = viewName;
                if (viewName == "SettingsLeft") MainVM.GetConfig();
                LeftView = newView;
                switch (viewName)
                {
                    case "HomeLeft":
                        if (!dislink)
                            NavigateRightView("HomeRight");
                        LeftWidth = 350;
                        break;
                    case "ServerLeft":
                        if (!dislink)
                            NavigateRightView("ServerStat");
                        LeftWidth = 250;
                        break;
                    case "DownloadLeft":
                        if (!dislink)
                            NavigateRightView("AutoDown");
                        LeftWidth = 150;
                        break;
                    case "SettingsLeft":
                        if (!dislink)
                            NavigateRightView("Common");
                        LeftWidth = 150;
                        break;
                }
                EventBus.Instance.Publish(new BarChangedEventArgs { NavigateTarget = viewName });//֪ͨ��Ҫ��ͼ����
                Debug.WriteLine("Left Page Switched:" + viewName);
            }
        }
        #endregion

        #region ����ͼ�л�����
        public void INavigateRight(string viewName) { NavigateRightView(viewName); }
        public void NavigateRightView(string viewName, bool force = false)
        {
            UserControl newView = ViewFactory.CreateView(viewName);
            if (newView != null && (viewName != CurrentRightView || force))
            {
                RightView = newView;
                if (CurrentLeftView == "SettingsLeft") ConfigManager.ConfirmConfig(MainVM.ViewConfigs);
                CurrentRightView = viewName;
                EventBus.Instance.Publish(new LeftChangedEventArgs { LeftView = CurrentLeftView, LeftTarget = viewName });
                Debug.WriteLine("Right Page Switched:" + viewName);
            }
        }
        #endregion

        #region ȫ����ͼ�л�����
        public void NavigateFullScreenView(string viewName)
        {
            double originalLeftWidth = LeftWidth;
            string originalLeftView = new string(CurrentLeftView);
            string originalRightView = new string(CurrentRightView);
            Dictionary<string, string> TitleMatcher = new()
            {
                { "AddCore", "�Ӻ�����ӷ�����" },
                { "EditSC", "�޸ķ���������" },
            };
            AppState.FullScreenTitle = TitleMatcher.TryGetValue(viewName, out string? value) ? value : viewName;
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
        #endregion

        #region ����ͼǿ��ˢ������
        public void RefreshRightView()
        {
            string original = CurrentRightView;
            NavigateRightView(original, true);
            EventBus.Instance.Publish(new ViewBroadcastArgs { Target = "ServerTerminal.axaml.cs", Message = "ScrollToEnd" });
        }
        #endregion

        #region ������ȶ���
        private double _leftWidth;
        public double LeftWidth
        {
            get => _leftWidth;
            set => this.RaiseAndSetIfChanged(ref _leftWidth, value);
        }
        #endregion

    }
}