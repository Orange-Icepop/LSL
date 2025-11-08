using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class LeftRegionViewModel : RegionalViewModelBase<LeftRegionViewModel>
{
    public UserControl CurrentView { [ObservableAsProperty] get; }

    [Reactive] public double LeftWidth { get; set; }

    public LeftRegionViewModel(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
    {
        CurrentView = null!;
        ResetHighlight();
        AppState.WhenAnyValue(stateLayer => stateLayer.CurrentGeneralPage)
            .Where(generalPageState => generalPageState != GeneralPageState.Undefined)
            .Select(NavigateLeft)
            .ToPropertyEx(this, t => t.CurrentView);
        AppState.WhenAnyValue(stateLayer => stateLayer.CurrentRightPage)
            .Where(rightPageState => rightPageState != RightPageState.Undefined)
            .Subscribe(ChangeLeftHighlight);

    }

    #region 左栏导航逻辑
    private UserControl NavigateLeft(GeneralPageState page)
    {
        switch (page)
        {
            case GeneralPageState.Undefined:
                return CurrentView;
            case GeneralPageState.Empty:
                LeftWidth = 0;
                return new UserControl();
            default:
            {
                double lw = page switch
                {
                    GeneralPageState.Home => 350,
                    GeneralPageState.Server => 250,
                    _ => 150
                };
                LeftWidth = lw;
                return ViewFactory.CreateView(page + "Left");
            }
        }
    }
    #endregion

    #region 左栏高亮
    [Reactive] public bool HighLightServerGeneral { get; set; }
    [Reactive] public bool HighLightServerStat { get; set; }
    [Reactive] public bool HighLightServerTerminal { get; set; }
    [Reactive] public bool HighLightServerConf { get; set; }
    [Reactive] public bool HighLightAutoDown { get; set; }
    [Reactive] public bool HighLightManualDown { get; set; }
    [Reactive] public bool HighLightAddServer { get; set; }
    [Reactive] public bool HighLightModDown { get; set; }
    [Reactive] public bool HighLightCommonSettings { get; set; }
    [Reactive] public bool HighLightDownloadSettings { get; set; }
    [Reactive] public bool HighLightPanelSettings { get; set; }
    [Reactive] public bool HighLightStyleSettings { get; set; }
    [Reactive] public bool HighLightAbout { get; set; }
    private void ResetHighlight()
    {
        HighLightServerGeneral = false;
        HighLightServerStat = false;
        HighLightServerTerminal = false;
        HighLightServerConf = false;
        HighLightAutoDown = false;
        HighLightManualDown = false;
        HighLightAddServer = false;
        HighLightModDown = false;
        HighLightCommonSettings = false;
        HighLightDownloadSettings = false;
        HighLightPanelSettings = false;
        HighLightStyleSettings = false;
        HighLightAbout = false;
    }
    private void ChangeLeftHighlight(RightPageState rps)// 切换状态
    {
        ResetHighlight();
        switch (rps)
        {
            case RightPageState.ServerGeneral: HighLightServerGeneral = true; break;
            case RightPageState.ServerStat: HighLightServerStat = true; break;
            case RightPageState.ServerTerminal: HighLightServerTerminal = true; break;
            case RightPageState.ServerConf: HighLightServerConf = true; break;
            case RightPageState.AutoDown: HighLightAutoDown = true; break;
            case RightPageState.ManualDown: HighLightManualDown = true; break;
            case RightPageState.AddServer: HighLightAddServer = true; break;
            case RightPageState.ModDown: HighLightModDown = true; break;
            case RightPageState.CommonSettings: HighLightCommonSettings = true; break;
            case RightPageState.DownloadSettings: HighLightDownloadSettings = true; break;
            case RightPageState.PanelSettings: HighLightPanelSettings = true; break;
            case RightPageState.StyleSettings: HighLightStyleSettings = true; break;
            case RightPageState.About: HighLightAbout = true; break;
        }
    }
    #endregion
}