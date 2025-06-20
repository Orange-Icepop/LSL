using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace LSL.Components
{
    public class MyCard : ContentControl
    {
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<MyCard, string>(nameof(Text));
        
        public static readonly StyledProperty<IBrush> TitleColorProperty =
            AvaloniaProperty.Register<MyCard, IBrush>(nameof(TitleColor));
        
        public static readonly StyledProperty<IBrush> CardBackgroundProperty =
            AvaloniaProperty.Register<MyCard, IBrush>(nameof(CardBackground));

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public IBrush TitleColor
        {
            get => GetValue(TitleColorProperty);
            set => SetValue(TitleColorProperty, value);
        }

        public IBrush CardBackground
        {
            get => GetValue(CardBackgroundProperty);
            set => SetValue(CardBackgroundProperty, value);
        }
        
        private static readonly IBrush defaultBackground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
        private static readonly IBrush orllowBrush = new SolidColorBrush(Color.Parse("#33f3e5"));

        public MyCard()
        {
            // 初始化资源
            CardBackground = defaultBackground;
            TitleColor = Brushes.Black;
            
            // 绑定事件
            PointerEntered += OnPointerEnter;
            PointerExited += OnPointerLeave;
        }

        private void OnPointerEnter(object? sender, PointerEventArgs e)
        {
            CardBackground = Brushes.White;
            TitleColor = orllowBrush;
        }

        private void OnPointerLeave(object? sender, PointerEventArgs e)
        {
            CardBackground = defaultBackground;
            TitleColor = Brushes.Black;
        }
    }
}