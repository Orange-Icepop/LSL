using Avalonia.Controls;
using LSL.ViewModels;
using LSL.Views.Settings;
using System.Diagnostics;

namespace LSL.Views.Download
{
    public partial class DownloadLeft : UserControl
    {
        public DownloadLeft()
        {
            InitializeComponent();
            LeftChangedPublisher.Instance.MessageReceived += HandleLeftChangeReceived;
        }
        private void HandleLeftChangeReceived(string navigateTarget)
        {
            ChangeLeftColor(navigateTarget);
        }

        //设置Left按钮样式方法
        private void ChangeLeftColor(string NowPage)
        {
            AutoDownButton.Classes.Remove("selLeft");
            ManualDownButton.Classes.Remove("selLeft");
            ModDownButton.Classes.Remove("selLeft");
            switch (NowPage)
            {
                case "AutoDown":
                    AutoDownButton.Classes.Add("selLeft");
                    break;
                case "ManualDown":
                    ManualDownButton.Classes.Add("selLeft");
                    break;
                case "ModDown":
                    ModDownButton.Classes.Add("selLeft");
                    break;
            }
            Debug.WriteLine("Left Color Switched:" + NowPage);
        }

    }
}
