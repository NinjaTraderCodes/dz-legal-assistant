using DzLegal.App.Services;
using DzLegal.App.ViewModels;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.Law;
using DzLegal.ModelProviders.Stub;
using DzLegal.Storage;

namespace DzLegal.App.Tests;

/// <summary>In-memory file pickers and clipboard.</summary>
internal sealed class FakeFiles : IUserFiles
{
    public byte[]? Saved { get; private set; }

    public string? Clipboard { get; private set; }

    public Task<(Stream Stream, string Name)?> OpenAsync() =>
        Task.FromResult<(Stream, string)?>(Saved is null ? null : (new MemoryStream(Saved), "saved" + DraftFile.Extension));

    public Dictionary<SaveKind, byte[]> SavedByKind { get; } = [];

    public Task<(Stream Stream, string Name)?> SaveAsync(string suggestedName, SaveKind kind) =>
        Task.FromResult<(Stream, string)?>((new CapturingStream(bytes =>
        {
            SavedByKind[kind] = bytes;
            if (kind == SaveKind.Draft)
            {
                Saved = bytes;
            }
        }), suggestedName));

    public Task CopyTextAsync(string text)
    {
        Clipboard = text;
        return Task.CompletedTask;
    }

    private sealed class CapturingStream(Action<byte[]> onClose) : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                onClose(ToArray());
            }

            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            onClose(ToArray());
            return base.DisposeAsync();
        }
    }
}

/// <summary>Answers revision prompts with a fixed rewrite and drafting prompts with the notes.</summary>
internal sealed class ScriptedProvider : IModelProvider
{
    public const string Revision = "حيث إن المادة 106 من القانون المدني تنص على أن العقد شريعة المتعاقدين، وقد أخل المدعى عليه بالتزامه.";

    public string Name => "scripted";

    public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(request.UserPrompt.Contains("النص الحالي للقسم", StringComparison.Ordinal)
            ? request.UserPrompt.Contains("ما هو", StringComparison.Ordinal) ? SectionReviser.RefusalMarker : Revision
            : request.SourceText);
}

/// <summary>Records every prompt and answers with the section notes (like template mode).</summary>
internal sealed class RecordingProvider : IModelProvider
{
    public List<ModelRequest> Requests { get; } = [];

    public string Name => "recording";

    public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(request.SourceText);
    }
}

internal sealed class FailingProvider : IModelProvider
{
    public string Name => "failing";

    public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) =>
        throw new ModelProviderException("The local model server is not reachable at http://127.0.0.1:11434/.");
}

internal static class Vm
{
    public static readonly string EvaluationsFolder = Path.Combine(Path.GetTempPath(), "dzlegal-app-tests-" + Guid.NewGuid().ToString("N"));

    /// <summary>The shipped claim catalog; no official text is imported yet, so articles show without text.</summary>
    public static readonly LegalKnowledge ShippedKnowledge = LegalKnowledgeLoader.Load(Path.Combine(AppContext.BaseDirectory, "data", "law"));

    /// <summary>
    /// The shipped catalog with a FICTITIOUS civil-code library (some loan articles only), to test what happens
    /// once texts are imported. Never shown as real law.
    /// </summary>
    public static readonly LegalKnowledge FixtureKnowledge = new(
        new LawLibrary(
        [
            new LawCode
            {
                Id = "civil-code",
                Title = new Domain.LocalizedText("قانون تجريبي", "Fictitious code"),
                CitationName = "القانون المدني",
                Reference = "نص وهمي للاختبار",
                Source = new LawSource { Url = "fixture" },
                Articles =
                [
                    new LawArticle { Number = "106", Text = "نص وهمي للاختبار: العقد الوهمي يلزم طرفيه." },
                    new LawArticle { Number = "323", Text = "نص وهمي للاختبار: على الدائن الوهمي إثبات الالتزام وعلى المدين إثبات التخلص منه." },
                    new LawArticle { Number = "450", Text = "نص وهمي للاختبار: القرض الوهمي عقد يلتزم به المقرض بنقل مبلغ على أن يرد إليه المقترض مثله." },
                ],
            },
        ]),
        ShippedKnowledge.Manifest,
        ShippedKnowledge.Claims);

    public static MainWindowViewModel Create(
        FakeFiles? files = null,
        string provider = "stub",
        Loc? loc = null,
        string? autosaveFile = null,
        LegalKnowledge? knowledge = null,
        RecordingProvider? recorder = null)
    {
        loc ??= new Loc();
        var definition = DefinitionLoader.Load(Path.Combine(AppContext.BaseDirectory, "data", "document-types", "requete-introductive"));
        List<ProviderOption> providers =
        [
            new ProviderOption("stub", l => l["ProviderStub"], () => new StubModelProvider()),
            new ProviderOption("failing", _ => "failing", () => new FailingProvider(), _ => Task.FromResult(new ModelStatus(ModelState.NotRunning))),
            new ProviderOption("scripted", _ => "scripted", () => new ScriptedProvider()),
        ];
        if (recorder is not null)
        {
            providers.Add(new ProviderOption("recording", _ => "recording", () => recorder));
            provider = "recording";
        }

        return new MainWindowViewModel(definition, providers, files ?? new FakeFiles(), new EvaluationStore(EvaluationsFolder), loc, provider, () => new DateOnly(2026, 9, 15), autosaveFile, knowledge ?? ShippedKnowledge);
    }

    /// <summary>Fills every mandatory field with fictitious data, like a lawyer would.</summary>
    public static MainWindowViewModel Filled(this MainWindowViewModel vm)
    {
        var values = new Dictionary<string, string>
        {
            ["court.council"] = "الجزائر",
            ["court.name"] = "محكمة سيدي امحمد",
            ["court.section"] = "القسم المدني",
            ["plaintiff.lastName"] = "بن علي",
            ["plaintiff.firstName"] = "كريم",
            ["plaintiff.domicile"] = "12 شارع ديدوش مراد، الجزائر",
            ["defendant.lastName"] = "حمداني",
            ["defendant.firstName"] = "سمير",
            ["defendant.domicile"] = "5 نهج العربي بن مهيدي، الجزائر",
            ["claim.subject"] = "المطالبة بدين",
            ["notes.facts"] = "حيث إن العارض أقرض المدعى عليه مبلغ 500000 دج بتاريخ 10/01/2025 بموجب reconnaissance de dette، ولم يسدده.",
            ["notes.grounds"] = "حيث إن المادة 106 من القانون المدني تنص على أن العقد شريعة المتعاقدين.",
            ["notes.requests"] = "إلزام المدعى عليه بأن يدفع للعارض مبلغ 500000 دج.",
            ["documents.list"] = "عقد القرض\nإنذار بالدفع",
            ["lawyer.name"] = "نادية سعدي",
            ["lawyer.bar"] = "محامية لدى مجلس قضاء الجزائر",
            ["document.place"] = "الجزائر",
        };
        foreach (var (id, value) in values)
        {
            vm.Field(id).Value = value;
        }

        return vm;
    }
}
