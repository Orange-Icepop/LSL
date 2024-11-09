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
        // Popup������
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

        private TaskCompletionSource<string> PopupTcs;// ����һ��TaskCompletionSource����������ĵĶ��������ܹ��ȴ��û�����

        // ��ʾPopup
        public async Task<string> ShowPopup(int type = 0, string title = "�յ���", string message = "����һ���յĵ�����")
        {
            PopupTcs = new TaskCompletionSource<string>();
            // type���壺0-��ʾ��ֻ��ȷ�ϣ���1-���棨��/��/ȡ������2-���棨��/�񣩣�3-����ȷ�ϣ�������Ϣ��
            switch (type)
            {
                case 0:
                    popupTitle = "��ʾ";
                    popupContent = message;
                    popupColor = new SolidColorBrush(Color.Parse("#33e0e5"));
                    break;
                case 1:
                    popupTitle = "����";
                    popupContent = message;
                    popupColor = new SolidColorBrush(Colors.Yellow);
                    cancelShow = true;
                    break;
                case 2:
                    popupTitle = "����";
                    popupContent = message;
                    popupColor = new SolidColorBrush(Colors.Yellow);
                    break;
                case 3:
                    popupTitle = "��������";
                    popupContent = $"LSL������һ������\r {message}";
                    popupColor = new SolidColorBrush(Colors.Red);
                    break;
                default:
                    Debug.WriteLine("Unknown popup type");
                    return null;
            }
            if (title != "�յ���")
            {
                popupTitle = title;
            }
            popupShow = true;
            var result = await PopupTcs.Task;
            popupShow = false;
            return result;
        }

        public ICommand PopupConfirm { get; set; }//Popupȷ�ϰ�ť
        public ICommand PopupCancel { get; set; }//Popupȡ����ť
        public ICommand PopupYes { get; set; }//Popup�ǰ�ť
        public ICommand PopupNo { get; set; }//Popup��ť
    }
}