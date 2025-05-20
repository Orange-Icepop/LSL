using Avalonia.Controls;
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
    public class RightRegionVM : RegionalVMBase
    {
        [Reactive] public UserControl CurrentView { get; set; }

        public RightRegionVM(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
        {
            AppState.WhenAnyValue(AS => AS.CurrentRightPage)
                .Where(CV => CV != RightPageState.Undefined)
                .Select(CV => NavigateRight(CV))
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
