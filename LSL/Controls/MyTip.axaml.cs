using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LSL.Controls;

public partial class MyTip : Border
{
    public MyTip()
    {
        InitializeComponent();
    }
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MyTip, string>(nameof(Text));
    public string Text
    {
        get => GetValue(TextProperty);
        set
        {
            SetValue(TextProperty, value);
            this.Tip.Text = value;
        }
    }
}