using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DzLegal.Storage;

namespace DzLegal.App.Services;

/// <summary>Coordinates the existing Avalonia theme and the workbench's existing theme command.</summary>
public sealed partial class ThemeController : ObservableObject, IDisposable
{
    private readonly Application _application;
    private readonly ThemePreferences _preferences;
    private readonly Dictionary<Window, MainWindowViewModel?> _windows = [];
    private bool _updating;

    public ThemeController(Application application, ThemePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(preferences);
        _application = application;
        _preferences = preferences;
        IsDark = preferences.LoadDarkTheme();
        application.RequestedThemeVariant = CurrentVariant;
    }

    [ObservableProperty]
    public partial bool IsDark { get; private set; }

    [ObservableProperty]
    public partial bool PreferenceSaveFailed { get; private set; }

    public ThemeVariant CurrentVariant => IsDark ? ThemeVariant.Dark : ThemeVariant.Light;

    [RelayCommand]
    private void Toggle() => SetDarkTheme(!IsDark);

    public void SetDarkTheme(bool dark)
    {
        if (_updating || IsDark == dark)
        {
            return;
        }

        _updating = true;
        try
        {
            IsDark = dark;
            _application.RequestedThemeVariant = CurrentVariant;
            foreach (var vm in _windows.Values.OfType<MainWindowViewModel>())
            {
                vm.IsDarkTheme = dark;
            }

            PreferenceSaveFailed = !_preferences.Save(dark);
        }
        finally
        {
            _updating = false;
        }
    }

    public void TrackWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (!_windows.TryAdd(window, null))
        {
            return;
        }

        window.DataContextChanged += OnDataContextChanged;
        window.Closed += OnWindowClosed;
        Connect(window);
    }

    private void Connect(Window window)
    {
        if (_windows[window] is { } previous)
        {
            previous.PropertyChanged -= OnWorkbenchChanged;
        }

        var vm = window.DataContext as MainWindowViewModel;
        _windows[window] = vm;
        if (vm is not null)
        {
            vm.IsDarkTheme = IsDark;
            vm.PropertyChanged += OnWorkbenchChanged;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (sender is Window window) Connect(window);
    }

    private void OnWorkbenchChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsDarkTheme) && sender is MainWindowViewModel vm)
        {
            SetDarkTheme(vm.IsDarkTheme);
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is Window window) Untrack(window);
    }

    private void Untrack(Window window)
    {
        window.DataContextChanged -= OnDataContextChanged;
        window.Closed -= OnWindowClosed;
        if (_windows.Remove(window, out var vm) && vm is not null)
        {
            vm.PropertyChanged -= OnWorkbenchChanged;
        }
    }

    public void Dispose()
    {
        foreach (var window in _windows.Keys.ToArray()) Untrack(window);
    }
}
