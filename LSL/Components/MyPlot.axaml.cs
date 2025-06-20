using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using OxyPlot;

namespace LSL.Components;

public partial class MyPlot : ContentControl
{
    private static readonly StyledProperty<PlotModel> SourceProperty = 
        AvaloniaProperty.Register<MyPlot, PlotModel>(nameof(Source), defaultBindingMode:BindingMode.OneWayToSource);
    
    public MyPlot()
    {
        
    }

    public PlotModel Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }
}