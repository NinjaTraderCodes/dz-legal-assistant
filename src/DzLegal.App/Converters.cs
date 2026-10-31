using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace DzLegal.App;

public static class Converters
{
    public static readonly IValueConverter SectionAlignment = new FuncValueConverter<Domain.BlockAlign, Avalonia.Media.TextAlignment>(align => align switch
    {
        Domain.BlockAlign.Center or Domain.BlockAlign.End => Avalonia.Media.TextAlignment.Center,
        Domain.BlockAlign.Justify => Avalonia.Media.TextAlignment.Justify,
        _ => Avalonia.Media.TextAlignment.Right,
    });

    public static readonly IValueConverter SectionWeight = new FuncValueConverter<bool, Avalonia.Media.FontWeight>(bold =>
        bold ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal);

    public static readonly IValueConverter SectionSpacing = new FuncValueConverter<float, Avalonia.Thickness>(points => new(0, points * 1.12, 0, 0));

    public static readonly IValueConverter SectionWidth = new FuncValueConverter<Domain.BlockAlign, double>(align => align == Domain.BlockAlign.End ? 260 : double.PositiveInfinity);

    public static readonly IValueConverter SectionPosition = new FuncValueConverter<Domain.BlockAlign, Avalonia.Layout.HorizontalAlignment>(align =>
        align == Domain.BlockAlign.End ? Avalonia.Layout.HorizontalAlignment.Left : Avalonia.Layout.HorizontalAlignment.Stretch);

    public static readonly IValueConverter Strike =
        new FuncValueConverter<bool, Avalonia.Media.TextDecorationCollection?>(on => on ? Avalonia.Media.TextDecorations.Strikethrough : null);

    public static readonly IValueConverter RightToLeft =
        new FuncValueConverter<bool, Avalonia.Media.FlowDirection>(rtl => rtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight);

    public static readonly IValueConverter DarkTheme =
        new FuncValueConverter<bool, Avalonia.Styling.ThemeVariant>(dark => dark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light);

    /// <summary>The law-library badge icon, chosen by name in its view model.</summary>
    public static readonly IValueConverter Symbol = new FuncValueConverter<string, FluentIcons.Common.Symbol>(
        name => Enum.TryParse<FluentIcons.Common.Symbol>(name, out var symbol) ? symbol : FluentIcons.Common.Symbol.Info);

    public static readonly IValueConverter IsZero = new FuncValueConverter<int, bool>(v => v == 0);

    public static readonly IValueConverter IsOne = new FuncValueConverter<int, bool>(v => v == 1);

    /// <summary>PNG bytes (a rendered PDF page) to an image.</summary>
    public static readonly IValueConverter PngToBitmap =
        new FuncValueConverter<byte[], Bitmap?>(png => png is null ? null : new Bitmap(new MemoryStream(png)));
}
