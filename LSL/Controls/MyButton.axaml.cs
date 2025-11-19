using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace LSL.Controls;

public enum MyButtonColorType
{
    Default,
    Highlight,
    Red
}
public class MyButton : Button
{
    public static readonly StyledProperty<MyButtonColorType> ColorTypeProperty =
        AvaloniaProperty.Register<MyButton, MyButtonColorType>(nameof(ColorType));

    public MyButtonColorType ColorType
    {
        get => GetValue(ColorTypeProperty);
        set => SetValue(ColorTypeProperty, value);
    }
    static MyButton()
    {
        ColorTypeProperty.Changed.AddClassHandler<MyButton>((o, _) => o.UpdateStyles());
        AffectsRender<MyButton>(ColorTypeProperty);
    }
    public MyButton()
    {
    }
    public MyButton(MyButtonColorType color, string content, ICommand command)
    {   
        this.ColorType = color;
        this.Content = content;
        this.Command = command;
    }

    public MyButton(string color, string content, ICommand command) : this(
        Enum.TryParse(color, out MyButtonColorType colorType) ? colorType : MyButtonColorType.Default, content,
        command) {}

    //根据ColorType更改按钮样式
    private void UpdateStyles()
    {
        PseudoClasses.Set(":highlight", ColorType == MyButtonColorType.Highlight);
        PseudoClasses.Set(":red", ColorType == MyButtonColorType.Red);
    }
}