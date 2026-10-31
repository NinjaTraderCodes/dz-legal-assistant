using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DzLegal.Presentation.ViewModels;

namespace DzLegal.App.Views;

public partial class TourOverlay : UserControl
{
    private TourViewModel? _model;
    private IInputElement? _previousFocus;
    private object? _previousContext;
    private Window? _window;
    private Size _positionedSize;
    private string? _positionedAnnouncement;
    public Rect? HighlightBounds { get; private set; }

    public TourOverlay()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Observe();
        SizeChanged += (_, _) => PositionCard();
        LayoutUpdated += (_, _) => PositionCard();
        AttachedToVisualTree += (_, _) =>
        {
            _window = TopLevel.GetTopLevel(this) as Window;
            _window?.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            PositionCard();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            _window?.RemoveHandler(KeyDownEvent, OnKeyDown);
            _window = null;
            Unobserve();
        };
    }

    private void Observe()
    {
        Unobserve();
        _model = DataContext as TourViewModel;
        if (_model is null) return;
        _model.Opening += Opening;
        _model.Closed += Closed;
        _model.PropertyChanged += Changed;
    }
    private void Unobserve()
    {
        if (_model is null) return;
        _model.Opening -= Opening; _model.Closed -= Closed; _model.PropertyChanged -= Changed;
        _model = null;
    }
    private void Opening(object? sender, EventArgs e)
    {
        _previousFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        _previousContext = (_previousFocus as Control)?.DataContext;
    }
    private void Closed(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        if (_previousFocus is Control { IsEffectivelyVisible: true } control && TopLevel.GetTopLevel(control) is not null) control.Focus();
        else if (_previousContext is not null)
            _window?.GetVisualDescendants().OfType<Control>().FirstOrDefault(control => control.Focusable && control.IsEffectivelyVisible && ReferenceEquals(control.DataContext, _previousContext))?.Focus();
    }, DispatcherPriority.Loaded);
    private void Changed(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TourViewModel.AnchorNames) or nameof(TourViewModel.IsOpen) or nameof(TourViewModel.IsRightToLeft))
            Dispatcher.UIThread.Post(() => { PositionCard(); if (_model?.IsOpen == true) TourCard.Focus(); }, DispatcherPriority.Loaded);
    }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_model?.IsOpen != true) return;
        if (e.Key == Key.Escape) _model.Close();
        else if (e.Key is Key.Left or Key.Right)
        {
            var next = e.Key == (_model.IsRightToLeft ? Key.Left : Key.Right);
            if (next) _model.NextCommand.Execute(null);
            else if (_model.BackCommand.CanExecute(null)) _model.BackCommand.Execute(null);
        }
        else if (e.Key == Key.Enter && ReferenceEquals(e.Source, TourCard)) _model.NextCommand.Execute(null);
        else if (e.KeyModifiers == KeyModifiers.None) return;
        e.Handled = true;
    }

    public void PositionCard()
    {
        if (_model?.IsOpen != true || Bounds.Width <= 0 || Bounds.Height <= 0 || _window is null) return;
        var origin = this.TranslatePoint(default, _window);
        if (origin is null) return;
        Rect? target = null;
        foreach (var name in _model.AnchorNames)
        {
            var control = _window.FindControl<Control>(name);
            if (control?.IsEffectivelyVisible != true) { target = null; break; }
            // Convert through the window: sibling-to-sibling translation includes an extra RTL offset in Avalonia.
            var first = control.TranslatePoint(default, _window);
            var last = control.TranslatePoint(new Point(control.Bounds.Width, control.Bounds.Height), _window);
            if (first is null || last is null) { target = null; break; }
            var rect = new Rect(Math.Min(first.Value.X, last.Value.X) - origin.Value.X,
                Math.Min(first.Value.Y, last.Value.Y) - origin.Value.Y,
                Math.Abs(first.Value.X - last.Value.X), Math.Abs(first.Value.Y - last.Value.Y)).Intersect(new Rect(Bounds.Size));
            if (rect.Width < 4 || rect.Height < 4) { target = null; break; }
            target = target?.Union(rect) ?? rect;
        }
        if (HighlightBounds == target && _positionedSize == Bounds.Size && _positionedAnnouncement == _model.Announcement) return;
        HighlightBounds = target;
        _positionedSize = Bounds.Size;
        _positionedAnnouncement = _model.Announcement;
        Shade.Children.Clear();
        void Dim(Rect rectangle)
        {
            var border = new Border { Width = rectangle.Width, Height = rectangle.Height };
            border.Bind(Border.BackgroundProperty, this.GetResourceObservable("OnboardingTourBackdropBrush"));
            Canvas.SetLeft(border, rectangle.X); Canvas.SetTop(border, rectangle.Y); Shade.Children.Add(border);
        }
        if (target is { } anchorRect)
        {
            var gap = (double)this.FindResource("OnboardingTourHighlightGap")!;
            var t = anchorRect.Inflate(gap).Intersect(new Rect(Bounds.Size));
            Dim(new Rect(0, 0, Bounds.Width, t.Top));
            Dim(new Rect(0, t.Bottom, Bounds.Width, Math.Max(0, Bounds.Height - t.Bottom)));
            Dim(new Rect(0, t.Top, t.Left, t.Height));
            Dim(new Rect(t.Right, t.Top, Math.Max(0, Bounds.Width - t.Right), t.Height));
            var outline = new Border { Width = t.Width, Height = t.Height };
            outline.Bind(Border.BorderThicknessProperty, this.GetResourceObservable("OnboardingFocusThickness"));
            outline.Bind(Border.CornerRadiusProperty, this.GetResourceObservable("ControlRadius"));
            outline.Bind(Border.BorderBrushProperty, this.GetResourceObservable("OnboardingHighlightBrush"));
            Canvas.SetLeft(outline, t.X); Canvas.SetTop(outline, t.Y); Shade.Children.Add(outline);
        }
        else Dim(new Rect(Bounds.Size));
        var edgeSpace = (double)this.FindResource("OnboardingTourEdgeSpace")!;
        var minWidth = (double)this.FindResource("OnboardingTourMinWidth")!;
        var anchorGap = (double)this.FindResource("OnboardingTourAnchorGap")!;
        var maxWidth = Math.Min(TourCard.MaxWidth, Bounds.Width - edgeSpace * 2);
        TourCard.Width = maxWidth;
        TourCard.Measure(new Size(maxWidth, Bounds.Height - edgeSpace * 2));
        var size = TourCard.DesiredSize;
        var x = (Bounds.Width - size.Width) / 2;
        var y = (Bounds.Height - size.Height) / 2;
        if (target is { } anchor)
        {
            var rightSpace = Bounds.Width - edgeSpace - (anchor.Right + anchorGap);
            var leftSpace = anchor.Left - anchorGap - edgeSpace;
            if (rightSpace >= size.Width) x = anchor.Right + anchorGap;
            else if (leftSpace >= size.Width) x = anchor.Left - anchorGap - size.Width;
            else if (anchor.Bottom + anchorGap + size.Height <= Bounds.Height - edgeSpace) y = anchor.Bottom + anchorGap;
            else if (anchor.Top - anchorGap - size.Height >= edgeSpace) y = anchor.Top - anchorGap - size.Height;
            else if (Math.Max(rightSpace, leftSpace) >= minWidth)
            {
                // A large anchor can leave less than the preferred card width. Narrow the card
                // on the wider side instead of covering the control the lawyer must see.
                TourCard.Width = Math.Max(rightSpace, leftSpace);
                TourCard.Measure(new Size(TourCard.Width, Bounds.Height - edgeSpace * 2));
                size = TourCard.DesiredSize;
                x = rightSpace >= leftSpace ? anchor.Right + anchorGap : anchor.Left - anchorGap - size.Width;
                y = Math.Max(edgeSpace, Math.Min((Bounds.Height - size.Height) / 2, Bounds.Height - edgeSpace - size.Height));
            }
        }
        Canvas.SetLeft(TourCard, x); Canvas.SetTop(TourCard, y);
    }
}
