using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        public AppStateLayer AppState { get; }
        public BarRegionVM BarVM { get; }
        public LeftRegionVM LeftVM { get; }
        public RightRegionVM RightVM { get; }
        public ShellViewModel()
        {
            AppState = new AppStateLayer();
            BarVM = new BarRegionVM(AppState);
            LeftVM = new LeftRegionVM(AppState);
            RightVM = new RightRegionVM(AppState);
        }
    }
}
