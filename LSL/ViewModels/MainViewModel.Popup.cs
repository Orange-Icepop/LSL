using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        #region Popup回复
        // 回复内容字段
        private string _popupResponse;
        public string PopupResponse
        {
            get => _popupResponse;
            set => this.RaiseAndSetIfChanged(ref _popupResponse, value);
        }
        // 等待Popup回复的任务
        public async Task WaitPopupResponse()
        {
            await this.WhenAnyValue(x => x.PopupResponse)
                .Skip(1)
                .FirstAsync();
        }
        #endregion

        // Popup的内容
        public string popupTitle;
        public string popupContent;
        public SolidColorBrush popupColor;
        public int popupType;
        public bool popupShow;
        public bool cancelShow;

        public void RestorePopup()
        {
            popupTitle = "";
            popupContent = "";
            popupColor = new SolidColorBrush(Colors.White);
            popupType = 0;
            popupShow = false;
            cancelShow = false;
        }

        // 显示Popup
        public void ShowPopup(string type, string message)
        {
            switch (type)
            {
                case "info":
                    popupTitle = "提示";
                    popupContent = message;
                    popupColor = new SolidColorBrush(Color.Parse("#33e0e5"));
                    popupType = 1;
                    break;
                case "warn":
                    popupTitle = "警告";
                    popupContent = message;
                    popupColor = new SolidColorBrush(Colors.Yellow);
                    popupType = 2;
                    cancelShow = true;
                    break;
                case "error":
                    popupTitle = "错误";
                    popupContent = $"LSL发生了一个错误。\r {message}";
                    popupColor = new SolidColorBrush(Colors.Red);
                    popupType = 3;
                    break;
                case "deadlyerror":
                    popupTitle = "致命错误";
                    popupContent = $"LSL发生了一个致命错误，即将关闭。\r {message}";
                    popupColor = new SolidColorBrush(Colors.Red);
                    popupType = 4;
                    break;
                default:
                    Debug.WriteLine("Unknown popup type");
                    return;
            }
            PopupPublisher.Instance.PopupMessage("", "");
        }

        public ICommand PopupConfirm { get; set; }//Popup确认按钮
        public ICommand PopupCancel { get; set; }//Popup取消按钮
    }
}