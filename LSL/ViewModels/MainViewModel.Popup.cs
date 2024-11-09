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
        // Popup����ֶ�
        private string popupTitle;
        private string popupContent;
        private SolidColorBrush popupColor;
        private int popupType;
        private bool popupVisible;
        private bool cancelShow;

        private bool confirmButton;
        private bool cancelButton;
        private bool yesButton;
        private bool noButton;

        #region Popup������
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
        public bool CancelShow
        {
            get => cancelShow;
            set => this.RaiseAndSetIfChanged(ref cancelShow, value);
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

        public void ResetPopup()
        {
            PopupTitle = "";
            PopupContent = "";
            PopupColor = new SolidColorBrush(Colors.Black);
            PopupVisible = false;

            ConfirmButton = false;
            CancelButton = false;
            YesButton = false;
            NoButton = false;
            this.RaisePropertyChanged(nameof(popupVisible));
        }

        private TaskCompletionSource<string> PopupTcs;// ����һ��TaskCompletionSource����������ĵĶ��������ܹ��ȴ��û�����

        // ��ʾPopup
        public async Task<string> ShowPopup(int type = 0, string title = "�յ���", string message = "����һ���յĵ�����")
        {
            PopupTcs = new TaskCompletionSource<string>();
            // type���壺0-��ʾ��ֻ��ȷ�ϣ���1-���棨��/��/ȡ������2-���棨��/�񣩣�3-����ȷ�ϣ�������Ϣ��
            switch (type)
            {
                case 0:
                    PopupTitle = "��ʾ";
                    PopupContent = message;
                    PopupColor = new SolidColorBrush(Color.Parse("#33e0e5"));
                    break;
                case 1:
                    PopupTitle = "����";
                    PopupContent = message;
                    PopupColor = new SolidColorBrush(Colors.Yellow);
                    CancelShow = true;
                    break;
                case 2:
                    PopupTitle = "����";
                    PopupContent = message;
                    PopupColor = new SolidColorBrush(Colors.Yellow);
                    break;
                case 3:
                    PopupTitle = "��������";
                    PopupContent = $"LSL������һ������\r {message}";
                    PopupColor = new SolidColorBrush(Colors.Red);
                    break;
                default:
                    Debug.WriteLine("Unknown popup type");
                    return null;
            }
            if (title != "�յ���")
            {
                PopupTitle = title;
            }
            PopupVisible = true;
            var result = await PopupTcs.Task;
            ResetPopup();
            return result;
        }

        public ICommand PopupConfirm { get; set; }//Popupȷ�ϰ�ť
        public ICommand PopupCancel { get; set; }//Popupȡ����ť
        public ICommand PopupYes { get; set; }//Popup�ǰ�ť
        public ICommand PopupNo { get; set; }//Popup��ť
    }
}