using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DzLegal.App.Services;
using DzLegal.Presentation.ViewModels;
using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace DzLegal.App.Views;

public partial class SetupView : UserControl
{
    private SetupViewModel? _observedSetup;
    private ThemeController? _theme;
    private Border? _externalThemeHost;
    public SetupView()
        : this(WelcomeMotion.IsEnabled())
    {
    }

    public SetupView(bool animate)
    {
        InitializeComponent();
        // Resource names are case-sensitive, even on Windows. Accept the approved asset's
        // existing casing without renaming, copying, or rewriting the supplied files.
        var image = AssetLoader.GetAssets(new Uri("avares://DzLegalAssistant/"), null)
            .Single(uri => uri.AbsolutePath.Equals("/Assets/Images/welcome-bg.jpg", StringComparison.OrdinalIgnoreCase));
        using (var stream = AssetLoader.Open(image))
        {
            SetupBackground.Source = new Bitmap(stream);
        }

        if (animate)
        {
            Loaded += async (_, _) => await new Animation
            {
                Duration = TimeSpan.FromMilliseconds(120),
                Children =
                {
                    new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, 0d) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, 1d) } },
                },
            }.RunAsync(this);
        }

        DataContextChanged += (_, _) => ObserveSetup();
        AttachedToVisualTree += (_, _) =>
        {
            ObserveSetup();
            _theme = (Application.Current as App)?.Themes;
            if (_theme is not null) _theme.PropertyChanged += OnThemeChanged;
        };
        DetachedFromVisualTree += (_, _) =>
        {
            if (_observedSetup is not null) _observedSetup.PropertyChanged -= OnSetupPropertyChanged;
            _observedSetup = null;
            if (_theme is not null) _theme.PropertyChanged -= OnThemeChanged;
            _theme = null;
        };
        KeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape || DataContext is not SetupViewModel setup) return;
            if (setup.IsConfirmingImport) setup.CancelImportCommand.Execute(null);
            else if (setup.IsConfirmingLeave) setup.CancelLeaveCommand.Execute(null);
            else return;
            e.Handled = true;
        };
    }

    private void ObserveSetup()
    {
        if (_observedSetup is not null) _observedSetup.PropertyChanged -= OnSetupPropertyChanged;
        _observedSetup = DataContext as SetupViewModel;
        if (_observedSetup is not null) _observedSetup.PropertyChanged += OnSetupPropertyChanged;
        ShowStep();
    }

    private void OnSetupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SetupViewModel.Step)) ShowStep();
        if (e.PropertyName == nameof(SetupViewModel.IsConfirmingLeave) && _observedSetup?.IsConfirmingLeave == true)
            SaveBeforeLeaving.Focus();
        if (e.PropertyName == nameof(SetupViewModel.IsConfirmingImport) && _observedSetup?.IsConfirmingImport == true)
            AcceptProfileImport.Focus();
        if (e.PropertyName == nameof(SetupViewModel.IsRightToLeft)) PositionThemeHost();
    }

    /// <summary>Retains the launch host's shared toggle while the real setup fills its content.</summary>
    public Grid CreateHost()
    {
        if (_externalThemeHost is not null) throw new InvalidOperationException("Setup already has a host.");
        _externalThemeHost = SetupThemeHost;
        ((Panel)SetupThemeHost.Parent!).Children.Remove(SetupThemeHost);
        SetupThemeHost.Margin = new Thickness(48, 24);
        SetupThemeHost.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        SetupThemeHost.ZIndex = 1;
        PositionThemeHost();
        return new Grid { DataContext = DataContext, Children = { SetupThemeHost, this } };
    }

    private void PositionThemeHost()
    {
        if (_externalThemeHost is not null)
            _externalThemeHost.HorizontalAlignment = _observedSetup?.IsRightToLeft == true
                ? Avalonia.Layout.HorizontalAlignment.Left : Avalonia.Layout.HorizontalAlignment.Right;
    }

    private void ShowStep()
    {
        if (_observedSetup is not { } setup) { SetupStepHost.Content = null; return; }
        SetupStepHost.Content = setup.Step switch
        {
            0 => new SetupStepLawyerView { DataContext = setup },
            1 => new SetupStepOfficeView { DataContext = setup },
            2 => new SetupStepPrefsView { DataContext = setup },
            _ => null,
        };
        StepScroll.Offset = default;
    }

    private void OnThemeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ThemeController.IsDark) && _observedSetup is not null && _theme is not null)
            _observedSetup.ObserveTheme(_theme.IsDark);
    }

    private void GoLawyer(object? sender, RoutedEventArgs e) { if (_observedSetup is not null) _observedSetup.Step = 0; }
    private void GoOffice(object? sender, RoutedEventArgs e) { if (_observedSetup is not null) _observedSetup.Step = 1; }
    private void GoPreferences(object? sender, RoutedEventArgs e) { if (_observedSetup is not null) _observedSetup.Step = 2; }
}
