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
        public void NavigateLeftView(string viewName, bool dislink = false)
        {
            GeneralPageState gps = GeneralPageState.Undefined;
            RightPageState rps = RightPageState.Undefined;
            switch (viewName)
            {
                case "HomeLeft":
                    gps = GeneralPageState.Home;
                    if (!dislink)
                        rps = RightPageState.HomeRight;
                    break;
                case "ServerLeft":
                    gps = GeneralPageState.Server;
                    if (!dislink)
                        rps = RightPageState.ServerGeneral;
                    break;
                case "DownloadsLeft":
                    gps = GeneralPageState.Downloads;
                    if (!dislink)
                        rps = RightPageState.AutoDown;
                    break;
                case "SettingsLeft":
                    gps = GeneralPageState.Settings;
                    if (!dislink)
                        rps = RightPageState.Common;
                    break;
            }
            NavigateToPage(gps, rps);
        }
        #endregion

        #region ����ͼ�л�����
        public void NavigateRightView(string viewName, bool force = false)
        {
            if (Enum.TryParse<RightPageState>(viewName, out var RV))
            {
                NavigateToPage(GeneralPageState.Undefined, RV, force);
            }
            else Debug.WriteLine("Unknown right page name");
        }
        #endregion

        #region ��ͼ�л�����
        public void NavigateToPage(GeneralPageState gps, RightPageState rps, bool force = false)
        {
            if (gps == AppState.CurrentGeneralPage && !force) return;
            else if (rps == AppState.CurrentRightPage && !force) return;
            else
            {
                if (AppState.CurrentGeneralPage == GeneralPageState.Settings) ConfigVM.ConfirmConfig();
                if (gps == GeneralPageState.Settings) ConfigVM.GetConfig();
                MessageBus.Current.SendMessage(new NavigateArgs { LeftTarget = gps, RightTarget = rps });
                Debug.WriteLine("Page Switched:" + gps.ToString() + ", " + rps.ToString());
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