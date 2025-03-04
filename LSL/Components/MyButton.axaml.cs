using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using LSL.Components;
using LSL.ViewModels;
using Avalonia.Media;
using System.Diagnostics;
using System;

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
            this.Initialized += StartUpdatingStyles;
        }
        private void StartUpdatingStyles(object sender, EventArgs e)
        {
            UpdateStyles();
        }
        //根据ColorType更改按钮样式
        private void UpdateStyles()
        {
            switch(ColorType)
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
