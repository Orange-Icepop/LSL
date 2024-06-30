using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Windows.Input;

namespace LSL.Views
{
    public partial class Bar : UserControl
    {

        /*public static readonly StyledProperty<bool> IsSeamlessProperty =
        AvaloniaProperty.Register<Bar, bool>(nameof(IsSeamless));
        public event EventHandler<PointerPressedEventArgs> OnPointerMouseHander;

        public bool IsSeamless
        {
            get { return GetValue(IsSeamlessProperty); }
            set
            {
                SetValue(IsSeamlessProperty, value);
                if (Title != null &&
                    Navigation != null &&
                    WinHandle != null)
                {
                    Title.IsVisible = IsSeamless ? false : true;
                    Navigation.IsVisible = IsSeamless ? false : true;
                    WinHandle.IsVisible = IsSeamless ? true : false;

                    if (IsSeamless == false)
                    {
                        Bar.Resources["SystemControlForegroundBaseHighBrush"] = new SolidColorBrush { Color = new Color(255, 0, 0, 0) };
                    }
                }
            }
        }*/

        public Bar()
        {
            InitializeComponent();
        }
    }

}
