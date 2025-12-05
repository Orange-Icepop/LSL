using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using LSL.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class LeftRegionViewModel : RegionalViewModelBase<LeftRegionViewModel>
{
    public UserControl CurrentView { [ObservableAsProperty] get; }

    [Reactive] public double LeftWidth { get; set; }
    [Reactive] public RightPageState CurrentRightPageState { get; private set; }

    public LeftRegionViewModel(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
    {
        CurrentView = null!;
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

    private void ChangeLeftHighlight(RightPageState rps)// 切换状态
    {
        if (rps is not (RightPageState.HomeRight or RightPageState.ServerConfEdit or RightPageState.AddCore
            or RightPageState.AddFolder or RightPageState.Empty or RightPageState.Undefined or RightPageState.Hold))
        {
            CurrentRightPageState = rps;
        }
    }
}