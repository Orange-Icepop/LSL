using Avalonia.Controls;
using LSL.ViewModels;

namespace LSL.Views;

public partial class MainView : UserControl
{
    public MainViewModel mainViewModel = new MainViewModel();
    public MainView()
    {
        InitializeComponent();
    }
}
