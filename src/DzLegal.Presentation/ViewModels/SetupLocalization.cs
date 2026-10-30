using System.ComponentModel;
using System.Globalization;

namespace DzLegal.Presentation.ViewModels;

/// <summary>The shared application localization, with change notification for a screen that may close first.</summary>
public sealed class SetupLocalization : INotifyPropertyChanged
{
    public SetupLocalization(Loc? source = null)
    {
        Source = source ?? new Loc();
        // A closed screen must not be retained by the shared localization.
        var weak = new WeakReference<SetupLocalization>(this);
        PropertyChangedEventHandler? handler = null;
        handler = (sender, args) =>
        {
            if (weak.TryGetTarget(out var current)) current.PropertyChanged?.Invoke(current, args);
            else if (sender is Loc localization) localization.PropertyChanged -= handler;
        };
        Source.PropertyChanged += handler;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public Loc Source { get; }
    public string Language => Source.Language;
    public bool IsRightToLeft => Source.IsRightToLeft;
    public CultureInfo Culture => Source.Culture;
    public string this[string key] => Source[key];
    public string Format(string key, params object?[] values) => string.Format(Culture, this[key], values);
    public void SetLanguage(string language) => Source.SetLanguage(language);
}
