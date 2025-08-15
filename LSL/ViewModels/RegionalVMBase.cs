using Microsoft.Extensions.Logging;

namespace LSL.ViewModels
{
    public abstract class RegionalVMBase : ViewModelBase
    {
        protected readonly AppStateLayer AppState;
        protected readonly ServiceConnector Connector;
        protected readonly ILogger _logger;
        protected RegionalVMBase(AppStateLayer appState, ServiceConnector connector)
        {
            AppState = appState;
            //SetupRxSubscripions();
            Connector = connector;
            var t = GetType();
            _logger = appState.LoggerFactory.CreateLogger(t);
            _logger.LogDebug("Logger of {TypeName} initialized", t.Name);
        }
        //protected virtual void SetupRxSubscripions() { }
    }
}
