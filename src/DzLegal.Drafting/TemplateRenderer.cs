using System.Collections.Concurrent;
using System.Globalization;
using DzLegal.Domain;
using Scriban;
using Scriban.Runtime;

namespace DzLegal.Drafting;

/// <summary>
/// Renders Scriban templates against the case inputs. Field ids such as "plaintiff.lastName" become
/// nested members (<c>{{ plaintiff.lastName }}</c>); blank values are null, boolean fields are booleans,
/// <c>lines</c> splits a multi-line value into its non-blank lines, and
/// <c>gendered g "masculine" "feminine"</c> picks the form matching a gender choice ("male"/"female"),
/// and falls back to the masculine generic, the unmarked form in Arabic legal drafting, while none is chosen.
/// </summary>
public sealed class TemplateRenderer
{
    private readonly ConcurrentDictionary<string, Template> _cache = new(StringComparer.Ordinal);

    public string RenderFile(DocumentTypeDefinition definition, string relativePath, IReadOnlyDictionary<string, string> inputs, IReadOnlyDictionary<string, object?>? extra = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var path = Path.Combine(definition.SourceDirectory, relativePath);
        var template = _cache.GetOrAdd(path, Parse);
        return Render(template, BuildModel(definition, inputs, extra));
    }

    public static string RenderText(DocumentTypeDefinition definition, string templateText, IReadOnlyDictionary<string, string> inputs, IReadOnlyDictionary<string, object?>? extra = null) =>
        Render(ParseText(templateText, "<inline>"), BuildModel(definition, inputs, extra));

    internal static ScriptObject BuildModel(DocumentTypeDefinition definition, IReadOnlyDictionary<string, string> inputs, IReadOnlyDictionary<string, object?>? extra)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inputs);
        var root = new ScriptObject();
        foreach (var field in definition.Fields)
        {
            var raw = inputs.GetValueOrDefault(field.Id);
            object? value = field.Kind == FieldKind.Boolean
                ? string.Equals(raw?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
                : string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
            SetPath(root, field.Id, value);
        }

        if (extra is not null)
        {
            foreach (var (key, value) in extra)
            {
                root[key] = value;
            }
        }

        root.Import("lines", new Func<string?, ScriptArray>(SplitLines));
        root.Import("gendered", new Func<object?, string, string, string>(Gendered));
        return root;
    }

    internal static string Gendered(object? gender, string masculine, string feminine) => (gender as string) switch
    {
        "male" => masculine,
        "female" => feminine,
        _ => masculine,
    };

    private static ScriptArray SplitLines(string? value)
    {
        var array = new ScriptArray();
        if (value is null)
        {
            return array;
        }

        foreach (var line in value.Split('\n'))
        {
            var trimmed = line.Trim().TrimStart('-', '•', '*').Trim();
            if (trimmed.Length > 0)
            {
                array.Add(trimmed);
            }
        }

        return array;
    }

    private static void SetPath(ScriptObject root, string path, object? value)
    {
        var parts = path.Split('.');
        var current = root;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (current[parts[i]] is not ScriptObject child)
            {
                child = new ScriptObject();
                current[parts[i]] = child;
            }

            current = child;
        }

        current[parts[^1]] = value;
    }

    private static string Render(Template template, ScriptObject model)
    {
        var context = new TemplateContext { StrictVariables = false, EnableRelaxedMemberAccess = true };
        context.PushCulture(CultureInfo.InvariantCulture);
        context.PushGlobal(model);
        return Normalize(template.Render(context));
    }

    private static Template Parse(string path) => ParseText(File.ReadAllText(path), path);

    private static Template ParseText(string text, string sourcePath)
    {
        var template = Template.Parse(text, sourcePath);
        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                $"Template error in {sourcePath}: {string.Join("; ", template.Messages.Select(m => m.ToString()))}");
        }

        return template;
    }

    /// <summary>Trims lines, drops leading/trailing blank lines and collapses runs of blank lines.</summary>
    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(l => l.Trim());
        var result = new List<string>();
        foreach (var line in lines)
        {
            if (line.Length == 0 && (result.Count == 0 || result[^1].Length == 0))
            {
                continue;
            }

            result.Add(line);
        }

        while (result.Count > 0 && result[^1].Length == 0)
        {
            result.RemoveAt(result.Count - 1);
        }

        return string.Join('\n', result);
    }
}
