using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        // Popup相关字段
        private string popupTitle;
        private string popupContent;
        private SolidColorBrush popupColor;
        private int popupType;
        private bool popupVisible;
        private double popupOpacity;

        private bool confirmButton;
        private bool cancelButton;
        private bool yesButton;
        private bool noButton;

        #region Popup访问器
        public string PopupTitle
        {
            get => popupTitle;
            set => this.RaiseAndSetIfChanged(ref popupTitle, value);
        }
        public string PopupContent
        {
            get => popupContent;
            set => this.RaiseAndSetIfChanged(ref popupContent, value);
        }
        public SolidColorBrush PopupColor
        {
            get => popupColor;
            set => this.RaiseAndSetIfChanged(ref popupColor, value);
        }
        public int PopupType
        {
            get => popupType;
            set => this.RaiseAndSetIfChanged(ref popupType, value);
        }
        public bool PopupVisible
        {
            get => popupVisible;
            set => this.RaiseAndSetIfChanged(ref popupVisible, value);
        }
        public double PopupOpacity
        {
            get => popupOpacity;
            set => this.RaiseAndSetIfChanged(ref popupOpacity, value);
        }
        public bool ConfirmButton
        {
            get => confirmButton;
            set => this.RaiseAndSetIfChanged(ref confirmButton, value);
        }
        public bool CancelButton
        {
            get => cancelButton;
            set => this.RaiseAndSetIfChanged(ref cancelButton, value);
        }
        public bool NoButton
        {
            get => noButton;
            set => this.RaiseAndSetIfChanged(ref noButton, value);
        }
        public bool YesButton
        {
            get => yesButton;
            set => this.RaiseAndSetIfChanged(ref yesButton, value);
        }
        #endregion

        public async void ResetPopup()
        {
            PopupOpacity = 0;
            await Task.Delay(200);
            Task.Run(ResetPopupS2);
        }

        public void ResetPopupS2()
        {
            PopupVisible = false;
            PopupTitle = "";
            PopupContent = "";
            PopupColor = new SolidColorBrush(Colors.Black);

            ConfirmButton = false;
            CancelButton = false;
            YesButton = false;
            NoButton = false;
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
                    PopupTitle = "提示";
                    PopupContent = message;
                    PopupColor = new SolidColorBrush(Color.Parse("#33e0e5"));
                    ConfirmButton = true;
                    break;
                case 1:
                    PopupTitle = "警告";
                    PopupContent = message;
                    PopupColor = new SolidColorBrush(Colors.Yellow);
                    CancelButton = true;
                    YesButton = true;
                    NoButton = true;
                    break;
                case 2:
                    PopupTitle = "警告";
                    PopupContent = message;
                    PopupColor = new SolidColorBrush(Colors.Yellow);
                    YesButton = true;
                    NoButton = true;
                    break;
                case 3:
                    PopupTitle = "错误";
                    PopupContent = $"LSL发生了一个错误。\r {message}";
                    PopupColor = new SolidColorBrush(Colors.Red);
                    ConfirmButton = true;
                    break;
                default:
                    Debug.WriteLine("Unknown popup type");
                    return null;
            }
            if (title != "空弹窗")
            {
                PopupTitle = title;
            }
            PopupVisible = true;
            PopupOpacity = 1;
            var result = await PopupTcs.Task;
            ResetPopup();
            return result;
        }

        public ICommand PopupConfirm { get; set; }//Popup确认按钮
        public ICommand PopupCancel { get; set; }//Popup取消按钮
        public ICommand PopupYes { get; set; }//Popup是按钮
        public ICommand PopupNo { get; set; }//Popup否按钮
    }
}