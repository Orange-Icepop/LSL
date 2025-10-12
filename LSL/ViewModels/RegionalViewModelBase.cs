using Microsoft.Extensions.Logging;

namespace LSL.ViewModels
{
    public abstract class RegionalViewModelBase : ViewModelBase
    {
        protected readonly AppStateLayer AppState;
        protected readonly ServiceConnector Connector;
        protected readonly ILogger Logger;
        protected RegionalViewModelBase(AppStateLayer appState, ServiceConnector connector)
        {
            AppState = appState;
            //SetupRxSubscripions();
            Connector = connector;
            var t = GetType();
            Logger = appState.LoggerFactory.CreateLogger(t);
            Logger.LogDebug("Logger of {TypeName} initialized", t.Name);
        }
        //protected virtual void SetupRxSubscripions() { }
    }
}
