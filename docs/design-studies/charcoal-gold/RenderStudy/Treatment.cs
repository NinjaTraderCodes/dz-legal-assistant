using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace DzLegal.DesignStudy;

/// <summary>Review-only overrides applied to an instance of the unchanged production view.</summary>
internal static class Treatment
{
    private static SolidColorBrush Brush(string value) => new(Color.Parse(value));
    private static readonly SolidColorBrush Gold = Brush("#C6A76D");
    private static readonly SolidColorBrush Ink = Brush("#28231D");
    private static readonly SolidColorBrush Text = Brush("#F2EDE5");
    private static readonly SolidColorBrush Muted = Brush("#C2B8A9");
    private static readonly SolidColorBrush Border = Brush("#8C7B65");
    private static readonly SolidColorBrush PaperMuted = Brush("#6F6252");
    private static readonly SolidColorBrush PaperAccent = Brush("#78561F");

    public static void Prepare(Window window)
    {
        foreach (var (key, value) in new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CanvasBrush"] = "#171411", ["SurfaceBrush"] = "#24201B", ["SurfaceAltBrush"] = "#302A23",
            ["PaperBrush"] = "#FFFEFA", ["PaperCanvasBrush"] = "#1C1915", ["HairlineBrush"] = "#51483B",
            ["TextPrimaryBrush"] = "#F2EDE5", ["TextMutedBrush"] = "#C2B8A9",
            ["AppBarBrush"] = "#1D1812", ["AppBarTextBrush"] = "#F2EDE5",
            ["AccentBrush"] = "#D2B981", ["AccentSoftBrush"] = "#423724", ["PrimaryButtonBrush"] = "#C6A76D",
            ["DangerBrush"] = "#FFC0B3", ["DangerSoftBrush"] = "#462C27",
            ["WarningBrush"] = "#F2CB83", ["WarningSoftBrush"] = "#43331E",
            ["SuccessBrush"] = "#ACD3BD", ["SuccessSoftBrush"] = "#263B30",
        }) window.Resources[key] = Brush(value);
        window.Resources["SystemAccentColor"] = Color.Parse("#C6A76D");
        window.FontSize = 14;
        window.Styles.Add(new Style(s => s.OfType<Control>()) { Setters = { new Setter(TemplatedControl.TransitionsProperty, null) } });
        window.Styles.Add(new Style(s => s.OfType<TextBox>().Class("input"))
        {
            Setters = { new Setter(TextBox.BorderBrushProperty, Border), new Setter(TemplatedControl.FontSizeProperty, 18d),
                new Setter(TextBox.CaretBrushProperty, Text), new Setter(TextBox.MinHeightProperty, 44d),
                new Setter(TemplatedControl.FontWeightProperty, FontWeight.Normal) }
        });
        window.Styles.Add(new Style(s => s.OfType<ListBoxItem>())
        {
            Setters = { new Setter(TemplatedControl.FontSizeProperty, 13d), new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(7)) }
        });
        window.Styles.Add(new Style(s => s.OfType<ListBoxItem>().Class(":selected"))
        {
            Setters = { new Setter(TemplatedControl.BackgroundProperty, Gold), new Setter(TemplatedControl.ForegroundProperty, Ink) }
        });
    }

    public static void Finish(Window window, int width)
    {
        var list = window.FindControl<ItemsControl>("SectionsList")!;
        var paper = list.GetVisualAncestors().OfType<Border>().First(b => b.MaxWidth == 860);
        paper.Padding = new Thickness(width < 1500 ? 28 : 40, 28);
        paper.BorderBrush = Brush("#D9CFBD");
        paper.BorderThickness = new Thickness(1);
        paper.BoxShadow = new BoxShadows();
        var paperControls = paper.GetVisualDescendants().OfType<Control>().ToHashSet();
        var descendants = window.GetVisualDescendants().OfType<Control>().ToArray();
        foreach (var text in descendants.OfType<TextBlock>())
        {
            if (text.FontSize < 12.5) text.FontSize = 12.5;
            if (paperControls.Contains(text))
            {
                text.Foreground = text.Classes.Contains("review") ? Brush("#80501C")
                    : text.FontWeight >= FontWeight.SemiBold ? PaperAccent : PaperMuted;
            }
            else if (text.Classes.Contains("title"))
            {
                text.Foreground = Gold;
                text.FontWeight = FontWeight.SemiBold;
                text.FontSize = 18;
            }
            else if (text.Classes.Contains("muted")) text.Foreground = Muted;
            else if (text.Foreground is ISolidColorBrush old && old.Color.ToString() is "#ffb9d3ca" or "#ffcfebdd") text.Foreground = Muted;
        }
        foreach (var input in descendants.OfType<TextBox>().Where(t => t.Classes.Contains("document")))
        {
            input.FontSize = 20;
            input.Foreground = Ink;
            input.CaretBrush = Ink;
        }
        foreach (var button in descendants.OfType<Button>())
        {
            if (button.Classes.Contains("primary"))
            {
                button.Background = Gold; button.Foreground = Ink;
                foreach (var text in button.GetVisualDescendants().OfType<TextBlock>()) text.Foreground = Ink;
                foreach (var icon in button.GetVisualDescendants().OfType<FluentIcons.Avalonia.SymbolIcon>()) icon.Foreground = Ink;
            }
            else if (paperControls.Contains(button))
            {
                button.Foreground = PaperAccent;
                foreach (var text in button.GetVisualDescendants().OfType<TextBlock>()) text.Foreground = PaperAccent;
                foreach (var icon in button.GetVisualDescendants().OfType<FluentIcons.Avalonia.SymbolIcon>()) icon.Foreground = PaperAccent;
            }
            if (button.Classes.Contains("outline")) button.BorderBrush = Border;
        }
        foreach (var box in descendants.OfType<ComboBox>()) box.BorderBrush = Border;
        foreach (var expander in descendants.OfType<Expander>())
        {
            expander.Background = Brush("#302A23");
            foreach (var toggle in expander.GetVisualDescendants().OfType<ToggleButton>())
            {
                toggle.Background = Brush("#302A23"); toggle.BorderBrush = Border;
            }
        }
        foreach (var input in descendants.OfType<TextBox>().Where(t => t.Classes.Contains("input")))
        {
            input.FontWeight = FontWeight.Normal;
            foreach (var presenter in input.GetVisualDescendants().OfType<TextPresenter>()) presenter.FontWeight = FontWeight.Normal;
        }
        var scoreGrid = descendants.OfType<TextBlock>().First(t => t.Text == ((DzLegal.Presentation.ViewModels.MainWindowViewModel)window.DataContext!).L["ComplianceScore"]).Parent as Grid;
        if (scoreGrid is not null) scoreGrid.Children.OfType<TextBlock>().First(t => Grid.GetColumn(t) == 0).TextWrapping = TextWrapping.Wrap;
        foreach (var border in descendants.OfType<Border>().Where(b => b.Classes.Contains("panel")))
        {
            border.BorderBrush = Brush("#51483B");
            border.Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = [new GradientStop(Color.Parse("#2C261E"), 0), new GradientStop(Color.Parse("#24201B"), 1)],
            };
        }
        foreach (var card in descendants.OfType<Border>().Where(b => b.Classes.Contains("card")))
        {
            card.BorderBrush = Brush("#62533F");
            card.BorderThickness = new Thickness(1);
        }
        var steps = window.FindControl<ListBox>("StepsList")!;
        foreach (var item in steps.GetVisualDescendants().OfType<ListBoxItem>().Where(i => i.IsSelected))
        {
            foreach (var presenter in item.GetVisualDescendants().OfType<ContentPresenter>()) presenter.Background = Gold;
            foreach (var text in item.GetVisualDescendants().OfType<TextBlock>()) text.Foreground = Ink;
            foreach (var icon in item.GetVisualDescendants().OfType<FluentIcons.Avalonia.SymbolIcon>()) icon.Foreground = Ink;
        }
        // Keep the three original panes; let the existing draft controls fit inside their own pane.
        var draft = paper.GetVisualAncestors().OfType<Border>().First(b => b.Classes.Contains("panel"));
        var dock = (DockPanel)draft.Child!;
        var header = (Grid)dock.Children[0];
        var actions = header.Children.OfType<StackPanel>().Single(s => Grid.GetColumn(s) == 3);
        header.RowDefinitions = new RowDefinitions("Auto,Auto");
        header.ColumnDefinitions = new ColumnDefinitions("*,Auto");
        Grid.SetRow(actions, 1); Grid.SetColumn(actions, 0); Grid.SetColumnSpan(actions, 2);
        actions.Margin = new Thickness(0, 10, 0, 0);
        actions.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        if (width < 1500)
        {
            foreach (var control in actions.Children.OfType<Control>()) control.Margin = new Thickness(0);
            actions.Spacing = 6;
            var picker = actions.Children.OfType<ComboBox>().Single();
            picker.MinWidth = 220; picker.MaxWidth = 260;
            foreach (var label in picker.GetVisualDescendants().OfType<TextBlock>()) label.TextTrimming = TextTrimming.CharacterEllipsis;
        }
        var tabsGrid = dock.Children.OfType<Grid>().ElementAt(1);
        var exports = tabsGrid.Children.OfType<StackPanel>().Single();
        if (width < 1500)
        {
            tabsGrid.RowDefinitions = new RowDefinitions("Auto,Auto");
            Grid.SetRow(exports, 1); Grid.SetColumn(exports, 0); Grid.SetColumnSpan(exports, 2);
            exports.Margin = new Thickness(0, 8, 0, 4);
            exports.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        }
        var error = window.FindControl<Border>("ExportBlockersPanel")!;
        error.BorderBrush = Brush("#B28D50"); error.BorderThickness = new Thickness(1);
        error.Padding = new Thickness(12, 10);
        var shell = window.GetVisualDescendants().OfType<DockPanel>().First(d => d.Parent == window);
        var chrome = (Border)shell.Children[0];
        chrome.BorderBrush = Brush("#62533F"); chrome.BorderThickness = new Thickness(0, 0, 0, 1);
        var chromeGrid = (Grid)chrome.Child!;
        var rightControls = chromeGrid.Children.OfType<StackPanel>().First(s => Grid.GetColumn(s) == 2);
        chromeGrid.ColumnDefinitions = new ColumnDefinitions("*,Auto");
        Grid.SetColumn(rightControls, 1);
        var brand = chromeGrid.Children.OfType<StackPanel>().First(s => Grid.GetColumn(s) == 0);
        brand.ClipToBounds = true;
        var mark = brand.Children.OfType<FluentIcons.Avalonia.SymbolIcon>().First();
        brand.Children.Remove(mark);
        mark.Foreground = Ink; mark.FontSize = 20;
        brand.Children.Insert(0, new Border { Child = mark, Background = Gold, CornerRadius = new CornerRadius(8), Padding = new Thickness(7) });
        var wordmark = brand.Children.OfType<TextBlock>().First();
        wordmark.FontSize = 19; wordmark.FontWeight = FontWeight.SemiBold;
        if (width < 1500) brand.Children.OfType<TextBlock>().Skip(1).First().IsVisible = false;
        brand.Spacing = 8;
        var save = window.FindControl<TextBlock>("SaveStateText")!;
        save.MaxWidth = width < 1500 ? 180 : 240; save.TextTrimming = TextTrimming.CharacterEllipsis;
        ToolTip.SetTip(save, save.Text);
    }
}
