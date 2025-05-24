using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels
{
    public class AppStateLayer : ReactiveObject
    {
        public InteractionUnits ITAUnits { get; } // 为了方便把这东西放在这里了，实际上这个东西应该是全局的，但是ShellVM传到所有VM里面太麻烦了
        public IObservable<Dictionary<int,ServerConfig>> ServerConfigChanged { get; private set; }
        public IObservable<int> ServerIndexChanged { get; private set; }
        public IObservable<int> ServerIdChanged { get; private set; }

        public AppStateLayer(InteractionUnits interUnit)
        {
            ITAUnits = interUnit;
            CurrentBarState = BarState.Common;
            CurrentGeneralPage = GeneralPageState.Home;
            CurrentRightPage = RightPageState.HomeRight;
            MessageBus.Current.Listen<NavigateArgs>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(args => Navigate(args));
            MessageBus.Current.Listen<NavigateCommand>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(arg => arg.CommandType)
                .Subscribe(NavigateCommandHandler);
            // 配置公共监听属性
            ServerConfigChanged = this.WhenAnyValue(AS => AS.CurrentServerConfigs).ObserveOn(RxApp.MainThreadScheduler);
            ServerIndexChanged = this.WhenAnyValue(AS=>AS.SelectedServerIndex).ObserveOn(RxApp.MainThreadScheduler);
            ServerIdChanged = this.WhenAnyValue(AS => AS.SelectedServerId).ObserveOn(RxApp.MainThreadScheduler);
            // 监听
            ServerIndexChanged.Subscribe(_ => MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.Refresh)));
            ServerIndexChanged.Select(index => index < ServerIDs?.Count ? ServerIDs[index] : -1) 
                .ToPropertyEx(this, x => x.SelectedServerId, scheduler: RxApp.MainThreadScheduler);
            ServerConfigChanged.Select(s => new ObservableCollection<int>(s.Keys))
                .ToPropertyEx(this, x => x.ServerIDs, scheduler: RxApp.MainThreadScheduler);
            ServerConfigChanged.Select(s => new ObservableCollection<string>(s.Values.Select(v => v.name)))
                .ToPropertyEx(this, x => x.ServerNames, scheduler: RxApp.MainThreadScheduler);
            ServerConfigChanged.Subscribe(SC =>
            {
                if (SC.Count <= 0) return;
                SelectedServerIndex = 0;
                Debug.WriteLine("Selected server index reset to 0");
            });
        }

        #region 导航相关

        [Reactive] public BarState CurrentBarState { get; set; }
        [Reactive] public GeneralPageState CurrentGeneralPage { get; set; }
        [Reactive] public RightPageState CurrentRightPage { get; set; }

        private (BarState, GeneralPageState, RightPageState) LastPage = (BarState.Common, GeneralPageState.Undefined,
            RightPageState.Undefined);

        private void Navigate(BarState bar, GeneralPageState gen, RightPageState right)
        {
            Navigate(new NavigateArgs() { BarTarget = bar, LeftTarget = gen, RightTarget = right });
        }

        private void Navigate(NavigateArgs args) // ASL不负责查重操作
        {
            (BarState, GeneralPageState, RightPageState) _lastPage = (BarState.Common, CurrentGeneralPage,
                CurrentRightPage);
            if (args.LeftTarget != GeneralPageState.Undefined)
            {
                CurrentGeneralPage = args.LeftTarget;
            }

            if (args.RightTarget != RightPageState.Undefined)
            {
                CurrentRightPage = args.RightTarget;
            }

            if (args.BarTarget != BarState.Undefined)
            {
                if (args.BarTarget == BarState.FullScreen && CurrentBarState == BarState.Common)
                {
                    CurrentBarState = BarState.FullScreen;
                }
                else if (args.BarTarget == BarState.Common && CurrentBarState == BarState.FullScreen)
                {
                    CurrentBarState = BarState.Common;
                    if (LastPage != (BarState.Common, GeneralPageState.Undefined, RightPageState.Undefined))
                    {
                        CurrentBarState = LastPage.Item1;
                        CurrentGeneralPage = LastPage.Item2;
                        CurrentRightPage = LastPage.Item3;
                    }
                    else
                    {
                        CurrentGeneralPage = GeneralPageState.Home;
                        CurrentRightPage = RightPageState.HomeRight;
                    }

                    _lastPage = (BarState.Common, GeneralPageState.Undefined, RightPageState.Undefined);
                }
                else CurrentBarState = args.BarTarget;
            }

            LastPage = _lastPage;
        }

        private void NavigateCommandHandler(NavigateCommandType command)
        {
            switch (command)
            {
                case NavigateCommandType.FS2Common:
                    Navigate(LastPage.Item1, LastPage.Item2, LastPage.Item3);
                    break;
                case NavigateCommandType.Refresh:
                {
                    /*
                    var last = CurrentRightPage;
                    Navigate(BarState.Undefined, GeneralPageState.Undefined, RightPageState.Empty);
                    Navigate(BarState.Undefined, GeneralPageState.Undefined, last);*/
                    break;
                }
                default:
                    break;
            }
        }

        #endregion

        #region 配置相关

        [Reactive] public Dictionary<string, object> CurrentConfigs { get; set; } = [];
        [Reactive] public Dictionary<int, ServerConfig> CurrentServerConfigs { get; set; } = [];
        [Reactive] public Dictionary<int, JavaInfo> CurrentJavaDict { get; set; } = [];

        #endregion

        #region 选项相关

        [Reactive] public int SelectedServerIndex { get; set; } = -1;// RNMD这玩意儿死活不在启动时触发更新通知，只能先手动设置默认值强制更新了

        public int SelectedServerId { [ObservableAsProperty] get; }
        public ObservableCollection<int> ServerIDs { [ObservableAsProperty] get; }
        public ObservableCollection<string> ServerNames { [ObservableAsProperty] get; }

        #endregion

        #region 服务器相关

        [Reactive] public ConcurrentDictionary<int, ObservableCollection<ColoredLines>> TerminalTexts { get; set; } = new();

        [Reactive] public ConcurrentDictionary<int, ServerStatus> ServerStatuses { get; set; } = new();
        [Reactive] public ConcurrentDictionary<int, ObservableCollection<UUID_User>> UserDict { get; set; } = new();

        [Reactive] public ConcurrentDictionary<int, ObservableCollection<UserMessageLine>> MessageDict { get; set; } = new();

        #endregion
    }
}