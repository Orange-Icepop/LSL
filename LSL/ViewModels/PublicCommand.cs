using System;
using System.Diagnostics;
using System.Reactive.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using LSL.Services;
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
            SearchJava = ReactiveCommand.Create(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(() => AppState.ITAUnits.NotifyITA.Handle(new(0, "正在搜索Java", "请耐心等待......")));
                await Connector.FindJava();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AppState.ITAUnits.NotifyITA.Handle(new(1, "Java搜索完成！", $"搜索到了{AppState.CurrentJavaDict.Count}个Java")).Subscribe();
                });
            });// 搜索Java命令-实现
        }

        #region About页面的相关内容
        public ICommand OpenWebPageCmd { get; }
        public async void OpenWebPage(string url)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(url);
                if (url.IndexOf("http://") != 1 && url.IndexOf("https://") != 1) throw new ArgumentException("URL格式错误");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                AppState.ITAUnits.NotifyITA.Handle(new NotifyArgs(1, "成功打开了网页！", url)).Subscribe();
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Error_Confirm, "打开网页失败", $"LSL未能成功打开网页{url}，请检查您的系统是否设置了默认浏览器。\r错误内容：{noBrowser.Message}")).ToTask();
            }
            catch (Exception ex)
            {
                await AppState.ITAUnits.PopupITA.Handle(new InvokePopupArgs(PopupType.Error_Confirm, "打开网页失败", $"LSL未能成功打开网页{url}，这是由于非浏览器配置错误造成的。\r如果这是在自定义主页中发生的，请检查您的自定义主页是否正确配置了网址；否则，这可能是一个Bug，请您提交一个issue反馈。\r错误内容：{ex.Message}")).ToTask();
            }
        }
        #endregion

        public ICommand SearchJava { get; }
    }
}
