using Microsoft.Extensions.Logging;

namespace LSL.ViewModels
{
    /// <summary>
    /// Base class of view models for specific parts of the view.
    /// </summary>
    /// <typeparam name="T">Type of the child class, used for creating ILogger.</typeparam>
    public abstract class RegionalViewModelBase<T> : ViewModelBase where T : RegionalViewModelBase<T>
    {
        protected readonly AppStateLayer AppState;
        protected readonly ServiceConnector Connector;
        protected RegionalViewModelBase(AppStateLayer appState, ServiceConnector connector) : base(appState.LoggerFactory.CreateLogger<T>())
        {
            AppState = appState;
            Connector = connector;
            Logger.LogDebug("Logger of {TypeName} initialized", typeof(T).Name);
        }
    }
}
