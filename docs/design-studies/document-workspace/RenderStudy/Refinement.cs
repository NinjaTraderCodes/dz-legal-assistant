using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DzLegal.Domain;
using DzLegal.Presentation.ViewModels;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace DzLegal.DesignStudy;

/// <summary>Static review composition only. Not a product editor or an export renderer.</summary>
internal static class Refinement
{
    private static SolidColorBrush B(string value) => new(Color.Parse(value));
    private static readonly IBrush Gold = B("#C6A76D"), Ink = B("#27241F"), Muted = B("#C2B8A9");
    private static readonly FontFamily Ui = new("fonts:Inter#Inter, fonts:Naskh#Noto Naskh Arabic");
    private static readonly FontFamily Document = new("fonts:Naskh#Noto Naskh Arabic, fonts:Inter#Inter");

    internal sealed record Composition(Border Paper, Grid Toolbar, Border FieldTools, ListBox Outline,
        IReadOnlyList<string> Paragraphs, int PageCount, double Zoom, CheckBox ReviewCheck);

    public static Composition Apply(Window window, int width, bool glass, bool manual = false)
    {
        var vm = (MainWindowViewModel)window.DataContext!;
        var ar = vm.L.Language == "ar";
        var sections = window.FindControl<ItemsControl>("SectionsList")!;
        var oldPaper = sections.GetVisualAncestors().OfType<Border>().First(b => b.MaxWidth == 860);
        var draft = oldPaper.GetVisualAncestors().OfType<Border>().First(b => b.Classes.Contains("panel"));
        var dock = (DockPanel)draft.Child!;
        var toolbar = dock.Children.OfType<Grid>().ElementAt(1);
        RefineToolbar(toolbar, width, ar);
        var outline = RefineOutline(window, vm);
        var fieldTools = FieldTools(vm.Sections.First(s => s.Id == "header"), vm, ar, manual);
        DockPanel.SetDock(fieldTools, Dock.Top);
        dock.Children.Insert(dock.Children.IndexOf(toolbar) + 1, fieldTools);

        var review = new CheckBox
        {
            Name = "StudyReviewCheck", IsChecked = vm.Sections.Single(s => s.Id == "grounds").IsReviewed,
            Content = ar ? "راجعت القسم المكتوب بالذكاء الاصطناعي" : "AI-written section reviewed",
            FontFamily = Ui, FontSize = ar ? 14.5 : 12.5, Foreground = B("#E6D5B2"), MinHeight = 30,
        };
        var reviewRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        reviewRow.Children.Add(T(ar ? "المناقشة القانونية" : "Legal grounds", ar ? 14.5 : 12.5, Muted));
        Grid.SetColumn(review, 1); reviewRow.Children.Add(review);
        var reviewBar = new Border
        {
            Name = "StudyReviewBar", Background = B("#28231C"), BorderBrush = B("#51483B"),
            BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(10, 5), Child = reviewRow,
        };
        DockPanel.SetDock(reviewBar, Dock.Bottom);
        dock.Children.Insert(dock.Children.Count - 1, reviewBar);

        // The original three pane widths remain. A4 is fitted into the available central pane.
        var pageAreaWidth = draft.Bounds.Width - draft.Padding.Left - draft.Padding.Right - 36;
        var zoom = Math.Min(1, pageAreaWidth / 794d);
        var (pages, paragraphs) = CreatePages(vm);
        var pageStack = new StackPanel { Spacing = 22, FlowDirection = FlowDirection.LeftToRight };
        foreach (var page in pages)
        {
            pageStack.Children.Add(new LayoutTransformControl
            {
                LayoutTransform = new ScaleTransform(zoom, zoom), Child = page,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
        }
        var ruler = new PageRuler { Width = 794 * zoom, Height = 24, FlowDirection = FlowDirection.LeftToRight };
        var canvas = new DockPanel { Background = B("#181715"), ClipToBounds = true };
        DockPanel.SetDock(ruler, Dock.Top); canvas.Children.Add(ruler);
        var zoomLabel = T(ar ? $"A4  ·  ملاءمة العرض {zoom:P0}" : $"A4  ·  Fit width {zoom:P0}", 11.5, Muted);
        zoomLabel.FlowDirection = FlowDirection.LeftToRight;
        var caption = new Border { Padding = new Thickness(10, 3), Child = zoomLabel };
        DockPanel.SetDock(caption, Dock.Bottom); canvas.Children.Add(caption);
        canvas.Children.Add(new ScrollViewer { Content = pageStack, Padding = new Thickness(12, 6, 12, 16) });
        dock.Children[^1] = canvas;
        if (glass) ApplyGlass(window);
        return new Composition(pages[0], toolbar, fieldTools, outline, paragraphs, pages.Count, zoom, review);
    }

    private static ListBox RefineOutline(Window window, MainWindowViewModel vm)
    {
        var old = window.FindControl<ListBox>("StepsList")!;
        var ar = vm.L.Language == "ar";
        var parent = (DockPanel)old.Parent!;
        var list = new ListBox
        {
            Name = "StudyOutline", ItemsSource = vm.FieldGroups, SelectedItem = vm.SelectedStep,
            Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 14),
            ItemTemplate = new FuncDataTemplate<FieldGroupViewModel>((group, _) =>
            {
                if (group is null) return new TextBlock();
                var selected = group.Id == vm.SelectedStep?.Id;
                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("3,30,*,20"), Height = ar ? 28 : 26 };
                row.Children.Add(new Border { Background = selected ? Gold : Brushes.Transparent, CornerRadius = new CornerRadius(1) });
                var number = T(group.Number.ToString("00", System.Globalization.CultureInfo.InvariantCulture), 10.5, selected ? Gold : B("#AA9C87"));
                number.HorizontalAlignment = HorizontalAlignment.Center; Grid.SetColumn(number, 1); row.Children.Add(number);
                var label = T(group.Label, ar ? 15.5 : 13, selected ? B("#F1D7A4") : B("#D8D0C4"));
                label.FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal;
                Grid.SetColumn(label, 2); row.Children.Add(label);
                var icon = new SymbolIcon
                {
                    Symbol = group.IsDone ? Symbol.CheckmarkCircle : group.IsMissing || group.IsWarning ? Symbol.ErrorCircle : Symbol.Circle,
                    FontSize = 13, Foreground = group.IsDone ? B("#ACD3BD") : group.IsMissing ? B("#FFC0B3") : Gold,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(icon, 3); row.Children.Add(icon);
                return new Border
                {
                    Background = selected ? B("#423724") : Brushes.Transparent, CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, group.Number is 4 or 7 ? 4 : 0, 0, 0), Child = row,
                };
            }),
        };
        list.Styles.Add(new Avalonia.Styling.Style(s => s.OfType<ListBoxItem>())
        {
            Setters = { new Avalonia.Styling.Setter(Layoutable.MarginProperty, new Thickness(0)),
                new Avalonia.Styling.Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
                new Avalonia.Styling.Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
                new Avalonia.Styling.Setter(TemplatedControl.MinHeightProperty, ar ? 28d : 26d),
                new Avalonia.Styling.Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch) }
        });
        DockPanel.SetDock(list, Dock.Top);
        parent.Children[parent.Children.IndexOf(old)] = list;
        return list;
    }

    private static void RefineToolbar(Grid toolbar, int width, bool ar)
    {
        var exports = toolbar.Children.OfType<StackPanel>().Single();
        toolbar.Children.Clear();
        toolbar.RowDefinitions = new RowDefinitions("Auto");
        toolbar.ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto");
        toolbar.Height = 40;
        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        foreach (var (label, selected) in new[] { (ar ? "تحرير" : "Edit", true), (ar ? "معاينة" : "Preview", false) })
        {
            tabs.Children.Add(new Button
            {
                Content = label, FontFamily = Ui, FontSize = ar ? 15 : 12.5, Height = ar ? 36 : 32, MinHeight = 32,
                Padding = new Thickness(9, 5), Foreground = selected ? B("#F1D7A4") : Muted,
                Background = selected ? B("#423724") : Brushes.Transparent, CornerRadius = new CornerRadius(4),
                BorderBrush = selected ? Gold : Brushes.Transparent, BorderThickness = new Thickness(0, 0, 0, 2),
            });
        }
        toolbar.Children.Add(tabs);
        Grid.SetRow(exports, 0); Grid.SetColumn(exports, 2); Grid.SetColumnSpan(exports, 1);
        toolbar.Children.Add(exports);
        toolbar.Margin = new Thickness(0, 3, 0, 9);
        exports.Margin = new Thickness(8, 0, 0, 0); exports.Spacing = width < 1500 ? 5 : 8;
        exports.HorizontalAlignment = HorizontalAlignment.Right;
        var watermark = exports.Children.OfType<CheckBox>().Single();
        watermark.Content = ar ? "وسم المسودة" : "Draft mark"; watermark.FontSize = ar ? 14.5 : 12.5;
        ToolTip.SetTip(watermark, ar ? "إظهار علامة مسودة على المستند المصدّر" : "Show the Draft watermark on the exported document");
        foreach (var button in exports.Children.OfType<Button>())
        {
            button.MinHeight = 32; button.Padding = new Thickness(width < 1500 ? 7 : 10, 5);
            if (button.Content is not StackPanel content) continue;
            var label = content.Children.OfType<TextBlock>().Single();
            var originalLabel = label.Text;
            label.Text = originalLabel?.Contains("PDF", StringComparison.Ordinal) == true ? "PDF" : "Word";
            label.FontSize = 12.5; content.Spacing = 4;
            foreach (var icon in content.Children.OfType<SymbolIcon>()) icon.FontSize = 16;
            ToolTip.SetTip(button, originalLabel);
        }
    }

    private static Border FieldTools(SectionViewModel section, MainWindowViewModel vm, bool ar, bool manual)
    {
        var text = B("#344D5A");
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var title = T(section.Title, ar ? 15 : 12.5, text); title.FontWeight = FontWeight.SemiBold;
        row.Children.Add(title);
        var source = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0) };
        source.Children.Add(new SymbolIcon { Symbol = manual ? Symbol.Edit : Symbol.LockClosed, FontSize = 12, Foreground = text });
        source.Children.Add(T(manual ? (ar ? "تعديل يدوي" : "Edited by hand") : vm.L["GeneratedSectionHint"], ar ? 14.5 : 12, text));
        Grid.SetColumn(source, 1); row.Children.Add(source);
        var action = new Button
        {
            Content = manual ? vm.L["RestoreFromForm"] : vm.L["EditManually"],
            Command = manual ? section.RestoreCommand : section.OverrideCommand,
            FontFamily = Ui, FontSize = ar ? 14.5 : 12, Foreground = text, Background = B("#F0F4F5"),
            BorderBrush = B("#A9BBC2"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4), MinHeight = 28,
        };
        Grid.SetColumn(action, 2); row.Children.Add(action);
        var bar = new Border
        {
            Name = "StudyFieldTools", Background = B("#DFE7E9"), BorderBrush = B("#B0C0C5"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5), Padding = new Thickness(10, 5), Margin = new Thickness(0, 0, 0, 8), Child = row,
        };
        ToolTip.SetTip(bar, ar ? "أدوات الحقل المحدد؛ لا تُطبع مع المستند" : "Selected field controls; not printed with the document");
        return bar;
    }

    private static (List<Border> Pages, List<string> Paragraphs) CreatePages(MainWindowViewModel vm)
    {
        const double pageWidth = 794, pageHeight = 1123, margin = 76, usable = 959;
        var contents = new List<StackPanel> { new() { Spacing = 0 } };
        var used = 0d;
        var paragraphs = new List<string>();
        foreach (var section in vm.Sections.Where(s => !string.IsNullOrWhiteSpace(s.Text)))
        {
            var block = new StackPanel { Margin = new Thickness(0, section.Definition.Layout.SpaceBefore * 1.12, 0, 0), Spacing = 0 };
            if (section.Definition.ShowTitle)
            {
                var heading = LegalText(section.Title, 20.5, TextAlignment.Right);
                // Printed headings are always Arabic, independent of the interface language.
                heading.Text = section.Definition.Title.Ar + ":";
                heading.FontWeight = FontWeight.Bold; heading.TextDecorations = TextDecorations.Underline;
                block.Children.Add(heading);
            }
            var layout = section.Definition.Layout;
            var alignment = layout.Align switch { BlockAlign.Center => TextAlignment.Center, BlockAlign.Justify => TextAlignment.Justify, _ => TextAlignment.Right };
            var lines = section.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            foreach (var line in lines)
            {
                paragraphs.Add(line);
                if (line.Length == 0) { block.Children.Add(new Border { Height = 10 }); continue; }
                var isTitle = section.Id == "header" && line.Contains("عريضة", StringComparison.Ordinal);
                var text = LegalText(line, isTitle ? 25 : section.Id == "header" ? 20 : 20.5, alignment);
                text.FontWeight = layout.Bold || isTitle ? FontWeight.Bold : FontWeight.Normal;
                if (isTitle) text.Margin = new Thickness(0, 16, 0, 0);
                if (layout.Align == BlockAlign.End) { text.Width = 260; text.HorizontalAlignment = HorizontalAlignment.Left; text.TextAlignment = TextAlignment.Center; }
                block.Children.Add(text);
            }
            block.Measure(new Size(pageWidth - margin * 2, double.PositiveInfinity));
            if (used + block.DesiredSize.Height > usable && used > 0) { contents.Add(new StackPanel()); used = 0; }
            contents[^1].Children.Add(block); used += block.DesiredSize.Height;
        }
        var pages = new List<Border>();
        for (var index = 0; index < contents.Count; index++)
        {
            var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Thickness(margin, 64, margin, 36) };
            grid.Children.Add(contents[index]);
            var number = T($"الصفحة {index + 1} من {contents.Count}", 11, B("#77736D"));
            number.HorizontalAlignment = HorizontalAlignment.Center; Grid.SetRow(number, 1); grid.Children.Add(number);
            var layer = new Grid();
            if (vm.DraftWatermark)
            {
                layer.Children.Add(new TextBlock
                {
                    Text = "مسودة", FontFamily = Document, FontSize = 120, FontWeight = FontWeight.Bold,
                    Foreground = B("#1027241F"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    RenderTransform = new RotateTransform(-35), RenderTransformOrigin = RelativePoint.Center,
                });
            }
            layer.Children.Add(grid);
            pages.Add(new Border
            {
                Name = "StudyPaper" + index, Width = pageWidth, Height = pageHeight, Background = B("#FFFEFC"),
                BorderBrush = B("#E5DED1"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(1), Child = layer,
                BoxShadow = BoxShadows.Parse("0 5 16 0 #55000000"), FlowDirection = FlowDirection.RightToLeft,
            });
        }
        return (pages, paragraphs);
    }

    private static TextBlock LegalText(string text, double size, TextAlignment align) => new()
    {
        Text = text, FontFamily = Document, FontSize = size, FontWeight = FontWeight.Normal, LineHeight = 31,
        Foreground = Ink, TextAlignment = align, TextWrapping = TextWrapping.Wrap, FlowDirection = FlowDirection.RightToLeft,
    };

    private static TextBlock T(string text, double size, IBrush brush) => new()
    {
        Text = text, FontFamily = Ui, FontSize = size, Foreground = brush, VerticalAlignment = VerticalAlignment.Center,
    };

    private static void ApplyGlass(Window window)
    {
        // A static layered appearance: no live desktop capture, backdrop blur or animation.
        window.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = [new(Color.Parse("#5C4932"), 0), new(Color.Parse("#1D201E"), .46), new(Color.Parse("#3B3029"), 1)],
        };
        foreach (var panel in window.GetVisualDescendants().OfType<Border>().Where(b => b.Classes.Contains("panel")))
        {
            panel.Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops = [new(Color.Parse("#D43C352B"), 0), new(Color.Parse("#EB24221E"), .45), new(Color.Parse("#E822211E"), 1)],
            };
            panel.BorderBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = [new(Color.Parse("#A5C0AB83"), 0), new(Color.Parse("#565D5547"), .5), new(Color.Parse("#8B8D7958"), 1)],
            };
            panel.BoxShadow = BoxShadows.Parse("0 8 22 0 #38000000");
        }
    }

    private sealed class PageRuler : Control
    {
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            var pen = new Pen(B("#827566"), 1);
            for (var tick = 0; tick <= 42; tick++)
            {
                var x = tick * Bounds.Width / 42;
                context.DrawLine(pen, new Point(x, 23), new Point(x, tick % 2 == 0 ? 16 : 20));
            }
            context.DrawLine(new Pen(B("#51483B"), 1), new Point(0, 23), new Point(Bounds.Width, 23));
        }
    }
}
