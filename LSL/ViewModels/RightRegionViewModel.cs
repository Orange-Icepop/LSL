using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels
{
    public class RightRegionViewModel : RegionalViewModelBase<RightRegionViewModel>
    {
        [Reactive] public UserControl CurrentView { get; set; }

        public RightRegionViewModel(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
        {
            CurrentView = null!;
            AppState.WhenAnyValue(stateLayer => stateLayer.CurrentRightPage)
                .Where(rightPageState => rightPageState != RightPageState.Undefined)
                .Select(NavigateRight)
                .Subscribe(t => CurrentView = t);
            MessageBus.Current.Listen<NavigateCommand>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(e =>
                {
                    if (e.CommandType == NavigateCommandType.Refresh)
                        CurrentView = NavigateRight(AppState.CurrentRightPage);
                });
        }

        #region 右侧视图导航逻辑
        private UserControl NavigateRight(RightPageState page)
        {
            if (page == RightPageState.Undefined) return CurrentView;
            else if (page == RightPageState.Empty) return new UserControl();
            else return ViewFactory.CreateView(page.ToString());
        }
        #endregion
    }
}
