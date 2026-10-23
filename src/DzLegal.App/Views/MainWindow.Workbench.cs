using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DzLegal.App.Services;

namespace DzLegal.App.Views;

public partial class MainWindow
{
    private readonly WorkbenchLayoutStore? _layoutStore;
    private WorkbenchLayout _layout;
    private readonly Grid _workbench;
    private readonly Grid _editorTools;
    private MainWindowViewModel? _workbenchVm;
    private bool _applyingLayout;
    private bool? _preferredCompact;

    private double Metric(string name) => (double)this.FindResource(name)!;

    private void FitNarrativeEditors()
    {
        var scroll = this.FindControl<ScrollViewer>("FormScroll")!;
        var limit = Math.Clamp(scroll.Bounds.Height - Metric("FieldChromeHeight"), Metric("NarrativeMinHeight"), Metric("NarrativeMaxHeight"));
        var padding = (Thickness)this.FindResource("Inset12_8")!;
        var edge = (Thickness)this.FindResource("EdgeThin")!;
        var frame = padding.Top + padding.Bottom + edge.Top + edge.Bottom;
        limit = Math.Max(Metric("NarrativeMinHeight"), Math.Floor((limit - frame) / Metric("InputLineHeight")) * Metric("InputLineHeight") + frame);
        foreach (var input in scroll.GetVisualDescendants().OfType<TextBox>().Where(t => t.DataContext is FieldViewModel { IsMultiline: true }))
        {
            input.MaxHeight = limit;
        }
    }

    private void AttachWorkbench()
    {
        if (_workbenchVm is not null)
        {
            _workbenchVm.PropertyChanged -= WorkbenchPropertyChanged;
        }

        _workbenchVm = DataContext as MainWindowViewModel;
        if (_workbenchVm is not null)
        {
            _workbenchVm.PropertyChanged += WorkbenchPropertyChanged;
            _applyingLayout = true;
            _workbenchVm.IsOutlineVisible = _layout.OutlineVisible;
            _workbenchVm.IsOutlineCompact = _preferredCompact ?? Width < Metric("OutlineBreakpoint");
            _workbenchVm.WorkbenchTool = _layout.Tool;
            _workbenchVm.IsToolsOpen = _layout.ToolsOpen;
            _applyingLayout = false;
            ApplyWorkbenchLayout();
        }
    }

    private void WorkbenchPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_applyingLayout)
        {
            return;
        }

        if (e.PropertyName == nameof(MainWindowViewModel.IsDraftDetached))
        {
            ApplyDraftWindow();
        }

        if (e.PropertyName == nameof(MainWindowViewModel.HasProposal) && _workbenchVm?.HasProposal == true)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => this.FindControl<Button>("AcceptButton")?.BringIntoView());
        }

        if (e.PropertyName == nameof(MainWindowViewModel.SelectedStep))
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(FitNarrativeEditors, Avalonia.Threading.DispatcherPriority.Background);
        }

        if (e.PropertyName is nameof(MainWindowViewModel.IsOutlineCompact)
            or nameof(MainWindowViewModel.IsOutlineVisible) or nameof(MainWindowViewModel.IsDraftMaximized)
            or nameof(MainWindowViewModel.IsToolsOpen) or nameof(MainWindowViewModel.WorkbenchTool))
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsOutlineCompact))
            {
                _preferredCompact = _workbenchVm?.IsOutlineCompact;
            }

            ApplyWorkbenchLayout();
            SaveWorkbenchLayout();
        }
    }

    private void ApplyWorkbenchLayout()
    {
        if (_applyingLayout || _workbenchVm is not { } vm)
        {
            return;
        }

        _applyingLayout = true;
        try
        {
            var width = ClientSize.Width > 0 ? ClientSize.Width : Width;
            var height = _workbench.Bounds.Height > 0 ? _workbench.Bounds.Height : Height - Metric("ChromeMinHeight");
            var editorMin = Metric("EditorMinWidth");
            var splitterSize = Metric("SplitterSize");
            var toolsMin = Metric("ToolsMinHeight");
            if (_preferredCompact is null)
            {
                vm.IsOutlineCompact = width < Metric("OutlineBreakpoint");
            }

            var focus = vm.IsDraftMaximized;
            var outline = !vm.IsOutlineVisible || focus ? 0 : vm.IsOutlineCompact ? Metric("OutlineCompactWidth") : _layout.OutlineWidth;
            var editor = focus ? 0 : Math.Clamp(_layout.EditorWidth, editorMin, Math.Max(editorMin, width - outline - 2 * splitterSize - Metric("DraftMinWidth")));
            _workbench.ColumnDefinitions[0].MinWidth = outline == 0 ? 0 : vm.IsOutlineCompact ? Metric("OutlineCompactWidth") : Metric("OutlineMinWidth");
            _workbench.ColumnDefinitions[0].MaxWidth = outline == 0 ? 0 : vm.IsOutlineCompact ? Metric("OutlineCompactWidth") : Metric("OutlineMaxWidth");
            _workbench.ColumnDefinitions[0].Width = new GridLength(outline);
            _workbench.ColumnDefinitions[1].Width = new GridLength(outline == 0 ? 0 : splitterSize);
            _workbench.ColumnDefinitions[2].MinWidth = focus ? 0 : editorMin;
            _workbench.ColumnDefinitions[2].MaxWidth = focus ? 0 : Metric("EditorMaxWidth");
            _workbench.ColumnDefinitions[2].Width = new GridLength(editor);
            _workbench.ColumnDefinitions[3].Width = new GridLength(focus ? 0 : splitterSize);
            _workbench.ColumnDefinitions[4].Width = new GridLength(1, GridUnitType.Star);
            this.FindControl<Border>("OutlinePane")!.IsVisible = outline > 0;
            this.FindControl<GridSplitter>("OutlineSplitter")!.IsVisible = outline > 0;
            _editorTools.IsVisible = !focus;
            this.FindControl<GridSplitter>("DraftSplitter")!.IsVisible = !focus;

            var toolsLimit = Math.Max(toolsMin, Math.Min(height * .44, height - Metric("FormMinHeight") - splitterSize - this.FindControl<Grid>("ToolsStrip")!.Bounds.Height));
            var tools = vm.IsToolsOpen ? Math.Clamp(_layout.ToolsHeight, toolsMin, toolsLimit) : 0;
            _editorTools.RowDefinitions[1].Height = new GridLength(tools > 0 ? splitterSize : 0);
            _editorTools.RowDefinitions[2].Height = new GridLength(tools);
            _editorTools.RowDefinitions[2].MinHeight = tools > 0 ? toolsMin : 0;
            _editorTools.RowDefinitions[2].MaxHeight = tools > 0 ? toolsLimit : 0;
            this.FindControl<GridSplitter>("ToolsSplitter")!.IsVisible = tools > 0;
        }
        finally
        {
            _applyingLayout = false;
        }
    }

    private void CaptureWorkbenchLayout()
    {
        if (_applyingLayout || _workbenchVm is not { IsDraftMaximized: false } vm)
        {
            return;
        }

        _layout = _layout with
        {
            OutlineWidth = vm.IsOutlineCompact || !vm.IsOutlineVisible ? _layout.OutlineWidth : Math.Clamp(_workbench.ColumnDefinitions[0].ActualWidth, Metric("OutlineMinWidth"), Metric("OutlineMaxWidth")),
            EditorWidth = Math.Clamp(_workbench.ColumnDefinitions[2].ActualWidth, Metric("EditorMinWidth"), Metric("EditorMaxWidth")),
            ToolsHeight = vm.IsToolsOpen ? Math.Clamp(_editorTools.RowDefinitions[2].ActualHeight, Metric("ToolsMinHeight"), Metric("ToolsMaxHeight")) : _layout.ToolsHeight,
        };
        SaveWorkbenchLayout();
    }

    private void SaveWorkbenchLayout()
    {
        if (_applyingLayout || _workbenchVm is not { } vm)
        {
            return;
        }

        _layout = _layout with { OutlineCompact = _preferredCompact, OutlineVisible = vm.IsOutlineVisible, ToolsOpen = vm.IsToolsOpen, Tool = vm.WorkbenchTool };
        _layoutStore?.Save(_layout);
    }

    private void WireWorkbenchSplitters()
    {
        foreach (var name in new[] { "OutlineSplitter", "DraftSplitter", "ToolsSplitter" })
        {
            var splitter = this.FindControl<GridSplitter>(name)!;
            splitter.DragCompleted += (_, _) => CaptureWorkbenchLayout();
            splitter.DoubleTapped += (_, e) =>
            {
                var defaults = new WorkbenchLayout();
                _layout = name switch
                {
                    "OutlineSplitter" => _layout with { OutlineWidth = defaults.OutlineWidth },
                    "DraftSplitter" => _layout with { EditorWidth = defaults.EditorWidth },
                    _ => _layout with { ToolsHeight = defaults.ToolsHeight },
                };
                if (name == "OutlineSplitter") _preferredCompact = null;
                ApplyWorkbenchLayout();
                SaveWorkbenchLayout();
                e.Handled = true;
            };
            splitter.AddHandler(KeyUpEvent, (_, e) =>
            {
                if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
                {
                    CaptureWorkbenchLayout();
                }
            }, RoutingStrategies.Bubble, handledEventsToo: true);
        }
    }
}
