using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LSL.Common;
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

    public Task<PopupResult> ThrowError(string title, string message)
    {
        return PopupInteraction.Handle(new InvokePopupArgs(PopupType.ErrorConfirm, title, message)).ToTask();
    }

    public Task<Result<T>> SubmitServiceError<T>(Result<T> result, bool suppressWarning = false)
    {
        return result.TapAsync(res => ShowServiceError(res), result);
    }

    private async Task ShowServiceError<T>(Result<T> result)
    {
        string fin;
        if (result.Error is not null) fin = result.Error.ToString();
        else return;

        var level = result.Kind switch
        {
            ResultType.Error => PopupType.ErrorConfirm,
            ResultType.Warning => PopupType.WarningConfirm,
            _ => PopupType.ErrorConfirm
        };
        await PopupInteraction.Handle(new InvokePopupArgs(level, "服务错误", fin));
    }

    public void Notify(NotifyType type, string? title, string? message)
    {
        NotifyInteraction.Handle(new NotifyArgs(type, title, message)).Subscribe();
    }

    public async Task WaitNotify(NotifyType type, string? title, string? message) =>
        await NotifyInteraction.Handle(new NotifyArgs(type, title, message));

    public class ServiceResultCommitWrapper
    {
        private readonly Lazy<Task>
            _lazyTask;
        private Task Task => _lazyTask.Value;
        public TaskAwaiter GetAwaiter() => Task.GetAwaiter();
        public bool IsSuccess { get; }
        public static ServiceResultCommitWrapper Commit(Func<Task> taskFactory, Result result, bool suppressWarning = false) => new(taskFactory, result, suppressWarning);
        public static ServiceResultCommitWrapper<T> Commit<T>(Func<Task> taskFactory, Result<T> result, bool suppressWarning = false) => new(taskFactory, result, suppressWarning);
        private ServiceResultCommitWrapper(Func<Task> taskFactoryIfHasError, Result result, bool suppressWarning = false)
        {
            _lazyTask = new Lazy<Task>(result.IsSuccess || (suppressWarning && result.IsWarning)
                ? () => Task.CompletedTask
                : taskFactoryIfHasError);
            IsSuccess = !result.IsFailed;
        }
    }

    public class ServiceResultCommitWrapper<T>
    {
        private readonly Lazy<Task> _lazyTask;
        internal ServiceResultCommitWrapper(Func<Task> taskFactoryIfHasError, Result<T> result, bool suppressWarning = false)
        {
            _lazyTask = new Lazy<Task>(result.IsSuccess || (suppressWarning && result.IsWarning)
                ? () => Task.CompletedTask
                : taskFactoryIfHasError);
            IsSuccess = !result.IsFailed;
            Result = result.Value;
        }

        private Task Task => _lazyTask.Value;
        public TaskAwaiter GetAwaiter() => Task.GetAwaiter();

        [MemberNotNullWhen(true, nameof(Result))]
        public bool IsSuccess { get; }
        public T? Result { get; }
    }
}