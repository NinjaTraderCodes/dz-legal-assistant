using System.Text.Json;
using DzLegal.Domain.Serialization;

namespace DzLegal.Law;

/// <summary>
/// Loads the law library, the source manifest and the claim catalog from <c>data/law</c>, and checks them against
/// each other: every claim cites a known code, and once a code is imported every cited article must exist and be
/// in force. Until then the citation stays visible but its text is shown as not yet available.
/// </summary>
public static class LegalKnowledgeLoader
{
    public const string ClaimsFolder = "claims";

    public static readonly IReadOnlyList<string> Areas = ["civil", "commercial", "family", "social"];

    public static LegalKnowledge Load(string lawRoot)
    {
        if (!File.Exists(Path.Combine(lawRoot, LawSourceManifest.FileName)))
        {
            return LegalKnowledge.Empty;
        }

        var manifest = LawSourceManifest.Load(lawRoot);
        var library = LawLibraryLoader.Load(lawRoot);
        var claims = new List<ClaimType>();
        var folder = Path.Combine(lawRoot, ClaimsFolder);
        foreach (var path in Directory.Exists(folder) ? Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal).ToArray() : [])
        {
            ClaimCatalogFile? file;
            try
            {
                file = JsonSerializer.Deserialize<ClaimCatalogFile>(File.ReadAllText(path), DefinitionJson.Options);
            }
            catch (JsonException ex)
            {
                throw new LawDataException(path, [$"Invalid JSON: {ex.Message}"]);
            }

            if (file is null)
            {
                throw new LawDataException(path, ["File is empty."]);
            }

            var errors = Validate(file, manifest, library);
            if (errors.Count > 0)
            {
                throw new LawDataException(path, errors);
            }

            claims.AddRange(file.Claims.Select(c => c with { Area = file.Area }));
        }

        var duplicate = claims.GroupBy(c => c.Id).FirstOrDefault(g => g.Count() > 1);
        return duplicate is not null
            ? throw new LawDataException(folder, [$"Claim id '{duplicate.Key}' is defined more than once."])
            : new LegalKnowledge(library, manifest, claims);
    }

    public static List<string> Validate(ClaimCatalogFile file, LawSourceManifest manifest, LawLibrary library)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(library);
        var errors = new List<string>();
        if (!Areas.Contains(file.Area))
        {
            errors.Add($"Area '{file.Area}' must be one of: {string.Join(", ", Areas)}.");
        }

        foreach (var claim in file.Claims)
        {
            var where = $"Claim '{claim.Id}'";
            if (claim.Triggers.Count == 0 || claim.Triggers.Any(t => t.Any.Count == 0 || t.Any.Any(string.IsNullOrWhiteSpace) || t.Weight <= 0))
            {
                errors.Add($"{where}: every trigger group needs words and a positive weight, and there must be at least one.");
            }

            if (!claim.Triggers.Any(t => t.Required))
            {
                errors.Add($"{where}: at least one trigger group must be required, so the claim is never suggested on generic words alone.");
            }

            if (claim.Articles.Count == 0)
            {
                errors.Add($"{where}: no articles.");
            }

            foreach (var article in claim.Articles)
            {
                if (manifest.Codes.All(c => c.Id != article.Code))
                {
                    errors.Add($"{where}: unknown code '{article.Code}'.");
                    continue;
                }

                if (article.Number != ArticleNumbers.Canonical(article.Number))
                {
                    errors.Add($"{where}: article number '{article.Number}' is not canonical.");
                }

                if (library.FindCode(article.Code) is { } code)
                {
                    var found = code.FindArticle(article.Number);
                    if (found is null)
                    {
                        errors.Add($"{where}: article {article.Number} does not exist in the imported {article.Code}.");
                    }
                    else if (found.Repealed)
                    {
                        errors.Add($"{where}: article {article.Number} of {article.Code} is repealed.");
                    }
                }
            }
        }

        return errors;
    }
}
