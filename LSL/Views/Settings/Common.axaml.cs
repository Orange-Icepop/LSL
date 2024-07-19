using Avalonia.Controls;
using LSL.ViewModels;
using System.Collections.Generic;

namespace LSL.Views.Settings
{
    
    public partial class Common : UserControl
    {

        
        public Common()
        {
            InitializeComponent();
            this.DataContext = new ConfigViewModel();
        }
    }
}
