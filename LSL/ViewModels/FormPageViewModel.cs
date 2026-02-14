using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using LSL.Common.Models;
using LSL.Common.Models.ServerConfig;
using LSL.Common.Utilities.Minecraft;
using LSL.Common.Validation;
using LSL.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class FormPageViewModel : RegionalViewModelBase<FormPageViewModel>
{
    public FormPageViewModel(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
    {
        // Reset to not null
        ServerName = string.Empty;
        CorePath = string.Empty;
        MinMem = string.Empty;
        MaxMem = string.Empty;
        ExtJvm = "-Dlog4j2.formatMsgNoLookups=true";
        ExistedServerPath = string.Empty;
        JavaList = [];
        JavaPath = string.Empty;
        // end
        this.WhenAnyValue(formPageVM => formPageVM.SelectedJavaIndex)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(GetJavaPath)
            .Subscribe(val => JavaPath = val);
        AppState.WhenAnyValue(stateLayer => stateLayer.CurrentJavaDict)
            .Select(javaInfos => javaInfos.Values)
            .Select(vals =>
            {
                ObservableCollection<string> fin = [];
                foreach (var val in vals) fin.Add($"{val.Vendor} {val.Version} ({val.Path})");

                return fin;
            })
            .ToPropertyEx(this, formPageVM => formPageVM.JavaList);
        SelectCoreCmd = ReactiveCommand.CreateFromTask(SelectCore);
        SelectFolderCoreCmd = ReactiveCommand.CreateFromTask(SelectExistedServerCore);
        AddCoreCmd = ReactiveCommand.CreateFromTask(AddServerCore);
        EditServerCmd = ReactiveCommand.CreateFromTask(EditServer);
        AddExistedServerCmd = ReactiveCommand.CreateFromTask(AddServerFolder);
    }

    public ObservableCollection<string> JavaList { [ObservableAsProperty] get; }
    public ICommand SelectCoreCmd { get; }
    public ICommand SelectFolderCoreCmd { get; }
    public ICommand AddCoreCmd { get; }
    public ICommand EditServerCmd { get; }
    public ICommand AddExistedServerCmd { get; }

    private async Task SelectCore()
    {
        var result = await AppState.Coordinator.FilePickerInteraction.Handle(FilePickerType.CoreFile);
        await Dispatcher.UIThread.InvokeAsync(() => { CorePath = result; });
    }

    private async Task SelectExistedServerCore()
    {
        var result = await AppState.Coordinator.FilePickerInteraction.Handle(FilePickerType.CoreFile);
        var parent = Directory.GetParent(result);
        if (parent is null)
        {
            Logger.LogError("Error getting the parent folder info of an existing server's core:{result}", result);
            await AppState.Coordinator.ThrowError("解析服务器根目录时发生错误", $"无法获取到指定服务器核心的根目录:{result}");
            return;
        }

        var parentPath = "服务器文件夹：" + parent.FullName;
        if (File.Exists(Path.Combine(parent.FullName, "lslconfig.json")))
        {
            var res = await ServerConfigHelper.ReadSingleConfigAsync(parent.FullName);
            if (res.IsSuccess)
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    DeployConfig(res.Value);
                    ExistedServerPath = parentPath;
                });
        }
        else
        {
            var parentName = parent.Name;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ExistedServerPath = parentPath;
                CorePath = result;
                ServerName = parentName;
            });
        }
    }
    
    #region 新增服务器逻辑

    private async Task AddServerCore()
    {
        LocatedServerConfig serverInfo = new(ServerName, CorePath, MinMem, MaxMem, JavaPath, ExtJvm);
        var vResult = await ServiceConnector.ValidateNewServerConfig(serverInfo);
        if (vResult.IsFailed)
        {
            await AppState.Coordinator.SubmitServiceError(vResult, "表单错误");
            return;
        }

        if (vResult.IsWarning)
        {
            var confirm =
                await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo,
                    "未知的Minecraft核心文件", vResult.Error.Message));
            if (confirm == PopupResult.No) return;
        }

        if (int.Parse(serverInfo.MaxMem) < 512)
        {
            var confirm =
                await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo, "内存可能不足",
                    "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃。\n建议分配至少2048MB（即2GB）内存。确定要继续吗？"));
            if (confirm == PopupResult.No) return;
        }

        var coreTypeResult = await ServiceConnector.GetCoreType(serverInfo.CorePath);
        if (!coreTypeResult.IsSuccess)
        {
            await AppState.Coordinator.ThrowError("无法获取服务器类型信息", coreTypeResult.Error.Message);
            return;
        }

        var confirmResult = await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo,
            "确定添加此服务器吗？",
            $"服务器信息：\n名称：{serverInfo.ServerName}\nJava路径：{serverInfo.JavaPath}\n核心文件路径：{serverInfo.CorePath}\n服务器类型：{coreTypeResult.Value.Explain()}\n内存范围：{serverInfo.MinMem} ~ {serverInfo.MaxMem}\n附加JVM参数：{serverInfo.ExtJvm}"));
        if (confirmResult == PopupResult.Yes)
        {
            var success = AppState.Coordinator.SubmitServiceError(await Connector.AddServerUsingCore(serverInfo, CorePath));
            if (success.IsSuccess)
            {
                AppState.Coordinator.Notify(NotifyType.Success, null, "服务器配置成功！");
                MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FullScreenToCommon));
            }
            else
            {
                await success;
            }
        }
    }

    #endregion

    #region 修改服务器逻辑

    private async Task EditServer()
    {
        var id = AppState.SelectedServerId;
        FormedServerConfig info = new(ServerName, "", MinMem, MaxMem, JavaPath, ExtJvm);
        var vResult = await ServiceConnector.ValidateNewServerConfig(info, true);
        if (vResult.IsFailed)
        {
            await AppState.Coordinator.ThrowError("表单错误", vResult.Error.Message);
            return;
        }

        if (int.Parse(info.MaxMem) < 512)
        {
            var confirm =
                await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningYesNo, "内存可能不足",
                    "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃。\n建议分配至少2048MB（即2GB）内存。确定要继续吗？"));
            if (confirm == PopupResult.No) return;
        }

        var confirmResult = await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo,
            "确定修改此服务器吗？",
            $"服务器信息：\n服务器路径：{info.CorePath}\n名称：{info.ServerName}\nJava路径：{info.JavaPath}\n内存范围：{info.MinMem}MB ~ {info.MaxMem}MB\n附加JVM参数：{info.ExtJvm}"));
        if (confirmResult == PopupResult.Yes)
        {
            var success = await AppState.Coordinator.SubmitServiceError(await Connector.EditServer(id, info));
            if (success.IsSuccess)
            {
                AppState.Coordinator.Notify(NotifyType.Success, null, "服务器配置修改成功！");
                MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FullScreenToCommon));
            }
        }
    }

    #endregion

    #region 添加服务器文件夹逻辑

    private async Task AddServerFolder()
    {
        FormedServerConfig serverInfo = new(ServerName, CorePath, MinMem, MaxMem, JavaPath, ExtJvm);
        var vResult = await ServiceConnector.ValidateNewServerConfig(serverInfo);
        if (vResult.IsFailed)
        {
            await AppState.Coordinator.ThrowError("表单错误", vResult.Error.Message);
            return;
        }

        if (vResult.IsWarning)
        {
            var confirm =
                await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo,
                    "未知的Minecraft核心文件", vResult.Error.Message));
            if (confirm == PopupResult.No) return;
        }

        if (int.Parse(serverInfo.MaxMem) < 512)
        {
            var confirm =
                await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo, "内存可能不足",
                    "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃。\n建议分配至少2048MB（即2GB）内存。确定要继续吗？"));
            if (confirm == PopupResult.No) return;
        }

        var coreTypeResult = await ServiceConnector.GetCoreType(serverInfo.CorePath);
        if (!coreTypeResult.IsSuccess)
        {
            await AppState.Coordinator.ThrowError("无法获取服务器类型信息", coreTypeResult.Error.Message);
            return;
        }

        var confirmResult = await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo,
            "确定添加此服务器吗？",
            $"服务器信息：\n名称：{serverInfo.ServerName}\nJava路径：{serverInfo.JavaPath}\n核心文件路径：{serverInfo.CorePath}\n服务器类型：{coreTypeResult.Value}\n内存范围：{serverInfo.MinMem} ~ {serverInfo.MaxMem}\n附加JVM参数：{serverInfo.ExtJvm}"));
        if (confirmResult == PopupResult.Yes)
        {
            var success = AppState.Coordinator.SubmitServiceError(await Connector.AddServerFolder(serverInfo));
            if (success.IsSuccess)
            {
                AppState.Coordinator.Notify(NotifyType.Success, null, "服务器配置成功！");
                MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FullScreenToCommon));
            }
            else
            {
                await success;
            }
        }
    }

    #endregion

    #region 表单绑定属性

    [Reactive] [ServerNameValidator] public string ServerName { get; set; }
    [Reactive] [ServerCorePathValidator] public string CorePath { get; set; }
    [Reactive] public string ExistedServerPath { get; private set; }
    [Reactive] [MinMemValidator] public string MinMem { get; set; }
    [Reactive] [MaxMemValidator] public string MaxMem { get; set; }
    [Reactive] [JavaPathValidator] public string JavaPath { get; set; }
    [Reactive] [ExtJvmValidator] public string ExtJvm { get; set; }
    private int _selectedJavaIndex;

    public int SelectedJavaIndex
    {
        get => _selectedJavaIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedJavaIndex,
            AppState.CurrentJavaDict.Count == 0 ? -1 : Math.Clamp(value, 0, AppState.CurrentJavaDict.Count - 1));
    }

    private string GetJavaPath(int index)
    {
        if (AppState.CurrentJavaDict.Count == 0) return string.Empty;
        return AppState.CurrentJavaDict.TryGetValue(index, out var info) ? info.Path : string.Empty;
    }

    #endregion

    #region 重置表单

    public void ClearForm(RightPageState rps)
    {
        switch (rps)
        {
            case RightPageState.ServerConfEdit:
            {
                if (!AppState.CurrentServerConfigs.TryGetValue(AppState.SelectedServerId, out var tmp))
                    throw new Exception("选中的服务器不存在已经被读取的配置。\nLSL作者认为LSL理论上不应该抛出该异常，因为您不可能在不存在该服务器时编辑其配置。");
                DeployConfig(tmp.LocatedConfig);
                break;
            }
            case RightPageState.AddCore:
            case RightPageState.AddFolder:
            {
                ServerName = string.Empty;
                CorePath = string.Empty;
                MinMem = string.Empty;
                MaxMem = string.Empty;
                ExistedServerPath = string.Empty;
                SelectedJavaIndex = 0;
                JavaPath = GetJavaPath(0);
                ExtJvm = "-Dlog4j2.formatMsgNoLookups=true";
                break;
            }
        }
    }

    private void DeployConfig(LocatedServerConfig config)
    {
        ServerName = config.ServerName;
        CorePath = config.ServerPath;
        MinMem = config.MinMemory.ToString();
        MaxMem = config.MaxMemory.ToString();
        ExistedServerPath = string.Empty;
        JavaPath = config.JavaPath;
        ExtJvm = config.ExtraJvmArgs;
    }

    #endregion
}