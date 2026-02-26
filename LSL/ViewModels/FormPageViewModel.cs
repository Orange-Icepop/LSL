using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using FluentResults;
using LSL.Common.Extensions;
using LSL.Common.Models;
using LSL.Common.Models.ServerConfig;
using LSL.Common.Utilities.Minecraft;
using LSL.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace LSL.ViewModels;

public partial class FormPageViewModel : RegionalViewModelBase<FormPageViewModel>
{
    public FormPageViewModel(AppStateLayer appState, ServiceConnector connector, DialogCoordinator coordinator, PublicCommand commands) : base(appState, connector, coordinator, commands)
    {
        // Reset to not null
        EditingConfig = MutableLocatedServerConfig.New;
        CorePath = string.Empty;
        _javaList = [];
        ExtraJvmArgs = string.Empty;
        // end
        this.WhenAnyValue(formPageVM => formPageVM.SelectedJavaIndex)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(GetJavaPath)
            .Subscribe(val => EditingConfig.JavaPath = val);
        _javaListHelper = AppState.WhenAnyValue(stateLayer => stateLayer.CurrentJavaDict)
            .Select(javaInfos => javaInfos.Values)
            .Select(vals =>
            {
                ObservableCollection<string> fin = [];
                foreach (var val in vals) fin.Add($"{val.Vendor} {val.Version} ({val.Path})");

                return fin;
            })
            .ToProperty(this, formPageVM => formPageVM.JavaList);
        SelectCoreCmd = ReactiveCommand.CreateFromTask(SelectCore);
        SelectFolderCoreCmd = ReactiveCommand.CreateFromTask(SelectExistedServerCore);
        AddCoreCmd = ReactiveCommand.CreateFromTask(AddServerCore);
        EditServerCmd = ReactiveCommand.CreateFromTask(EditServer);
        AddExistedServerCmd = ReactiveCommand.CreateFromTask(AddServerFolder);
    }

    [ObservableAsProperty] private ObservableCollection<string> _javaList;
    public ICommand SelectCoreCmd { get; }
    public ICommand SelectFolderCoreCmd { get; }
    public ICommand AddCoreCmd { get; }
    public ICommand EditServerCmd { get; }
    public ICommand AddExistedServerCmd { get; }

    private async Task SelectCore()
    {
        var result = await Coordinator.FilePickerInteraction.Handle(FilePickerType.CoreFile);
        await Dispatcher.UIThread.InvokeAsync(() => { CorePath = result; });
    }

    private async Task SelectExistedServerCore()
    {
        var result = await Coordinator.FilePickerInteraction.Handle(FilePickerType.CoreFile);
        var file = new FileInfo(result);
        if (!file.Exists)
        {
            Logger.LogError("Server core does not exist:{result}", result);
            await Coordinator.ThrowError("服务器核心不存在", $"{result}不存在或无法访问。");
        }
        var parent = file.Directory;
        if (parent is null)
        {
            Logger.LogError("Error getting the parent folder info of an existing server's core:{result}", result);
            await Coordinator.ThrowError("解析服务器根目录时发生错误", $"无法获取到指定服务器核心的根目录:{result}");
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
                    DisplayJvmArgs();
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
                EditingConfig.CommonCoreInfo = new CommonCoreConfigV1() { JarName = file.Name };
            });
        }
    }

    #region 新增服务器逻辑

    private async Task AddServerCore()
    {
        await Dispatcher.UIThread.InvokeAsync(DeployJvmArgs);
        var vResult = await EditingConfig.CheckAndFixAsync(true);
        var vErrors = vResult.GetErrors();
        if (vErrors.Count > 0)
        {
            await Coordinator.ThrowError("表单错误", vErrors.GetMessages());
            return;
        }

        var immutable = vResult.Value;

        if (immutable.MaxMemory < 512)
        {
            var confirm =
                await Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo, "内存可能不足",
                    "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃。\n建议分配至少2048MB（即2GB）内存。确定要继续吗？"));
            if (confirm == PopupResult.No) return;
        }

        var coreTypeResult = await HandleCoreType(await CoreTypeHelper.GetCoreType(CorePath));

        if (coreTypeResult.Item1 is PopupResult.No) return;

        var confirmResult = await Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo,
            "确定添加此服务器吗？", $@"服务器信息：
名称：{immutable.ServerName}
Java路径：{immutable.JavaPath}
核心文件路径：{CorePath}
服务器类型：{coreTypeResult.Item2.Explain()}
内存范围：{immutable.MinMemory} ~ {immutable.MaxMemory}
附加JVM参数：{immutable.ExtraJvmArgs}"));
        if (confirmResult == PopupResult.Yes)
        {
            var success =
                await Coordinator.SubmitServiceError(
                    await Connector.AddServerUsingCore(immutable, CorePath), "添加服务器时出现错误");
            if (success.IsSuccess)
            {
                Coordinator.Notify(NotifyType.Success, null, "服务器配置成功！");
                MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FullScreenToCommon));
            }
        }
    }

    #endregion

    #region 修改服务器逻辑

    private async Task EditServer()
    {
        await Dispatcher.UIThread.InvokeAsync(DeployJvmArgs);
        var id = AppState.SelectedServerId;
        var vResult = await EditingConfig.CheckAndFixAsync();
        var errors = vResult.GetErrors();
        if (errors.Count > 0)
        {
            await Coordinator.ThrowError("表单错误", errors.GetMessages());
            return;
        }
        
        var immutable = vResult.Value;

        if (immutable.MaxMemory < 512)
        {
            var confirm =
                await Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningYesNo, "内存可能不足",
                    "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃。\n建议分配至少2048MB（即2GB）内存。确定要继续吗？"));
            if (confirm == PopupResult.No) return;
        }

        var confirmResult = await Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo,
            "确定修改此服务器吗？",
            $@"服务器信息：
服务器路径：{immutable.ServerPath}
名称：{immutable.ServerName}
Java路径：{immutable.JavaPath}
内存范围：{immutable.MinMemory}MB ~ {immutable.MaxMemory}MB
附加JVM参数：{immutable.ExtraJvmArgs}"));
        if (confirmResult == PopupResult.Yes)
        {
            var success = await Coordinator.SubmitServiceError(await Connector.EditServer(id, immutable));
            if (success.IsSuccess)
            {
                Coordinator.Notify(NotifyType.Success, null, "服务器配置修改成功！");
                MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FullScreenToCommon));
            }
        }
    }

    #endregion

    #region 添加服务器文件夹逻辑

    private async Task AddServerFolder()
    {
        await Dispatcher.UIThread.InvokeAsync(DeployJvmArgs);
        var vResult = await EditingConfig.CheckAndFixAsync();
        var vErrors = vResult.GetErrors();
        if (vResult.IsFailed)
        {
            await Coordinator.ThrowError("表单错误", vErrors.GetMessages());
            return;
        }
        
        var immutable = vResult.Value;
        
        if (immutable.MaxMemory < 512)
        {
            var confirm =
                await Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo, "内存可能不足",
                    "您为该服务器分配的最大内存大小不足512M，这可能会导致服务器运行时的严重卡顿（尤其是在服务器人数较多的情况下）甚至崩溃。\n建议分配至少2048MB（即2GB）内存。确定要继续吗？"));
            if (confirm == PopupResult.No) return;
        }

        var coreTypeResult = await HandleCoreType(await CoreTypeHelper.GetCoreType(CorePath));

        if (coreTypeResult.Item1 is PopupResult.No) return;

        var confirmResult = await Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.InfoYesNo,
            "确定添加此服务器吗？",
            $@"服务器信息：
名称：{immutable.ServerName}
Java路径：{immutable.JavaPath}
服务器路径：{immutable.ServerPath}
服务器类型：{coreTypeResult.Item2}
内存范围：{immutable.MinMemory} ~ {immutable.MaxMemory}
附加JVM参数：{immutable.ExtraJvmArgs}
注意：如果该服务器没有位于LSL的服务器目录中，将进行一次完整的服务器拷贝，这可能耗费极长时间。请确保有足够的储存空间，否则请手动将服务器移动至LSL的服务器目录下。"));
        if (confirmResult == PopupResult.Yes)
        {
            var success = await Coordinator.SubmitServiceError(await Connector.AddServerFolder(immutable));// TODO:等待/立即结束
            if (success.IsSuccess)
            {
                Coordinator.Notify(NotifyType.Success, null, "服务器配置成功！");
                MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FullScreenToCommon));
            }
        }
    }

    #endregion

    [Reactive] public partial MutableLocatedServerConfig EditingConfig { get; private set; }
    [Reactive] public partial string CorePath { get; set; }
    [Reactive] public partial string ExtraJvmArgs { get; set; }

    public int SelectedJavaIndex
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field,
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

    private async Task<(PopupResult, ServerCoreType)> HandleCoreType(Result<ServerCoreType> result)
    {
        var errors = result.GetErrors();
        if (errors.Count > 0)
        {
            await Coordinator.ThrowError("获取服务器类型信息时发生错误", errors.FlattenToString());
            return (PopupResult.No, ServerCoreType.Error);
        }
        
        switch (result.Value)
        {
            case ServerCoreType.ForgeInstaller:
            {
                await Coordinator.ThrowError("服务器类型错误", "您选择的文件是一个Forge安装器，而不是一个Minecraft服务端核心文件。LSL暂不支持Forge服务器的添加与启动。");
                return (PopupResult.No, ServerCoreType.ForgeInstaller);
            }
            case ServerCoreType.FabricInstaller:
            {
                await Coordinator.ThrowError("服务器类型错误", "您选择的文件是一个Fabric安装器，而不是一个Minecraft服务端核心文件。请下载Fabric官方服务器jar文件，而不是安装器。");
                return (PopupResult.No, ServerCoreType.FabricInstaller);
            }
            case ServerCoreType.Unknown:
            {
                return (await Coordinator.PopupInteraction.Handle(new InvokePopupArgs(PopupType.WarningYesNo, "未知服务器类型", @"LSL无法确认您选择的文件是否为Minecraft服务端核心文件。
这可能是由于LSL没有收集足够的关于服务器核心的辨识信息造成的。如果这是确实一个Minecraft服务端核心并且具有一定的知名度，请您前往LSL的仓库（https://github.com/Orange-Icepop/LSL）提交相关Issue。
您可以直接点击确认绕过校验，但是LSL及其开发团队不为因此造成的后果作担保。")), ServerCoreType.Unknown);
            }
            case ServerCoreType.Client:
            {
                await Coordinator.ThrowError("服务器类型错误", "您选择的文件是一个Minecraft客户端核心文件，而不是一个服务端核心文件。");
                return (PopupResult.No, ServerCoreType.Client);
            }
            default:
                return (PopupResult.Yes, result.Value);
        }
    }

    private void DisplayJvmArgs()
    {
        ExtraJvmArgs = string.Join('\n', EditingConfig.ExtraJvmArgs);
    }

    private void DeployJvmArgs()
    {
        var lines = ExtraJvmArgs.Split(
            ["\r\n", "\r", "\n"],
            StringSplitOptions.RemoveEmptyEntries
        );

        EditingConfig.ExtraJvmArgs = lines
            .Select(line => line.Trim())
            .ToList();
    }
}