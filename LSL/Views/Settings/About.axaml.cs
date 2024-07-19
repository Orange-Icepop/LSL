using Avalonia.Controls;
using LSL.Services;
using LSL.ViewModels;
using System.Diagnostics;

namespace LSL.Views.Settings
{
    public partial class About : UserControl
    {
        //private ConfigurationManager configManager = new ConfigurationManager();
        public About()
        {
            InitializeComponent();
            this.DataContext = new ConfigViewModel();
        }
}
}
