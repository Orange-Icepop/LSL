using System;
using System.Reactive;
using LSL.IPC;
using LSL.Views;
using ReactiveUI;

namespace LSL.ViewModels
{
    public class InteractionUnits : ViewModelBase
    {
        public Interaction<InvokePopupArgs, PopupResult> PopupITA { get; } = new();
        public Interaction<NotifyArgs, Unit> NotifyITA { get; } = new();// 0消息，1成功，2警告，3错误
        public Interaction<FilePickerType, string> FilePickerITA { get; } = new();

        public IObservable<PopupResult> ThrowError(string title, string message)
        {
            return PopupITA.Handle(new InvokePopupArgs(PopupType.Error_Confirm, title, message));
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

                if (error.ErrorCode == 1) NotifyITA.Handle(new NotifyArgs(3, null, fin)).Subscribe();
                else NotifyITA.Handle(new NotifyArgs(4, null, fin)).Subscribe();
            }
        }

        public void Notify(int type, string? title, string? message)
        {
            NotifyITA.Handle(new NotifyArgs(type, title, message)).Subscribe();
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