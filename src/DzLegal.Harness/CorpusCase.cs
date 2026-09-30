using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;

namespace DzLegal.Harness;

/// <summary>
/// One corpus entry: a document plus the elements it is known to be missing.
/// A case may <c>extend</c> another file and apply small changes, so each defect is one readable diff.
/// Every element not listed in <see cref="ExpectedMissing"/> or <see cref="ExpectedNotApplicable"/>
/// is expected to be present.
/// </summary>
public sealed record CorpusCase
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required DraftDocument Draft { get; init; }
    public IReadOnlyList<string> ExpectedMissing { get; init; } = [];
    public IReadOnlyList<string> ExpectedNotApplicable { get; init; } = [];

    public ElementStatus ExpectedStatus(string elementId) =>
        ExpectedMissing.Contains(elementId) ? ElementStatus.Missing
        : ExpectedNotApplicable.Contains(elementId) ? ElementStatus.NotApplicable
        : ElementStatus.Present;
}

public static class CorpusLoader
{
    /// <summary>Loads every <c>*.case.json</c> file under <paramref name="folder"/>, resolving <c>extends</c>.</summary>
    public static IReadOnlyList<CorpusCase> LoadAll(string folder) =>
        Directory.GetFiles(folder, "*.case.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(Load)
            .ToList();

    public static CorpusCase Load(string path)
    {
        var node = Resolve(path, depth: 0);
        return node.Deserialize<CorpusCase>(DefinitionJson.Options)
            ?? throw new InvalidDataException($"Empty corpus case: {path}");
    }

    private static JsonObject Resolve(string path, int depth)
    {
        if (depth > 5)
        {
            throw new InvalidDataException($"'extends' chain too deep at {path}");
        }

        var docOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        var node = JsonNode.Parse(File.ReadAllText(path), documentOptions: docOptions)?.AsObject()
            ?? throw new InvalidDataException($"Empty corpus file: {path}");

        if (node["extends"]?.GetValue<string>() is not { } parentName)
        {
            return node;
        }

        var parent = Resolve(Path.Combine(Path.GetDirectoryName(path)!, parentName), depth + 1);
        var draft = parent["draft"]!.AsObject();

        if (node["setInputs"] is JsonObject setInputs)
        {
            var inputs = draft["inputs"]!.AsObject();
            foreach (var (key, value) in setInputs)
            {
                inputs[key] = value?.DeepClone();
            }
        }

        var sections = draft["sections"]!.AsArray();
        if (node["setSections"] is JsonObject setSections)
        {
            foreach (var (id, text) in setSections)
            {
                var existing = sections.FirstOrDefault(s => s!["id"]!.GetValue<string>() == id);
                if (existing is null)
                {
                    sections.Add(new JsonObject { ["id"] = id, ["text"] = text?.DeepClone() });
                }
                else
                {
                    existing["text"] = text?.DeepClone();
                }
            }
        }

        if (node["removeSections"] is JsonArray remove)
        {
            var ids = remove.Select(r => r!.GetValue<string>()).ToHashSet();
            foreach (var s in sections.Where(s => ids.Contains(s!["id"]!.GetValue<string>())).ToList())
            {
                sections.Remove(s);
            }
        }

        if (node["sectionOrder"] is JsonArray order)
        {
            var ids = order.Select(o => o!.GetValue<string>()).ToList();
            var reordered = sections.OrderBy(s =>
            {
                var i = ids.IndexOf(s!["id"]!.GetValue<string>());
                return i < 0 ? int.MaxValue : i;
            }).Select(s => s!.DeepClone()).ToList();
            sections.Clear();
            foreach (var s in reordered)
            {
                sections.Add(s);
            }
        }

        var result = new JsonObject
        {
            ["name"] = node["name"]?.DeepClone(),
            ["description"] = node["description"]?.DeepClone(),
            ["draft"] = draft.DeepClone(),
            ["expectedMissing"] = node["expectedMissing"]?.DeepClone() ?? new JsonArray(),
            ["expectedNotApplicable"] = node["expectedNotApplicable"]?.DeepClone() ?? parent["expectedNotApplicable"]?.DeepClone() ?? new JsonArray(),
        };
        return result;
    }
}
