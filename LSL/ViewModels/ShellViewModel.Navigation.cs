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
        //��ǰView
        public string CurrentLeftView { get; set; }
        public string CurrentRightView { get; set; }

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

        #region ����ͼ�л�����
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

        #region ȫ����ͼ�л�����
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

        #region ����ͼǿ��ˢ������
        public void RefreshRightView()
        {
            var original = AppState.CurrentRightPage;
            MessageBus.Current.SendMessage(new NavigateArgs { RightTarget = original });
            EventBus.Instance.Publish(new ViewBroadcastArgs { Target = "ServerTerminal.axaml.cs", Message = "ScrollToEnd" });
        }
        #endregion
    }
}