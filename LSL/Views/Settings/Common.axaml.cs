using Avalonia.Controls;
using System.Collections.Generic;

namespace LSL.Views.Settings
{
    
    public partial class Common : UserControl
    {

        
        public Common()
        {
            InitializeComponent();
            Priority.SelectedIndex = 1;
            JavaSelection.SelectedIndex = 0;
        }
    }
}
