using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace LSL.Components
{
    public enum ColorType
    {
        Default,
        Highlight,
        Red
    }
    public partial class MyButton : Button
    {
        public static readonly StyledProperty<ColorType> ColorTypeProperty =
            AvaloniaProperty.Register<MyButton, ColorType>(nameof(ColorType), ColorType.Default);

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
            this.Initialized += (s, e) => UpdateStyles();
        }
        public MyButton(string color, string content, ICommand command)
        {
            InitializeComponent();
            this.ColorType = Enum.TryParse(color, out ColorType colorType) ? colorType : ColorType.Default;
            this.Content = content;
            this.Command = command;
            this.Initialized += (s, e) => UpdateStyles();
        }

        //根据ColorType更改按钮样式
        private void UpdateStyles()
        {
            switch (ColorType)
            {
                case ColorType.Highlight:
                    this.Classes.Add("highlight");
                    break;
                case ColorType.Red:
                    this.Classes.Add("red");
                    break;
                default:
                    break;
            }
        }
    }
}
