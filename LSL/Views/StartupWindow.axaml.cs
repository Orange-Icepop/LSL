using System.Reactive;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using LSL.ViewModels;
using ReactiveUI;

namespace LSL.Views;

public partial class StartupWindow : ReactiveWindow<InitializationVM>
{
    public StartupWindow()
    {
        InitializeComponent();
        this.WhenActivated(action =>
        {
            action(this.ViewModel!.AppState.ITAUnits.PopupITA.RegisterHandler(HandlePopup));
            action(this.ViewModel!.AppState.ITAUnits.NotifyITA.RegisterHandler(HandleNotify));
        });
    }
    #region 弹窗
    private async Task HandlePopup(IInteractionContext<InvokePopupArgs, PopupResult> interaction)
    {
        var args = interaction.Input;
        var dialog = new PopupWindow(args.PType, args.PTitle, args.PContent);
        var result = await dialog.ShowDialog<PopupResult>(this);
        interaction.SetOutput(result);
    }
    #endregion

    private void HandleNotify(IInteractionContext<NotifyArgs, Unit> interaction)
    {
        interaction.SetOutput(Unit.Default);
    }
}