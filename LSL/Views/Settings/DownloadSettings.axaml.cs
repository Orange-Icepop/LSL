using Avalonia.Controls;

namespace LSL.Views.Settings
{
    public partial class DownloadSettings : UserControl
    {
        public DownloadSettings()
        {
            InitializeComponent();
            DownloadSource.SelectedIndex = 0;
        }
    }
}
