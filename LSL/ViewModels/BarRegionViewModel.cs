using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using LSL.Models;
using LSL.Views;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class BarRegionViewModel : RegionalViewModelBase<BarRegionViewModel>
{
    [Reactive] public UserControl CurrentView { get; private set; }

    #region 全屏顶栏板块
    [Reactive] public string FullScreenTitle { get; set; }
    [Reactive] public GeneralPageState CurrentMainPage { get; private set; }
    #endregion

    public BarRegionViewModel(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
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

    private void ChangeActiveButton(GeneralPageState state)
    {
        switch (state)
        {
            case GeneralPageState.Home or GeneralPageState.Server or GeneralPageState.Downloads
                or GeneralPageState.Settings:
                CurrentMainPage = state;
                break;
            case GeneralPageState.Empty:
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
                break;
        }
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
}