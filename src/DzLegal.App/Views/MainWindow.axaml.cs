using Avalonia.Controls;
using Avalonia.Threading;

namespace DzLegal.App.Views;

public partial class MainWindow : Window
{
    private void EnginePickerLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is ComboBox picker) picker.Focus();
    }

    private void EngineFlyoutOpened(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => this.FindControl<ComboBox>("EnginePicker")?.Focus(), DispatcherPriority.Background);

    private readonly DispatcherTimer _autosave = new() { Interval = TimeSpan.FromSeconds(30) };

    public MainWindow() : this(null)
    {
    }

    public MainWindow(string? layoutFile)
    {
        InitializeComponent();
        _workbench = this.FindControl<Grid>("WorkbenchGrid")!;
        _editorTools = this.FindControl<Grid>("EditorToolsGrid")!;
        _layoutStore = layoutFile is null ? null : new Services.WorkbenchLayoutStore(layoutFile);
        _layout = _layoutStore?.Load() ?? new Services.WorkbenchLayout();
        _preferredCompact = _layout.OutlineCompact;
        DataContextChanged += (_, _) => AttachWorkbench();
        SizeChanged += (_, _) => ApplyWorkbenchLayout();
        _workbench.SizeChanged += (_, _) => ApplyWorkbenchLayout();
        this.FindControl<ScrollViewer>("FormScroll")!.SizeChanged += (_, _) => FitNarrativeEditors();
        WireWorkbenchSplitters();

        // Autosave every 30 s and when the window closes, so a crash or an accidental close loses nothing.
        _autosave.Tick += (_, _) => (DataContext as MainWindowViewModel)?.AutosaveNow();
        Opened += async (_, _) =>
        {
            ApplyWorkbenchLayout();
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
            CaptureWorkbenchLayout();
            if (_workbenchVm is not null)
            {
                _workbenchVm.PropertyChanged -= WorkbenchPropertyChanged;
            }
            _autosave.Stop();
            if (DataContext is MainWindowViewModel vm)
            {
                vm.AutosaveNow();
                vm.ReleaseModels();
                vm.Dispose();
            }
        };
    }
}
