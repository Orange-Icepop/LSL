using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using LSL.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LSL.Controls
{
    public enum ColorType
    {
        Default,
        Highlight
    }
    public partial class MyButton : Button
    {
        public static readonly StyledProperty<ColorType> ColorTypeProperty =AvaloniaProperty.Register<MyButton, ColorType>(nameof(ColorType), ColorType.Default);

        public ColorType ColorType
        {
            get => GetValue(ColorTypeProperty);
            set => SetValue(ColorTypeProperty, value);
        }
        static MyButton()
        {
            // 你可以在这里注册附加属性，如果需要的话  
        }

        public MyButton()
        {
            InitializeComponent();
        }
    }
}
