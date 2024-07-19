using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using LSL.Controls;

namespace LSL.Views
{
    public partial class MyButton : Button
    {
        //∂®“Âtitle
        private static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<MyCard, string>(nameof(Text), defaultBindingMode: BindingMode.OneWay);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        static MyButton()
        {

        }
        public MyButton()
        {
            InitializeComponent();
        }
    }
}
