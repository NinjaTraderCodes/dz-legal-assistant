using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace DzLegal.App.Views;

public sealed class SetupImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes) return null;
        try
        {
            using var stream = new MemoryStream(bytes);
            return Bitmap.DecodeToWidth(stream, 128);
        }
        // Treat the decoder's truncated-image failure like any other unreadable saved logo.
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException or IOException or NullReferenceException)
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
