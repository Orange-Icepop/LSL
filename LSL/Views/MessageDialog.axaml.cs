using Avalonia.ReactiveUI;
using LSL.ViewModels;

namespace LSL.Views;

public partial class MessageDialog : ReactiveUserControl<InitializationViewModel>
{
    public MessageDialog()
    {
        InitializeComponent();
    }
}