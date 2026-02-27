using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace LSL.Views.Server;

public partial class ServerConf : UserControl
{
    public ServerConf()
    {
        InitializeComponent();
    }
}

public class JvmParamConverter : IValueConverter
{
    public static readonly JvmParamConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, 
        CultureInfo culture)
    {
        if (value is IEnumerable<string> source && targetType.IsAssignableTo(typeof(string)))
        {
            return string.Join(' ', source);
        }
        // converter used for the wrong type
        return new BindingNotification(new InvalidCastException(), 
            BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, 
        object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return s.Split(
                ["\r\n", "\r", "\n"],
                StringSplitOptions.RemoveEmptyEntries
            ).Select(line => line.Trim()).ToList();
        }
        throw new InvalidCastException();
    }
}