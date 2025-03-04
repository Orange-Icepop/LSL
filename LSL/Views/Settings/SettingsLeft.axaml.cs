using Avalonia.Controls;
using LSL.Services;
using LSL.ViewModels;
using System.Diagnostics;

namespace LSL.Views.Settings
{
    public partial class SettingsLeft : UserControl
    {
        public SettingsLeft()
        {
            InitializeComponent();
            EventBus.Instance.Subscribe<LeftChangedEventArgs>(LeftChangeHandler);
        }
        private void LeftChangeHandler(LeftChangedEventArgs args)
        {
            if (args.LeftView == "SettingsLeft") ChangeLeftColor(args.LeftTarget);
        }

        //设置Left按钮样式方法
        private void ChangeLeftColor(string NowPage)
        {
            Common.Classes.Remove("selLeft");
            Download.Classes.Remove("selLeft");
            Panel.Classes.Remove("selLeft");
            StyleButton.Classes.Remove("selLeft");
            About.Classes.Remove("selLeft");
            switch (NowPage)
            {
                case "Common":
                    Common.Classes.Add("selLeft");
                    break;
                case "DownloadSettings":
                    Download.Classes.Add("selLeft");
                    break;
                case "PanelSettings":
                    Panel.Classes.Add("selLeft");
                    break;
                case "StyleSettings":
                    StyleButton.Classes.Add("selLeft");
                    break;
                case "About":
                    About.Classes.Add("selLeft");
                    break;
            }
            Debug.WriteLine("Left Color Switched:" + NowPage);
        }

    }
}
