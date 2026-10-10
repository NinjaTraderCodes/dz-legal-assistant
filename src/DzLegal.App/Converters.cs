using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace DzLegal.App;

public static class Converters
{
    public static readonly IValueConverter Strike =
        new FuncValueConverter<bool, Avalonia.Media.TextDecorationCollection?>(on => on ? Avalonia.Media.TextDecorations.Strikethrough : null);

    public static readonly IValueConverter RightToLeft =
        new FuncValueConverter<bool, Avalonia.Media.FlowDirection>(rtl => rtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight);

    public static readonly IValueConverter DarkTheme =
        new FuncValueConverter<bool, Avalonia.Styling.ThemeVariant>(dark => dark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light);

    public static readonly IValueConverter IsZero = new FuncValueConverter<int, bool>(v => v == 0);

    public static readonly IValueConverter IsOne = new FuncValueConverter<int, bool>(v => v == 1);

    /// <summary>PNG bytes (a rendered PDF page) to an image.</summary>
    public static readonly IValueConverter PngToBitmap =
        new FuncValueConverter<byte[], Bitmap?>(png => png is null ? null : new Bitmap(new MemoryStream(png)));
}
