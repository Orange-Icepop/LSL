using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using LSL.Models;

namespace LSL.Controls;

[PseudoClasses(":selected")]
public class MyLeftButton : Button
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MyLeftButton, string>(nameof(Text));

    public static readonly DirectProperty<MyLeftButton, RightPageState> CurrentPageStateProperty =
        AvaloniaProperty.RegisterDirect<MyLeftButton, RightPageState>(
            nameof(CurrentPageState),
            o => o.CurrentPageState,
            (o, v) => o.CurrentPageState = v);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private RightPageState _currentPageState;

    public RightPageState CurrentPageState
    {
        get => _currentPageState;
        set
        {
            PseudoClasses.Set(":selected", value == Page);
            SetAndRaise(CurrentPageStateProperty, ref _currentPageState, value);
        }
    }

    public RightPageState Page { get; init; }
}