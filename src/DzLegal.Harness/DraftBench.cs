using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.FormEngine;

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

    /// <summary>How many times each case is drafted per model (to measure stability).</summary>
    public int Runs { get; init; } = 1;

    /// <summary>Only this case (its file name without .json), or every case.</summary>
    public string? Case { get; init; }

    /// <summary>Seconds to wait before each model call (hosted rate limits); negative: 3 for a hosted engine, 0 for a local one.</summary>
    public double DelaySeconds { get; init; } = -1;

    /// <summary>Hosted engines: how long one call may wait out "too many requests".</summary>
    public TimeSpan RateLimitWait { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>Stop a model's batch after this many failed cases in a row (0: never).</summary>
    public int MaxConsecutiveFailures { get; init; } = 3;

    /// <summary>
    /// The law library: the official texts of the articles each case's grounds notes cite are given to the model, as
    /// the app does for ticked articles, and its quotes are checked against them. Null: no texts.
    /// </summary>
    public string? LawRoot { get; init; }
}

/// <summary>
/// Runs every drafting case through every model and writes, per model and case, the full document plus a
/// review table (time per section, retries/fallbacks, number and script issues), and a summary.md.
/// Meaning-level problems (swapped roles, invented reasoning) still need a human read of the outputs.
/// </summary>
public static partial class DraftBench
{
    public static async Task RunAsync(BenchOptions options, TextWriter log)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(log);
        var definition = DefinitionLoader.Load(Path.Combine(options.DataRoot, "requete-introductive"));
        var cases = DraftingCaseLoader.LoadAll(options.CasesFolder).Where(c => options.Case is null || c.Id == options.Case).ToList();
        var knowledge = options.LawRoot is null ? DzLegal.Law.LegalKnowledge.Empty : DzLegal.Law.LegalKnowledgeLoader.Load(options.LawRoot);
        var runs = Math.Max(1, options.Runs);
        var tally = new List<(string Model, string Case, int WrongLaw, bool Removed, bool FellBack, bool Complete, int LegalPoints, double Seconds, bool Failed)>();
        Directory.CreateDirectory(options.OutFolder);
        var ci = CultureInfo.InvariantCulture;
        var summary = new StringBuilder()
            .AppendLine("# Drafting bench").AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Rules v{definition.Version}, {cases.Count} cases, run {DateTime.Now:yyyy-MM-dd HH:mm}.").AppendLine()
            .AppendLine("Automatic columns cannot see swapped roles or invented reasoning: read each output too.").AppendLine()
            .AppendLine("| model | case | run | total s | slowest section s | hard issues | number issues | stray Latin words | ungrounded terms | wrong law (misquoted or from memory) | wrong-law paragraphs removed | legal points | example copies removed | retried | fell back | formally complete | error |")
            .AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");

        var speed = new StringBuilder();
        foreach (var model in options.Models)
        {
            var speedLog = new SpeedLog();
            if (HarnessModels.IsHosted(model))
            {
                log.WriteLine($"[{model}] hosted engine: the (fictitious) test cases are sent to that service. About {cases.Count * runs * 2}-{cases.Count * runs * 4} calls.");
            }

            var consecutiveFailures = 0;
            var stopped = false;
            for (var run = 1; run <= runs; run++)
            {
                var modelFolder = runs > 1 ? Path.Combine(options.OutFolder, Safe(model), $"run-{run}") : Path.Combine(options.OutFolder, Safe(model));
                Directory.CreateDirectory(modelFolder);
                foreach (var drafting in cases)
                {
                    log.WriteLine($"[{model}] {drafting.Id}{(runs > 1 ? $" (run {run}/{runs})" : string.Empty)} …");
                    var inputs = CaseInputs.CreateDefaults(definition, DateOnly.FromDateTime(DateTime.Now));
                    foreach (var (k, v) in drafting.Inputs)
                    {
                        inputs[k] = v;
                    }

                    var timed = new TimingProvider(new PacedModelProvider(HarnessModels.Create(model, options.Endpoint, options.Timeout, speedLog, HarnessModels.Waiting(options.RateLimitWait, log)), HarnessModels.Pace(model, options.DelaySeconds)));
                    var legalTexts = LegalTextsCited(knowledge, inputs.GetValueOrDefault("notes.grounds", string.Empty));
                    var clock = Stopwatch.StartNew();
                    DraftDocument? draft = null;
                    string? error = null;
                    try
                    {
                        draft = await new DraftingService(timed).DraftAsync(definition, inputs, new DraftOptions { LegalTexts = legalTexts });
                    }
                    catch (Exception ex) when (ex is ModelProviderException or HttpRequestException or IOException or TaskCanceledException)
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
                        summary.AppendLine(CultureInfo.InvariantCulture, $"| {model} | {drafting.Id} | {run} | {clock.Elapsed.TotalSeconds:0} | | | | | | | | | | | | | {error} |");
                        tally.Add((model, drafting.Id, 0, false, false, false, 0, clock.Elapsed.TotalSeconds, true));
                        if (++consecutiveFailures >= options.MaxConsecutiveFailures && options.MaxConsecutiveFailures > 0)
                        {
                            log.WriteLine($"[{model}] stopped: the engine failed {consecutiveFailures} cases in a row (last error: {error})");
                            summary.AppendLine(CultureInfo.InvariantCulture, $"| {model} | stopped early | {run} | | | | | | | | | | | | | | the engine failed {consecutiveFailures} cases in a row |");
                            stopped = true;
                            break;
                        }

                        continue;
                    }

                    consecutiveFailures = 0;

                    var narrative = definition.Sections.Where(s => s.Kind == SectionKind.Narrative).ToList();
                    var issues = narrative.ToDictionary(
                        s => s.Id,
                        s => NarrativeChecks.Check(draft.FindSection(s.Id)?.Text ?? string.Empty, inputs.GetValueOrDefault(s.SourceField!, string.Empty), inputs, definition.GroundedTerms, legalTexts));
                    var report = ComplianceChecker.Check(definition, draft);
                    var hard = issues.Values.Sum(l => l.Count(i => i.IsHard));
                    var numbers = issues.Values.Sum(l => l.Count(i => i.Kind is NarrativeIssueKind.UnknownNumber or NarrativeIssueKind.MissingNumber));
                    var retried = draft.Sections.Count(s => s.DraftingNotes.Contains(DraftingNoteCodes.Retried));
                    var fellBack = draft.Sections.Count(s => s.DraftingNotes.Contains(DraftingNoteCodes.FellBackToNotes));
                    var copied = draft.Sections.Count(s => s.DraftingNotes.Contains(DraftingNoteCodes.RemovedCopiedExample));
                    var latin = issues.Values.Sum(l => l.Count(i => i.Kind == NarrativeIssueKind.UnexpectedLatinWord));
                    var ungrounded = issues.Values.Sum(l => l.Count(i => i.Kind == NarrativeIssueKind.UngroundedTerm));
                    var slowest = timed.Durations.Count == 0 ? 0 : timed.Durations.Values.Max(d => d.TotalSeconds);
                    var wrongLaw = issues.Values.Sum(l => l.Count(i => i.IsWrongArticleContent || i.Kind == NarrativeIssueKind.UncitedArticle));
                    var removedWrongLaw = draft.Sections.Count(s => s.DraftingNotes.Contains(DraftingNoteCodes.RemovedWrongLaw));
                    var drafted = (draft.FindSection("grounds")?.Text ?? string.Empty) + "\n" + (draft.FindSection("requests")?.Text ?? string.Empty);
                    var legalPoints = SubstanceChecks.Check(inputs, [], [], drafted);
                    tally.Add((model, drafting.Id, wrongLaw, removedWrongLaw > 0, fellBack > 0, report.IsAdmissibleInForm, legalPoints.Count, clock.Elapsed.TotalSeconds, false));

                    var file = new StringBuilder()
                        .AppendLine(CultureInfo.InvariantCulture, $"# {drafting.Name}").AppendLine()
                        .AppendLine(CultureInfo.InvariantCulture, $"Model: `{model}` · total {clock.Elapsed.TotalSeconds:0} s · formally complete: {(report.IsAdmissibleInForm ? "yes" : "NO")}").AppendLine()
                        .AppendLine("## Document").AppendLine().AppendLine("```text").AppendLine(DocumentText.Compose(definition, draft)).AppendLine("```").AppendLine()
                        .AppendLine("## Narrative sections").AppendLine()
                        .AppendLine(legalTexts.Count == 0 ? "No official text given to the model." : "Official texts given to the model: " + string.Join("، ", legalTexts.Select(t => t.Citation))).AppendLine()
                        .AppendLine(legalPoints.Count == 0 ? "Legal points: none." : "Legal points: " + string.Join("; ", legalPoints.Select(p => p.Article is { } a ? $"{p.Kind} ({a.Code}:{a.Number})" : p.Kind.ToString()))).AppendLine()
                        .AppendLine("| section | seconds (all attempts) | drafting note | automatic issues |")
                        .AppendLine("|---|---:|---|---|");
                    foreach (var s in narrative)
                    {
                        var seconds = timed.Durations.TryGetValue(s.Id, out var d) ? d.TotalSeconds.ToString("0", ci) : "-";
                        var list = issues[s.Id].Count == 0 ? "none" : string.Join("; ", issues[s.Id].Select(i => $"{i.Kind}: {i.Detail}"));
                        file.AppendLine(CultureInfo.InvariantCulture, $"| {s.Id} | {seconds} | {(draft.FindSection(s.Id)?.DraftingNotes is { Count: > 0 } n ? string.Join(", ", n) : "-")} | {list} |");
                    }

                    var removed = draft.Sections.SelectMany(s => s.RemovedText.Select(t => (s.Id, Text: t))).ToList();
                    if (removed.Count > 0)
                    {
                        file.AppendLine().AppendLine("## Removed paragraphs (wrong law)").AppendLine();
                        foreach (var (id, text) in removed)
                        {
                            file.AppendLine(CultureInfo.InvariantCulture, $"- {id}: {text}");
                        }
                    }

                    file.AppendLine().AppendLine("## Read for yourself").AppendLine()
                        .AppendLine("- [ ] Roles kept (who lent, who owes, who claims)?")
                        .AppendLine("- [ ] Nothing invented (facts, contract types, articles, reasoning)?")
                        .AppendLine("- [ ] Formal legal Arabic, correct grammar?")
                        .AppendLine("- [ ] Requests identical in substance and amounts to the notes?");
                    await File.WriteAllTextAsync(Path.Combine(modelFolder, drafting.Id + ".md"), file.ToString());

                    summary.AppendLine(CultureInfo.InvariantCulture,
                        $"| {model} | {drafting.Id} | {run} | {clock.Elapsed.TotalSeconds:0} | {slowest:0} | {hard} | {numbers} | {latin} | {ungrounded} | {wrongLaw} | {removedWrongLaw} | {legalPoints.Count} | {copied} | {retried} | {fellBack} | {(report.IsAdmissibleInForm ? "yes" : "NO")} | |");
                }

                if (stopped)
                {
                    break;
                }
            }

            speed.AppendLine(CultureInfo.InvariantCulture, $"### {model}").AppendLine().AppendLine(speedLog.Table());
            log.WriteLine($"[{model}] speed:");
            log.WriteLine(speedLog.Table());
        }

        if (runs > 1)
        {
            summary.AppendLine().AppendLine(CultureInfo.InvariantCulture, $"## Stability over {runs} runs").AppendLine()
                .AppendLine("Per model and case: wrong law found in the final text (mean, min-max), and how often a paragraph had to be removed, the draft fell back to the notes, or the run failed.").AppendLine()
                .AppendLine("| model | case | wrong law mean | min-max | runs with removals | fell back | formally complete | legal points mean | failed | mean s |")
                .AppendLine("|---|---|---:|---|---:|---:|---:|---:|---:|---:|");
            foreach (var g in tally.GroupBy(t => (t.Model, t.Case)))
            {
                var ok = g.Where(t => !t.Failed).ToList();
                if (ok.Count == 0)
                    summary.AppendLine(ci, $"| {g.Key.Model} | {g.Key.Case} | | | | | | | {g.Count()} | |");
                else
                    summary.AppendLine(ci, $"| {g.Key.Model} | {g.Key.Case} | {ok.Average(t => t.WrongLaw):0.0} | {ok.Min(t => t.WrongLaw)}-{ok.Max(t => t.WrongLaw)} | {ok.Count(t => t.Removed)}/{ok.Count} | {ok.Count(t => t.FellBack)}/{ok.Count} | {ok.Count(t => t.Complete)}/{ok.Count} | {ok.Average(t => t.LegalPoints):0.0} | {g.Count(t => t.Failed)} | {ok.Average(t => t.Seconds):0} |");
            }
        }

        summary.AppendLine().AppendLine("## Where the time goes").AppendLine()
            .AppendLine("Load = reading the model from disk (should be ~0 after the first call). Prompt read = how fast the model reads what we send; write = how fast it writes.").AppendLine()
            .Append(speed);
        await File.WriteAllTextAsync(Path.Combine(options.OutFolder, "summary.md"), summary.ToString());
        log.WriteLine($"Done: {Path.GetFullPath(Path.Combine(options.OutFolder, "summary.md"))}");
    }

    /// <summary>
    /// The official texts of the articles a case's grounds notes cite (Civil Code unless another code is named), as the
    /// app gives the model the texts of the ticked articles.
    /// </summary>
    public static IReadOnlyList<LegalText> LegalTextsCited(DzLegal.Law.LegalKnowledge knowledge, string groundsNotes)
    {
        var codes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["القانون المدني"] = "civil-code", ["ق م"] = "civil-code", ["ق.م"] = "civil-code",
            ["القانون التجاري"] = "commercial-code", ["ق ت"] = "commercial-code", ["ق.ت"] = "commercial-code",
            ["قانون الإجراءات المدنية والإدارية"] = "cpca",
        };
        var french = NarrativeChecks.FrenchArticleReferences(groundsNotes).Select(r => (Code: codes[r.CodeName], r.Number));
        return NarrativeChecks.FullArticleReferences(groundsNotes)
            .Select(r => (Code: r.Code is { } name && codes.TryGetValue(name, out var id) ? id : "civil-code", r.Number))
            .Concat(french)
            .Distinct()
            .Select(r => (r.Code, Article: knowledge.Library.FindArticle(r.Code, r.Number)))
            .Where(r => r.Article is { Repealed: false })
            .Select(r => new LegalText(r.Article!.Number, knowledge.CitationName(r.Code), r.Article.Text))
            .ToList();
    }

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
