using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
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
        
        public async Task<bool> SubmitServiceError(ServiceError error)
        {
            if (error.ErrorCode == 0) return false;
            await ShowServiceError(error);
            return error.ErrorCode == 1;
        }

        private async Task ShowServiceError(ServiceError error)
        {
            string fin;
            if (error.Message is not null) fin = error.Message;
            else if (error.Error is not null) fin = error.Error.Message;
            else return;

            var level = error.ErrorCode switch
            {
                1 => PopupType.Error_Confirm,
                2 => PopupType.Warning_Confirm,
                _ => PopupType.Error_Confirm
            };
            await PopupITA.Handle(new InvokePopupArgs(level, "服务错误", fin));
        }

        public void Notify(int type, string? title, string? message)
        {
            NotifyITA.Handle(new NotifyArgs(type, title, message)).Subscribe();
        }
    }
}