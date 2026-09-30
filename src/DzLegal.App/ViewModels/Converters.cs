using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DzLegal.App.ViewModels;

public static class Converters
{
    public static readonly IValueConverter ErrorBrush =
        new FuncValueConverter<bool, IBrush>(isError => isError ? Brushes.DarkRed : Brushes.Black);
}
