using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    public abstract class RegionalVMBase : ViewModelBase
    {
        protected readonly AppStateLayer AppState;
        protected readonly ServiceConnector Connector;
        protected RegionalVMBase(AppStateLayer appState, ServiceConnector connector)
        {
            AppState = appState;
            SetupRxSubscripions();
            Connector = connector;
        }
        protected virtual void SetupRxSubscripions() { }
    }
}
