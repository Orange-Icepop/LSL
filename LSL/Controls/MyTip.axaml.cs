using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace LSL.Controls;

public partial class MyTip : TemplatedControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MyTip, string>(nameof(Text));
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}