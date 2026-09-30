using DzLegal.Domain;
using DzLegal.Domain.Serialization;

namespace DzLegal.Drafting.Tests;

internal static class Fixtures
{
    public static DocumentTypeDefinition Requete { get; } = DefinitionLoader.Load(
        Path.Combine(AppContext.BaseDirectory, "data", "document-types", "requete-introductive"));

    /// <summary>Complete inputs for a natural-person requête (fictitious parties).</summary>
    public static Dictionary<string, string> CompleteInputs()
    {
        var inputs = CaseInputs.CreateDefaults(Requete, new DateOnly(2026, 9, 15));
        inputs["court.council"] = "الجزائر";
        inputs["court.name"] = "محكمة سيدي امحمد";
        inputs["court.section"] = "القسم المدني";
        inputs["plaintiff.lastName"] = "بن علي";
        inputs["plaintiff.firstName"] = "كريم";
        inputs["plaintiff.domicile"] = "12 شارع ديدوش مراد، الجزائر";
        inputs["defendant.lastName"] = "حمداني";
        inputs["defendant.firstName"] = "سمير";
        inputs["defendant.domicile"] = "5 نهج العربي بن مهيدي، الجزائر";
        inputs["claim.subject"] = "المطالبة بدين";
        inputs["notes.facts"] = "قرض 500000 دج بتاريخ 10/01/2025 لم يسدد.";
        inputs["notes.grounds"] = "المادة 106 من القانون المدني.";
        inputs["notes.requests"] = "إلزام المدعى عليه بدفع 500000 دج.";
        inputs["documents.list"] = "- عقد القرض\n\n- إنذار بالدفع\n";
        inputs["lawyer.name"] = "نادية سعدي";
        inputs["lawyer.bar"] = "محامية لدى مجلس قضاء الجزائر";
        inputs["document.place"] = "الجزائر";
        return inputs;
    }
}

/// <summary>Records every request and answers with a canned reply.</summary>
internal sealed class RecordingProvider(Func<ModelRequest, string> reply) : IModelProvider
{
    public List<ModelRequest> Requests { get; } = [];

    public string Name => "recording";

    public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(reply(request));
    }
}
