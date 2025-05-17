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
        public UserControl CurrentView { [ObservableAsProperty] get; }

        public RightRegionVM(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
        {
            AppState.WhenAnyValue(AS => AS.CurrentRightPage)
                .Where(CV => CV != RightPageState.Undefined)
                .Select(CV => NavigateRight(CV))
                .ToPropertyEx(this, t => t.CurrentView);
            MessageBus.Current.Listen<NavigateCommand>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(e =>
                {
                    if (e.CommandType == NavigateCommandType.Refresh)
                        MessageBus.Current.SendMessage(new NavigateArgs { BarTarget = BarState.Undefined, LeftTarget = GeneralPageState.Undefined, RightTarget = AppState.CurrentRightPage });
                });
        }

        #region 右侧视图导航逻辑
        public UserControl NavigateRight(RightPageState page)
        {
            if (page == RightPageState.Undefined) return CurrentView;
            else if (page == RightPageState.Empty) return new UserControl();
            else return ViewFactory.CreateView(page.ToString());
        }
        #endregion
    }
}
