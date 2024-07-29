using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using LSL.Controls;
using LSL.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;
using System.Diagnostics;
using System;

namespace LSL.Controls
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
                default:
                    break;
            }
            Debug.WriteLine("UpdateStyles");
        }
    }
}
