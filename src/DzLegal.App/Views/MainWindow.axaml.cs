using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace DzLegal.App.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _autosave = new() { Interval = TimeSpan.FromSeconds(30) };
    private MainWindowViewModel? _observed;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ObserveViewModel();
        SizeChanged += (_, _) => UpdateWorkbenchLayout();
        Closed += (_, _) => StopObserving();
        UpdateWorkbenchLayout();

        // Autosave every 30 s and when the window closes, so a crash or an accidental close loses nothing.
        _autosave.Tick += (_, _) => (DataContext as MainWindowViewModel)?.AutosaveNow();
        Opened += async (_, _) =>
        {
            _autosave.Start();
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.CheckModelStatusAsync();
            }
        };

        // Re-check the local engine every 30 s too (Ollama may be started after the app).
        _autosave.Tick += async (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.CheckModelStatusAsync();
            }
        };
        Closing += (_, _) =>
        {
            _autosave.Stop();
            if (DataContext is MainWindowViewModel vm)
            {
                vm.AutosaveNow();
                vm.ReleaseModels();
                vm.Dispose();
            }
        };
    }

    private void ObserveViewModel()
    {
        StopObserving();
        _observed = DataContext as MainWindowViewModel;
        if (_observed is not null)
        {
            _observed.PropertyChanged += OnViewModelChanged;
            _observed.Sections.CollectionChanged += OnSectionsChanged;
        }
        ResetActiveSection();
        UpdateWorkbenchLayout();
    }

    private void StopObserving()
    {
        if (_observed is null) return;
        _observed.PropertyChanged -= OnViewModelChanged;
        _observed.Sections.CollectionChanged -= OnSectionsChanged;
        _observed = null;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedStep))
        {
            var section = _observed?.Sections.FirstOrDefault(s => s.Definition.FormGroup == _observed.SelectedStep?.Id);
            if (section is not null) ActiveFieldTools.Content = section;
        }
        if (e.PropertyName == nameof(MainWindowViewModel.IsRightToLeft)) UpdateWorkbenchLayout();
    }

    private void OnSectionsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ResetActiveSection();

    private void ResetActiveSection()
    {
        if (ActiveFieldTools.Content is not SectionViewModel active || _observed?.Sections.Contains(active) != true)
            ActiveFieldTools.Content = _observed?.Sections.FirstOrDefault();
    }

    private void UpdateWorkbenchLayout()
    {
        var width = Bounds.Width > 0 ? Bounds.Width : Width;
        Classes.Set("compact", width < 1500);
        Classes.Set("tight", width < 1280);
        Classes.Set("arabic", _observed?.IsRightToLeft == true);
        var left = width < 1280 ? 300 : 390;
        var right = width < 1280 ? 270 : 330;
        if (WorkspaceGrid.ColumnDefinitions[0].Width.Value != left)
            WorkspaceGrid.ColumnDefinitions[0].Width = new GridLength(left);
        if (WorkspaceGrid.ColumnDefinitions[2].Width.Value != right)
            WorkspaceGrid.ColumnDefinitions[2].Width = new GridLength(right);
    }

    private void OnSectionPointerPressed(object? sender, PointerPressedEventArgs e) => SelectSection(sender);

    private void OnSectionGotFocus(object? sender, RoutedEventArgs e) => SelectSection(sender);

    private void SelectSection(object? sender)
    {
        if (sender is Control { DataContext: SectionViewModel section }) ActiveFieldTools.Content = section;
    }
}
