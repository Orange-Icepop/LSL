using Avalonia.Controls;
using LSL.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool HomeButtonClass { get; set; }
        public bool ServerButtonClass { get; set; }
        public bool DownloadButtonClass { get; set; }
        public bool SettingsButtonClass { get; set; }

        public BarRegionVM(AppStateLayer appState) : base(appState)
        {
            CurrentView = new Bar();
            HomeButtonClass = true;
            ServerButtonClass = false;
            DownloadButtonClass = false;
            SettingsButtonClass = false;
        }
    }
}
