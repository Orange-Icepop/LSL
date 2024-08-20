using System;
using System.Collections.Generic;
using Avalonia.Controls;
using System.Diagnostics;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace LSL.ViewModels
{
	public partial class MainViewModel : ViewModelBase, INavigationService
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
                BarChangedPublisher.Instance.PublishMessage(viewName);//֪ͨ��������ť��ʽ����
                Debug.WriteLine("Left Page Switched:" + viewName);
            }
        }
        //����ͼ
        public void NavigateRightView(string viewName)
        {
            UserControl newView = ViewFactory.CreateView(viewName);
            if (newView != null && viewName != CurrentRightView)
            {
                RightView = newView;
                CurrentRightView = viewName;
            }
            LeftChangedPublisher.Instance.LeftPublishMessage(viewName);
            Debug.WriteLine("Right Page Switched:" + viewName);
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