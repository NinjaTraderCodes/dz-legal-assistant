using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace DzLegal.App.Views;

/// <summary>Optional explanation only. Blocking notes remain separate permanent elements in the field template.</summary>
public partial class FieldHelpView : UserControl
{
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<FieldHelpView, string?>(nameof(Label));
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<FieldHelpView, string?>(nameof(Text));
    private readonly Border _disclosure;
    private readonly Button _button;
    private bool _pinned;

    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public bool IsHelpOpen => _disclosure.IsVisible;

    public FieldHelpView()
    {
        InitializeComponent();
        _disclosure = this.FindControl<Border>("HelpDisclosure")!;
        _button = this.FindControl<Button>("HelpButton")!;
        _button.PointerEntered += (_, _) => OpenHelp();
        _button.GotFocus += (_, _) => OpenHelp();
        _button.LostFocus += (_, _) => { if (!_pinned && !IsPointerOver) CloseHelp(); };
        PointerExited += (_, _) => { if (!_pinned && !_button.IsFocused) CloseHelp(); };
        _button.Click += (_, _) =>
        {
            _pinned = !_pinned;
            if (_pinned) OpenHelp(); else CloseHelp();
        };
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Escape && IsHelpOpen)
            {
                _pinned = false;
                CloseHelp();
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
    }

    private void OpenHelp() => _disclosure.IsVisible = !string.IsNullOrWhiteSpace(Text);
    private void CloseHelp() => _disclosure.IsVisible = false;
}
