using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace LSL.ViewModels
{
    public abstract class RegionalVMBase : ViewModelBase
    {
        protected readonly AppStateLayer AppState;
        protected readonly ServiceConnector Connector;
        protected readonly ILogger Logger;
        protected RegionalVMBase(AppStateLayer appState, ServiceConnector connector)
        {
            AppState = appState;
            //SetupRxSubscripions();
            Connector = connector;
            var t = GetType();
            Logger = appState.LoggerFactory.CreateLogger(t);
            Logger.LogDebug("{TypeName}'s base initialized", t.Name);
        }
        //protected virtual void SetupRxSubscripions() { }
    }
}
