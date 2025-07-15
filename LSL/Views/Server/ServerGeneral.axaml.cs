using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace LSL.Views.Server;

public partial class ServerGeneral : UserControl
{
    public ServerGeneral()
    {
        InitializeComponent();
    }
}

public sealed class GeneralCpuMetricsConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Count != 1 || !targetType.IsAssignableFrom(typeof(string))) 
            throw new NotSupportedException();
        if (values[0] is not uint cu) return BindingOperations.DoNothing;
        return $"CPU总占用：{cu}%";
    }
}

public sealed class GeneralMemMetricsConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Count != 3 || !targetType.IsAssignableFrom(typeof(string))) 
            throw new NotSupportedException();
        if (values[0] is not uint ru ||
            values[1] is not long rv ||
            values[2] is not long sr) return BindingOperations.DoNothing;
        float crv = (float)Math.Round((double)rv / 1024 / 1024 / 1024, 1);
        float crm = (float)Math.Round((double)sr / 1024 / 1024 / 1024, 1);
        return $"内存总占用：{ru}% ({crv}/{crm}GB)";
    }
}
