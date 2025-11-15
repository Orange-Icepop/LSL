using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LSL.Common.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace LSL.ViewModels;

public class InteractionUnits(ILogger<InteractionUnits> logger) : ViewModelBase(logger)
{
    public Interaction<InvokePopupArgs, PopupResult> PopupInteraction { get; } = new();
    public Interaction<NotifyArgs, Unit> NotifyInteraction { get; } = new(); // 0消息，1成功，2警告，3错误
    public Interaction<FilePickerType, string> FilePickerInteraction { get; } = new();

    public Task<PopupResult> ThrowError(string title, string message)
    {
        return PopupInteraction.Handle(new InvokePopupArgs(PopupType.ErrorConfirm, title, message)).ToTask();
    }

    public ServiceResultCommitWrapper SubmitServiceError(ServiceResult result, bool suppressWarning = false)
    {
        return ServiceResultCommitWrapper.Commit(() => ShowServiceError(result), result);
    }

    public ServiceResultCommitWrapper<T> SubmitServiceError<T>(ServiceResult<T> result, bool suppressWarning = false)
    {
        return ServiceResultCommitWrapper.Commit(() => ShowServiceError(result), result);
    }

    private async Task ShowServiceError(IServiceResult result)
    {
        string fin;
        if (result.Error is not null) fin = result.Error.ToString();
        else return;

        var level = result.ResultType switch
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

    public class ServiceResultCommitWrapper
    {
        private readonly Lazy<Task>
            _lazyTask;
        private Task Task => _lazyTask.Value;
        public TaskAwaiter GetAwaiter() => Task.GetAwaiter();
        public bool IsSuccess { get; }
        public static ServiceResultCommitWrapper Commit(Func<Task> taskFactory, ServiceResult result, bool suppressWarning = false) => new(taskFactory, result, suppressWarning);
        public static ServiceResultCommitWrapper<T> Commit<T>(Func<Task> taskFactory, ServiceResult<T> result, bool suppressWarning = false) => new(taskFactory, result, suppressWarning);
        private ServiceResultCommitWrapper(Func<Task> taskFactoryIfHasError, ServiceResult result, bool suppressWarning = false)
        {
            _lazyTask = new Lazy<Task>(result.IsSuccess || (suppressWarning && result.IsFinishedWithWarning)
                ? () => Task.CompletedTask
                : taskFactoryIfHasError);
            IsSuccess = !result.IsError;
        }
    }

    public class ServiceResultCommitWrapper<T>
    {
        private readonly Lazy<Task> _lazyTask;
        internal ServiceResultCommitWrapper(Func<Task> taskFactoryIfHasError, ServiceResult<T> result, bool suppressWarning = false)
        {
            _lazyTask = new Lazy<Task>(result.IsSuccess || (suppressWarning && result.IsFinishedWithWarning)
                ? () => Task.CompletedTask
                : taskFactoryIfHasError);
            IsSuccess = !result.IsError;
            Result = result.Result;
        }

        private Task Task => _lazyTask.Value;
        public TaskAwaiter GetAwaiter() => Task.GetAwaiter();

        [MemberNotNullWhen(true, nameof(Result))]
        public bool IsSuccess { get; }
        public T? Result { get; }
    }
}