using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using static System.Net.Mime.MediaTypeNames;

namespace LSL.Views
{
    public partial class MyCard : Panel
    {
        //����titie
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
        }
    }
}