using Avalonia.Controls;
using LSL.Services;
using LSL.ViewModels;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace LSL.Views.Settings
{
    public partial class About : UserControl
    {
        //private ConfigManager configManager = new ConfigManager();
        public About()
        {
            InitializeComponent();
            Version.Text = $"µ±Ç°°æ±¾: { MainViewModel.CurrentVersion }";
        }
    }
}
