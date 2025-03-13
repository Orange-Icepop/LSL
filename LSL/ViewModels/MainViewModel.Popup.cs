using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using LSL.Services;
using ReactiveUI;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        #region Popup视图内部字段与访问器
        // Popup视图相关字段
        private string popupTitle;
        private string popupContent;
        private SolidColorBrush popupColor;
        private bool popupVisible;
        private double popupOpacity;
        // 控制按钮的显示
        private bool confirmButton;
        private bool cancelButton;
        private bool yesButton;
        private bool noButton;
        // Popup视图相关访问器
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
        //Popup按钮命令
        public ICommand PopupConfirm { get; set; }//Popup确认按钮
        public ICommand PopupCancel { get; set; }//Popup取消按钮
        public ICommand PopupYes { get; set; }//Popup是按钮
        public ICommand PopupNo { get; set; }//Popup否按钮
        #endregion

        public async void ResetPopup()
        {
            Dispatcher.UIThread.Post(() => PopupOpacity = 0);
            await Task.Delay(200);// 等待动画结束
            Dispatcher.UIThread.Post(() =>
            {
                PopupVisible = false;
                PopupTitle = "";
                PopupContent = "";
                PopupColor = new SolidColorBrush(Colors.Black);

                ConfirmButton = false;
                CancelButton = false;
                YesButton = false;
                NoButton = false;
            });
        }

        //Popup外部访问器
        public async void ReceivePopupMessage(PopupMessageArgs args)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowPopup(args.Type, args.Title, args.Message));
        }

        #region Popup处理机制
        private ConcurrentQueue<PopupRequest> PopupQueue = new();

        private bool PopupIsProcessing;// 防止覆盖

        private TaskCompletionSource<string> PopupTcs;

        // type定义：0-提示（只有确认），1-警告（是/否/取消），2-提示（是/否），3-警告（是/否），4-内部错误（确认，复制消息），5-表单错误（确认，复制消息）
        public async Task<string> ShowPopup(int type = 0, string title = "空弹窗", string message = "我是一个空的弹窗！")
        {
            PopupRequest request;
            TaskCompletionSource<string> tcs = new();
            switch (type)
            {
                case 0: request = new PopupRequest(PopupType.InfoConfirm, "提示", message, tcs); break;
                case 1: request = new PopupRequest(PopupType.WarnYesNoCancel, "警告", message, tcs); break;
                case 2: request = new PopupRequest(PopupType.InfoYesNo, "提示", message, tcs); break;
                case 3: request = new PopupRequest(PopupType.WarnYesNo, "警告", message, tcs); break;
                case 4: request = new PopupRequest(PopupType.ErrorConfirm, "错误", $"LSL发生了一个错误。\r{message}", tcs); break;
                case 5: request = new PopupRequest(PopupType.ErrorConfirm, "表单错误", $"您提交的表单有误。\r{message}\r请重新确认表单无误后再提交。", tcs); break;
                default:
                    Debug.WriteLine("Unknown popup type");
                    return "Unknown popup type";
            }
            if (title != "空弹窗")
            {
                request.Title = title;
            }
            PopupQueue.Enqueue(request);
            Task.Run(HandlePopup);
            var result = await tcs.Task;
            return result;
        }

        private async void HandlePopup()
        {
            if (PopupIsProcessing || PopupQueue.IsEmpty) return;
            else
            {
                PopupIsProcessing = true;
            }
            while (PopupQueue.TryDequeue(out var request))
            {
                await ProcessPopup(request);
                Thread.Sleep(500);
            }
            PopupIsProcessing = false;
        }

        // 显示Popup
        private async Task<string> ProcessPopup(PopupRequest request)
        {
            PopupTcs = request.Tcs;
            switch (request.Type)
            {
                case PopupType.InfoConfirm:
                    Dispatcher.UIThread.Post(() =>
                    {
                        ConfirmButton = true;
                        PopupColor = new SolidColorBrush(Color.Parse("#33e0e5"));
                    });
                    break;
                case PopupType.WarnYesNoCancel:
                    Dispatcher.UIThread.Post(() =>
                    {
                        YesButton = true;
                        NoButton = true;
                        CancelButton = true;
                        PopupColor = new SolidColorBrush(Colors.Yellow);
                    });
                    break;
                case PopupType.InfoYesNo:
                    Dispatcher.UIThread.Post(() =>
                    {
                        YesButton = true;
                        NoButton = true;
                        PopupColor = new SolidColorBrush(Color.Parse("#33e0e5"));
                    });
                    break;
                case PopupType.WarnYesNo:
                    Dispatcher.UIThread.Post(() =>
                    {
                        YesButton = true;
                        NoButton = true;
                        PopupColor = new SolidColorBrush(Colors.Yellow);
                    });
                    break;
                case PopupType.ErrorConfirm:
                    Dispatcher.UIThread.Post(() =>
                    {
                        ConfirmButton = true;
                        PopupColor = new SolidColorBrush(Colors.Red);
                    });
                    break;
                default: throw new NotImplementedException("Unknown popup type");
            }
            PopupTitle = request.Title;
            PopupContent = request.Content;
            PopupVisible = true;
            PopupOpacity = 1;
            var result = await request.Tcs.Task;
            ResetPopup();
            return result;
        }
        #endregion

        #region Popup内部使用的数据类型
        private enum PopupType
        {
            InfoConfirm,
            InfoYesNo,
            WarnYesNo,
            WarnYesNoCancel,
            ErrorConfirm,
        }
        private class PopupRequest(PopupType type, string title, string content, TaskCompletionSource<string> tcs)
        {
            public PopupType Type { get; set; } = type;
            public string Title { get; set; } = title;
            public string Content { get; set; } = content;
            public TaskCompletionSource<string> Tcs { get; set; } = tcs;
        }
        #endregion
    }
}