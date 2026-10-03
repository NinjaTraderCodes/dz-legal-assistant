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
        Assert.StartsWith("حيث إن العارض أقرض المدعى عليه مبلغ 500000 دج", vm.Sections.Single(s => s.Id == "facts").Text, StringComparison.Ordinal);
        Assert.False(vm.IsError);
        Assert.StartsWith("تمت صياغة المسودة", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EditingTheDraftRechecksIt()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var grounds = vm.Sections.Single(s => s.Id == "grounds");

        grounds.Text = "   ";
        Assert.True(vm.Findings.Single(f => f.ElementId == "art15.5.grounds").IsMissingMandatory);
        Assert.False(vm.IsAdmissible);

        grounds.Text = "حيث إن المادة 106 من القانون المدني تنص على ذلك.";
        Assert.True(vm.Findings.Single(f => f.ElementId == "art15.5.grounds").IsPresent);
        Assert.True(vm.IsAdmissible);
    }

    [Fact]
    public async Task FormChangesKeepTheLawyersNarrativeEdits()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        vm.Sections.Single(s => s.Id == "grounds").Text = "نص حرره المحامي";

        vm.Field("court.name").Value = "محكمة بئر مراد رايس";

        Assert.Equal("نص حرره المحامي", vm.Sections.Single(s => s.Id == "grounds").Text);
        Assert.Contains("محكمة بئر مراد رايس", vm.Sections.Single(s => s.Id == "header").Text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedSectionsAreReadOnlyAndNarrativeSectionsEditable()
    {
        var vm = Vm.Create();
        Assert.True(vm.Sections.Single(s => s.Id == "header").IsReadOnly);
        Assert.True(vm.Sections.Single(s => s.Id == "facts").IsReadOnly);
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
        vm.Sections.Single(s => s.Id == "grounds").Text = "نص موجود";

        await vm.GenerateCommand.ExecuteAsync(null);

        Assert.True(vm.IsError);
        Assert.Contains("127.0.0.1", vm.StatusMessage, StringComparison.Ordinal);
        Assert.Equal("نص موجود", vm.Sections.Single(s => s.Id == "grounds").Text);
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
    public async Task ExportsPdfAndWordFiles()
    {
        var files = new FakeFiles();
        var vm = Vm.Create(files).Filled();
        await vm.GenerateCommand.ExecuteAsync(null);

        await vm.ExportPdfCommand.ExecuteAsync(null);
        await vm.ExportWordCommand.ExecuteAsync(null);

        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(files.SavedByKind[Services.SaveKind.Pdf], 0, 4), StringComparison.Ordinal);
        Assert.Equal((byte)'P', files.SavedByKind[Services.SaveKind.Word][0]); // .docx is a zip ("PK")
        Assert.StartsWith("تم التصدير", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreviewRendersThePrintedPages()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);

        await vm.RefreshPreviewAsync();

        Assert.InRange(vm.PreviewPages.Count, 1, 3);
        Assert.Equal(0x89, vm.PreviewPages[0][0]); // PNG
    }

    [Fact]
    public async Task StepsShowWhereSomethingIsMissing()
    {
        var vm = Vm.Create();
        Assert.Equal(9, vm.FieldGroups.Count);
        Assert.True(vm.FieldGroups.Single(g => g.Id == "court").IsMissing);

        vm.Filled();
        Assert.True(vm.FieldGroups.Single(g => g.Id == "court").IsDone);
        Assert.True(vm.FieldGroups.Single(g => g.Id == "grounds").IsMissing); // grounds not drafted yet
        Assert.True(vm.FieldGroups.Single(g => g.Id == "requests").IsMissing);
        Assert.True(vm.FieldGroups.Single(g => g.Id == "facts").IsDone);

        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.False(vm.FieldGroups.Any(g => g.IsMissing), string.Join(",", vm.FieldGroups.Where(g => g.IsMissing).Select(g => g.Id)));
    }

    [Fact]
    public void MissingFieldsAreMarkedWithTheirArticle()
    {
        var vm = Vm.Create().Filled();
        vm.Field("defendant.domicile").Value = string.Empty;

        Assert.True(vm.Field("defendant.domicile").IsMissing);
        Assert.Contains("CPCA art. 15-3", vm.Field("defendant.domicile").MissingNote, StringComparison.Ordinal);
        Assert.False(vm.Field("plaintiff.domicile").IsMissing);
        Assert.True(vm.FieldGroups.Single(g => g.Id == "defendant").IsMissing);
    }

    [Fact]
    public void ClickingAFindingOpensTheStepThatFixesIt()
    {
        var vm = Vm.Create().Filled();
        vm.Field("lawyer.name").Value = string.Empty;
        var finding = vm.IssueFindings.Single(f => f.ElementId == "art14.signature");

        vm.GoToFindingCommand.Execute(finding);

        Assert.Equal("signature", vm.SelectedStep!.Id);
    }

    [Fact]
    public void ComplianceScoreCountsApplicableElements()
    {
        var vm = Vm.Create().Filled();
        Assert.Equal(vm.ApplicableCount - vm.IssueFindings.Count, vm.PresentCount);
        Assert.Equal(vm.PresentCount, vm.PresentFindings.Count);
        Assert.Equal($"{vm.PresentCount} من {vm.ApplicableCount}", vm.ScoreText);
    }

    [Fact]
    public void NextAndBackMoveThroughTheSteps()
    {
        var vm = Vm.Create();
        vm.NextStepCommand.Execute(null);
        Assert.Equal("plaintiff", vm.SelectedStep!.Id);
        vm.PreviousStepCommand.Execute(null);
        vm.PreviousStepCommand.Execute(null);
        Assert.Equal("court", vm.SelectedStep!.Id);
        Assert.Equal("الخطوة 1 من 9", vm.StepPosition);
    }

    [Fact]
    public async Task AGeneratedSectionCanBeEditedByHandAndRestored()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var opening = vm.Sections.Single(s => s.Id == "opening");
        Assert.True(opening.IsReadOnly);

        opening.OverrideCommand.Execute(null);
        Assert.False(opening.IsReadOnly);
        opening.Text = "إلى السادة رئيس وأعضاء المحكمة المحترمين";
        vm.Field("court.name").Value = "محكمة الشراقة";
        Assert.Equal("إلى السادة رئيس وأعضاء المحكمة المحترمين", opening.Text);
        Assert.Contains("عدّلت هذا القسم يدويا", opening.ReviewText, StringComparison.Ordinal);

        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Equal("إلى السادة رئيس وأعضاء المحكمة المحترمين", opening.Text);

        opening.RestoreCommand.Execute(null);
        Assert.True(opening.IsReadOnly);
        Assert.Contains("محكمة الشراقة", opening.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AutosavedWorkCanBeRestoredInANewSession()
    {
        var file = Path.Combine(Vm.EvaluationsFolder, "autosave-" + Guid.NewGuid().ToString("N") + ".json");
        var first = Vm.Create(autosaveFile: file).Filled();
        first.Sections.Single(s => s.Id == "grounds").Text = "نص لم يحفظ بعد";
        first.AutosaveNow();

        var second = Vm.Create(autosaveFile: file);
        Assert.True(second.HasRecoverableDraft);
        await second.RestoreLastSessionCommand.ExecuteAsync(null);

        Assert.Equal("بن علي", second.Field("plaintiff.lastName").Value);
        Assert.Equal("نص لم يحفظ بعد", second.Sections.Single(s => s.Id == "grounds").Text);
        Assert.False(second.HasRecoverableDraft);

        second.DismissRecoveryCommand.Execute(null);
        Assert.False(File.Exists(file));
    }

    /// <summary>
    /// Autosave runs on the UI thread. Blocking there on async I/O deadlocked once a filled-in draft outgrew the
    /// write buffers (the continuation waited for the blocked UI thread), freezing the app 30 s after data entry.
    /// </summary>
    [Fact]
    public void AutosaveDoesNotDeadlockOnTheUiThreadWithALargeDraft()
    {
        var dir = Directory.CreateTempSubdirectory("dzlegal-autosave-");
        var file = Path.Combine(dir.FullName, "last-session" + Storage.DraftFile.Extension);
        var vm = Vm.Create(autosaveFile: file).Filled();
        var notes = string.Concat(Enumerable.Repeat("اقترض المدعى عليه مبلغا من المدعي ولم يرده رغم الإنذار.\n", 400));
        vm.Field("notes.facts").Value = notes;

        Exception? error = null;
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new BlockedUiContext());
            try
            {
                vm.AutosaveNow();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Autosave deadlocked on the UI thread.");
        Assert.Null(error);
        var restored = Vm.Create(autosaveFile: file);
        Assert.True(restored.HasRecoverableDraft);
        dir.Delete(recursive: true);
    }

    /// <summary>A UI context whose thread is busy: posted continuations never run.</summary>
    private sealed class BlockedUiContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
        }
    }

    [Fact]
    public void ThemeToggles()
    {
        var vm = Vm.Create();
        Assert.Equal(Avalonia.Styling.ThemeVariant.Light, vm.RequestedTheme);
        vm.ToggleThemeCommand.Execute(null);
        Assert.Equal(Avalonia.Styling.ThemeVariant.Dark, vm.RequestedTheme);
    }

    [Fact]
    public async Task AssistantProposesARevisionShownAsADiffThenAccepted()
    {
        var vm = Vm.Create(provider: "scripted").Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var grounds = vm.Sections.Single(s => s.Id == "grounds");
        var before = grounds.Text;

        grounds.OpenAssistantCommand.Execute(null);
        Assert.True(vm.IsAssistantOpen);
        Assert.NotEmpty(vm.Shortcuts);
        vm.AssistantInstruction = "أكثر رسمية";
        await vm.SendInstructionCommand.ExecuteAsync(null);

        Assert.True(vm.HasProposal);
        Assert.Contains(vm.ProposalDiff, t => t.IsAdded);
        Assert.Equal(before, grounds.Text); // nothing applied before the lawyer accepts

        vm.AcceptProposalCommand.Execute(null);
        Assert.Equal(ScriptedProvider.Revision, grounds.Text);
        Assert.True(grounds.CanUndo);
        grounds.UndoCommand.Execute(null);
        Assert.Equal(before, grounds.Text);
    }

    [Fact]
    public async Task AssistantRefusesLegalQuestions()
    {
        var vm = Vm.Create(provider: "scripted").Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        vm.Sections.Single(s => s.Id == "grounds").OpenAssistantCommand.Execute(null);

        vm.AssistantInstruction = "ما هو أجل التقادم؟";
        await vm.SendInstructionCommand.ExecuteAsync(null);

        Assert.False(vm.HasProposal);
        Assert.Equal(vm.L["AssistantScope"], vm.AssistantThread[^1].Text);
    }

    [Fact]
    public async Task RedraftingKeepsSectionsTheLawyerEdited()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        vm.Sections.Single(s => s.Id == "grounds").Text = "نص كتبه المحامي";

        await vm.GenerateCommand.ExecuteAsync(null);

        Assert.Equal("نص كتبه المحامي", vm.Sections.Single(s => s.Id == "grounds").Text);
        Assert.Contains("أُبقي على الأقسام التي عدّلتها", vm.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SectionRedraftAsksForConfirmationAndCanBeUndone()
    {
        var vm = Vm.Create().Filled();
        await vm.GenerateCommand.ExecuteAsync(null);
        var grounds = vm.Sections.Single(s => s.Id == "grounds");
        grounds.Text = "نص كتبه المحامي";

        await grounds.RedraftCommand.ExecuteAsync(null);
        Assert.True(grounds.IsConfirmingRedraft);
        Assert.Equal("نص كتبه المحامي", grounds.Text);

        await grounds.RedraftCommand.ExecuteAsync(null);
        Assert.False(grounds.IsConfirmingRedraft);
        Assert.NotEqual("نص كتبه المحامي", grounds.Text);

        grounds.UndoCommand.Execute(null);
        Assert.Equal("نص كتبه المحامي", grounds.Text);
    }

    [Fact]
    public async Task ModelStatusTellsWhenTheEngineIsNotRunning()
    {
        var vm = Vm.Create(provider: "failing");
        await vm.CheckModelStatusAsync();
        Assert.False(vm.IsModelReady);
        Assert.Contains("Ollama", vm.ModelStatusText, StringComparison.Ordinal);

        vm.SelectedProvider = vm.Providers.Single(p => p.Key == "stub");
        await vm.CheckModelStatusAsync();
        Assert.True(vm.IsModelReady);
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
