using LSL.ViewModels;
using ReactiveUI.Avalonia;

namespace LSL.Views;

public partial class MessageDialog : ReactiveUserControl<InitializationViewModel>
{
    public MessageDialog()
    {
        InitializeComponent();
    }
}