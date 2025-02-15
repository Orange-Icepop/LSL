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
        protected RegionalVMBase(AppStateLayer appState)
        {
            AppState = appState;
            SetupRxSubscripions();
        }
        protected virtual void SetupRxSubscripions() { }
    }
}
