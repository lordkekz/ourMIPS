using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ourMIPSSharp_App.Converters;

/// <summary>
/// Based on <a href="https://github.com/AvaloniaUI/Avalonia/discussions/9875">this thread</a>.
/// </summary>
public class DataChangedHighlightRowConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        Debug.WriteLine(value);
        return value is true ? new SolidColorBrush(Colors.Green, .3) : Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return null;
    }
}