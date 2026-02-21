using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using LSL.Models;
using LSL.Views;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace LSL.ViewModels;

public partial class BarRegionViewModel : RegionalViewModelBase<BarRegionViewModel>
{
    public BarRegionViewModel(AppStateLayer appState, ServiceConnector connector, DialogCoordinator coordinator, PublicCommand commands) : base(appState, connector, coordinator, commands)
    {
        CurrentView = new Bar();
        FullScreenTitle = string.Empty;
        AppState.WhenAnyValue(stateLayer => stateLayer.CurrentGeneralPage)
            .Where(pageState => pageState != GeneralPageState.Undefined)
            .Subscribe(ChangeActiveButton);
        AppState.WhenAnyValue(stateLayer => stateLayer.CurrentBarState)
            .Where(barState => barState != BarState.Undefined)
            .Subscribe(ChangeBarCont);
    }

    [Reactive] public partial UserControl CurrentView { get; private set; }

    private void ChangeActiveButton(GeneralPageState state)
    {
        CurrentMainPageState = state;
        if (state == GeneralPageState.Empty)
            Dispatcher.UIThread.Post(() => // 使用Post延迟操作以避免CRP未更新
            {
                FullScreenTitle = AppState.CurrentRightPage switch
                {
                    RightPageState.ServerConfEdit => "修改服务器配置",
                    RightPageState.AddCore => "从核心添加服务器",
                    RightPageState.AddFolder => "导入服务器文件夹",
                    _ => ""
                };
            });
    }

    private void ChangeBarCont(BarState state)
    {
        CurrentView = state switch
        {
            BarState.Common => new Bar(),
            BarState.FullScreen => new FullScreenBar(),
            _ => CurrentView
        };
    }

    #region 全屏顶栏板块

    [Reactive] public partial string FullScreenTitle { get; set; }
    [Reactive] public partial GeneralPageState CurrentMainPageState { get; private set; }

    #endregion
}