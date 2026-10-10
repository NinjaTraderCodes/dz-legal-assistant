using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace DzLegal.Presentation;

/// <summary>
/// UI strings from Resources/Strings*.resx. Arabic is the neutral language; English is a satellite.
/// Adding a language is a translation task: add Strings.&lt;code&gt;.resx, no code change.
/// Bind with <c>{Binding L[Key]}</c>; switching language notifies every binding.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    private static readonly ResourceManager Resources = new("DzLegal.Presentation.Resources.Strings", typeof(Loc).Assembly);

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("ar-DZ");

    /// <summary>Two-letter language code ("ar" or "en"), used to pick <see cref="Domain.LocalizedText"/> values.</summary>
    public string Language => Culture.TwoLetterISOLanguageName;

    public bool IsRightToLeft => Culture.TextInfo.IsRightToLeft;

    public string this[string key] => Resources.GetString(key, Culture) ?? $"[{key}]";

    public string Format(string key, params object?[] args) => string.Format(Culture, this[key], args);

    public void SetLanguage(string code)
    {
        Culture = CultureInfo.GetCultureInfo(code == "en" ? "en-GB" : "ar-DZ");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRightToLeft)));
    }
}
