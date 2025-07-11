﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Threading;
using LSL.Common.Collections;
using LSL.Common.Contracts;
using LSL.Common.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels
{
    public class AppStateLayer : ReactiveObject
    {
        public ILoggerFactory LoggerFactory { get; }
        private ILogger<AppStateLayer> Logger { get; }
        public InteractionUnits ITAUnits { get; } // 为了方便把这东西放在这里了，实际上这个东西应该是全局的，但是ShellVM传到所有VM里面太麻烦了
        public IObservable<Dictionary<int,ServerConfig>> ServerConfigChanged { get; private set; }
        public IObservable<int> ServerIndexChanged { get; private set; }
        public IObservable<int> ServerIdChanged { get; private set; }

        public AppStateLayer(InteractionUnits interUnit, ILoggerFactory loggerFactory)
        {
            ITAUnits = interUnit;
            LoggerFactory = loggerFactory;
            Logger = LoggerFactory.CreateLogger<AppStateLayer>();
            CurrentBarState = BarState.Common;
            CurrentGeneralPage = GeneralPageState.Home;
            CurrentRightPage = RightPageState.HomeRight;
            MessageBus.Current.Listen<NavigateArgs>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(Navigate);
            MessageBus.Current.Listen<NavigateCommand>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(arg => arg.CommandType)
                .Subscribe(NavigateCommandHandler);
            // 配置公共监听属性
            ServerConfigChanged = this.WhenAnyValue(AS => AS.CurrentServerConfigs).ObserveOn(RxApp.MainThreadScheduler);
            ServerIndexChanged = this.WhenAnyValue(AS => AS.SelectedServerIndex).ObserveOn(RxApp.MainThreadScheduler);
            ServerIdChanged = this.WhenAnyValue(AS => AS.SelectedServerId).ObserveOn(RxApp.MainThreadScheduler);
            
            #region 监听
            // 在索引更新时刷新右视图
            ServerIndexChanged.Subscribe(_ => MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.Refresh)));
            ServerIndexChanged.Select(index =>// 更新服务器ID
                {
                    if (ServerIDs is not null && ServerIDs.Contains(index)) return ServerIDs.ElementAt(index);
                    else return -1;
                }) 
                .ToPropertyEx(this, x => x.SelectedServerId, scheduler: RxApp.MainThreadScheduler);
            // 配置文件更新的连带更新
            ServerConfigChanged.Select(s => new ObservableCollection<int>(s.Keys))
                .ToPropertyEx(this, x => x.ServerIDs, scheduler: RxApp.MainThreadScheduler);
            ServerConfigChanged.Select(s => new ObservableCollection<string>(s.Values.Select(v => v.name)))
                .ToPropertyEx(this, x => x.ServerNames, scheduler: RxApp.MainThreadScheduler);
            ServerConfigChanged.Subscribe(SC =>
            {
                if (SC.Count <= 0) return;
                SelectedServerIndex = 0;
                Logger.LogInformation("Selected server index reset to 0");
            });
            ServerConfigChanged.Select(SC => !SC.TryGetValue(-1, out _))
                .ToPropertyEx(this, x => x.NotTemplateServer);
            ServerConfigChanged.Select(SC =>
                {
                    if (SC.Count <= 0) return 0;
                    return SC.TryGetValue(-1, out _) ? 0 : SC.Count;
                })
                .ToPropertyEx(this, x => x.TotalServerCount);
            #endregion
            
        }

        #region 导航相关

        [Reactive] public BarState CurrentBarState { get; private set; }
        [Reactive] public GeneralPageState CurrentGeneralPage { get; private set; }
        [Reactive] public RightPageState CurrentRightPage { get; private set; }

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
        public int TotalServerCount { [ObservableAsProperty] get; }
        [Reactive] public int RunningServerCount { get; set; } = 0;
        public bool NotTemplateServer { [ObservableAsProperty] get; }

        #endregion
        
        #region 性能监控相关
        [Reactive] public ConcurrentDictionary<int, MetricsStorage> MetricsDict { get; set; } = new();
        [Reactive] public RangedObservableLinkedList<uint> GeneralCpuMetrics { get; set; } = new(30, 0);
        [Reactive] public RangedObservableLinkedList<uint> GeneralRamMetrics { get; set; } = new(30, 0);
        #endregion
    }
}