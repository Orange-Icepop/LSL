using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using LSL.IPC;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels
{
    public class FormPageVM : RegionalVMBase
    {
        public FormPageVM(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
        {
            this.WhenAnyValue(FPVM => FPVM.SelectedJavaIndex)
                .Select(index =>
                {
                    if (AppState.CurrentJavaDict.TryGetValue(index, out var info))
                    {
                        return info.Path;
                    }
                    else AppState.ITAUnits.Notify(3, null, "选定的索引没有对应的Java路径");
                    return string.Empty;
                })
                .Subscribe(val => JavaPath = val);
            AppState.WhenAnyValue(AS => AS.CurrentJavaDict)
                .Select(Dict => Dict.Values)
                .Select(vals =>
                {
                    ObservableCollection<string> fin = [];
                    foreach (var val in vals)
                    {
                        fin.Add(val.Vendor + " " + val.Version + " (" + val.Path + ")");
                    }

                    return fin;
                })
                .ToPropertyEx(this, FPVM => FPVM.JavaList);
            SelectCoreCmd = ReactiveCommand.Create(async () => await SelectCore());
            AddServerCmd = ReactiveCommand.Create(async () => await AddServer());
        }

        #region 表单绑定属性

        [Reactive] [ServerNameValidator] public string ServerName { get; set; }
        [Reactive] [ServerCorePathValidator] public string CorePath { get; set; }
        [Reactive] [MinMemValidator] public string MinMem { get; set; }
        [Reactive] [MaxMemValidator] public string MaxMem { get; set; }
        [Reactive] [JavaPathValidator] public string JavaPath { get; set; }
        [Reactive] [ExtJvmValidator] public string ExtJvm { get; set; }
        private int _selectedJavaIndex = 0;

        public int SelectedJavaIndex
        {
            get => _selectedJavaIndex;
            set { this.RaiseAndSetIfChanged(ref _selectedJavaIndex, value); }
        }

        #endregion

        public ObservableCollection<string> JavaList { [ObservableAsProperty] get; }
        public ICommand SelectCoreCmd { get; }
        public ICommand AddServerCmd { get; }

        private async Task SelectCore()
        {
            string result = await AppState.ITAUnits.FilePickerITA.Handle(Views.FilePickerType.CoreFile);
            await Dispatcher.UIThread.InvokeAsync(() => { CorePath = result; });
        }
        
        #region 新增服务器逻辑
        private async Task AddServer()
        {
            FormedServerConfig ServerInfo = new(ServerName, CorePath, MinMem, MaxMem, JavaPath, ExtJvm);
            var vResult = Connector.ValidateNewServerConfig(ServerInfo);
            if (vResult.Item1 == 0)
            {
                if (vResult.Item2 is not null) await AppState.ITAUnits.ThrowError("表单错误", vResult.Item2);
                return;
            }
            if (vResult is { Item1: -1, Item2: not null })
            {
                var confirm =
                    await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Info_YesNo, "未知的Minecraft核心文件", vResult.Item2));
                if (confirm == PopupResult.No) return;
            }

            if (int.Parse(ServerInfo.MaxMem) < 512)
            {
                var confirm =
                    await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Info_YesNo, "内存可能不足", "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃" + Environment.NewLine + "确定要继续吗？"));
                if (confirm == PopupResult.No) return;
            }
            var coreType = Connector.GetCoreType(ServerInfo.CorePath);
            var confirmResult = await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Info_YesNo, "确定添加此服务器吗？",
                $"服务器信息：\r名称：{ServerInfo.ServerName}\rJava路径：{ServerInfo.JavaPath}\r核心文件路径：{ServerInfo.CorePath}\r服务器类型：{coreType}\r内存范围：{ServerInfo.MinMem} ~ {ServerInfo.MaxMem}\r附加JVM参数：{ServerInfo.ExtJvm}"));
            if (confirmResult != PopupResult.Yes) return;
            else
            {
                var success = Connector.AddServer(ServerInfo);
                if (success)
                {
                    AppState.ITAUnits.Notify(1, null, "服务器配置成功！");
                    MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FS2Common));
                }
            }
        }
        #endregion
        
    }
}