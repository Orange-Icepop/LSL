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
            EditServerCmd = ReactiveCommand.Create(async () => await EditServer());
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
            set => this.RaiseAndSetIfChanged(ref _selectedJavaIndex, value);
        }

        #endregion

        public ObservableCollection<string> JavaList { [ObservableAsProperty] get; }
        public ICommand SelectCoreCmd { get; }
        public ICommand AddServerCmd { get; }
        public ICommand EditServerCmd { get; }

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
                    await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Info_YesNo, "内存可能不足", "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃。" + Environment.NewLine + "建议分配至少2048MB（即2GB）内存。确定要继续吗？"));
                if (confirm == PopupResult.No) return;
            }
            var coreType = Connector.GetCoreType(ServerInfo.CorePath);
            var confirmResult = await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Info_YesNo, "确定添加此服务器吗？",
                $"服务器信息：\r名称：{ServerInfo.ServerName}\rJava路径：{ServerInfo.JavaPath}\r核心文件路径：{ServerInfo.CorePath}\r服务器类型：{coreType}\r内存范围：{ServerInfo.MinMem} ~ {ServerInfo.MaxMem}\r附加JVM参数：{ServerInfo.ExtJvm}"));
            if (confirmResult != PopupResult.Yes) return;
            else
            {
                var success = await Connector.AddServer(ServerInfo);
                if (success)
                {
                    AppState.ITAUnits.Notify(1, null, "服务器配置成功！");
                    MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FS2Common));
                }
            }
        }
        #endregion
        
        #region 修改服务器逻辑

        private async Task EditServer()
        {
            int id = AppState.SelectedServerId;
            FormedServerConfig info = new(ServerName, "", MinMem, MaxMem, JavaPath, ExtJvm);
            var vResult = Connector.ValidateNewServerConfig(info, true);
            if (vResult.Item1 == 0)
            {
                if (vResult.Item2 is not null) await AppState.ITAUnits.ThrowError("表单错误", vResult.Item2);
                return;
            }
            if (int.Parse(info.MaxMem) < 512)
            {
                var confirm =
                    await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Info_YesNo, "内存可能不足", "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃。" + Environment.NewLine + "建议分配至少2048MB（即2GB）内存。确定要继续吗？"));
                if (confirm == PopupResult.No) return;
            }
            var confirmResult = await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Info_YesNo, "确定修改此服务器吗？",
                $"服务器信息：\r服务器路径：{info.CorePath}\r名称：{info.ServerName}\rJava路径：{info.JavaPath}\r内存范围：{info.MinMem}MB ~ {info.MaxMem}MB\r附加JVM参数：{info.ExtJvm}"));
            if (confirmResult != PopupResult.Yes) return;
            else
            {
                var success = await Connector.EditServer(id, info);
                if (success)
                {
                    AppState.ITAUnits.Notify(1, null, "服务器配置修改成功！");
                    MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FS2Common));
                }
            }

        }
        #endregion
        
        #region 重置表单

        public void ClearForm(RightPageState rps)
        {
            switch (rps)
            {
                case RightPageState.EditSC:
                {
                    if (AppState.CurrentServerConfigs.TryGetValue(AppState.SelectedServerId, out var tmp))
                    {
                        ServerName = tmp.name;
                        CorePath = tmp.server_path;
                        MinMem = tmp.min_memory.ToString();
                        MaxMem = tmp.max_memory.ToString();
                        JavaPath = tmp.using_java;
                        ExtJvm = tmp.ext_jvm;
                        break;
                    }
                    else
                    {
                        throw new ArgumentNullException("选中的服务器不存在已经被读取的配置。"+ Environment.NewLine + "作者认为LSL理论上不应该抛出该异常，因为您不可能在不存在该服务器时编辑其配置。");
                    }
                }
                case RightPageState.AddCore:
                {
                    ServerName = string.Empty;
                    CorePath = string.Empty;
                    MinMem = string.Empty;
                    MaxMem = string.Empty;
                    ExtJvm = "-Dlog4j2.formatMsgNoLookups=true";
                    break;
                }
            }
        }
        #endregion
    }
}