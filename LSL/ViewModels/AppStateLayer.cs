﻿using LSL.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    #region 页面状态枚举
    public enum BarState
    {
        Common,
        FullScreen,
        Undefined
    }
    public enum GeneralPageState
    {
        Home,
        Server,
        Downloads,
        Settings,
        FullScreen,
        Empty,
        Undefined
    }
    public enum RightPageState
    {
        HomeRight,
        //Server
        ServerGeneral,
        ServerStat,
        ServerTerminal,
        ServerConf,
        //Downloads
        AutoDown,
        ManualDown,
        AddServer,
        ModDown,
        //Settings
        Common,
        DownloadSettings,
        PanelSettings,
        StyleSettings,
        About,
        //FullScreen
        EditSC,
        AddCore,
        //Others
        Empty,
        Undefined,
        Hold
    }

    public enum NavigateCommandType
    {
        None,
        Refresh,
        FS2Common
    }
    #endregion

    public class AppStateLayer : ReactiveObject
    {
        [Reactive] public BarState CurrentBarState { get; set; }
        [Reactive] public GeneralPageState CurrentGeneralPage { get; set; }
        [Reactive] public RightPageState CurrentRightPage { get; set; }

        private Tuple<GeneralPageState, RightPageState> _lastPage;

        public AppStateLayer()
        {
            CurrentGeneralPage = GeneralPageState.Home;
            MessageBus.Current.Listen<NavigateArgs>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args => Navigate(args));
        }
        private void Navigate(NavigateArgs args)// ASL不负责查重操作
        {
            if (args.LeftTarget != GeneralPageState.Undefined)
            {
                CurrentGeneralPage = args.LeftTarget;
            }
            if (args.RightTarget != RightPageState.Undefined)
            {
                CurrentRightPage = args.RightTarget;
            }
            if (args.BarTarget != BarState.Undefined)
            {
                if (args.BarTarget == BarState.FullScreen && CurrentBarState == BarState.Common)
                {
                    _lastPage = new Tuple<GeneralPageState, RightPageState>(CurrentGeneralPage, CurrentRightPage);
                    CurrentBarState = args.BarTarget;
                }
                else if (args.BarTarget == BarState.Common && CurrentBarState == BarState.FullScreen)
                {
                    if (_lastPage != null)
                    {
                        CurrentGeneralPage = _lastPage.Item1;
                        CurrentRightPage = _lastPage.Item2;
                    }
                    else
                    {
                        CurrentGeneralPage = GeneralPageState.Home;
                        CurrentRightPage = RightPageState.HomeRight;
                    }
                }
                else CurrentBarState = args.BarTarget;
            }
        }
    }
}
