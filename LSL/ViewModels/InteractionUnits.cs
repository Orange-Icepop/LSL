using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using LSL.Common.Models;
using ReactiveUI;

namespace LSL.ViewModels
{
    public class InteractionUnits : ViewModelBase
    {
        public Interaction<InvokePopupArgs, PopupResult> PopupInteraction { get; } = new();
        public Interaction<NotifyArgs, Unit> NotifyInteraction { get; } = new();// 0消息，1成功，2警告，3错误
        public Interaction<FilePickerType, string> FilePickerInteraction { get; } = new();

        public Task<PopupResult> ThrowError(string title, string message)
        {
            return PopupInteraction.Handle(new InvokePopupArgs(PopupType.ErrorConfirm, title, message)).ToTask();
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
                ServiceResultType.Error => PopupType.ErrorConfirm,
                ServiceResultType.FinishWithWarning => PopupType.WarningConfirm,
                _ => PopupType.ErrorConfirm
            };
            await PopupInteraction.Handle(new InvokePopupArgs(level, "服务错误", fin));
        }

        public void Notify(int type, string? title, string? message)
        {
            NotifyInteraction.Handle(new NotifyArgs(type, title, message)).Subscribe();
        }

        public async Task WaitNotify(int type, string? title, string? message) =>
            await NotifyInteraction.Handle(new NotifyArgs(type, title, message));
    }
}