using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace DzLegal.App.ViewModels;

public static class Converters
{
    public static readonly IValueConverter ErrorBrush =
        new FuncValueConverter<bool, IBrush>(isError => isError ? Brushes.DarkRed : Brushes.Black);

    /// <summary>PNG bytes (a rendered PDF page) to an image.</summary>
    public static readonly IValueConverter PngToBitmap =
        new FuncValueConverter<byte[], Bitmap?>(png => png is null ? null : new Bitmap(new MemoryStream(png)));
}
