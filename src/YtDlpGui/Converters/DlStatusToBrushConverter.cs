using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using YtDlpGui.Core.Models;

namespace YtDlpGui.Converters;

public class DlStatusToBrushConverter : IValueConverter
{
    public static readonly DlStatusToBrushConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value is DlStatus s ? s : DlStatus.Queued;
        var key = status switch
        {
            DlStatus.Downloading => "BrushDlBg",
            DlStatus.Complete    => "BrushDoneBg",
            DlStatus.Error       => "BrushErrBg",
            DlStatus.Cancelled   => "BrushCancelBg",
            _                    => "BrushQueued"
        };
        var theme = Application.Current?.ActualThemeVariant ?? ThemeVariant.Dark;
        object? resource = null;
        Application.Current?.TryGetResource(key, theme, out resource);
        return resource as IBrush ?? Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
