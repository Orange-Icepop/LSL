using System;
using System.Reactive;
using LSL.Services;
using LSL.Views;
using ReactiveUI;

namespace LSL.ViewModels
{
    public class InteractionUnits : ReactiveObject
    {
        public Interaction<InvokePopupArgs, PopupResult> PopupITA { get; } = new();
        public Interaction<NotifyArgs, Unit> NotifyITA { get; } = new();
        public IObservable<PopupResult> ThrowError(string message)
        {
            return PopupITA.Handle(new InvokePopupArgs(PopupType.Error_Confirm, "非致命错误", message));
        }
        public void ShowServiceError(ServiceError error)
        {
            if (error.ErrorCode == 0) return;
            else 
            {
                string fin;
                if (error.Message is not null) fin = error.Message;
                else if (error.Error is not null) fin = error.Error.Message;
                else return;

                if (error.ErrorCode == 1) NotifyITA.Handle(new(3, null, fin));
                else NotifyITA.Handle(new(4, null, fin));
            }
        }
    }
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
    public record InvokePopupArgs(PopupType PType, string PTitle, string PContent);

}
