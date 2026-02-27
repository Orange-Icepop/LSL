using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Validation.Helpers;

namespace LSL.ViewModels;

public abstract class ViewModelBase(ILogger logger) : ReactiveValidationObject
{
    protected readonly ILogger Logger = logger;
}