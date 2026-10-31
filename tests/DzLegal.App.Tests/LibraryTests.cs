using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using DzLegal.App.Views;
using DzLegal.Domain;
using DzLegal.Law;
using DzLegal.Presentation;
using DzLegal.Presentation.ViewModels;
using FluentIcons.Avalonia;

namespace DzLegal.App.Tests;

/// <summary>
/// The read-only law library. The shipped texts are the real ones, but every fixture used to
/// exercise a branch is fictitious and lives only in this test assembly.
/// </summary>
public sealed class LibraryTests
{
    private sealed record Display(string Language, bool Dark, int Width, int Height, double Scale)
    {
        public string Suffix => $"{Language}-{(Dark ? "dark" : "light")}-{Width}x{Height}";
        public override string ToString() => $"{Suffix}/{Scale}";
    }

    private static IEnumerable<Display> Displays()
    {
        foreach (var language in new[] { "ar", "en" })
        foreach (var dark in new[] { false, true })
        foreach (var size in new[] { (1366, 768), (1920, 1080) })
        foreach (var scale in new[] { 1d, 1.25 })
            yield return new Display(language, dark, size.Item1, size.Item2, scale);
    }

    private static IEnumerable<Display> ContrastDisplays() =>
        Displays().Where(display => display.Width == 1366 && display.Scale == 1);

    private static Loc Localization(Display display)
    {
        var loc = new Loc();
        loc.SetLanguage(display.Language);
        return loc;
    }

    private static LibraryViewModel Create(
        Loc? loc = null,
        FakeFiles? files = null,
        FakeLinks? links = null,
        LawLibrary? library = null,
        LawSourceManifest? manifest = null,
        Func<int, Task>? openHelp = null)
    {
        var knowledge = Vm.ShippedKnowledge;
        return new LibraryViewModel(
            library ?? knowledge.Library,
            manifest ?? knowledge.Manifest,
            loc ?? new Loc(),
            links ?? new FakeLinks(),
            files ?? new FakeFiles(),
            openHelp ?? (_ => Task.CompletedTask));
    }

    private static LibraryWindow CreateWindow(
        Loc loc,
        FakeFiles files,
        FakeLinks links,
        LawLibrary? library = null,
        LawSourceManifest? manifest = null)
    {
        var knowledge = Vm.ShippedKnowledge;
        return new LibraryWindow(
            library ?? knowledge.Library,
            manifest ?? knowledge.Manifest,
            loc,
            links,
            files,
            () => Task.CompletedTask);
    }

    [Fact]
    public void ACardShowsItsDateInWordsInBothLanguages()
    {
        var loc = new Loc();
        Assert.Equal("27 فبراير 2005", LibraryTextViewModel.FormatDate(loc, "2005-02-27"));
        Assert.Equal("12 يوليو 2022", LibraryTextViewModel.FormatDate(loc, "2022-07-12"));
        loc.SetLanguage("en");
        Assert.Equal("27 February 2005", LibraryTextViewModel.FormatDate(loc, "2005-02-27"));
        Assert.Equal("1975", LibraryTextViewModel.FormatDate(loc, "1975"));
        Assert.Equal("2005-13-27", LibraryTextViewModel.FormatDate(loc, "2005-13-27"));
    }

    [Fact]
    public void CardsComeFromTheLoadedLibraryAndSummaryCountsThem()
    {
        var loc = new Loc();
        var library = Vm.ShippedKnowledge.Library;
        var vm = Create(loc: loc, library: library);

        Assert.Equal(library.Codes.Count, vm.Texts.Count);
        Assert.Equal(
            loc.Format(
                "LibrarySummary",
                library.Codes.Sum(code => code.Articles.Count),
                library.Codes.Count,
                library.Codes.Count(code => !code.Partial)),
            vm.Summary);
        Assert.Null(vm.OpenedText);
        Assert.Empty(vm.Articles);

        foreach (var code in library.Codes)
        {
            var card = vm.Texts.Single(text => text.Code.Id == code.Id);
            Assert.Equal(code.Title.Get(loc.Language), card.Title);
            Assert.Equal(code.Reference, card.Reference);
            Assert.Equal(code.Edition ?? string.Empty, card.Edition);
            Assert.Equal(
                string.IsNullOrWhiteSpace(code.ConsolidatedAsOf) ? string.Empty : loc.Format("LibraryAsOf", LibraryTextViewModel.FormatDate(loc, code.ConsolidatedAsOf)),
                card.AsOfText);
            Assert.Equal(loc.Format("LibraryArticles", code.Articles.Count), card.ArticlesText);
            Assert.Equal(code.Partial ? loc["LibraryPartial"] : loc["LibraryComplete"], card.CoverageText);
            Assert.Equal(code.Partial ? loc["LibraryPartialHint"] : string.Empty, card.CoverageHint);
            Assert.Equal(code.Partial, card.IsPartial);
        }

        var expected = library.Codes
            .OrderByDescending(code => !code.Partial)
            .ThenByDescending(code => code.Articles.Count)
            .ThenBy(code => code.Id, StringComparer.Ordinal)
            .Select(code => code.Id);
        Assert.Equal(expected, vm.Texts.Select(card => card.Code.Id));
    }

    [Fact]
    public void CheckAndReviewBadgesDeriveFromIndependentFacts()
    {
        var loc = new Loc();
        foreach (var check in Enum.GetValues<LawCheck>())
        {
            var pending = Create(
                loc: loc,
                library: new LawLibrary([Fixture(check, verified: false)]),
                manifest: new LawSourceManifest());
            var pendingCard = Assert.Single(pending.Texts);
            Assert.Equal(ExpectedCheckText(loc, check), pendingCard.CheckBadge.Text);
            Assert.Equal(ExpectedCheckTone(check), pendingCard.CheckBadge.Tone);
            Assert.Equal(loc["LibraryNotReviewed"], pendingCard.ReviewBadge.Text);
            Assert.Equal("pending", pendingCard.ReviewBadge.Tone);
            Assert.Equal("Person", pendingCard.ReviewBadge.Icon);
            Assert.Equal(loc["Help7Body3"], pendingCard.ReviewBadge.Hint);
            Assert.Equal(loc["LibraryWhat"], pendingCard.CheckBadge.WhatLabel);
            Assert.Equal(loc["LibraryWhat"], pendingCard.ReviewBadge.WhatLabel);

            var reviewed = Create(
                loc: loc,
                library: new LawLibrary([Fixture(check, verified: true)]),
                manifest: new LawSourceManifest());
            var reviewedCard = Assert.Single(reviewed.Texts);
            Assert.Equal(ExpectedCheckText(loc, check), reviewedCard.CheckBadge.Text);
            Assert.Equal(loc["LibraryReviewed"], reviewedCard.ReviewBadge.Text);
            Assert.Equal("reviewed", reviewedCard.ReviewBadge.Tone);
            Assert.Equal("ShieldCheckmark", reviewedCard.ReviewBadge.Icon);
        }
    }

    [Fact]
    public async Task BadgeWhatLinksOpenHelpPageSeven()
    {
        var pages = new List<int>();
        var vm = Create(openHelp: page => { pages.Add(page); return Task.CompletedTask; });
        var card = vm.Texts[0];

        await card.CheckBadge.WhatCommand.ExecuteAsync(null);
        await card.ReviewBadge.WhatCommand.ExecuteAsync(null);

        Assert.Equal([7, 7], pages);
    }

    [Fact]
    public void SearchByNumberWordAndLetterVariantStaysInsideTheOpenedText()
    {
        var vm = Create();
        var cpca = vm.Texts.Single(text => text.Code.Id == "cpca");

        cpca.OpenCommand.Execute(null);
        Assert.True(vm.IsReading);
        Assert.False(vm.IsList);
        Assert.Equal(cpca.Code.Articles.Select(article => article.Number), vm.Articles.Select(article => article.Article.Number));

        vm.SearchText = "66";
        Assert.NotEmpty(vm.Articles);
        Assert.All(vm.Articles, article => Assert.Equal("66", article.Article.Number));
        Assert.False(vm.HasNoResult);

        vm.SearchText = "المادة 66";
        Assert.NotEmpty(vm.Articles);
        Assert.All(vm.Articles, article => Assert.Equal("66", article.Article.Number));

        vm.SearchText = "أتعاب";
        var withHamza = vm.Articles.Select(article => article.Article.Number).Order(StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(withHamza);
        vm.SearchText = "اتعاب";
        Assert.Equal(withHamza, vm.Articles.Select(article => article.Article.Number).Order(StringComparer.Ordinal));

        vm.BackCommand.Execute(null);
        Assert.True(vm.IsList);
        Assert.Empty(vm.Articles);
        var civil = vm.Texts.Single(text => text.Code.Id == "civil-code");
        civil.OpenCommand.Execute(null);
        vm.SearchText = "أتعاب";
        Assert.Empty(vm.Articles);
        Assert.True(vm.HasNoResult);

        vm.SearchText = "لايوجدنصكهذا";
        Assert.Empty(vm.Articles);
        Assert.True(vm.HasNoResult);
    }

    [Fact]
    public void AmendedArticleKeepsItsAmendingReference()
    {
        var loc = new Loc();
        var vm = Create(loc: loc);
        var cpca = vm.Texts.Single(text => text.Code.Id == "cpca");
        cpca.OpenCommand.Execute(null);
        vm.SearchText = "32";
        var article = vm.Articles.Single(item => item.Article.Number == "32");

        Assert.True(article.HasAmendedBy);
        Assert.Equal(loc.Format("LibraryAmendedBy", article.Article.AmendedBy), article.AmendedByText);
    }

    [Fact]
    public async Task CopyUsesTheAppendixAFormatAndReportsItInBothLanguages()
    {
        foreach (var language in new[] { "ar", "en" })
        {
            var loc = new Loc();
            loc.SetLanguage(language);
            var files = new FakeFiles();
            var vm = Create(loc: loc, files: files);
            var card = vm.Texts.Single(text => text.Code.Id == "cpca");
            await card.OpenCommand.ExecuteAsync(null);
            var article = vm.Articles[0];

            await article.CopyCommand.ExecuteAsync(null);

            Assert.Equal(
                loc.Format("LibraryCopyFormat", article.Article.Number, card.Code.CitationName, article.Article.Text),
                files.Clipboard);
            Assert.Equal(loc["LibraryCopied"], vm.StatusMessage);
            Assert.True(vm.HasStatus);
        }
    }

    [Fact]
    public async Task OfficialSourceIsExposedOnlyOnClickAndNeverOpenedAutomatically()
    {
        var links = new FakeLinks();
        var vm = Create(links: links);
        var civil = vm.Texts.Single(text => text.Code.Id == "civil-code");

        Assert.Empty(links.Opened);
        Assert.Equal(new Uri("https://www.joradp.dz"), civil.OfficialSource);
        Assert.True(civil.OfficialSourceCommand.CanExecute(null));

        await civil.OfficialSourceCommand.ExecuteAsync(null);
        Assert.Equal([new Uri("https://www.joradp.dz")], links.Opened);

        var cpca = vm.Texts.Single(text => text.Code.Id == "cpca");
        Assert.Equal(new Uri(cpca.Code.Source!.Url), cpca.OfficialSource);
        await cpca.OfficialSourceCommand.ExecuteAsync(null);
        Assert.Equal(2, links.Opened.Count);
        Assert.All(links.Opened, uri => Assert.True(uri.IsAbsoluteUri && uri.Scheme == Uri.UriSchemeHttps));
    }

    [Fact]
    public async Task HelpIndexAndPageSevenOfferTheLibraryOnlyWhenItIsAvailable()
    {
        var calls = 0;
        using var help = new HelpViewModel(new Loc(), () => Task.CompletedTask, () =>
        {
            calls++;
            return Task.CompletedTask;
        }, goHome: () => Task.CompletedTask);

        Assert.True(help.CanOpenLibrary);
        Assert.False(help.ShowLibraryPageLink);
        help.Pages[6].OpenCommand.Execute(null);
        Assert.True(help.IsLibraryPage);
        Assert.True(help.ShowLibraryPageLink);
        await help.LibraryCommand.ExecuteAsync(null);
        Assert.Equal(1, calls);

        using var without = new HelpViewModel(new Loc(), () => Task.CompletedTask);
        Assert.False(without.CanOpenLibrary);
        without.Pages[6].OpenCommand.Execute(null);
        Assert.False(without.ShowLibraryPageLink);
        Assert.False(without.LibraryCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task LibraryListUsesLoadedTextsBadgesAndTheHomeSurfaceInEveryDisplayMode()
    {
        foreach (var display in Displays())
        {
            var loc = Localization(display);
            var files = new FakeFiles();
            var links = new FakeLinks();
            var window = CreateWindow(loc, files, links);
            var vm = Assert.IsType<LibraryViewModel>(window.DataContext);
            Show(window, display);
            var view = Assert.IsType<LibraryView>(window.Content);

            var heading = view.FindControl<Border>("LibraryHeadingPanel")!;
            var summary = view.FindControl<Border>("LibrarySummaryPanel")!;
            var cardsHost = view.FindControl<ItemsControl>("LibraryCards")!;
            var note = view.FindControl<TextBlock>("LibraryNoteText")!;
            var title = view.FindControl<TextBlock>("LibraryTitleText")!;
            var intro = view.FindControl<TextBlock>("LibraryIntroText")!;
            AssertInside(window, heading);
            AssertInside(window, summary);
            AssertInside(window, note);
            Assert.False(Bounds(heading, window).Intersects(Bounds(summary, window)));
            Assert.True(Bounds(intro, window).Top >= Bounds(title, window).Bottom - 1);
            Assert.Equal(vm.Title, title.Text);
            Assert.Equal(loc["LibraryIntro"], intro.Text);
            Assert.Equal(vm.Summary, view.FindControl<TextBlock>("LibrarySummaryText")!.Text);
            Assert.Equal(loc["LibraryNote"], note.Text);
            Assert.Equal(loc.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, window.FlowDirection);
            Assert.True(view.FindControl<ScrollViewer>("LibraryListScroll")!.IsVisible);

            var cards = cardsHost.GetVisualDescendants().OfType<Border>()
                .Where(border => border.Classes.Contains("library-card")).ToArray();
            Assert.Equal(vm.Texts.Count, cards.Length);
            var rectangles = cards.Select(card => Bounds(card, window)).ToArray();
            foreach (var rectangle in rectangles)
            {
                Assert.True(rectangle.Left >= -1 && rectangle.Right <= window.Bounds.Width + 1,
                    $"{display}: card clips horizontally: {rectangle}");
            }
            for (var first = 0; first < rectangles.Length; first++)
            for (var second = first + 1; second < rectangles.Length; second++)
                if (Math.Abs(rectangles[first].Top - rectangles[second].Top) < 1)
                    Assert.False(rectangles[first].Intersects(rectangles[second]),
                        $"{display}: overlapping cards {first} and {second}");

            foreach (var card in vm.Texts)
            {
                var border = cards.Single(item => item.GetVisualDescendants().OfType<TextBlock>()
                    .Any(text => text.Text == card.Title));
                var badges = border.GetVisualDescendants().OfType<Border>()
                    .Where(item => item.Classes.Contains("library-badge")).ToArray();
                Assert.Equal(2, badges.Length);
                AssertBadge(badges[0], card.CheckBadge);
                AssertBadge(badges[1], card.ReviewBadge);
                var buttons = border.GetVisualDescendants().OfType<Button>()
                    .Where(button => button.Classes.Contains("home")).ToArray();
                Assert.Contains(buttons, button => ReferenceEquals(button.Command, card.OpenCommand));
                Assert.Contains(buttons, button => ReferenceEquals(button.Command, card.OfficialSourceCommand));
                Assert.Contains(border.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == card.ArticlesText);
                Assert.Contains(border.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == card.CoverageText);
                if (card.HasEdition && card.HasAsOf)
                {
                    var edition = border.GetVisualDescendants().OfType<TextBlock>()
                        .First(text => text.Text == card.Edition);
                    var asOf = border.GetVisualDescendants().OfType<TextBlock>()
                        .First(text => text.Text == card.AsOfText);
                    Assert.False(
                        Bounds(edition, window).Intersects(Bounds(asOf, window)),
                        $"{display}/{card.Title}: edition {Bounds(edition, window)} overlaps as-of {Bounds(asOf, window)}");
                }
            }

            Assert.Empty(links.Opened);
            await vm.Texts[0].OfficialSourceCommand.ExecuteAsync(null);
            Assert.Single(links.Opened);
            Capture(window, display, "phase2-library-list");
            var scroll = view.FindControl<ScrollViewer>("LibraryListScroll")!;
            scroll.Offset = new Vector(0, scroll.Extent.Height);
            Layout(window);
            Capture(window, display, "phase2-library-list-end");
            CheckTypography(view);
            if (IsContrast(display)) CheckTokenContrast(view, display, "library-list");
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task OpenedTextShowsEveryArticleLazilyWithSearchCopyAndKeyboard()
    {
        foreach (var display in ContrastDisplays())
        {
            var loc = Localization(display);
            var files = new FakeFiles();
            var links = new FakeLinks();
            var window = CreateWindow(loc, files, links);
            var vm = Assert.IsType<LibraryViewModel>(window.DataContext);
            Show(window, display);
            var view = Assert.IsType<LibraryView>(window.Content);

            Assert.Empty(vm.Articles);
            Assert.DoesNotContain(view.GetVisualDescendants().OfType<Border>(), border => border.Classes.Contains("library-article"));
            var cpca = vm.Texts.Single(text => text.Code.Id == "cpca");
            await cpca.OpenCommand.ExecuteAsync(null);
            Layout(window);

            Assert.True(vm.IsReading);
            Assert.Equal(cpca.Code.Articles.Count, vm.Articles.Count);
            var list = view.FindControl<ListBox>("LibraryArticles")!;
            Assert.True(list.IsVisible);
            var realized = list.GetVisualDescendants().OfType<Border>()
                .Count(border => border.Classes.Contains("library-article"));
            Assert.InRange(realized, 1, 30);
            Assert.True(realized < cpca.Code.Articles.Count, $"{display}: the article list is not virtualised");

            var firstArticle = vm.Articles[0];
            var firstText = list.GetVisualDescendants().OfType<TextBlock>()
                .First(text => text.Classes.Contains("library-article-text"));
            Assert.Equal(firstArticle.Text, firstText.Text);
            Assert.Equal(TextWrapping.Wrap, firstText.TextWrapping);
            Assert.Equal(TextTrimming.None, firstText.TextTrimming);
            Assert.Equal(0, firstText.MaxLines);
            Assert.True(firstText.FontSize >= 15);
            Assert.Equal(FlowDirection.RightToLeft, firstText.FlowDirection);
            Assert.True(firstText.Bounds.Height > 0);
            Assert.True(firstText.TextLayout.Height <= firstText.Bounds.Height + 1);
            Assert.Equal(firstArticle.NumberText, list.GetVisualDescendants().OfType<TextBlock>()
                .First(text => text.Text == firstArticle.NumberText).Text);
            Assert.Equal(firstArticle.PathText, list.GetVisualDescendants().OfType<TextBlock>()
                .First(text => text.Text == firstArticle.PathText).Text);

            vm.SearchText = "66";
            Layout(window);
            Assert.NotEmpty(vm.Articles);
            Assert.All(vm.Articles, article => Assert.Equal("66", article.Article.Number));
            Assert.True(list.IsVisible);

            vm.SearchText = "zzzz-not-a-word";
            Layout(window);
            Assert.True(vm.HasNoResult);
            Assert.False(list.IsVisible);
            var noResult = view.FindControl<TextBlock>("LibraryNoResult")!;
            Assert.True(noResult.IsVisible);
            Assert.Equal(loc["LibraryNoResult"], noResult.Text);

            vm.SearchText = string.Empty;
            Layout(window);
            var article = vm.Articles[0];
            await article.CopyCommand.ExecuteAsync(null);
            Layout(window);
            Assert.Equal(
                loc.Format("LibraryCopyFormat", article.Article.Number, cpca.Code.CitationName, article.Article.Text),
                files.Clipboard);
            var status = view.FindControl<TextBlock>("LibraryStatus")!;
            Assert.True(status.IsVisible);
            Assert.Equal(loc["LibraryCopied"], status.Text);
            Capture(window, display, "phase2-library-reading");
            CheckTypography(view);

            vm.BackCommand.Execute(null);
            Layout(window);
            Assert.True(vm.IsList);
            Assert.True(view.FindControl<ScrollViewer>("LibraryListScroll")!.IsVisible);

            var firstOpen = view.GetVisualDescendants().OfType<Button>()
                .First(button => button.Classes.Contains("home") && button.Classes.Contains("primary"));
            firstOpen.Focus(NavigationMethod.Tab);
            Layout(window);
            Assert.Same(firstOpen, window.FocusManager!.GetFocusedElement());
            Assert.Equal(ColorOf(ResourceBrush(firstOpen, "WelcomeFocusBrush")), ColorOf(firstOpen.BorderBrush!));
            Assert.True(firstOpen.BorderThickness.Left >= 3);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Layout(window);
            Assert.True(vm.IsReading);

            var search = view.FindControl<TextBox>("LibrarySearchBox")!;
            search.Focus();
            Layout(window);
            Assert.True(search.IsFocused);
            Assert.True(search.BorderThickness.Left >= 3);
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Layout(window);
            Assert.True(vm.IsList);
            Assert.True(window.IsVisible);

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Assert.False(window.IsVisible);
        }
    }

    [AvaloniaFact]
    public void OpenedTextLayoutFitsAtEverySizeLanguageThemeAndScale()
    {
        foreach (var display in Displays())
        {
            var loc = Localization(display);
            var window = CreateWindow(loc, new FakeFiles(), new FakeLinks());
            var vm = Assert.IsType<LibraryViewModel>(window.DataContext);
            Show(window, display);
            var view = Assert.IsType<LibraryView>(window.Content);
            var cpca = vm.Texts.Single(text => text.Code.Id == "cpca");
            cpca.OpenCommand.Execute(null);
            Layout(window);

            Assert.True(vm.IsReading);
            var heading = view.FindControl<Border>("LibraryHeadingPanel")!;
            var panel = view.FindControl<Border>("LibraryReadingPanel")!;
            var search = view.FindControl<TextBox>("LibrarySearchBox")!;
            var list = view.FindControl<ListBox>("LibraryArticles")!;
            var note = view.FindControl<TextBlock>("LibraryNoteText")!;
            var reference = view.FindControl<TextBlock>("LibraryReadingReference")!;
            var back = view.FindControl<Button>("LibraryBack")!;
            AssertInside(window, heading);
            AssertInside(window, panel);
            AssertInside(window, search);
            AssertInside(window, list);
            AssertInside(window, note);
            AssertInside(window, reference);
            AssertInside(window, back);
            Assert.False(Bounds(reference, window).Intersects(Bounds(back, window)),
                $"{display}: the reference {Bounds(reference, window)} overlaps Back {Bounds(back, window)}");
            Assert.False(Bounds(heading, window).Intersects(Bounds(panel, window)));
            Assert.False(Bounds(panel, window).Intersects(Bounds(note, window)));
            Assert.True(Bounds(search, window).Top >= Bounds(panel, window).Top - 1);
            Assert.True(Bounds(list, window).Top >= Bounds(panel, window).Bottom - 1);
            Assert.True(Bounds(list, window).Bottom <= Bounds(note, window).Top + 1);

            var firstRow = list.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(border => border.Classes.Contains("library-article"));
            Assert.NotNull(firstRow);
            AssertInside(window, firstRow);
            Assert.Equal(loc["LibrarySearch"], search.PlaceholderText);
            Assert.True(search.FontSize >= 14);
            Assert.True(search.MinHeight >= 44);
            Capture(window, display, "phase2-library-reading");
            CheckTypography(view);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ListTabOrderMovesInTheReadingDirection()
    {
        foreach (var display in ContrastDisplays())
        {
            var loc = Localization(display);
            var window = CreateWindow(loc, new FakeFiles(), new FakeLinks());
            var vm = Assert.IsType<LibraryViewModel>(window.DataContext);
            Show(window, display);
            var view = Assert.IsType<LibraryView>(window.Content);
            var card = view.FindControl<ItemsControl>("LibraryCards")!.GetVisualDescendants()
                .OfType<Border>().First(border => border.Classes.Contains("library-card"));
            var open = card.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Classes.Contains("primary"));
            var official = card.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Classes.Contains("home") && !button.Classes.Contains("primary"));

            open.Focus(NavigationMethod.Tab);
            Layout(window);
            Assert.Same(open, window.FocusManager!.GetFocusedElement());
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            Layout(window);
            Assert.Same(official, window.FocusManager!.GetFocusedElement());

            var openX = Bounds(open, window).Center.X;
            var officialX = Bounds(official, window).Center.X;
            Assert.True(loc.IsRightToLeft ? officialX <= openX : officialX >= openX,
                $"{display}: Tab moved against the reading direction ({openX} -> {officialX})");
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HelpIndexAndPageSevenShowTheLibraryLinkWithoutClipping()
    {
        foreach (var display in ContrastDisplays())
        {
            var loc = Localization(display);
            var calls = 0;
            using var help = new HelpViewModel(loc, () => Task.CompletedTask, () =>
            {
                calls++;
                return Task.CompletedTask;
            }, goHome: () => Task.CompletedTask);
            var window = new HelpWindow { DataContext = help };
            Show(window, display);
            var index = window.FindControl<Button>("HelpLibrary")!;
            Assert.True(index.IsEffectivelyVisible);
            AssertInside(window, index);
            Assert.Equal(loc["HomeLibrary"], index.GetVisualDescendants().OfType<TextBlock>().Single().Text);
            Capture(window, display, "phase2-help-index");

            help.Pages[6].OpenCommand.Execute(null);
            Layout(window);
            var page = window.FindControl<Button>("HelpLibraryPage")!;
            var back = window.FindControl<Button>("HelpBack")!;
            Assert.True(page.IsEffectivelyVisible);
            AssertInside(window, page);
            AssertInside(window, back);
            Assert.False(Bounds(page, window).Intersects(Bounds(back, window)));
            Capture(window, display, "phase2-help-page-seven");
            page.Command!.Execute(null);
            Assert.Equal(1, calls);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LibraryTextAndBadgeTokensMeetContrastInBothThemes()
    {
        foreach (var display in ContrastDisplays())
        {
            var loc = Localization(display);
            var window = CreateWindow(loc, new FakeFiles(), new FakeLinks());
            Show(window, display);
            var view = Assert.IsType<LibraryView>(window.Content);
            CheckTokenContrast(view, display, "library-tokens");
            window.Close();
        }
    }

    [Fact]
    public void EveryLibraryWordingComesFromBothResourceFiles()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var xaml = string.Join('\n', Directory
            .GetFiles(Path.Combine(root, "src", "DzLegal.App", "Views"), "Library*.axaml")
            .Select(File.ReadAllText));
        var code = string.Join('\n', Directory
            .GetFiles(Path.Combine(root, "src", "DzLegal.Presentation", "ViewModels"), "Library*.cs")
            .Select(File.ReadAllText));
        Assert.NotEmpty(xaml);
        Assert.NotEmpty(code);
        var keys = Regex.Matches(xaml, @"L\[(\w+)\]").Select(match => match.Groups[1].Value)
            .Concat(Regex.Matches(code, @"L\[([^\]]+)\]").SelectMany(match =>
                Regex.Matches(match.Groups[1].Value, @"""(\w+)""").Select(quoted => quoted.Groups[1].Value)))
            .Concat(Regex.Matches(code, @"L\.Format\(\s*""(\w+)""").Select(match => match.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.Contains("LibraryTitle", keys);
        Assert.Contains("LibraryCopyFormat", keys);
        Assert.Contains("LibraryNoResult", keys);
        Assert.Contains("Help7Body3", keys);
        foreach (var language in new[] { "ar", "en" })
        {
            var loc = new Loc();
            loc.SetLanguage(language);
            Assert.All(keys, key =>
            {
                Assert.DoesNotContain("[", loc[key], StringComparison.Ordinal);
                Assert.False(string.IsNullOrWhiteSpace(loc[key]), key);
            });
        }

        var ar = new Loc();
        Assert.Equal("المكتبة القانونية", ar["LibraryTitle"]);
        Assert.Equal("{0} مادة من {1} نصوص قانونية، منها {2} كاملة.", ar["LibrarySummary"]);
        Assert.Equal("المادة {0} من {1}: {2}", ar["LibraryCopyFormat"]);
        Assert.Equal("المادة {0}", ar["LibraryArticle"]);
        Assert.Equal("معدّلة بـ {0}", ar["LibraryAmendedBy"]);
        Assert.Equal("لا توجد مادة تطابق البحث.", ar["LibraryNoResult"]);
        Assert.Equal("العودة إلى القائمة", ar["LibraryBack"]);
    }

    private static LawCode Fixture(LawCheck check, bool verified) => new()
    {
        Id = "test-code",
        Title = new LocalizedText("قانون اختبار", "Test code"),
        CitationName = "قانون الاختبار",
        Reference = "نص وهمي للاختبار",
        Check = check,
        Verified = verified,
        Articles =
        [
            new LawArticle { Number = "1", Text = "نص وهمي للاختبار فقط." },
        ],
    };

    private static string ExpectedCheckText(Loc loc, LawCheck check) => check switch
    {
        LawCheck.OfficialJournalPage => loc["LibraryCheckJournal"],
        LawCheck.OfficialPdfText => loc["LibraryCheckPdf"],
        LawCheck.PageImagesReadTwice => loc["LibraryCheckImages"],
        _ => throw new ArgumentOutOfRangeException(nameof(check), check, null),
    };

    private static string ExpectedCheckTone(LawCheck check) => check switch
    {
        LawCheck.OfficialJournalPage => "journal",
        LawCheck.OfficialPdfText => "pdf",
        LawCheck.PageImagesReadTwice => "images",
        _ => throw new ArgumentOutOfRangeException(nameof(check), check, null),
    };

    private static void AssertBadge(Border badge, LibraryBadgeViewModel model)
    {
        Assert.Equal(model.Text, AutomationProperties.GetName(badge));
        Assert.Equal(model.Hint, AutomationProperties.GetHelpText(badge));
        Assert.Equal(model.Hint, ToolTip.GetTip(badge));
        Assert.Contains(badge.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == model.Text);
        Assert.Contains(badge.GetVisualDescendants().OfType<SymbolIcon>(), icon => icon.Symbol.ToString() == model.Icon);
    }

    private static void Show(Window window, Display display)
    {
        window.Width = display.Width / display.Scale;
        window.Height = display.Height / display.Scale;
        window.RequestedThemeVariant = display.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        window.Show();
        window.SetRenderScaling(display.Scale);
        Layout(window);
        Assert.InRange(window.Bounds.Width, display.Width / display.Scale - 1, display.Width / display.Scale + 1);
        Assert.InRange(window.Bounds.Height, display.Height / display.Scale - 1, display.Height / display.Scale + 1);
    }

    private static bool IsContrast(Display display) => display.Width == 1366 && display.Scale == 1;

    private static void CheckTypography(Control root)
    {
        var text = root.GetVisualDescendants().OfType<TextBlock>()
            .Where(control => control.IsEffectivelyVisible && !control.GetVisualAncestors().OfType<SymbolIcon>().Any());
        foreach (var label in text)
        {
            var secondary = label.Classes.Contains("home-note")
                || label.Classes.Contains("library-path")
                || label.Classes.Contains("library-status")
                || label.Classes.Contains("library-badge-text")
                || label.Classes.Contains("library-what-text");
            var minimum = label.Classes.Contains("library-article-text") ? 15 : secondary ? 12.5 : 14;
            Assert.True(label.FontSize >= minimum,
                $"{label.Name ?? label.Classes.FirstOrDefault() ?? "text"} has {label.FontSize} px, needs {minimum}");
        }
    }

    private static void CheckTokenContrast(Control root, Display display, string screen)
    {
        foreach (var (inkKey, backgroundKey, minimum) in new[]
        {
            ("WelcomeTextBrush", "WelcomeControlBrush", 4.5),
            ("WelcomeSecondaryBrush", "WelcomeControlBrush", 4.5),
            ("TextPrimaryBrush", "SurfaceAltBrush", 4.5),
            ("AccentBrush", "AccentSoftBrush", 4.5),
            ("DangerBrush", "DangerSoftBrush", 4.5),
            ("SuccessBrush", "SuccessSoftBrush", 4.5),
            ("WarningBrush", "SurfaceAltBrush", 4.5),
            ("WelcomeEdgeBrush", "WelcomeControlBrush", 3d),
            ("WelcomeFocusBrush", "WelcomeControlBrush", 3d),
            ("AccentBrush", "AccentSoftBrush", 3d),
            ("DangerBrush", "DangerSoftBrush", 3d),
            ("SuccessBrush", "SuccessSoftBrush", 3d),
            ("WarningBrush", "SurfaceAltBrush", 3d),
        })
        {
            var ink = ColorOf(ResourceBrush(root, inkKey));
            var background = ColorOf(ResourceBrush(root, backgroundKey));
            var ratio = Ratio(ink, background);
            Assert.True(ratio >= minimum,
                $"{display}/{screen}: {inkKey} on {backgroundKey} is {ratio:F2}:1, needs {minimum}:1");
        }
    }

    private static IBrush ResourceBrush(Control control, string key)
    {
        Assert.True(control.TryFindResource(key, control.ActualThemeVariant, out var value),
            "Missing library resource: " + key);
        return Assert.IsAssignableFrom<IBrush>(value);
    }

    private static Color ColorOf(IBrush brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static readonly double[] Linear = Enumerable.Range(0, 256)
        .Select(channel => channel / 255d is var value && value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4))
        .ToArray();

    private static double Luminance(Color color) =>
        .2126 * Linear[color.R] + .7152 * Linear[color.G] + .0722 * Linear[color.B];

    private static double Ratio(Color ink, Color background)
    {
        var first = Luminance(ink);
        var second = Luminance(background);
        return (Math.Max(first, second) + .05) / (Math.Min(first, second) + .05);
    }

    private static Rect Bounds(Control control, Window window) =>
        new Rect(control.Bounds.Size).TransformToAABB(control.TransformToVisual(window)!.Value);

    private static void AssertInside(Window window, Control control)
    {
        var rect = Bounds(control, window);
        Assert.True(
            rect.Width > 0 && rect.Height > 0 && rect.Left >= -1 && rect.Top >= -1
            && rect.Right <= window.Bounds.Width + 1 && rect.Bottom <= window.Bounds.Height + 1,
            $"{control.Name ?? control.GetType().Name} clips: {rect} in {window.Bounds.Size}");
    }

    private static void Layout(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        window.CaptureRenderedFrame()?.Dispose();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static void Capture(Window window, Display display, string screen)
    {
        if (display.Scale != 1
            || Environment.GetEnvironmentVariable("DZLEGAL_SCREENSHOTS") is not { Length: > 0 } folder) return;
        Directory.CreateDirectory(folder);
        using var bitmap = window.CaptureRenderedFrame();
        Assert.NotNull(bitmap);
        bitmap.Save(Path.Combine(folder, $"{screen}-{display.Suffix}.png"), PngBitmapEncoderOptions.Default);
    }
}
