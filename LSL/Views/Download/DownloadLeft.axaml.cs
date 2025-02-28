using Avalonia.Controls;
using LSL.Services;
using LSL.ViewModels;
using LSL.Views.Settings;
using System.Diagnostics;

namespace LSL.Views.Download
{
    public partial class DownloadsLeft : UserControl
    {
        public DownloadsLeft()
        {
            InitializeComponent();
            EventBus.Instance.Subscribe<LeftChangedEventArgs>(LeftChangeHandler);
        }
        private void LeftChangeHandler(LeftChangedEventArgs args)
        {
            if (args.LeftView == "DownloadsLeft") ChangeLeftColor(args.LeftTarget);
        }

        //设置Left按钮样式方法
        private void ChangeLeftColor(string NowPage)
        {
            AutoDownButton.Classes.Remove("selLeft");
            ManualDownButton.Classes.Remove("selLeft");
            AddServerButton.Classes.Remove("selLeft");
            ModDownButton.Classes.Remove("selLeft");
            switch (NowPage)
            {
                case "AutoDown":
                    AutoDownButton.Classes.Add("selLeft");
                    break;
                case "ManualDown":
                    ManualDownButton.Classes.Add("selLeft");
                    break;
                case "AddServer":
                    AddServerButton.Classes.Add("selLeft");
                    break;
                case "ModDown":
                    ModDownButton.Classes.Add("selLeft");
                    break;
                case "AddCore":
                    AddServerButton.Classes.Add("selLeft");
                    break;

            }
            Debug.WriteLine("Left Color Switched:" + NowPage);
        }

    }
}
