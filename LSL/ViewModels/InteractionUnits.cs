using LSL.Views;
using ReactiveUI;

namespace LSL.ViewModels
{
    public enum PopupType
    {
        Info_Confirm,
        Info_YesNo,
        Warning_YesNoCancel,
        Warning_YesNo,
        Error_Confirm,
    }
    public enum PopupResult
    {
        Confirm,
        Yes,
        No,
        Cancel,
    }
    public class PopupInteraction : Interaction<InvokePopupArgs, PopupResult> { }
}
