using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using DzLegal.App.Services;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace DzLegal.App.Views;

/// <summary>The workbench's bar button, shared by every desktop host.</summary>
public sealed class ThemeToggleButton : Button
{
    public static readonly StyledProperty<Loc?> LocalizationProperty =
        AvaloniaProperty.Register<ThemeToggleButton, Loc?>(nameof(Localization));

    private readonly SymbolIcon _icon = new();
    private ThemeController? _theme;
    private MainWindowViewModel? _workbench;
    private Loc? _observedLoc;

    public ThemeToggleButton()
    {
        Classes.Add("bar");
        Classes.Add("global-theme-toggle");
        Content = _icon;
        _icon.Bind(SymbolIcon.ForegroundProperty, new DynamicResourceExtension("AppBarTextBrush"));
        AttachedToVisualTree += (_, _) => Observe();
        DetachedFromVisualTree += (_, _) => StopObserving();
        DataContextChanged += (_, _) => Observe();
    }

    protected override Type StyleKeyOverride => typeof(Button);

    public Loc? Localization
    {
        get => GetValue(LocalizationProperty);
        set => SetValue(LocalizationProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LocalizationProperty) Observe();
    }

    private void Observe()
    {
        StopObserving();
        _theme = (Application.Current as App)?.Themes;
        _workbench = DataContext as MainWindowViewModel;
        _observedLoc = Localization ?? _workbench?.L;
        if (_theme is not null) _theme.PropertyChanged += OnChanged;
        if (_workbench is not null) _workbench.PropertyChanged += OnChanged;
        if (_observedLoc is not null) _observedLoc.PropertyChanged += OnChanged;
        // Keep standalone/headless workbench hosts and the web's view model contract unchanged.
        Command = _theme is not null ? _theme.ToggleCommand : _workbench?.ToggleThemeCommand;
        Refresh();
    }

    private void StopObserving()
    {
        if (_theme is not null) _theme.PropertyChanged -= OnChanged;
        if (_workbench is not null) _workbench.PropertyChanged -= OnChanged;
        if (_observedLoc is not null) _observedLoc.PropertyChanged -= OnChanged;
        _theme = null;
        _workbench = null;
        _observedLoc = null;
    }

    private void OnChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        var dark = _theme?.IsDark ?? _workbench?.IsDarkTheme ?? false;
        _icon.Symbol = dark ? Symbol.WeatherSunny : Symbol.WeatherMoon;
        var text = (_observedLoc ?? new Loc())[dark ? "ThemeSwitchToLight" : "ThemeSwitchToDark"];
        ToolTip.SetTip(this, text);
        AutomationProperties.SetName(this, text);
    }
}
