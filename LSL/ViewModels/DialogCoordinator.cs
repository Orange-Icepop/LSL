using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using LSL.Common;
using LSL.Common.Models;
using LSL.Common.Results;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace LSL.ViewModels;

public class DialogCoordinator(ILogger<DialogCoordinator> logger) : ViewModelBase(logger)
{
    public Interaction<InvokePopupArgs, PopupResult> PopupInteraction { get; } = new();
    public Interaction<NotifyArgs, Unit> NotifyInteraction { get; } = new();
    public Interaction<FilePickerType, string> FilePickerInteraction { get; } = new();

    public IObservable<PopupResult> ThrowError(string title, string message) =>
        PopupInteraction.Handle(new InvokePopupArgs(PopupType.ErrorConfirm, title, message));

    public Task<Result<T>> SubmitServiceError<T>(Result<T> result, bool suppressWarning = false)
    {
        return suppressWarning
            ? result.MatchAsync(null, null, exception => ShowServiceError(ResultType.Error, exception))
            : result.MatchAsync(null, (_, exception) => ShowServiceError(ResultType.Warning, exception),
                exception => ShowServiceError(ResultType.Error, exception));
    }

    private async Task ShowServiceError(ResultType type, Exception error)
    {
        PopupType level;
        switch (type)
        {
            case ResultType.Error:
                level = PopupType.ErrorConfirm;
                break;
            case ResultType.Warning:
                level = PopupType.WarningConfirm;
                break;
            default:
                return;
        }
        await PopupInteraction.Handle(new InvokePopupArgs(level, "服务错误", error.ToString()));
    }

    public void Notify(NotifyType type, string? title, string? message) => NotifyInteraction.Handle(new NotifyArgs(type, title, message)).Subscribe();

    public async Task WaitNotify(NotifyType type, string? title, string? message) =>
        await NotifyInteraction.Handle(new NotifyArgs(type, title, message));
}