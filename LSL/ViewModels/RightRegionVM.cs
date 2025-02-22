using Avalonia.Controls;
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
    public class RightRegionVM : RegionalVMBase
    {
        private ObservableAsPropertyHelper<UserControl> _currentView;
        public UserControl CurrentView => _currentView.Value;

        public RightRegionVM(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
        {
            AppState.WhenAnyValue(AS => AS.CurrentRightPage)
                .Where(CV => CV != RightPageState.Undefined)
                .Select(CV => NavigateRight(CV))
                .ToProperty(this, t => t.CurrentView, out _currentView);
            MessageBus.Current.Listen<NavigateCommand>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(e =>
                {
                    if (e.Type == NavigateCommandType.Refresh)
                        MessageBus.Current.SendMessage(new NavigateArgs { RightTarget = AppState.CurrentRightPage });
                });
        }
        public UserControl NavigateRight(RightPageState page)
        {
            if (page == RightPageState.Undefined) return CurrentView;
            else if (page == RightPageState.Empty) return new UserControl();
            else return ViewFactory.CreateView(page.ToString());
        }
    }
}
