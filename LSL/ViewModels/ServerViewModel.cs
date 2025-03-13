using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    public class ServerViewModel : RegionalVMBase
    {
        public ServerViewModel(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
        {
        }
    }
    
}
