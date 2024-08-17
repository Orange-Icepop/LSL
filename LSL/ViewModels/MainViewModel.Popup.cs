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
        #region Popup�ظ�
        // �ظ������ֶ�
        private string _popupResponse;
        public string PopupResponse
        {
            get => _popupResponse;
            set => this.RaiseAndSetIfChanged(ref _popupResponse, value);
        }
        // �ȴ�Popup�ظ�������
        public async Task WaitPopupResponse()
        {
            await this.WhenAnyValue(x => x.PopupResponse)
                .Skip(1)
                .FirstAsync();
        }
        #endregion

        // Popup������
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

        // ��ʾPopup
        public void ShowPopup(string type, string message)
        {
            switch (type)
            {
                case "info":
                    popupTitle = "��ʾ";
                    popupContent = message;
                    popupColor = new SolidColorBrush(Color.Parse("#33e0e5"));
                    popupType = 1;
                    break;
                case "warn":
                    popupTitle = "����";
                    popupContent = message;
                    popupColor = new SolidColorBrush(Colors.Yellow);
                    popupType = 2;
                    cancelShow = true;
                    break;
                case "error":
                    popupTitle = "����";
                    popupContent = $"LSL������һ������\r {message}";
                    popupColor = new SolidColorBrush(Colors.Red);
                    popupType = 3;
                    break;
                case "deadlyerror":
                    popupTitle = "��������";
                    popupContent = $"LSL������һ���������󣬼����رա�\r {message}";
                    popupColor = new SolidColorBrush(Colors.Red);
                    popupType = 4;
                    break;
                default:
                    Debug.WriteLine("Unknown popup type");
                    return;
            }
            PopupPublisher.Instance.PopupMessage("", "");
        }

        public ICommand PopupConfirm { get; set; }//Popupȷ�ϰ�ť
        public ICommand PopupCancel { get; set; }//Popupȡ����ť
    }
}