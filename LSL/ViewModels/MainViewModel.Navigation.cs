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
        #region �������
        //ԭView
        private UserControl _leftView;
        private UserControl _rightView;

        //��ǰView
        public string CurrentLeftView { get; set; }
        public string CurrentRightView { get; set; }

        //���������ɱ䶯��ͼ
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

        //�����л���������
        public ICommand LeftViewCmd { get; }
        public ICommand RightViewCmd { get; }
        //��һ�����Ƕ����������ť�Ĳ��֣��������ñ��VM�ᵼ�¶�ջ�������ʱû�ҵ���������������Ȱ���
        //���о��Ǳ���ϣ�����Դ���һ���������������������ģ�����̫�鷳�ˣ������ȸ�����
        public ReactiveCommand<Unit, Unit> PanelConfigCmd { get; }
        public ReactiveCommand<Unit, Unit> DownloadConfigCmd { get; }
        public ReactiveCommand<Unit, Unit> CommonConfigCmd { get; }
        #endregion

        #region �л�����
        //����ͼ
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
                EventBus.Instance.Publish(new BarChangedEventArgs { NavigateTarget = viewName });//֪ͨ��Ҫ��ͼ����
                Debug.WriteLine("Left Page Switched:" + viewName);
            }
        }
        //����ͼ
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
        // ǿ��ˢ��
        public void RefreshRightView()
        {
            string original = CurrentRightView;
            NavigateRightView(original, true);
            EventBus.Instance.Publish(new ViewBroadcastArgs{ Target = "ServerTerminal.axaml.cs", Message = "ScrollToEnd" });
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