using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using static System.Net.Mime.MediaTypeNames;

namespace LSL.Components
{
    public partial class MyCard : Grid
    {
        //����title
        private static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<MyCard, string>(nameof(Text), defaultBindingMode: BindingMode.OneWay);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        static MyCard()
        {
            // �����������ע�ḽ�����ԣ������Ҫ�Ļ�  
        }

        public MyCard()
        {
            InitializeComponent();
            this.Resources["TransWhite"] = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            this.Resources["HeadTextColor"] = new SolidColorBrush(Colors.Black);
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
    }
}