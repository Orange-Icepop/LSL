using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    public class AppStateLayer
    {
        public enum GeneralPageState
        {
            Home,
            Server,
            Downloads,
            Settings,
            FullScreen
        }
        public enum RightPageState
        {
            HomeRight,
            //Server
            ServerGeneral,
            ServerStat,
            ServerTerminal,
            ServerConf,
            //Downloads
            AutoDown,
            ManualDown,
            AddServer,
            ModDown,
            //Settings
            Common,
            DownloadSettings,
            PanelSettings,
            StyleSettings,
            About,
            //FullScreen
            EditSC,
            AddCore,
        }

        public GeneralPageState CurrentGeneralPage { get; set; }
        public string FullScreenTitle { get; set; }

        public AppStateLayer()
        {
            CurrentGeneralPage = GeneralPageState.Home;
        }
    }
}
