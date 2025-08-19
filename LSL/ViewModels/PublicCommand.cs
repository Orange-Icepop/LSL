using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace LSL.ViewModels
{
    // 用于放置公共命令（仍然属于视图模型）
    // 主要成员为杂项ICommand
    public class PublicCommand : RegionalVMBase
    {
        public PublicCommand(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
        {
            OpenWebPageCmd = ReactiveCommand.Create<string>(OpenWebPage);// 打开网页命令-实现
            SearchJava = ReactiveCommand.CreateFromTask(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(() => AppState.ITAUnits.Notify(0, "正在搜索Java", "请耐心等待......"));
                var result = await Connector.FindJava();
                if (!result) return;
                await Dispatcher.UIThread.InvokeAsync(() => AppState.ITAUnits.Notify(1, "Java搜索完成！", $"搜索到了{AppState.CurrentJavaDict.Count}个Java"));
            });// 搜索Java命令-实现
            CheckUpdateCmd = ReactiveCommand.CreateFromTask(serveCon.CheckForUpdates);
        }

        #region 泛公共操作
        public ICommand OpenWebPageCmd { get; }
        public ICommand CheckUpdateCmd { get; }
        private async void OpenWebPage(string url)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(url);
                if (!Regex.IsMatch(url, "^https?://")) throw new ArgumentException("URL格式错误");
                if (OperatingSystem.IsWindows())
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                else if (OperatingSystem.IsLinux())
                    Process.Start("xdg-open", url); // xdg-utils dependency required
                else if (OperatingSystem.IsMacOS())
                    Process.Start("open", url);
                AppState.ITAUnits.Notify(1, "成功打开了网页！", url);
                _logger.LogInformation("Successfully opened web page {url}.", url);
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                {
                    await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Error_Confirm, "打开网页失败",
                        $"LSL未能成功打开网页{url}，请检查您的系统是否设置了默认浏览器。\r错误内容：{noBrowser.Message}"));
                    _logger.LogError(noBrowser, "Error opening webpage {url} because no default web browser is set.",
                        url);
                }
            }
            catch (ArgumentException ae)
            {
                _logger.LogError(ae, "Error opening webpage {url} because of invalid URL format.", url);
                await AppState.ITAUnits.ThrowError("打开网页失败", $"URL格式不正确：{url}");
            }
            catch (Exception ex)
            {
                var logMsg = OperatingSystem.IsLinux()
                    ? "Please install xdg-utils to open webpage."
                    : "Please check MacOS's default web browser configuration.";
                _logger.LogError(ex, "Error opening webpage {url}.\r{logMsg}", url, logMsg);
                var uiMsg = OperatingSystem.IsLinux()
                    ? "请安装 xdg-utils: sudo apt install xdg-utils"
                    : "macOS系统异常，请检查默认浏览器设置";
                await AppState.ITAUnits.ThrowError("打开网页失败", uiMsg + ex.Message);
            }
        }
        #endregion

        public ICommand SearchJava { get; }
    }
}
