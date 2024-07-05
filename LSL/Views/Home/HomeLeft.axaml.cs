using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LSL.ViewModels;
using ReactiveUI;
using System.Reactive;

namespace LSL.Views.Home
{
    public partial class HomeLeft : UserControl
    {
        public HomeLeft()
        {
            InitializeComponent();
            //DataContext = new MainViewModel();
        }
    }
    /*public class HomeLeftViewModel
    {
        private readonly INavigationService _navigationService;
        public ReactiveCommand<Unit, Unit> PanelConfigCmd { get; set; }

        public HomeLeftViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            PanelConfigCmd = ReactiveCommand.Create(() =>
            {
                _navigationService.NavigateLeftView("SettingsLeft");
                _navigationService.NavigateRightView("PanelSettings");
            });
        }

    }*/

}

