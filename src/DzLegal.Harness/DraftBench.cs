using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.FormEngine;
using DzLegal.ModelProviders.Ollama;
using DzLegal.ModelProviders.Stub;

namespace DzLegal.Harness;

/// <summary>A drafting test case: fictitious case inputs only. May <c>extend</c> another case file.</summary>
public sealed record DraftingCase(string Id, string Name, Dictionary<string, string> Inputs);

public static class DraftingCaseLoader
{
    public static IReadOnlyList<DraftingCase> LoadAll(string folder) =>
        Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal).Select(Load).ToList();

    public static DraftingCase Load(string path)
    {
        var node = Read(path);
        var inputs = new Dictionary<string, string>(StringComparer.Ordinal);
        if (node["extends"]?.GetValue<string>() is { } parent)
        {
            foreach (var (k, v) in Load(Path.Combine(Path.GetDirectoryName(path)!, parent)).Inputs)
            {
                inputs[k] = v;
            }
        }

        foreach (var (k, v) in node["inputs"]?.AsObject() ?? [])
        {
            inputs[k] = v?.GetValue<string>() ?? string.Empty;
        }

        return new DraftingCase(Path.GetFileNameWithoutExtension(path), node["name"]?.GetValue<string>() ?? path, inputs);
    }

    private static JsonObject Read(string path) =>
        JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })?.AsObject()
        ?? throw new InvalidDataException($"Empty drafting case: {path}");
}

public sealed record BenchOptions
{
    public required IReadOnlyList<string> Models { get; init; }
    public required string CasesFolder { get; init; }
    public required string OutFolder { get; init; }
    public required string DataRoot { get; init; }
    public Uri Endpoint { get; init; } = new("http://127.0.0.1:11434");
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Runs every drafting case through every model and writes, per model and case, the full document plus a
/// review table (time per section, retries/fallbacks, number and script issues), and a summary.md.
/// Meaning-level problems (swapped roles, invented reasoning) still need a human read of the outputs.
/// </summary>
public static class DraftBench
{
    public static async Task RunAsync(BenchOptions options, TextWriter log)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(log);
        var definition = DefinitionLoader.Load(Path.Combine(options.DataRoot, "requete-introductive"));
        var cases = DraftingCaseLoader.LoadAll(options.CasesFolder);
        Directory.CreateDirectory(options.OutFolder);
        var ci = CultureInfo.InvariantCulture;
        var summary = new StringBuilder()
            .AppendLine("# Drafting bench").AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Rules v{definition.Version}, {cases.Count} cases, run {DateTime.Now:yyyy-MM-dd HH:mm}.").AppendLine()
            .AppendLine("Automatic columns cannot see swapped roles or invented reasoning: read each output too.").AppendLine()
            .AppendLine("| model | case | total s | slowest section s | hard issues | number issues | stray Latin words | ungrounded terms | example copies removed | retried | fell back | formally complete | error |")
            .AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");

        var speed = new StringBuilder();
        foreach (var model in options.Models)
        {
            var speedLog = new SpeedLog();
            var modelFolder = Path.Combine(options.OutFolder, Safe(model));
            Directory.CreateDirectory(modelFolder);
            foreach (var drafting in cases)
            {
                log.WriteLine($"[{model}] {drafting.Id} …");
                var inputs = CaseInputs.CreateDefaults(definition, DateOnly.FromDateTime(DateTime.Now));
                foreach (var (k, v) in drafting.Inputs)
                {
                    inputs[k] = v;
                }

                var timed = new TimingProvider(CreateProvider(model, options, speedLog));
                var clock = Stopwatch.StartNew();
                DraftDocument? draft = null;
                string? error = null;
                try
                {
                    draft = await new DraftingService(timed).DraftAsync(definition, inputs);
                }
                catch (ModelProviderException ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    (timed.Inner as IDisposable)?.Dispose();
                }

                clock.Stop();
                if (draft is null)
                {
                    summary.AppendLine(CultureInfo.InvariantCulture, $"| {model} | {drafting.Id} | {clock.Elapsed.TotalSeconds:0} | | | | | | | | | | {error} |");
                    continue;
                }

                var narrative = definition.Sections.Where(s => s.Kind == SectionKind.Narrative).ToList();
                var issues = narrative.ToDictionary(
                    s => s.Id,
                    s => NarrativeChecks.Check(draft.FindSection(s.Id)?.Text ?? string.Empty, inputs.GetValueOrDefault(s.SourceField!, string.Empty), inputs, definition.GroundedTerms));
                var report = ComplianceChecker.Check(definition, draft);
                var hard = issues.Values.Sum(l => l.Count(i => i.IsHard));
                var numbers = issues.Values.Sum(l => l.Count(i => i.Kind is NarrativeIssueKind.UnknownNumber or NarrativeIssueKind.MissingNumber));
                var retried = draft.Sections.Count(s => s.DraftingNotes.Contains(DraftingNoteCodes.Retried));
                var fellBack = draft.Sections.Count(s => s.DraftingNotes.Contains(DraftingNoteCodes.FellBackToNotes));
                var copied = draft.Sections.Count(s => s.DraftingNotes.Contains(DraftingNoteCodes.RemovedCopiedExample));
                var latin = issues.Values.Sum(l => l.Count(i => i.Kind == NarrativeIssueKind.UnexpectedLatinWord));
                var ungrounded = issues.Values.Sum(l => l.Count(i => i.Kind == NarrativeIssueKind.UngroundedTerm));
                var slowest = timed.Durations.Count == 0 ? 0 : timed.Durations.Values.Max(d => d.TotalSeconds);

                var file = new StringBuilder()
                    .AppendLine(CultureInfo.InvariantCulture, $"# {drafting.Name}").AppendLine()
                    .AppendLine(CultureInfo.InvariantCulture, $"Model: `{model}` · total {clock.Elapsed.TotalSeconds:0} s · formally complete: {(report.IsAdmissibleInForm ? "yes" : "NO")}").AppendLine()
                    .AppendLine("## Document").AppendLine().AppendLine("```text").AppendLine(DocumentText.Compose(definition, draft)).AppendLine("```").AppendLine()
                    .AppendLine("## Narrative sections").AppendLine()
                    .AppendLine("| section | seconds (all attempts) | drafting note | automatic issues |")
                    .AppendLine("|---|---:|---|---|");
                foreach (var s in narrative)
                {
                    var seconds = timed.Durations.TryGetValue(s.Id, out var d) ? d.TotalSeconds.ToString("0", ci) : "-";
                    var list = issues[s.Id].Count == 0 ? "none" : string.Join("; ", issues[s.Id].Select(i => $"{i.Kind}: {i.Detail}"));
                    file.AppendLine(CultureInfo.InvariantCulture, $"| {s.Id} | {seconds} | {(draft.FindSection(s.Id)?.DraftingNotes is { Count: > 0 } n ? string.Join(", ", n) : "-")} | {list} |");
                }

                file.AppendLine().AppendLine("## Read for yourself").AppendLine()
                    .AppendLine("- [ ] Roles kept (who lent, who owes, who claims)?")
                    .AppendLine("- [ ] Nothing invented (facts, contract types, articles, reasoning)?")
                    .AppendLine("- [ ] Formal legal Arabic, correct grammar?")
                    .AppendLine("- [ ] Requests identical in substance and amounts to the notes?");
                await File.WriteAllTextAsync(Path.Combine(modelFolder, drafting.Id + ".md"), file.ToString());

                summary.AppendLine(CultureInfo.InvariantCulture,
                    $"| {model} | {drafting.Id} | {clock.Elapsed.TotalSeconds:0} | {slowest:0} | {hard} | {numbers} | {latin} | {ungrounded} | {copied} | {retried} | {fellBack} | {(report.IsAdmissibleInForm ? "yes" : "NO")} | |");
            }

            speed.AppendLine(CultureInfo.InvariantCulture, $"### {model}").AppendLine().AppendLine(speedLog.Table());
            log.WriteLine($"[{model}] speed:");
            log.WriteLine(speedLog.Table());
        }

        summary.AppendLine().AppendLine("## Where the time goes").AppendLine()
            .AppendLine("Load = reading the model from disk (should be ~0 after the first call). Prompt read = how fast the model reads what we send; write = how fast it writes.").AppendLine()
            .Append(speed);
        await File.WriteAllTextAsync(Path.Combine(options.OutFolder, "summary.md"), summary.ToString());
        log.WriteLine($"Done: {Path.GetFullPath(Path.Combine(options.OutFolder, "summary.md"))}");
    }

    private static IModelProvider CreateProvider(string model, BenchOptions options, SpeedLog speed) => model == "stub"
        ? new StubModelProvider()
        : new OllamaModelProvider(new OllamaOptions
        {
            Endpoint = options.Endpoint,
            Model = model,
            Timeout = options.Timeout,
            // Reasoning models answer far faster with thinking disabled; others reject the flag.
            Think = model.StartsWith("qwen3", StringComparison.OrdinalIgnoreCase) ? false : null,
        })
        { OnTimings = speed.Add };

    private static string Safe(string name) =>
        string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));

    /// <summary>Measures time spent in the model per section, retries included.</summary>
    private sealed class TimingProvider(IModelProvider inner) : IModelProvider
    {
        public IModelProvider Inner { get; } = inner;

        public Dictionary<string, TimeSpan> Durations { get; } = [];

        public string Name => Inner.Name;

        public async Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken)
        {
            var clock = Stopwatch.StartNew();
            try
            {
                return await Inner.GenerateAsync(request, cancellationToken);
            }
            finally
            {
                Durations[request.SectionId] = Durations.GetValueOrDefault(request.SectionId) + clock.Elapsed;
            }
        }
    }
}
