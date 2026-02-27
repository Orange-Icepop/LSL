using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using LSL.Models;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;

namespace LSL.ViewModels;

public partial class RightRegionViewModel : RegionalViewModelBase<RightRegionViewModel>
{
    public RightRegionViewModel(AppStateLayer appState, ServiceConnector connector, DialogCoordinator coordinator, PublicCommand commands) : base(appState, connector, coordinator, commands)
    {
        CurrentView = null!;
        AppState.WhenAnyValue(stateLayer => stateLayer.CurrentRightPage)
            .Where(rightPageState => rightPageState != RightPageState.Undefined)
            .Select(NavigateRight)
            .Subscribe(t => CurrentView = t);
        MessageBus.Current.Listen<NavigateCommand>()
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(e =>
            {
                if (e.CommandType == NavigateCommandType.Refresh)
                    CurrentView = NavigateRight(AppState.CurrentRightPage);
            });
    }

    [Reactive] public partial UserControl CurrentView { get; set; }

    #region 右侧视图导航逻辑

    private UserControl NavigateRight(RightPageState page)
    {
        if (page == RightPageState.Undefined) return CurrentView;
        if (page == RightPageState.Empty) return new UserControl();
        return ViewFactory.CreateView(page.ToString());
    }

    #endregion
}