using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private ILogger<PublicCommand> _logger;
        public PublicCommand(AppStateLayer appState, ServiceConnector serveCon, ILogger<PublicCommand> logger) : base(appState, serveCon)
        {
            _logger = logger;
            OpenWebPageCmd = ReactiveCommand.Create<string>(OpenWebPage);// 打开网页命令-实现
            SearchJava = ReactiveCommand.CreateFromTask(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(() => AppState.ITAUnits.Notify(0, "正在搜索Java", "请耐心等待......"));
                var result = await Connector.FindJava();
                if (!result) return;
                await Dispatcher.UIThread.InvokeAsync(() => AppState.ITAUnits.Notify(1, "Java搜索完成！", $"搜索到了{AppState.CurrentJavaDict.Count}个Java"));
            });// 搜索Java命令-实现
            CheckUpdateCmd = ReactiveCommand.Create(serveCon.CheckForUpdates);
        }

        #region About页面的相关内容
        public ICommand OpenWebPageCmd { get; }
        public ICommand CheckUpdateCmd { get; }
        private async void OpenWebPage(string url)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(url);
                if (!Regex.IsMatch(url, "^https?://")) throw new ArgumentException("URL格式错误");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                AppState.ITAUnits.Notify(1, "成功打开了网页！", url);
                _logger.LogInformation("Successfully opened web page {url}.", url);
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                {
                    await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Error_Confirm, "打开网页失败",
                        $"LSL未能成功打开网页{url}，请检查您的系统是否设置了默认浏览器。\r错误内容：{noBrowser.Message}"));
                    _logger.LogError(noBrowser, "Error opening webpage {url} because no default web browser is set.", url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening webpage {url}.", url);
                await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Error_Confirm, "打开网页失败", $"LSL未能成功打开网页{url}，这是由于非浏览器配置错误造成的。\r如果这是在自定义主页中发生的，请检查您的自定义主页是否正确配置了网址；否则，这可能是一个Bug，请您提交一个issue反馈。\r错误内容：{ex}"));
            }
        }
        #endregion

        public ICommand SearchJava { get; }
    }
}
