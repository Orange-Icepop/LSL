using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace LSL.Components
{
    public partial class MyListItem : Button
    {
        private static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<MyCard, string>(nameof(Title), defaultBindingMode: BindingMode.OneWay);
        private static readonly StyledProperty<string> InfoProperty =
            AvaloniaProperty.Register<MyCard, string>(nameof(Info), defaultBindingMode: BindingMode.OneWay);

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public string Info
        {
            get => GetValue(InfoProperty);
            set => SetValue(InfoProperty, value);
        }

        public MyListItem()
        {
            InitializeComponent();
        }
    }
}