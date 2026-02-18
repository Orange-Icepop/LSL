using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using LSL.Common.Extensions;
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
        EditingConfig = MutableLocatedServerConfig.New;
        CorePath = string.Empty;
        JavaList = [];
        // end
        this.WhenAnyValue(formPageVM => formPageVM.SelectedJavaIndex)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(GetJavaPath)
            .Subscribe(val => EditingConfig.JavaPath = val);
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
        var file = new FileInfo(result);
        if (!file.Exists)
        {
            Logger.LogError("Server core does not exist:{result}", result);
            await AppState.Coordinator.ThrowError("服务器核心不存在", $"{result}不存在或无法访问。");
        }
        var parent = file.Directory;
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
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    EditingConfig = res.Value.CreateDraft();
                    EditingConfig.ServerPath = parentPath;
                });
            }
        }
        else
        {
            var parentName = parent.Name;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EditingConfig.ServerPath = parentPath;
                CorePath = result;
                EditingConfig.ServerName = parentName;
            });
        }
    }

    #region 新增服务器逻辑

    private async Task AddServerCore()
    {
        var vResult = await EditingConfig.CheckAndFixAsync(true);
        if (vResult.IsFailed)
        {
            await AppState.Coordinator.SubmitServiceError(vResult, "表单错误");
            return;
        }

        var immutable = EditingConfig.FinishDraft();

        if (EditingConfig.MaxMemory < 512)
        {
            var confirm =
                await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo, "内存可能不足",
                    "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃。\n建议分配至少2048MB（即2GB）内存。确定要继续吗？"));
            if (confirm == PopupResult.No) return;
        }

        var coreTypeResult = await ServiceConnector.GetCoreType(CorePath);
        var errors = coreTypeResult.GetErrors();
        if (errors.Count > 0)
        {
            await AppState.Coordinator.ThrowError("无法获取服务器类型信息", errors.FlattenToString());
            return;
        }

        var confirmResult = await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo,
            "确定添加此服务器吗？", $@"服务器信息：
名称：{immutable.ServerName}
Java路径：{immutable.JavaPath}
核心文件路径：{CorePath}
服务器类型：{coreTypeResult.Value.Explain()}
内存范围：{immutable.MinMemory} ~ {immutable.MaxMemory}
附加JVM参数：{immutable.ExtraJvmArgs}"));
        if (confirmResult == PopupResult.Yes)
        {
            var success =
                await AppState.Coordinator.SubmitServiceError(
                    await Connector.AddServerUsingCore(immutable, CorePath));
            if (success.IsSuccess)
            {
                AppState.Coordinator.Notify(NotifyType.Success, null, "服务器配置成功！");
                MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FullScreenToCommon));
            }
        }
    }

    #endregion

    #region 修改服务器逻辑

    private async Task EditServer()
    {
        var id = AppState.SelectedServerId;
        var vResult = await ServiceConnector.ValidateNewServerConfig(EditingConfig, true);
        var errors = vResult.GetErrors();
        if (errors.Count > 0)
        {
            await AppState.Coordinator.ThrowError("表单错误", errors.GetMessages());
            return;
        }

        if (EditingConfig.MaxMemory < 512)
        {
            var confirm =
                await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningYesNo, "内存可能不足",
                    "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃。\n建议分配至少2048MB（即2GB）内存。确定要继续吗？"));
            if (confirm == PopupResult.No) return;
        }

        var confirmResult = await AppState.Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo,
            "确定修改此服务器吗？",
            $@"服务器信息：
服务器路径：{EditingConfig.ServerPath}
名称：{EditingConfig.ServerName}
Java路径：{EditingConfig.JavaPath}
内存范围：{EditingConfig.MinMemory}MB ~ {EditingConfig.MaxMemory}MB
附加JVM参数：{EditingConfig.ExtraJvmArgs}"));
        if (confirmResult == PopupResult.Yes)
        {
            var success = await AppState.Coordinator.SubmitServiceError(await Connector.EditServer(id, EditingConfig));
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

    [Reactive] public MutableLocatedServerConfig EditingConfig { get; set; }
    [Reactive] public string CorePath { get; set; }
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

    #region 重置表单

    public void ClearForm(RightPageState rps)
    {
        switch (rps)
        {
            case RightPageState.ServerConfEdit:
            {
                if (!AppState.CurrentServerConfigs.TryGetValue(AppState.SelectedServerId, out var tmp))
                    throw new Exception("选中的服务器不存在已经被读取的配置。\nLSL作者认为LSL理论上不应该抛出该异常，因为您不可能在不存在该服务器时编辑其配置。");
                EditingConfig = tmp.LocatedConfig.CreateDraft();
                break;
            }
            case RightPageState.AddCore:
            case RightPageState.AddFolder:
            {
                EditingConfig = MutableLocatedServerConfig.New;
                CorePath = string.Empty;
                SelectedJavaIndex = 0;
                EditingConfig.JavaPath = GetJavaPath(0);
                break;
            }
        }
    }

    #endregion
}