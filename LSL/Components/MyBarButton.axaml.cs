using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;

namespace LSL.Components;

[PseudoClasses(":selected")]
public class MyBarButton : Button
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MyListItem, string>(nameof(Title));
    public static readonly StyledProperty<IImage?> IconProperty =
        AvaloniaProperty.Register<MyListItem, IImage?>(nameof(Icon));
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MyListItem, bool>(nameof(IsSelected));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public IImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set
        {
            SetValue(IsSelectedProperty, value); 
            PseudoClasses.Set(":selected", value);
        }
    }
}