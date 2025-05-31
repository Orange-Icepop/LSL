using System;
using System.Collections.Generic;
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
        private Queue<ServiceError> temp = new();
        private bool MainWindowInitialized { get; set; } = false;

        public IObservable<PopupResult> ThrowError(string title, string message)
        {
            return PopupITA.Handle(new InvokePopupArgs(PopupType.Error_Confirm, title, message));
        }
        
        public void SubmitServiceError(ServiceError error)
        {
            if (error.ErrorCode == 0) return;
            if (!MainWindowInitialized) temp.Enqueue(error);
            else ShowServiceError(error);
        }

        public void FlushServiceErrors()
        {
            MainWindowInitialized = true;
            while (temp.Count > 0)
            {
                ShowServiceError(temp.Dequeue());
            }
        }

        private void ShowServiceError(ServiceError error)
        {
            string fin;
            if (error.Message is not null) fin = error.Message;
            else if (error.Error is not null) fin = error.Error.Message;
            else return;

            if (error.ErrorCode == 1) NotifyITA.Handle(new NotifyArgs(3, "服务错误", fin)).Subscribe();
            else NotifyITA.Handle(new NotifyArgs(4, null, fin)).Subscribe();
        }

        public void Notify(int type, string? title, string? message)
        {
            NotifyITA.Handle(new NotifyArgs(type, title, message)).Subscribe();
        }
    }
}