using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using LSL.Common.Utilities;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace LSL.ViewModels;

// 用于放置公共命令（仍然属于视图模型）
// 主要成员为杂项ICommand
public class PublicCommand : RegionalViewModelBase<PublicCommand>
{
    public PublicCommand(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
    {
        OpenWebPageCmd = ReactiveCommand.CreateFromTask<string>(OpenWebPage); // 打开网页命令-实现
        OpenFileCmd = ReactiveCommand.CreateFromTask<string>(OpenExplorer);
        SearchJava = ReactiveCommand.CreateFromTask(async () =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => AppState.Coordinator.Notify(NotifyType.Info, "正在搜索Java", "请耐心等待......"));
            var success = AppState.Coordinator.SubmitServiceError(await Connector.FindJava());
            if (success.IsSuccess)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                    AppState.Coordinator.Notify(NotifyType.Success, "Java搜索完成！", $"搜索到了{success.Result}个Java"));
            }
            else await success;
        }); // 搜索Java命令-实现
        CheckUpdateCmd = ReactiveCommand.CreateFromTask(serveCon.CheckForUpdates);
    }

    #region 打开网页命令

    public ICommand OpenWebPageCmd { get; }

    public async Task OpenWebPage(string url)
    {
        var result = XPlatformOperationHelper.OpenWebBrowser(url);
        if (result.IsSuccess)
        {
            AppState.Coordinator.Notify(NotifyType.Success, "成功打开了网页！", url);
            Logger.LogInformation("Successfully opened web page {url}.", url);
        }
        else if (result.Error is ArgumentException ae)
        {
            Logger.LogError(ae, "Error opening webpage {url} because of invalid URL format.", url);
            await AppState.Coordinator.ThrowError("打开网页失败", $"URL格式不正确：{url}");
        }
        else
        {
            Logger.LogError(result.Error, "Error opening webpage {url}.", url);
            var uiMsg = new StringBuilder($"无法打开URL: {url}");
            uiMsg.AppendLine(OperatingSystem.IsLinux()
                ? "请确保安装了 xdg-utils 并且设置了默认浏览器以使用打开浏览器网址的功能。"
                : "在此系统上似乎无法正常打开网页，请检查默认浏览器设置。");
            uiMsg.AppendLine(result.Error.ToString());
            await AppState.Coordinator.ThrowError("打开网页失败", uiMsg.ToString());
        }
    }

    #endregion

    #region 打开文件（夹）命令

    public ICommand OpenFileCmd { get; }

    private async Task OpenExplorer(string url)
    {
        try
        {
            //classify
            bool isFile;
            if (Path.GetPathRoot(url) == url) isFile = false;
            else if (File.Exists(url)) isFile = true;
            else if (Directory.Exists(url)) isFile = false;
            else throw new ArgumentException("File or directory not found.", nameof(url));
            //execute
            if (isFile) OpenAndSelectFile(url);
            else OpenDir(url);
        }
        catch (ArgumentException ae)
        {
            Logger.LogError(ae, "File or directory not found when trying to open file explorer.");
            await AppState.Coordinator.ThrowError("打开文件失败", $"不存在位于{url}的文件或目录。");
        }
        catch (DirectoryNotFoundException dnfe)
        {
            Logger.LogError(dnfe, "Parent directory not found when trying to open file explorer.");
            await AppState.Coordinator.ThrowError("打开文件失败", $"无法获取位于{url}的文件的父目录。");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error executing open explorer task.");
            await AppState.Coordinator.ThrowError("打开文件失败", $"在文件资源管理器中打开{url}时出现以下报错：\n{e.Message}");
        }
    }

    private static void OpenDir(string url)
    {
        if (OperatingSystem.IsWindows())
            Process.Start("explorer.exe", [url]);
        else if (OperatingSystem.IsLinux())
            Process.Start("xdg-open", [url]); // xdg-utils dependency required
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", [url]);
    }

    private static void OpenAndSelectFile(string url)
    {
        var parent = Directory.GetParent(url);
        if (parent is null) throw new DirectoryNotFoundException($"Cannot get the parent directory of file {url}.");
        if (OperatingSystem.IsWindows())
            Process.Start("explorer.exe", ["/select,", url]);
        else if (OperatingSystem.IsLinux())
            Process.Start("xdg-open", [parent.FullName]); // xdg-utils dependency required
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", ["-R", url]);
    }

    #endregion

    public ICommand CheckUpdateCmd { get; }

    public ICommand SearchJava { get; }
}