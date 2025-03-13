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
            CurrentGeneralPage = GeneralPageState.Home;
            MessageBus.Current.Listen<NavigateArgs>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args => Navigate(args));
        }

        #region 导航相关
        [Reactive] public BarState CurrentBarState { get; set; }
        [Reactive] public GeneralPageState CurrentGeneralPage { get; set; }
        [Reactive] public RightPageState CurrentRightPage { get; set; }

        private (GeneralPageState, RightPageState) _lastPage = (GeneralPageState.Undefined, RightPageState.Undefined);

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
                    _lastPage = (CurrentGeneralPage, CurrentRightPage);
                    CurrentBarState = args.BarTarget;
                }
                else if (args.BarTarget == BarState.Common && CurrentBarState == BarState.FullScreen)
                {
                    if (_lastPage != (GeneralPageState.Undefined, RightPageState.Undefined))
                    {
                        CurrentGeneralPage = _lastPage.Item1;
                        CurrentRightPage = _lastPage.Item2;
                    }
                    else
                    {
                        CurrentGeneralPage = GeneralPageState.Home;
                        CurrentRightPage = RightPageState.HomeRight;
                    }
                    _lastPage = (GeneralPageState.Undefined, RightPageState.Undefined);
                }
                else CurrentBarState = args.BarTarget;
            }
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
