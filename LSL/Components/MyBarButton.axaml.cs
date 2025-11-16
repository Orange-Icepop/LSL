using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace LSL.Components;

public class MyBarButton : Button
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MyListItem, string>(nameof(Title));
    public static readonly StyledProperty<IImage?> IconProperty =
        AvaloniaProperty.Register<MyListItem, IImage?>(nameof(Icon));

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
        
    public MyBarButton()
    {
        Padding = new Thickness(10, 5);
    }

}