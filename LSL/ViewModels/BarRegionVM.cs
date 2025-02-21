using Avalonia.Controls;
using LSL.Views;
using ReactiveUI;
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
        private string _FSTitle;
        public string FSTitle
        {
            get => _FSTitle;
            set => this.RaiseAndSetIfChanged(ref _FSTitle, value);
        }

        private bool _homeButtonClass;
        public bool HomeButtonClass
        {
            get => _homeButtonClass;
            private set => this.RaiseAndSetIfChanged(ref _homeButtonClass, value);
        }
        private bool _serverButtonClass;
        public bool ServerButtonClass
        {
            get => _serverButtonClass;
            private set => this.RaiseAndSetIfChanged(ref _serverButtonClass, value);
        }
        private bool _downloadButtonClass;
        public bool DownloadButtonClass
        {
            get => _downloadButtonClass;
            private set => this.RaiseAndSetIfChanged(ref _downloadButtonClass, value);
        }
        private bool _settingsButtonClass;
        public bool SettingsButtonClass
        {
            get => _settingsButtonClass;
            private set => this.RaiseAndSetIfChanged(ref _settingsButtonClass, value);
        }
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
                    FSTitle = AppState.FullScreenTitle;
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
