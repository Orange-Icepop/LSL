using Avalonia.ReactiveUI;
using LSL.ViewModels;

namespace LSL.Views;

public partial class MainView : ReactiveUserControl<ShellViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }
}
