using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;
using LSL.Models;

namespace LSL.Controls;

[PseudoClasses(":selected")]
public class MyBarButton : Button
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MyBarButton, string>(nameof(Title));
    public static readonly StyledProperty<StreamGeometry?> IconProperty =
        AvaloniaProperty.Register<MyBarButton, StreamGeometry?>(nameof(Icon));

    public static readonly DirectProperty<MyBarButton, GeneralPageState> CurrentPageStateProperty =
        AvaloniaProperty.RegisterDirect<MyBarButton, GeneralPageState>(
            nameof(CurrentPageState),
            o => o.CurrentPageState,
            (o, v) => o.CurrentPageState = v);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public StreamGeometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    public GeneralPageState Page { get; init; }

    private GeneralPageState _currentPageState;
    public GeneralPageState CurrentPageState
    {
        get => _currentPageState;
        set
        {
            PseudoClasses.Set(":selected", value == Page);
            SetAndRaise(CurrentPageStateProperty, ref _currentPageState, value);
        }
    }
}