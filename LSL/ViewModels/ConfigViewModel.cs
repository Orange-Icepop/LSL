using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    public class ConfigViewModel : RegionalVMBase
    {
        public ConfigViewModel(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
        {
            cachedConfig = AppState.CurrentConfigs;
        }

        private Dictionary<string, object> cachedConfig;

    }
}
