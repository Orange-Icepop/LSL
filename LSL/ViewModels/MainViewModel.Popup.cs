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
        #region Popup��ͼ�ڲ��ֶ��������
        // Popup��ͼ����ֶ�
        private string popupTitle;
        private string popupContent;
        private SolidColorBrush popupColor;
        private bool popupVisible;
        private double popupOpacity;
        // ���ư�ť����ʾ
        private bool confirmButton;
        private bool cancelButton;
        private bool yesButton;
        private bool noButton;
        // Popup��ͼ��ط�����
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
        //Popup��ť����
        public ICommand PopupConfirm { get; set; }//Popupȷ�ϰ�ť
        public ICommand PopupCancel { get; set; }//Popupȡ����ť
        public ICommand PopupYes { get; set; }//Popup�ǰ�ť
        public ICommand PopupNo { get; set; }//Popup��ť
        #endregion

        public async void ResetPopup()
        {
            Dispatcher.UIThread.Post(() => PopupOpacity = 0);
            await Task.Delay(200);// �ȴ���������
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

        //Popup�ⲿ������
        public async void ReceivePopupMessage(PopupMessageArgs args)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowPopup(args.Type, args.Title, args.Message));
        }

        #region Popup�������
        private ConcurrentQueue<PopupRequest> PopupQueue = new();

        private bool PopupIsProcessing;// ��ֹ����

        private TaskCompletionSource<string> PopupTcs;

        // type���壺0-��ʾ��ֻ��ȷ�ϣ���1-���棨��/��/ȡ������2-��ʾ����/�񣩣�3-���棨��/�񣩣�4-�ڲ�����ȷ�ϣ�������Ϣ����5-������ȷ�ϣ�������Ϣ��
        public async Task<string> ShowPopup(int type = 0, string title = "�յ���", string message = "����һ���յĵ�����")
        {
            PopupRequest request;
            TaskCompletionSource<string> tcs = new();
            switch (type)
            {
                case 0: request = new PopupRequest(PopupType.InfoConfirm, "��ʾ", message, tcs); break;
                case 1: request = new PopupRequest(PopupType.WarnYesNoCancel, "����", message, tcs); break;
                case 2: request = new PopupRequest(PopupType.InfoYesNo, "��ʾ", message, tcs); break;
                case 3: request = new PopupRequest(PopupType.WarnYesNo, "����", message, tcs); break;
                case 4: request = new PopupRequest(PopupType.ErrorConfirm, "����", $"LSL������һ������\r{message}", tcs); break;
                case 5: request = new PopupRequest(PopupType.ErrorConfirm, "������", $"���ύ�ı�����\r{message}\r������ȷ�ϱ���������ύ��", tcs); break;
                default:
                    Debug.WriteLine("Unknown popup type");
                    return "Unknown popup type";
            }
            if (title != "�յ���")
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

        // ��ʾPopup
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

        #region Popup�ڲ�ʹ�õ���������
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