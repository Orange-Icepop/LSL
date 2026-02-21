using LSL.ViewModels;
using ReactiveUI.Avalonia;

namespace LSL.Views;

public partial class MainView : ReactiveUserControl<ShellViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }
}