using DzLegal.App.Services;
using DzLegal.App.ViewModels;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
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

internal sealed class FailingProvider : IModelProvider
{
    public string Name => "failing";

    public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) =>
        throw new ModelProviderException("The local model server is not reachable at http://127.0.0.1:11434/.");
}

internal static class Vm
{
    public static readonly string EvaluationsFolder = Path.Combine(Path.GetTempPath(), "dzlegal-app-tests-" + Guid.NewGuid().ToString("N"));

    public static MainWindowViewModel Create(FakeFiles? files = null, string provider = "stub", Loc? loc = null)
    {
        loc ??= new Loc();
        var definition = DefinitionLoader.Load(Path.Combine(AppContext.BaseDirectory, "data", "document-types", "requete-introductive"));
        IReadOnlyList<ProviderOption> providers =
        [
            new ProviderOption("stub", l => l["ProviderStub"], () => new StubModelProvider()),
            new ProviderOption("failing", _ => "failing", () => new FailingProvider()),
        ];
        return new MainWindowViewModel(definition, providers, files ?? new FakeFiles(), new EvaluationStore(EvaluationsFolder), loc, provider, () => new DateOnly(2026, 9, 15));
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
