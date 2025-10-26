using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace LSL.ViewModels;

public abstract class ViewModelBase(ILogger logger) : ReactiveObject
{
    protected readonly ILogger Logger = logger;
}
