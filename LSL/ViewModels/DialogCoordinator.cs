using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentResults;
using LSL.Common.Extensions;
using LSL.Common.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace LSL.ViewModels;

public class DialogCoordinator(ILogger<DialogCoordinator> logger) : ViewModelBase(logger)
{
    public Interaction<InvokePopupArgs, PopupResult> PopupInteraction { get; } = new();
    public Interaction<NotifyArgs, Unit> NotifyInteraction { get; } = new();
    public Interaction<FilePickerType, string> FilePickerInteraction { get; } = new();

    public IObservable<PopupResult> ThrowError(string title, string message)
    {
        return PopupInteraction.Handle(new InvokePopupArgs(PopupType.ErrorConfirm, title, message));
    }

    public Task<Result<T>> SubmitServiceError<T>(Result<T> result, bool suppressWarning = false)
    {
        return suppressWarning
            ? result.Handle(null, null, exception => ShowServiceError(PopupType.ErrorConfirm, exception))
            : result.Handle(null, (_, exception) => ShowServiceError(PopupType.WarningConfirm, exception),
                exception => ShowServiceError(PopupType.ErrorConfirm, exception));
    }
    public Task<Result> SubmitServiceError(Result result, bool suppressWarning = false)
    {
        return suppressWarning
            ? result.Handle(null, null, exception => ShowServiceError(PopupType.ErrorConfirm, exception))
            : result.Handle(null, exception => ShowServiceError(PopupType.WarningConfirm, exception),
                exception => ShowServiceError(PopupType.ErrorConfirm, exception));
    }

    private async Task ShowServiceError(PopupType type, IEnumerable<Exception> errors)
    {
        var exceptions = errors.ToArray();
        await PopupInteraction.Handle(new InvokePopupArgs(type, "服务错误", exceptions.GetMessages() + "\n堆栈追踪：\n" + exceptions.FlattenToString()));
    }

    public void Notify(NotifyType type, string? title, string? message)
    {
        NotifyInteraction.Handle(new NotifyArgs(type, title, message)).Subscribe();
    }

    public async Task WaitNotify(NotifyType type, string? title, string? message)
    {
        await NotifyInteraction.Handle(new NotifyArgs(type, title, message));
    }
}