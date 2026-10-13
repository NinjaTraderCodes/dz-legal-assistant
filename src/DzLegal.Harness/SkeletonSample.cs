using System.Diagnostics;
using System.Globalization;
using System.Text;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using DzLegal.Drafting;
using DzLegal.FormEngine;

namespace DzLegal.Harness;

/// <summary>
/// Writes, per model and case, the grounds as drafted today (A: the model writes everything, the checks clean up) beside
/// skeleton-first grounds (B: the code states each article from its official text, the model writes only the
/// argument), for a lawyer to compare before the app changes.
/// </summary>
public static class SkeletonSample
{
    public static async Task RunAsync(BenchOptions options, TextWriter log)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(log);
        var ci = CultureInfo.InvariantCulture;
        var definition = DefinitionLoader.Load(Path.Combine(options.DataRoot, "requete-introductive"));
        var cases = DraftingCaseLoader.LoadAll(options.CasesFolder).Where(c => options.Case is null || c.Id == options.Case).ToList();
        var knowledge = options.LawRoot is null ? DzLegal.Law.LegalKnowledge.Empty : DzLegal.Law.LegalKnowledgeLoader.Load(options.LawRoot);
        Directory.CreateDirectory(options.OutFolder);
        var summary = new StringBuilder()
            .AppendLine("# Grounds: today (A) and skeleton-first (B)").AppendLine()
            .AppendLine(ci, $"{cases.Count} cases, run {DateTime.Now:yyyy-MM-dd HH:mm}. In B the code writes every «المادة … تنص على أن «…»» from the official text; the model writes only the arguments.").AppendLine()
            .AppendLine("| model | case | A s | A wrong law | A removed | B s | B arguments kept | B wrong law | C s | C paragraphs kept | C wrong law | error |")
            .AppendLine("|---|---|---:|---:|---:|---:|---|---:|---:|---|---:|---|");
        foreach (var model in options.Models)
        {
            if (HarnessModels.IsHosted(model))
            {
                log.WriteLine($"[{model}] hosted engine: the (fictitious) test cases are sent to that service. About {cases.Count * 2}-{cases.Count * 3} calls.");
            }

            var folder = Path.Combine(options.OutFolder, string.Concat(model.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_')));
            Directory.CreateDirectory(folder);
            foreach (var drafting in cases)
            {
                log.WriteLine($"[{model}] {drafting.Id} …");
                var inputs = CaseInputs.CreateDefaults(definition, DateOnly.FromDateTime(DateTime.Now));
                foreach (var (k, v) in drafting.Inputs)
                {
                    inputs[k] = v;
                }

                var notes = inputs.GetValueOrDefault("notes.grounds", string.Empty);
                var legalTexts = DraftBench.LegalTextsCited(knowledge, notes);
                var provider = new PacedModelProvider(
                    HarnessModels.Create(model, options.Endpoint, options.Timeout, new SpeedLog(), HarnessModels.Waiting(options.RateLimitWait, log)),
                    HarnessModels.Pace(model, options.DelaySeconds));
                DraftSection? a = null;
                SkeletonDraft? b = null;
                SkeletonDraft? c = null;
                double aSeconds = 0, bSeconds = 0, cSeconds = 0;
                var errors = new List<string>();
                async Task<T?> Run<T>(string variant, Func<Task<T>> draft, Action<double> took)
                    where T : class
                {
                    var clock = Stopwatch.StartNew();
                    try
                    {
                        return await draft();
                    }
                    catch (Exception ex) when (ex is ModelProviderException or HttpRequestException or IOException or TaskCanceledException)
                    {
                        errors.Add($"{variant}: {ex.Message}");
                        return null;
                    }
                    finally
                    {
                        took(clock.Elapsed.TotalSeconds);
                    }
                }

                // Each version on its own: one that fails (an empty answer, a limit) does not hide the others.
                try
                {
                    a = await Run("A", () => new DraftingService(provider) { ClassicGrounds = true }.DraftSectionAsync(definition, "grounds", inputs, legalTexts: legalTexts), t => aSeconds = t);
                    b = await Run("B", () => new GroundsSkeleton(provider).DraftAsync(definition, definition.FindSection("grounds")! with { ArgumentPrompt = "prompts/grounds-argument.ar.txt" }, inputs, legalTexts), t => bSeconds = t);
                    c = await Run("C", () => new ArguedGrounds(provider).DraftAsync(definition, definition.FindSection("grounds")!, inputs, legalTexts, definition.FindSection("grounds")!.ArgumentPrompt!), t => cSeconds = t);
                }
                finally
                {
                    (provider.Inner as IDisposable)?.Dispose();
                }

                var error = errors.Count == 0 ? null : string.Join(" · ", errors);

                var aIssues = a is null ? [] : NarrativeChecks.Check(a.Text, notes, inputs, definition.GroundedTerms, legalTexts);
                var bIssues = b is null ? [] : NarrativeChecks.Check(b.Text, notes, inputs, definition.GroundedTerms, legalTexts);
                static int WrongLaw(IReadOnlyList<NarrativeIssue> l) => l.Count(i => i.IsWrongArticleContent || i.Kind == NarrativeIssueKind.UncitedArticle);
                var cIssues = c is null ? [] : NarrativeChecks.Check(c.Text, notes, inputs, definition.GroundedTerms, legalTexts);
                var cGroups = ArguedGrounds.Plan(notes, legalTexts, inputs.GetValueOrDefault("notes.requests", string.Empty)).Count;
                var kept = b is null ? string.Empty : $"{b.Paragraphs.Count - b.Problems.Count}/{b.Paragraphs.Count}";
                var cKept = c is null ? string.Empty : $"{cGroups - c.Problems.Count}/{cGroups}";
                summary.AppendLine(ci, $"| {model} | {drafting.Id} | {aSeconds:0} | {(a is null ? string.Empty : WrongLaw(aIssues))} | {(a is null ? string.Empty : a.RemovedText.Count)} | {bSeconds:0} | {kept} | {(b is null ? string.Empty : WrongLaw(bIssues))} | {cSeconds:0} | {cKept} | {(c is null ? string.Empty : WrongLaw(cIssues))} | {error} |");

                var file = new StringBuilder()
                    .AppendLine(ci, $"# {drafting.Name}").AppendLine()
                    .AppendLine(ci, $"Model: `{model}`").AppendLine()
                    .AppendLine("## The lawyer's grounds notes").AppendLine().AppendLine("```text").AppendLine(notes).AppendLine("```").AppendLine()
                    .AppendLine(legalTexts.Count == 0 ? "No official text loaded for these articles." : "Official texts used: " + string.Join("، ", legalTexts.Select(t => t.Citation))).AppendLine();
                if (error is not null)
                {
                    file.AppendLine(ci, $"Error: {error}").AppendLine();
                }

                if (a is not null)
                {
                    file.AppendLine(ci, $"## A. Today: the model writes everything ({aSeconds:0} s)").AppendLine()
                        .AppendLine("```text").AppendLine(a.Text).AppendLine("```").AppendLine()
                        .AppendLine("Checks: " + Describe(aIssues) + (a.RemovedText.Count > 0 ? "; removed: " + string.Join(" | ", a.RemovedText) : string.Empty)).AppendLine();
                }

                if (b is not null)
                {
                    file.AppendLine(ci, $"## B. Skeleton-first: the code states the law, the model argues ({bSeconds:0} s)").AppendLine()
                        .AppendLine("```text").AppendLine(b.Text).AppendLine("```").AppendLine()
                        .AppendLine("Checks: " + Describe(bIssues)).AppendLine()
                        .AppendLine(b.Problems.Count == 0 ? "Every argument was kept." : "Arguments left for the lawyer: " + string.Join("; ", b.Problems)).AppendLine();
                    AppendAnswers(file, b);
                }

                if (c is not null)
                {
                    file.AppendLine(ci, $"## C. Argued: articles cited by number, one paragraph per role ({cSeconds:0} s)").AppendLine()
                        .AppendLine("```text").AppendLine(c.Text).AppendLine("```").AppendLine()
                        .AppendLine("Checks: " + Describe(cIssues)).AppendLine()
                        .AppendLine(c.Problems.Count == 0 ? "Every paragraph was kept." : "Paragraphs left for the lawyer: " + string.Join("; ", c.Problems)).AppendLine();
                    AppendAnswers(file, c);
                }

                file.AppendLine("## Read for yourself").AppendLine()
                    .AppendLine("- [ ] Which version would you sign with fewer edits?")
                    .AppendLine("- [ ] In B, does each argument follow naturally from the article quoted before it?")
                    .AppendLine("- [ ] In B, are long article texts acceptable quoted in full, or should they be shortened?")
                    .AppendLine("- [ ] Which of A, B and C reads most like a petition you would file?");
                await File.WriteAllTextAsync(Path.Combine(folder, drafting.Id + ".md"), file.ToString());
            }
        }

        await File.WriteAllTextAsync(Path.Combine(options.OutFolder, "summary.md"), summary.ToString());
        log.WriteLine($"Done: {Path.GetFullPath(Path.Combine(options.OutFolder, "summary.md"))}");
    }

    /// <summary>When a paragraph was left for the lawyer, what the model actually wrote: the answers as received.</summary>
    private static void AppendAnswers(StringBuilder file, SkeletonDraft draft)
    {
        if (draft.Problems.Count == 0 || draft.RawAnswers.Count == 0)
        {
            return;
        }

        for (var i = 0; i < draft.RawAnswers.Count; i++)
        {
            file.AppendLine(i == 0 ? "The model's answer:" : "The model's answer on the retry:").AppendLine()
                .AppendLine("```text").AppendLine(draft.RawAnswers[i].Trim()).AppendLine("```").AppendLine();
        }
    }

    private static string Describe(IReadOnlyList<NarrativeIssue> issues) =>
        issues.Count == 0 ? "none" : string.Join(", ", issues.Select(i => $"{i.Kind} {i.Detail}"));
}
