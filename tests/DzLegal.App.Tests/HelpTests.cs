using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DzLegal.App.Views;

namespace DzLegal.App.Tests;

public sealed class HelpTests
{
    [AvaloniaFact]
    public void OptionalHelpWorksWithHoverFocusClickAndEscape()
    {
        var help = new FieldHelpView { Label = "وقائع / Facts", Text = "اختبار / Optional explanation" };
        var window = new Window { Content = help, Width = 400, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var button = help.FindControl<Button>("HelpButton")!;
        // Focus alone opens an explanation. Escape closes it without moving focus.
        button.Focus();
        Assert.True(help.IsHelpOpen);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Assert.False(help.IsHelpOpen);
        Assert.Same(button, window.FocusManager!.GetFocusedElement());
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(help.IsHelpOpen);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.False(help.IsHelpOpen);
        button.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, button, new Pointer(1, PointerType.Mouse, true), button, default, 0, new PointerPointProperties(), KeyModifiers.None));
        Assert.True(help.IsHelpOpen);
        window.Close();
    }
}
