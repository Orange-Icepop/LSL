using LSL.Services;
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
    public class AppStateLayer : ReactiveObject
    {
        public AppStateLayer()
        {
            CurrentBarState = BarState.Common;
            CurrentGeneralPage = GeneralPageState.Home;
            CurrentRightPage = RightPageState.HomeRight;
            MessageBus.Current.Listen<NavigateArgs>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args => Navigate(args));
            MessageBus.Current.Listen<NavigateCommand>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Where(args => args.Type == NavigateCommandType.FS2Common)
                .Subscribe(_ => Navigate(new NavigateArgs { BarTarget = LastPage.Item1, LeftTarget = LastPage.Item2, RightTarget = LastPage.Item3 }));
        }

        #region 导航相关
        [Reactive] public BarState CurrentBarState { get; set; }
        [Reactive] public GeneralPageState CurrentGeneralPage { get; set; }
        [Reactive] public RightPageState CurrentRightPage { get; set; }

        private (BarState, GeneralPageState, RightPageState) LastPage = (BarState.Common, GeneralPageState.Undefined, RightPageState.Undefined);

        private void Navigate(NavigateArgs args)// ASL不负责查重操作
        {
            (BarState, GeneralPageState, RightPageState) _lastPage = (BarState.Common, CurrentGeneralPage, CurrentRightPage);
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
                    CurrentBarState = BarState.FullScreen;
                }
                else if (args.BarTarget == BarState.Common && CurrentBarState == BarState.FullScreen)
                {
                    CurrentBarState = BarState.Common;
                    if (LastPage != (BarState.Common, GeneralPageState.Undefined, RightPageState.Undefined))
                    {
                        CurrentBarState = LastPage.Item1;
                        CurrentGeneralPage = LastPage.Item2;
                        CurrentRightPage = LastPage.Item3;
                    }
                    else
                    {
                        CurrentGeneralPage = GeneralPageState.Home;
                        CurrentRightPage = RightPageState.HomeRight;
                    }
                    _lastPage = (BarState.Common, GeneralPageState.Undefined, RightPageState.Undefined);
                }
                else CurrentBarState = args.BarTarget;
            }
            LastPage = _lastPage;
        }
        #endregion

        #region 配置相关
        [Reactive] public Dictionary<string, object> CurrentConfigs { get; set; } = [];
        [Reactive] public Dictionary<string, ServerConfig> CurrentServerConfigs { get; set; } = [];
        [Reactive] public Dictionary<string, JavaInfo> CurrentJavaDict { get; set; } = [];
        #endregion

        #region 选项相关
        [Reactive] public int SelectedServerIndex { get; set; }
        #endregion
    }
}
