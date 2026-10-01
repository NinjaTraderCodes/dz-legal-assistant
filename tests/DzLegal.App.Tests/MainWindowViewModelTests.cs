using System.Xml.Linq;
using DzLegal.Domain;

namespace DzLegal.App.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void EmptyFormIsReportedAsInadmissible()
    {
        var vm = Vm.Create();

        Assert.False(vm.IsAdmissible);
        var court = vm.Findings.Single(f => f.ElementId == "art15.1.court");
        Assert.True(court.IsMissingMandatory);
        Assert.Equal("CPCA art. 15-1", court.Article);
        Assert.True(court.IsUnverified);
        Assert.True(vm.Findings[0].IsMissingMandatory); // missing mandatory items are listed first
    }

    [Fact]
    public void TypingInTheFormUpdatesTheDraftAndTheReportLive()
    {
        var vm = Vm.Create();

        vm.Field("court.name").Value = "محكمة الشراقة";

        Assert.Contains("محكمة الشراقة", vm.Sections.Single(s => s.Id == "header").Text, StringComparison.Ordinal);
        Assert.Equal(ElementStatus.Present, vm.Report!.Find("art15.1.court")!.Status);
    }

    [Fact]
    public async Task GeneratingFromCompleteInputsGivesAnAdmissibleDraft()
    {
        var vm = Vm.Create().Filled();

        await vm.GenerateCommand.ExecuteAsync(null);

        Assert.True(vm.IsAdmissible, string.Join(", ", vm.Report!.Missing.Select(m => m.ElementId)));
        Assert.True(vm.HasGeneratedDraft);
        Assert.Contains("500000", vm.Sections.Single(s => s.Id == "facts").Text, StringComparison.Ordinal);
        Assert.False(vm.IsError);
        Assert.StartsWith("تمت صياغة المسودة", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EditingTheDraftRechecksIt()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var facts = vm.Sections.Single(s => s.Id == "facts");

        facts.Text = "   ";
        Assert.True(vm.Findings.Single(f => f.ElementId == "art15.5.facts").IsMissingMandatory);
        Assert.False(vm.IsAdmissible);

        facts.Text = "حيث إن الوقائع كما يلي.";
        Assert.True(vm.Findings.Single(f => f.ElementId == "art15.5.facts").IsPresent);
        Assert.True(vm.IsAdmissible);
    }

    [Fact]
    public async Task FormChangesKeepTheLawyersNarrativeEdits()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        vm.Sections.Single(s => s.Id == "facts").Text = "نص حرره المحامي";

        vm.Field("court.name").Value = "محكمة بئر مراد رايس";

        Assert.Equal("نص حرره المحامي", vm.Sections.Single(s => s.Id == "facts").Text);
        Assert.Contains("محكمة بئر مراد رايس", vm.Sections.Single(s => s.Id == "header").Text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedSectionsAreReadOnlyAndNarrativeSectionsEditable()
    {
        var vm = Vm.Create();
        Assert.True(vm.Sections.Single(s => s.Id == "header").IsReadOnly);
        Assert.True(vm.Sections.Single(s => s.Id == "grounds").IsEditable);
    }

    [Fact]
    public void LegalPersonToggleSwapsTheIdentificationFields()
    {
        var vm = Vm.Create();
        Assert.True(vm.Field("defendant.lastName").IsVisible);
        Assert.False(vm.Field("defendant.entityName").IsVisible);

        vm.Field("defendant.isLegalEntity").BoolValue = true;

        Assert.False(vm.Field("defendant.lastName").IsVisible);
        Assert.True(vm.Field("defendant.entityName").IsVisible);
        Assert.True(vm.Findings.Single(f => f.ElementId == "art15.4.defendant").IsMissingMandatory);
        Assert.True(vm.Findings.Single(f => f.ElementId == "art15.3.defendant").IsNotApplicable);
    }

    [Fact]
    public void RealPropertyClaimAddsThePublicationRequirement()
    {
        var vm = Vm.Create().Filled();
        Assert.DoesNotContain(vm.Sections, s => s.Id == "publication");

        vm.Field("claim.concernsRegisteredRealProperty").BoolValue = true;
        Assert.Contains(vm.Sections, s => s.Id == "publication");
        var publication = vm.Findings.Single(f => f.ElementId == "art17.publication");
        Assert.True(publication.IsMissingMandatory);
        Assert.Contains("مراجع إشهار العريضة", publication.MissingPartsText, StringComparison.Ordinal);

        vm.Field("claim.publicationReference").Value = "إيداع رقم 1234";
        Assert.True(vm.Findings.Single(f => f.ElementId == "art17.publication").IsPresent);
    }

    [Fact]
    public async Task ModelFailureIsShownAndNothingIsLost()
    {
        var vm = Vm.Create(provider: "failing").Filled();
        vm.Sections.Single(s => s.Id == "facts").Text = "نص موجود";

        await vm.GenerateCommand.ExecuteAsync(null);

        Assert.True(vm.IsError);
        Assert.Contains("127.0.0.1", vm.StatusMessage, StringComparison.Ordinal);
        Assert.Equal("نص موجود", vm.Sections.Single(s => s.Id == "facts").Text);
        Assert.False(vm.HasGeneratedDraft);
    }

    [Fact]
    public async Task SaveThenOpenRestoresTheDraft()
    {
        var files = new FakeFiles();
        var vm = Vm.Create(files).Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        vm.Sections.Single(s => s.Id == "requests").Text = "طلب معدل";
        await vm.SaveCommand.ExecuteAsync(null);

        var reopened = Vm.Create(files);
        await reopened.OpenCommand.ExecuteAsync(null);

        Assert.Equal("بن علي", reopened.Field("plaintiff.lastName").Value);
        Assert.Equal("طلب معدل", reopened.Sections.Single(s => s.Id == "requests").Text);
        Assert.Equal(vm.IsAdmissible, reopened.IsAdmissible);
    }

    [Fact]
    public async Task CopyTextPutsTheWholeDocumentOnTheClipboard()
    {
        var files = new FakeFiles();
        var vm = Vm.Create(files).Filled();
        await vm.GenerateCommand.ExecuteAsync(null);

        await vm.CopyTextCommand.ExecuteAsync(null);

        Assert.StartsWith("الجمهورية الجزائرية الديمقراطية الشعبية", files.Clipboard, StringComparison.Ordinal);
        Assert.Contains("الوقائع:", files.Clipboard, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EvaluationIsRecordedLocally()
    {
        var vm = Vm.Create().Filled();
        Assert.False(vm.SaveEvaluationCommand.CanExecute(null));
        await vm.GenerateCommand.ExecuteAsync(null);
        vm.MinutesSaved = "30";

        await vm.SaveEvaluationCommand.ExecuteAsync(null);

        Assert.Contains(Vm.EvaluationsFolder, vm.StatusMessage, StringComparison.Ordinal);
        var records = new Storage.EvaluationStore(Vm.EvaluationsFolder).LoadAll();
        var record = records[^1];
        Assert.Equal(30, record.MinutesSaved);
        Assert.Equal("stub (template mode, no AI)", record.GeneratedBy);
    }

    [Fact]
    public void GenderChoiceChangesTheWording()
    {
        var vm = Vm.Create().Filled();
        var plaintiff = vm.Sections.Single(s => s.Id == "plaintiff");
        Assert.StartsWith("لفائدة: بن علي كريم", plaintiff.Text, StringComparison.Ordinal);
        Assert.Contains("اختر جنس المدعي", plaintiff.ReviewText, StringComparison.Ordinal);

        var gender = vm.Field("plaintiff.gender");
        Assert.True(gender.IsChoice);
        gender.SelectedOption = gender.Options.Single(o => o.Value == "female");

        Assert.StartsWith("لفائدة: السيدة بن علي كريم", plaintiff.Text, StringComparison.Ordinal);
        Assert.Equal("أنثى", gender.SelectedOption!.Label);
        Assert.False(plaintiff.HasReview);
    }

    [Fact]
    public async Task InventedNumbersAreHintedUnderTheSectionUntilFixed()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var grounds = vm.Sections.Single(s => s.Id == "grounds");
        Assert.False(grounds.HasReview);

        grounds.Text += "\nوحيث إن المادة 999 تؤيد ذلك في هذا caso.";
        Assert.Contains("999", grounds.ReviewText, StringComparison.Ordinal);
        Assert.Contains("caso", grounds.ReviewText, StringComparison.Ordinal);

        grounds.Text = grounds.Text.Replace("\nوحيث إن المادة 999 تؤيد ذلك في هذا caso.", string.Empty, StringComparison.Ordinal);
        Assert.False(grounds.HasReview);
    }

    [Fact]
    public void NewResetsTheForm()
    {
        var vm = Vm.Create().Filled();
        vm.NewCommand.Execute(null);
        Assert.Equal(string.Empty, vm.Field("court.name").Value);
        Assert.Equal("15/09/2026", vm.Field("document.date").Value);
        Assert.False(vm.IsAdmissible);
    }

    [Fact]
    public void SwitchingLanguageTranslatesTheInterface()
    {
        var vm = Vm.Create();
        Assert.Equal(Avalonia.Media.FlowDirection.RightToLeft, vm.FlowDirection);
        Assert.Equal("المحكمة", vm.Field("court.name").Label);

        vm.SwitchLanguageCommand.Execute(null);

        Assert.Equal(Avalonia.Media.FlowDirection.LeftToRight, vm.FlowDirection);
        Assert.Equal("Court (tribunal)", vm.Field("court.name").Label);
        Assert.Equal("Court before which the action is brought", vm.Findings.Single(f => f.ElementId == "art15.1.court").Label);
        Assert.Equal("Draft narrative sections", vm.L["Generate"]);
    }

    [Fact]
    public void EveryUiStringExistsInBothLanguages()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DzLegal.App", "Resources"));
        static HashSet<string> Keys(string file) =>
            XDocument.Load(file).Root!.Elements("data").Select(d => (string)d.Attribute("name")!).ToHashSet();

        var arabic = Keys(Path.Combine(root, "Strings.resx"));
        var english = Keys(Path.Combine(root, "Strings.en.resx"));

        Assert.Equal(arabic.Order(), english.Order());
        var loc = new Loc();
        foreach (var key in arabic)
        {
            Assert.DoesNotContain("[", loc[key], StringComparison.Ordinal);
        }
    }
}
