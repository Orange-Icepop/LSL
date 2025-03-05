﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
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
        }
        #region About页面的相关内容
        public ICommand OpenWebPageCmd { get; }
        public async void OpenWebPage(string url)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(url);
                //if (url.IndexOf("http://") != 1 && url.IndexOf("https://") != 1) throw new ArgumentException("URL格式错误");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                QuickHandler.SendNotify(1, "成功打开了网页！", url);//TODO
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {/*
                if (noBrowser.ErrorCode == -2147467259)
                    await ShowPopup(4, "打开网页失败", $"LSL未能成功打开网页{url}，请检查您的系统是否设置了默认浏览器。\r错误内容：{noBrowser.Message}");*/
            }
            catch (Exception ex)
            {/*
                await ShowPopup(4, "打开网页失败", $"LSL未能成功打开网页{url}，这是由于非浏览器配置错误造成的。\r如果这是在自定义主页中发生的，请检查您的自定义主页是否正确配置了网址；否则，这可能是一个Bug，请您提交一个issue反馈。\r错误内容：{ex.Message}");*/
            }
        }
        #endregion

    }
}
