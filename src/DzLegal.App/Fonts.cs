using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace DzLegal.App;

public static class Fonts
{
    /// <summary>Bundled Noto Naskh Arabic (SIL OFL), so Arabic renders the same on every machine, offline.</summary>
    public static readonly Uri ArabicCollection = new("fonts:Naskh");

    public static AppBuilder WithArabicFont(this AppBuilder builder) =>
        builder.ConfigureFonts(manager =>
        {
            manager.AddFontCollection(new EmbeddedFontCollection(ArabicCollection, new Uri("avares://DzLegalAssistant/Assets/Fonts")));
            manager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:Plex"), new Uri("avares://DzLegalAssistant/Assets/UiFonts")));
        });
}
