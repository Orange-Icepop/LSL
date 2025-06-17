using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive.Linq;

namespace LSL.ViewModels
{
    public class LeftRegionVM : RegionalVMBase
    {
        public UserControl CurrentView { [ObservableAsProperty] get; }

        [Reactive] public double LeftWidth { get; set; }

        public LeftRegionVM(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
        {
            ResetHighlight();
            AppState.WhenAnyValue(AS => AS.CurrentGeneralPage)
                .Where(CV => CV != GeneralPageState.Undefined)
                .Select(CV => NavigateLeft(CV))
                .ToPropertyEx(this, t => t.CurrentView);
            AppState.WhenAnyValue(AS => AS.CurrentRightPage)
                .Where(CV => CV != RightPageState.Undefined)
                .Subscribe(CV => ChangeLeftHighlight(CV));

        }

        #region 左栏导航逻辑
        private UserControl NavigateLeft(GeneralPageState page)
        {
            if (page == GeneralPageState.Undefined) return CurrentView;
            else if (page == GeneralPageState.Empty)
            {
                LeftWidth = 0;
                return new UserControl();
            }
            else
            {
                double lw = page switch
                {
                    GeneralPageState.Home => 350,
                    GeneralPageState.Server => 250,
                    GeneralPageState.Downloads => 150,
                    GeneralPageState.Settings => 150,
                    _ => 150
                };
                LeftWidth = lw;
                return ViewFactory.CreateView(page.ToString() + "Left");
            }
        }
        #endregion

        #region 左栏高亮
        [Reactive] public bool HLServerGeneral { get; set; }
        [Reactive] public bool HLServerStat { get; set; }
        [Reactive] public bool HLServerTerminal { get; set; }
        [Reactive] public bool HLServerConf { get; set; }
        [Reactive] public bool HLAutoDown { get; set; }
        [Reactive] public bool HLManualDown { get; set; }
        [Reactive] public bool HLAddServer { get; set; }
        [Reactive] public bool HLModDown { get; set; }
        [Reactive] public bool HLCommonSettings { get; set; }
        [Reactive] public bool HLDownloadSettings { get; set; }
        [Reactive] public bool HLPanelSettings { get; set; }
        [Reactive] public bool HLStyleSettings { get; set; }
        [Reactive] public bool HLAbout { get; set; }
        private void ResetHighlight()
        {
            HLServerGeneral = false;
            HLServerStat = false;
            HLServerTerminal = false;
            HLServerConf = false;
            HLAutoDown = false;
            HLManualDown = false;
            HLAddServer = false;
            HLModDown = false;
            HLCommonSettings = false;
            HLDownloadSettings = false;
            HLPanelSettings = false;
            HLStyleSettings = false;
            HLAbout = false;
        }
        private void ChangeLeftHighlight(RightPageState rps)// 切换状态
        {
            ResetHighlight();
            switch (rps)
            {
                case RightPageState.ServerGeneral: HLServerGeneral = true; break;
                case RightPageState.ServerStat: HLServerStat = true; break;
                case RightPageState.ServerTerminal: HLServerTerminal = true; break;
                case RightPageState.ServerConf: HLServerConf = true; break;
                case RightPageState.AutoDown: HLAutoDown = true; break;
                case RightPageState.ManualDown: HLManualDown = true; break;
                case RightPageState.AddServer: HLAddServer = true; break;
                case RightPageState.ModDown: HLModDown = true; break;
                case RightPageState.CommonSettings: HLCommonSettings = true; break;
                case RightPageState.DownloadSettings: HLDownloadSettings = true; break;
                case RightPageState.PanelSettings: HLPanelSettings = true; break;
                case RightPageState.StyleSettings: HLStyleSettings = true; break;
                case RightPageState.About: HLAbout = true; break;
                default: break;
            }
        }
        #endregion
    }
}
