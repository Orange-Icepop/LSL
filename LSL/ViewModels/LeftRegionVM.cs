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
    public class LeftRegionVM : RegionalVMBase
    {
        private ObservableAsPropertyHelper<UserControl> _currentView;
        public UserControl CurrentView => _currentView.Value;

        public LeftRegionVM(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
        {
            AppState.WhenAnyValue(AS => AS.CurrentGeneralPage)
                .Where(CV => CV != GeneralPageState.Undefined)
                .Select(CV => NavigateLeft(CV))
                .ToProperty(this, t => t.CurrentView, out _currentView);
        }

        private UserControl NavigateLeft(GeneralPageState page)
        {
            if (page == GeneralPageState.Undefined) return CurrentView;
            else if (page == GeneralPageState.Empty) return new UserControl();
            else return ViewFactory.CreateView(page.ToString() + "Left");
        }
    }
}
