using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using LSL.Common.Contracts;
using LSL.Common.Models;
using LSL.Views;
using ReactiveUI;

namespace LSL.ViewModels
{
    public class InteractionUnits : ViewModelBase
    {
        public Interaction<InvokePopupArgs, PopupResult> PopupITA { get; } = new();
        public Interaction<NotifyArgs, Unit> NotifyITA { get; } = new();// 0消息，1成功，2警告，3错误
        public Interaction<FilePickerType, string> FilePickerITA { get; } = new();

        public Task<PopupResult> ThrowError(string title, string message)
        {
            return PopupITA.Handle(new InvokePopupArgs(PopupType.Error_Confirm, title, message)).ToTask();
        }
        
        public async Task<bool> SubmitServiceError(IServiceResult result)
        {
            if (result.ErrorCode == ServiceResultType.Success) return true;
            await ShowServiceError(result);
            return result.ErrorCode != ServiceResultType.Error;
        }

        private async Task ShowServiceError(IServiceResult result)
        {
            string fin;
            if (result.Error is not null) fin = result.Error.Message;
            else return;

            var level = result.ErrorCode switch
            {
                ServiceResultType.Error => PopupType.Error_Confirm,
                ServiceResultType.FinishWithWarning => PopupType.Warning_Confirm,
                _ => PopupType.Error_Confirm
            };
            await PopupITA.Handle(new InvokePopupArgs(level, "服务错误", fin));
        }

        public void Notify(int type, string? title, string? message)
        {
            NotifyITA.Handle(new NotifyArgs(type, title, message)).Subscribe();
        }

        public async Task WaitNotify(int type, string? title, string? message) =>
            await NotifyITA.Handle(new NotifyArgs(type, title, message));
    }
}