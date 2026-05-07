using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace YtDlpGui.Converters;

public class BoolToLogBrushConverter : IValueConverter
{
    public static readonly BoolToLogBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isError = value is true;
        var key     = isError ? "BrushLogError" : "BrushLogText";
        var theme   = Application.Current?.ActualThemeVariant ?? ThemeVariant.Dark;
        object? resource = null;
        Application.Current?.TryGetResource(key, theme, out resource);
        return resource as IBrush ?? Brushes.LightGray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
