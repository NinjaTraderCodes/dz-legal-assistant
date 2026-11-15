using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DzLegal.CoreLogic.Notes;

/// <summary>
/// The files the notes tool reads and writes, under one repository: <c>docs/research/core-logic</c> (the scheme, the guide,
/// the prompts) and <c>data/law</c> (the claim catalogue and the elements table, read only).
/// </summary>
public sealed class NotesFiles
{
    public NotesFiles(string repository)
    {
        Repository = Path.GetFullPath(repository);
        if (!File.Exists(Path.Combine(CoreLogic, "slots.json")))
        {
            throw new DirectoryNotFoundException($"{Repository} holds no docs/research/core-logic/slots.json.");
        }
    }

    public string Repository { get; }

    public string CoreLogic => Path.Combine(Repository, "docs", "research", "core-logic");

    public string Notes => Path.Combine(CoreLogic, "notes");

    public string Prompts => Path.Combine(CoreLogic, "prompts");

    public string Guide => Path.Combine(Notes, "LABELLING-GUIDE.md");

    public string GuideTest => Path.Combine(Notes, "guide-test.json");

    public string GuideTestResults => Path.Combine(Notes, "GUIDE-TEST-RESULTS.md");

    public string CellsTest => Path.Combine(Notes, "cells-test.json");

    public string Scheme => Path.Combine(Notes, "SCHEME.md");

    public string ClaimElements => Path.Combine(Repository, "data", "law", "claim-elements.json");

    public string Claims => Path.Combine(Repository, "data", "law", "claims");

    /// <summary>The repository given, or the one above the current folder, or the one a published copy names.</summary>
    public static NotesFiles Find(string? repository)
    {
        if (repository is not null)
        {
            return new NotesFiles(repository);
        }

        var pointer = Path.Combine(AppContext.BaseDirectory, "worktree.txt");
        if (File.Exists(pointer))
        {
            return new NotesFiles(File.ReadAllText(pointer).Trim());
        }

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
            {
                if (File.Exists(Path.Combine(folder.FullName, "docs", "research", "core-logic", "slots.json")))
                {
                    return new NotesFiles(folder.FullName);
                }
            }
        }

        throw new DirectoryNotFoundException("No repository found: pass --repo <folder>.");
    }
}

/// <summary>JSON as the notes files write it: Arabic kept readable, camelCase.</summary>
public static class NotesJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static readonly JsonSerializerOptions Compact = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static JsonNode Load(string path) =>
        JsonNode.Parse(File.ReadAllText(path)) ?? throw new InvalidDataException($"{path} is empty.");

    /// <summary>A string as a JSON literal, Arabic unescaped.</summary>
    public static string Literal(string value) => JsonSerializer.Serialize(value, Compact);

    public static void Write(string path, string text) =>
        File.WriteAllText(path, text.ReplaceLineEndings("\n"));
}
