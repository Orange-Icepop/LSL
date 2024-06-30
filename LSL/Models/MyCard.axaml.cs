using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Layout;

namespace LSL.Models
{
    public class MyCard : UserControl
    {
        private Border _border;
        private StackPanel _stackPanel;

        public MyCard()
        {
            _border = new Border
            {
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Colors.White),
                Child = _stackPanel = new StackPanel { }
            };
            Content = _border;
        }
        
    }
}
