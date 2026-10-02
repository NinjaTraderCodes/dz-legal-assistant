using System.Text.Json;
using DzLegal.Domain.Serialization;

namespace DzLegal.Law;

/// <summary>
/// Loads the law library from <c>data/law/codes/*.json</c> and validates it, so a bad import or edit fails at
/// load time and in CI. A missing folder means no texts have been imported yet: the library is empty.
/// </summary>
public static class LawLibraryLoader
{
    public const string CodesFolder = "codes";

    public static LawLibrary Load(string lawRoot)
    {
        var folder = Path.Combine(lawRoot, CodesFolder);
        if (!Directory.Exists(folder))
        {
            return LawLibrary.Empty;
        }

        var codes = Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal).Select(LoadCode).ToList();
        var duplicate = codes.GroupBy(c => c.Id).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new LawDataException(folder, [$"Code id '{duplicate.Key}' appears in more than one file."]);
        }

        return new LawLibrary(codes);
    }

    public static LawCode LoadCode(string path)
    {
        LawCode? code;
        try
        {
            code = JsonSerializer.Deserialize<LawCode>(File.ReadAllText(path), DefinitionJson.Options);
        }
        catch (JsonException ex)
        {
            throw new LawDataException(path, [$"Invalid JSON: {ex.Message}"]);
        }

        if (code is null)
        {
            throw new LawDataException(path, ["File is empty."]);
        }

        var errors = Validate(code);
        if (Path.GetFileNameWithoutExtension(path) != code.Id)
        {
            errors.Add($"File name must be the code id ('{code.Id}.json').");
        }

        return errors.Count > 0 ? throw new LawDataException(path, errors) : code;
    }

    public static List<string> Validate(LawCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(code.Id) || code.Id.Any(c => !(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-')))
        {
            errors.Add($"Code id '{code.Id}' must be lower-case letters, digits and dashes.");
        }

        if (string.IsNullOrWhiteSpace(code.Reference) || string.IsNullOrWhiteSpace(code.CitationName))
        {
            errors.Add("A code needs its official reference and citation name.");
        }

        if (code.Source is null || string.IsNullOrWhiteSpace(code.Source.Url))
        {
            errors.Add("A code needs the source it was imported from.");
        }

        foreach (var group in code.Articles.GroupBy(a => a.Number).Where(g => g.Count() > 1))
        {
            errors.Add($"Article {group.Key} appears {group.Count()} times.");
        }

        foreach (var article in code.Articles)
        {
            if (article.Number != ArticleNumbers.Canonical(article.Number))
            {
                errors.Add($"Article number '{article.Number}' is not canonical ('{ArticleNumbers.Canonical(article.Number)}').");
            }

            if (!article.Repealed && string.IsNullOrWhiteSpace(article.Text))
            {
                errors.Add($"Article {article.Number} has no text and is not marked repealed.");
            }
        }

        return errors;
    }
}

/// <summary>A law-library or claim-catalog file failed validation; every problem found is listed.</summary>
public sealed class LawDataException(string path, IReadOnlyList<string> errors)
    : Exception($"Invalid law data {path}:{Environment.NewLine}  " + string.Join(Environment.NewLine + "  ", errors))
{
    public string Path { get; } = path;

    public IReadOnlyList<string> Errors { get; } = errors;
}
