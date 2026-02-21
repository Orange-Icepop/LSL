using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using FluentResults;
using LSL.Common.Extensions;
using LSL.Common.Utilities;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace LSL.ViewModels;

// 用于放置公共命令（仍然属于视图模型）
// 主要成员为杂项ICommand
public class PublicCommand : ViewModelBase
{
    public PublicCommand(DialogCoordinator dc, ILogger<PublicCommand> logger) : base(logger)
    {
        _coordinator = dc;
        OpenWebPageCmd = ReactiveCommand.CreateFromTask<string>(OpenWebPage); // 打开网页命令-实现
        OpenFileCmd = ReactiveCommand.CreateFromTask<string>(OpenExplorer);
    }
    
    private readonly DialogCoordinator _coordinator;



    #region 打开网页命令

    public ICommand OpenWebPageCmd { get; }

    public async Task OpenWebPage(string url)
    {
        var result = XPlatformOperationHelper.OpenWebBrowser(url);
        if (result.IsSuccess)
        {
            _coordinator.Notify(NotifyType.Success, "成功打开了网页！", url);
            Logger.LogInformation("Successfully opened web page {url}.", url);
        }
        else if (result.Errors.OfType<ExceptionalError>().FirstOrDefault()?.Exception is ArgumentException ex)
        {
            Logger.LogError(ex, "Error opening webpage {url} because of invalid URL format.", url);
            await _coordinator.ThrowError("打开网页失败", $"URL格式不正确：{url}");
        }
        else
        {
            Logger.LogError("Error opening webpage {url}.", url);
            var uiMsg = new StringBuilder($"无法打开URL: {url}");
            uiMsg.AppendLine(OperatingSystem.IsLinux()
                ? "请确保安装了 xdg-utils 并且设置了默认浏览器以使用打开浏览器网址的功能。"
                : "在此系统上似乎无法正常打开网页，请检查默认浏览器设置。");
            uiMsg.AppendLine(result.GetErrors().FlattenToString());
            await _coordinator.ThrowError("打开网页失败", uiMsg.ToString());
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
            await _coordinator.ThrowError("打开文件失败", $"不存在位于{url}的文件或目录。");
        }
        catch (DirectoryNotFoundException dnfe)
        {
            Logger.LogError(dnfe, "Parent directory not found when trying to open file explorer.");
            await _coordinator.ThrowError("打开文件失败", $"无法获取位于{url}的文件的父目录。");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error executing open explorer task.");
            await _coordinator.ThrowError("打开文件失败", $"在文件资源管理器中打开{url}时出现以下报错：\n{e.Message}");
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
}