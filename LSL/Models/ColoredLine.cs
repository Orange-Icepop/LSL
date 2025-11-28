using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.Models;

public class ColoredLine : ReactiveObject
{
    [Reactive] public string Line { get; init; }
    [Reactive] public ISolidColorBrush LineColor { get; init; }
    public ColoredLine(string line, ISolidColorBrush lineColor)
    {
        Line = line;
        LineColor = lineColor;
    }
    public ColoredLine(string line, string colorHex)
    {
        Line = line;
        LineColor = new SolidColorBrush(Color.Parse(colorHex));
    }
}