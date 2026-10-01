using System.Text.Json;
using System.Text.RegularExpressions;

namespace DzLegal.Domain.Serialization;

/// <summary>
/// Loads document-type definitions from <c>data/document-types/&lt;id&gt;/definition.json</c>
/// and validates their internal consistency, so a bad data edit fails loudly at load time.
/// </summary>
public static class DefinitionLoader
{
    public const string DefinitionFileName = "definition.json";

    public static IReadOnlyList<DocumentTypeDefinition> LoadAll(string documentTypesRoot)
    {
        if (!Directory.Exists(documentTypesRoot))
        {
            throw new DirectoryNotFoundException($"Document-types folder not found: {documentTypesRoot}");
        }

        return Directory.GetDirectories(documentTypesRoot)
            .Where(d => File.Exists(Path.Combine(d, DefinitionFileName)))
            .Order(StringComparer.Ordinal)
            .Select(Load)
            .ToList();
    }

    public static DocumentTypeDefinition Load(string definitionDirectory)
    {
        var path = Path.Combine(definitionDirectory, DefinitionFileName);
        DocumentTypeDefinition? definition;
        try
        {
            definition = JsonSerializer.Deserialize<DocumentTypeDefinition>(File.ReadAllText(path), DefinitionJson.Options);
        }
        catch (JsonException ex)
        {
            throw new DefinitionValidationException(path, [$"Invalid JSON: {ex.Message}"]);
        }

        if (definition is null)
        {
            throw new DefinitionValidationException(path, ["File is empty."]);
        }

        definition = definition with { SourceDirectory = Path.GetFullPath(definitionDirectory) };
        var errors = Validate(definition);
        if (errors.Count > 0)
        {
            throw new DefinitionValidationException(path, errors);
        }

        return definition;
    }

    /// <summary>Returns every consistency problem found; an empty list means the definition is valid.</summary>
    public static List<string> Validate(DocumentTypeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var errors = new List<string>();
        var fieldIds = definition.Fields.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        var sectionIds = definition.Sections.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        var groupIds = definition.FieldGroups.Select(g => g.Id).ToHashSet(StringComparer.Ordinal);

        ReportDuplicates(definition.Fields.Select(f => f.Id), "field", errors);
        ReportDuplicates(definition.Sections.Select(s => s.Id), "section", errors);
        ReportDuplicates(definition.Elements.Select(e => e.Id), "element", errors);
        ReportDuplicates(definition.FieldGroups.Select(g => g.Id), "field group", errors);

        foreach (var field in definition.Fields)
        {
            if (field.Group is not null && !groupIds.Contains(field.Group))
            {
                errors.Add($"Field '{field.Id}' references unknown group '{field.Group}'.");
            }

            CheckConditions(field.VisibleWhen, $"field '{field.Id}'", fieldIds, errors);
            if (field.Kind == FieldKind.Choice)
            {
                if (field.Options.Count < 2)
                {
                    errors.Add($"Choice field '{field.Id}' needs at least two options.");
                }

                if (!string.IsNullOrEmpty(field.Default) && field.Options.All(o => o.Value != field.Default))
                {
                    errors.Add($"Choice field '{field.Id}' has a default that is not one of its options.");
                }
            }
        }

        foreach (var section in definition.Sections)
        {
            var where = $"section '{section.Id}'";
            CheckConditions(section.IncludeWhen, where, fieldIds, errors);
            switch (section.Kind)
            {
                case SectionKind.Generated:
                    CheckFile(definition, section.Template, $"{where} template", errors);
                    break;
                case SectionKind.Narrative:
                    CheckFile(definition, section.Prompt, $"{where} prompt", errors);
                    if (section.SourceField is null || !fieldIds.Contains(section.SourceField))
                    {
                        errors.Add($"{where} must name an existing sourceField.");
                    }

                    break;
            }
        }

        if (definition.Sections.Any(s => s.Kind == SectionKind.Narrative))
        {
            CheckFile(definition, definition.SystemPrompt, "systemPrompt", errors);
        }

        foreach (var element in definition.Elements)
        {
            var where = $"element '{element.Id}'";
            CheckConditions(element.AppliesWhen, where, fieldIds, errors);
            CheckRule(element.Check, where, fieldIds, sectionIds, errors);
        }

        return errors;
    }

    private static void CheckRule(RuleCheck check, string where, HashSet<string> fields, HashSet<string> sections, List<string> errors)
    {
        switch (check)
        {
            case FieldsPresentCheck c:
                if (c.Fields.Count == 0)
                {
                    errors.Add($"{where}: fieldsPresent needs at least one field.");
                }

                errors.AddRange(c.Fields.Where(f => !fields.Contains(f)).Select(f => $"{where}: unknown field '{f}'."));
                break;
            case SectionNotEmptyCheck c:
                RequireSection(c.Section, where, sections, errors);
                break;
            case SectionMatchesCheck c:
                RequireSection(c.Section, where, sections, errors);
                try
                {
                    _ = new Regex(c.Pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                }
                catch (ArgumentException ex)
                {
                    errors.Add($"{where}: invalid pattern: {ex.Message}");
                }

                break;
            case SectionOrderCheck c:
                foreach (var s in c.Sections)
                {
                    RequireSection(s, where, sections, errors);
                }

                break;
            case PredominantlyArabicCheck c:
                foreach (var s in c.Sections)
                {
                    RequireSection(s, where, sections, errors);
                }

                if (c.MinRatio is <= 0 or > 1)
                {
                    errors.Add($"{where}: minRatio must be in (0, 1].");
                }

                break;
            case AllOfCheck c:
                foreach (var inner in c.Checks)
                {
                    CheckRule(inner, where, fields, sections, errors);
                }

                break;
            case AnyOfCheck c:
                foreach (var inner in c.Checks)
                {
                    CheckRule(inner, where, fields, sections, errors);
                }

                break;
            default:
                errors.Add($"{where}: unsupported check {check.GetType().Name}.");
                break;
        }
    }

    private static void RequireSection(string id, string where, HashSet<string> sections, List<string> errors)
    {
        if (!sections.Contains(id))
        {
            errors.Add($"{where}: unknown section '{id}'.");
        }
    }

    private static void CheckConditions(IReadOnlyList<Condition> conditions, string where, HashSet<string> fields, List<string> errors)
    {
        foreach (var c in conditions)
        {
            if (!fields.Contains(c.Field))
            {
                errors.Add($"{where}: condition references unknown field '{c.Field}'.");
            }

            if ((c.Is is null) == (c.IsNot is null))
            {
                errors.Add($"{where}: condition on '{c.Field}' must set exactly one of 'is' / 'isNot'.");
            }
        }
    }

    private static void CheckFile(DocumentTypeDefinition definition, string? relativePath, string what, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            errors.Add($"{what} is not set.");
        }
        else if (!File.Exists(Path.Combine(definition.SourceDirectory, relativePath)))
        {
            errors.Add($"{what} file not found: {relativePath}");
        }
    }

    private static void ReportDuplicates(IEnumerable<string> ids, string what, List<string> errors) =>
        errors.AddRange(ids.GroupBy(i => i, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"Duplicate {what} id '{g.Key}'."));
}

public sealed class DefinitionValidationException : Exception
{
    public DefinitionValidationException(string path, IReadOnlyList<string> errors)
        : base($"Invalid document-type definition {path}:{Environment.NewLine}  " + string.Join(Environment.NewLine + "  ", errors))
    {
        Path = path;
        Errors = errors;
    }

    public DefinitionValidationException()
    {
        Path = string.Empty;
        Errors = [];
    }

    public DefinitionValidationException(string message)
        : base(message)
    {
        Path = string.Empty;
        Errors = [message];
    }

    public DefinitionValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Path = string.Empty;
        Errors = [message];
    }

    public string Path { get; }

    public IReadOnlyList<string> Errors { get; }
}
