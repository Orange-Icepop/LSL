using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSL.Services;

namespace LSL.ViewModels
{
    public class ServiceConnector
    {
        public AppStateLayer AppState { get; set; }
        public ServiceConnector(AppStateLayer appState)
        {
            AppState = appState;
        }

        public void GetConfig()
        {
            ConfigManager.LoadConfig();
            AppState.CurrentConfigs = ConfigManager.CurrentConfigs;
        }
    }
}
