using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Linq;

namespace LSL.Components
{
    public partial class MyCard : Grid
    {
        //定义title
        private static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<MyCard, string>(nameof(Text), defaultBindingMode: BindingMode.OneWay);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        static MyCard()
        {
            // 你可以在这里注册附加属性，如果需要的话  
        }

        public MyCard()
        {
            InitializeComponent();
            this.Loaded += MyCardLoaded;
        }

        private void OnPointerEnter(object sender, PointerEventArgs e)
        {
            this.Resources["TransWhite"] = new SolidColorBrush(Colors.White);
            this.Resources["HeadTextColor"] = new SolidColorBrush(Color.Parse("#33f3e9"));
        }

        private void OnPointerLeave(object sender, PointerEventArgs e)
        {
            this.Resources["TransWhite"] = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            this.Resources["HeadTextColor"] = new SolidColorBrush(Colors.Black);
        }

        private void MyCardLoaded(object? sender, RoutedEventArgs e)
        {
            this.Resources["TransWhite"] = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            this.Resources["HeadTextColor"] = new SolidColorBrush(Colors.Black);
            foreach (var item in this.Children.ToList())
            {
                if (item != border)
                {
                    this.Children.Remove(item);
                    stackpanel.Children.Add(item);
                }
            }
        }
    }
}