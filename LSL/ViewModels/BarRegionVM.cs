﻿using Avalonia.Controls;
using LSL.Views;
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
    public class BarRegionVM : RegionalVMBase
    {
        private UserControl _currentView;
        public UserControl CurrentView
        {
            get => _currentView;
            private set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        #region 全屏顶栏板块
        [Reactive] public string FSTitle { get; set; }
        [Reactive] public bool HomeButtonClass { get; set; }
        [Reactive] public bool ServerButtonClass { get; set; }
        [Reactive] public bool DownloadButtonClass { get; set; }
        [Reactive] public bool SettingsButtonClass { get; set; }
        #endregion

        public BarRegionVM(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
        {
            CurrentView = new Bar();
            HomeButtonClass = true;
            ServerButtonClass = false;
            DownloadButtonClass = false;
            SettingsButtonClass = false;
            FSTitle = string.Empty;
            AppState.WhenAnyValue(AS => AS.CurrentGeneralPage)
                .Where(CV => CV != GeneralPageState.Undefined)
                .Subscribe(CV => ChangeActiveButton(CV));
            AppState.WhenAnyValue(AS => AS.CurrentBarState)
                .Where(CV => CV != BarState.Undefined)
                .Subscribe(CV => ChangeBarCont(CV));
        }

        private void ChangeActiveButton(GeneralPageState state)
        {
            HomeButtonClass = false;
            ServerButtonClass = false;
            DownloadButtonClass = false;
            SettingsButtonClass = false;
            switch (state)
            {
                case GeneralPageState.Home:
                    HomeButtonClass = true;
                    break;
                case GeneralPageState.Server:
                    ServerButtonClass = true;
                    break;
                case GeneralPageState.Downloads:
                    DownloadButtonClass = true;
                    break;
                case GeneralPageState.Settings:
                    SettingsButtonClass = true;
                    break;
                case GeneralPageState.FullScreen:
                    FSTitle = AppState.CurrentRightPage switch
                    {
                        RightPageState.EditSC => "修改服务器配置",
                        RightPageState.AddCore => "从核心添加服务器",
                        _ => ""
                    };
                    break;
                default:
                    break;
            }
        }

        private void ChangeBarCont(BarState state)
        {
            switch (state)
            {
                case BarState.Common:
                    CurrentView = new Bar();
                    break;
                case BarState.FullScreen:
                    CurrentView = new FSBar();
                    break;
                default:
                    break;
            }
        }
    }
}
