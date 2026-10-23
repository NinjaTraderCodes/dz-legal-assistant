using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
            _workbenchVm.IsOutlineCompact = _preferredCompact ?? Width < 1500;
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
            var height = _workbench.Bounds.Height > 0 ? _workbench.Bounds.Height : Math.Max(320, Height - 120);
            if (_preferredCompact is null)
            {
                vm.IsOutlineCompact = width < 1500;
            }

            var focus = vm.IsDraftMaximized;
            var outline = !vm.IsOutlineVisible || focus ? 0 : vm.IsOutlineCompact ? 64 : _layout.OutlineWidth;
            var editor = focus ? 0 : Math.Clamp(_layout.EditorWidth, 320, Math.Max(320, width - outline - 12 - 440));
            _workbench.ColumnDefinitions[0].MinWidth = outline == 0 ? 0 : vm.IsOutlineCompact ? 64 : 160;
            _workbench.ColumnDefinitions[0].MaxWidth = outline == 0 ? 0 : vm.IsOutlineCompact ? 64 : 360;
            _workbench.ColumnDefinitions[0].Width = new GridLength(outline);
            _workbench.ColumnDefinitions[1].Width = new GridLength(outline == 0 ? 0 : 6);
            _workbench.ColumnDefinitions[2].MinWidth = focus ? 0 : 320;
            _workbench.ColumnDefinitions[2].MaxWidth = focus ? 0 : 900;
            _workbench.ColumnDefinitions[2].Width = new GridLength(editor);
            _workbench.ColumnDefinitions[3].Width = new GridLength(focus ? 0 : 6);
            _workbench.ColumnDefinitions[4].Width = new GridLength(1, GridUnitType.Star);
            this.FindControl<Border>("OutlinePane")!.IsVisible = outline > 0;
            this.FindControl<GridSplitter>("OutlineSplitter")!.IsVisible = outline > 0;
            _editorTools.IsVisible = !focus;
            this.FindControl<GridSplitter>("DraftSplitter")!.IsVisible = !focus;

            var tools = vm.IsToolsOpen ? Math.Clamp(_layout.ToolsHeight, 120, Math.Max(120, height * .44)) : 0;
            _editorTools.RowDefinitions[1].Height = new GridLength(tools > 0 ? 6 : 0);
            _editorTools.RowDefinitions[2].Height = new GridLength(tools);
            _editorTools.RowDefinitions[2].MinHeight = tools > 0 ? 120 : 0;
            _editorTools.RowDefinitions[2].MaxHeight = tools > 0 ? Math.Max(120, height - 166) : 0;
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
            OutlineWidth = vm.IsOutlineCompact || !vm.IsOutlineVisible ? _layout.OutlineWidth : Math.Clamp(_workbench.ColumnDefinitions[0].ActualWidth, 160, 360),
            EditorWidth = Math.Clamp(_workbench.ColumnDefinitions[2].ActualWidth, 320, 900),
            ToolsHeight = vm.IsToolsOpen ? Math.Clamp(_editorTools.RowDefinitions[2].ActualHeight, 120, 500) : _layout.ToolsHeight,
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
                _layout = new WorkbenchLayout { OutlineVisible = _workbenchVm?.IsOutlineVisible ?? true, ToolsOpen = _workbenchVm?.IsToolsOpen ?? false, Tool = _workbenchVm?.WorkbenchTool ?? "review" };
                _preferredCompact = null;
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
