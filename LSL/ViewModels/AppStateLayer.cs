using LSL.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
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
        Refresh
    }

    public enum NavigateCommandType
    {
        None,
        Refresh,
        FS2Common
    }

    public class AppStateLayer : ReactiveObject
    {
        private BarState _currentBarState;
        public BarState CurrentBarState
        {
            get => _currentBarState;
            set => this.RaiseAndSetIfChanged(ref _currentBarState, value);
        }
        private GeneralPageState _currentGeneralPage;
        public GeneralPageState CurrentGeneralPage
        {
            get => _currentGeneralPage;
            set => this.RaiseAndSetIfChanged(ref _currentGeneralPage, value);
        }
        private RightPageState _currentRightPage;
        public RightPageState CurrentRightPage
        {
            get => _currentRightPage;
            set => this.RaiseAndSetIfChanged(ref _currentRightPage, value);
        }
        private string _fullScreenTitle;
        public string FullScreenTitle
        {
            get => _fullScreenTitle;
            set => this.RaiseAndSetIfChanged(ref _fullScreenTitle, value);
        }

        private Tuple<GeneralPageState, RightPageState> _lastPage;

        public AppStateLayer()
        {
            CurrentGeneralPage = GeneralPageState.Home;
            MessageBus.Current.Listen<NavigateArgs>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args => Navigate(args));
        }
        private void Navigate(NavigateArgs args)
        {
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
            if (args.LeftTarget != GeneralPageState.Undefined)
            {
                CurrentGeneralPage = args.LeftTarget;
            }
            if (args.RightTarget != RightPageState.Undefined)
            {
                CurrentRightPage = args.RightTarget;
            }
        }
    }
}
