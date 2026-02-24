using System.ComponentModel;
using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace LSL.Models;

public partial class ColoredLine : ReactiveObject
{
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

    [Reactive]
    private string _line;

    [Reactive]
    private ISolidColorBrush _lineColor;
}