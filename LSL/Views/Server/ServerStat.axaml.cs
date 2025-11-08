using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace LSL.Views.Server;

public partial class ServerStat : UserControl
{
    public ServerStat()
    {
        InitializeComponent();
    }
}

public sealed class CurrentCpuMetricsConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Count != 1 || !targetType.IsAssignableFrom(typeof(string))) 
            throw new NotSupportedException();
        if (values[0] is not double cu) return BindingOperations.DoNothing;
        cu = Math.Round(cu, 1);
        return $"CPU占用：{cu}%";
    }
}
public sealed class CurrentMemMetricsConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Count != 3 || !targetType.IsAssignableFrom(typeof(string))) 
            throw new NotSupportedException();
        if (values[0] is not double ru ||
            values[1] is not long rv ||
            values[2] is not long rm) return BindingOperations.DoNothing;
        ru = Math.Round(ru, 1);
        double crv = Math.Round((double)rv / 1024 / 1024, 1);
        double crm = Math.Round((double)rm / 1024 / 1024, 1);
        return $"内存占用：{ru}% ({crv}/{crm}MB)";
    }
}