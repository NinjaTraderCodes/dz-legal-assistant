namespace DzLegal.Documents;

/// <summary>The bundled Noto Naskh Arabic font (SIL OFL), embedded in this assembly so PDFs render identically offline.</summary>
public static class DocumentFonts
{
    public const string Family = "Noto Naskh Arabic";

    /// <summary>Font Word uses for Arabic text: installed with Windows, so the .docx looks right on any lawyer's PC.</summary>
    public const string WordComplexScriptFamily = "Simplified Arabic";

    public static IEnumerable<Stream> OpenAll()
    {
        var assembly = typeof(DocumentFonts).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("Fonts.", StringComparison.Ordinal)))
        {
            yield return assembly.GetManifestResourceStream(name)!;
        }
    }
}
