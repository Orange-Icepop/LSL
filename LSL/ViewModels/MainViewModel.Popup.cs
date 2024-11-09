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
        // Popup的内容
        public string popupTitle;
        public string popupContent;
        public SolidColorBrush popupColor;
        public int popupType;
        public bool popupShow;
        public bool cancelShow;

        public bool confirmButton;
        public bool cancelButton;
        public bool yesButton;
        public bool noButton;

        public void RestorePopup()
        {
            popupTitle = "";
            popupContent = "";
            popupColor = new SolidColorBrush(Colors.White);
            popupShow = false;

            confirmButton = false;
            cancelButton = false;
            yesButton = false;
            noButton = false;
        }

        private TaskCompletionSource<string> PopupTcs;// 创建一个TaskCompletionSource，这是最核心的东西，它能够等待用户操作

        // 显示Popup
        public async Task<string> ShowPopup(int type = 0, string title = "空弹窗", string message = "我是一个空的弹窗！")
        {
            PopupTcs = new TaskCompletionSource<string>();
            // type定义：0-提示（只有确认），1-警告（是/否/取消），2-警告（是/否），3-错误（确认，复制消息）
            switch (type)
            {
                case 0:
                    popupTitle = "提示";
                    popupContent = message;
                    popupColor = new SolidColorBrush(Color.Parse("#33e0e5"));
                    break;
                case 1:
                    popupTitle = "警告";
                    popupContent = message;
                    popupColor = new SolidColorBrush(Colors.Yellow);
                    cancelShow = true;
                    break;
                case 2:
                    popupTitle = "警告";
                    popupContent = message;
                    popupColor = new SolidColorBrush(Colors.Yellow);
                    break;
                case 3:
                    popupTitle = "致命错误";
                    popupContent = $"LSL发生了一个错误。\r {message}";
                    popupColor = new SolidColorBrush(Colors.Red);
                    break;
                default:
                    Debug.WriteLine("Unknown popup type");
                    return null;
            }
            if (title != "空弹窗")
            {
                popupTitle = title;
            }
            popupShow = true;
            var result = await PopupTcs.Task;
            popupShow = false;
            return result;
        }

        public ICommand PopupConfirm { get; set; }//Popup确认按钮
        public ICommand PopupCancel { get; set; }//Popup取消按钮
        public ICommand PopupYes { get; set; }//Popup是按钮
        public ICommand PopupNo { get; set; }//Popup否按钮
    }
}