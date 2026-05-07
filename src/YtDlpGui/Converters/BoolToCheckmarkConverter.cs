using System.Globalization;
using Avalonia.Data.Converters;

namespace YtDlpGui.Converters;

public class BoolToCheckmarkConverter : IValueConverter
{
    public static readonly BoolToCheckmarkConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "✓" : "";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
