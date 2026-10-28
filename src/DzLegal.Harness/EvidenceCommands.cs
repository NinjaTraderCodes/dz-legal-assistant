using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using DzLegal.Drafting;
using DzLegal.Drafting.Evidence;
using DzLegal.Domain;
using DzLegal.Domain.Serialization;
using DzLegal.FormEngine;
using DzLegal.ModelProviders.Stub;
using DzLegal.Presentation;

namespace DzLegal.Harness;

public sealed record EvidenceCaseInput(string CaseId, long Revision, Dictionary<string, string> Inputs);

public sealed record EvidenceAuditCase(string Name, string Partition, string Reader, double? Seconds, IReadOnlyList<FactError> Errors, int RejectedLines, int DatesCorrect, int DatesTotal, int PolaritiesCorrect, int PolaritiesTotal);

public static class EvidenceCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter log, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(log);
        string Option(string key, string fallback = "") => Array.IndexOf(args, key) is var i && i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), EvidenceJson.Options) ?? throw new InvalidDataException("Empty JSON input.");
        void Write(string folder, string name, object value) => File.WriteAllText(Path.Combine(folder, name), JsonSerializer.Serialize(value, EvidenceJson.Options));
        try
        {
            var command = args.Length > 1 ? args[1] : "help";
            var output = Option("--out");
            if (command == "help")
            {
                log.WriteLine("evidence snapshot|check|evaluate --input <case.json> [--predictions <evidence.json>] [--findings <findings.json>] [--rules <rules.json>] --out <fresh-folder>");
                log.WriteLine("evidence audit --run <legacy-run-folder> | --predictions <predictions.json> [--cases <fact-cases>] --out <fresh-folder>");
                log.WriteLine("evidence dataset --dataset <reviewed-cases.json> --out <fresh-folder>");
                log.WriteLine("evidence demo --out <fresh-folder>    builds the fictitious lease review bundle, not a model benchmark");
                log.WriteLine("evidence score --input <case.json> --gold <evidence.json> --predictions <evidence.json> [--reference-format] [--exhaustive-gold] --out <fresh-folder>");
                return 0;
            }

            if (command == "dataset")
            {
                EvidenceDataset.ExportTraining(Read<List<EvidenceDatasetCase>>(Option("--dataset")), output);
                log.WriteLine("Reviewed training examples exported; no paid execution or model installation performed.");
                return 0;
            }

            if (command == "demo")
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(output);
                EvidenceDemo.Create(Option("--input", Path.Combine(AppContext.BaseDirectory, "drafting-cases-evidence", "civil-lease.input.json")), Option("--law-root", Path.Combine(AppContext.BaseDirectory, "data", "law")), output);
                return await RunAsync(["evidence", "evaluate", "--input", Path.Combine(output, "input.json"), "--predictions", Path.Combine(output, "evidence.json"), "--rules", Path.Combine(output, "rules.for-review.json"), "--out", Path.Combine(output, "evaluation")], log, cancellationToken);
            }

            if (command is not ("snapshot" or "check" or "evaluate" or "audit" or "score")) { throw new ArgumentException("Unknown evidence command."); }
            ArgumentException.ThrowIfNullOrWhiteSpace(output);
            if (Directory.Exists(output)) { throw new IOException("Choose a fresh output folder; existing artifacts are preserved."); }
            if (command == "audit")
            {
                var casesPath = Option("--cases", Path.Combine(AppContext.BaseDirectory, "fact-cases"));
                var cases = FactsEvaluator.LoadCases(casesPath);
                var audits = new List<EvidenceAuditCase>();
                var predictionPath = Option("--predictions");
                if (predictionPath.Length > 0)
                {
                    var report = FactsEvaluator.ScorePredictions(cases, Option("--reader", "external"), predictionPath);
                    using var raw = JsonDocument.Parse(File.ReadAllText(predictionPath));
                    foreach (var result in report.Results)
                    {
                        var additional = new List<FactError>();
                        if (result.Failure is not null) { additional.Add(new("execution or missing prediction", result.Failure, true)); }
                        if (raw.RootElement.TryGetProperty(result.Case.Name, out var p) && !p.TryGetProperty("answer", out _) && p.TryGetProperty("events", out var events))
                        {
                            foreach (var ev in events.EnumerateArray().Where(e => !e.TryGetProperty("polarity", out var polarity) || polarity.ValueKind != JsonValueKind.String)) { additional.Add(new("missing explicit polarity", ev.ToString(), true)); }
                        }

                        audits.Add(Audit(result.Case, report.Reader, result.Facts, result.Elapsed.TotalSeconds, additional));
                    }
                }
                else
                {
                    var run = Option("--run");
                    var knownNames = cases.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
                    var files = Directory.GetFiles(run, "*.md", SearchOption.AllDirectories).Where(f => knownNames.Contains(Path.GetFileNameWithoutExtension(f))).ToArray();
                    if (files.Length == 0) { throw new InvalidDataException("No case records found in the run folder."); }
                    foreach (var group in files.GroupBy(f => Path.GetDirectoryName(f)!, StringComparer.Ordinal))
                    {
                        foreach (var c in cases)
                        {
                            var file = group.SingleOrDefault(f => Path.GetFileNameWithoutExtension(f) == c.Name);
                            var answer = file is null ? string.Empty : LegacyAnswer(File.ReadAllText(file));
                            var facts = FactReader.Parse(answer, CaseText.From(c.Inputs));
                            audits.Add(Audit(c, Path.GetFileName(group.Key), facts, null, file is null ? [new("missing case record", c.Name, true)] : []));
                        }
                    }
                }

                Directory.CreateDirectory(output);
                Write(output, "audit.json", new { version = StrictFactsEvaluator.Version, cases = audits, timingNote = predictionPath.Length == 0 ? "Legacy Markdown timings are rounded; replay has no fresh latency measurement." : "Seconds supplied by the external reader.", goldNote = "Legacy gold is not exhaustive. Unannotated additions are reported separately; no precision certification is implied." });
                var lines = new List<string> { "# Strict fact audit", "", StrictFactsEvaluator.Version, "", "| reader | partition | cases | critical cases | dates correct / all expected | polarity correct / all expected |", "|---|---|---:|---:|---:|---:|" };
                foreach (var group in audits.GroupBy(a => (a.Reader, a.Partition)))
                {
                    lines.Add(FormattableString.Invariant($"| {group.Key.Reader} | {group.Key.Partition} | {group.Count()} | {group.Count(a => a.Errors.Any(e => e.Critical))} | {group.Sum(a => a.DatesCorrect)}/{group.Sum(a => a.DatesTotal)} | {group.Sum(a => a.PolaritiesCorrect)}/{group.Sum(a => a.PolaritiesTotal)} |"));
                }

                lines.AddRange(["", "Legacy results are preserved. Missing decisive values and unreadable output are critical here. Matching is one-to-one in prediction order, never selected by gold-value agreement. Unannotated additions need further annotation before they can be called false."]);
                var summary = string.Join(Environment.NewLine, lines);
                File.WriteAllText(Path.Combine(output, "summary.md"), summary);
                log.WriteLine(summary);
                RunManifest.Create("evidence audit", audits.Select(a => a.Reader).Distinct().ToArray(), new Dictionary<string, string> { ["cases"] = casesPath, ["source"] = predictionPath.Length > 0 ? predictionPath : Option("--run"), ["evaluatorAssembly"] = typeof(StrictFactsEvaluator).Assembly.Location, ["draftingAssembly"] = typeof(CaseEvidence).Assembly.Location }, new Dictionary<string, string> { ["evaluator"] = StrictFactsEvaluator.Version }).Write(output);
                return 0;
            }

            var totalWatch = System.Diagnostics.Stopwatch.StartNew();
            var input = Read<EvidenceCaseInput>(Option("--input"));
            var snapshot = CaseSnapshot.Create(input.CaseId, input.Revision, input.Inputs);
            if (command == "snapshot")
            {
                Directory.CreateDirectory(output);
                Write(output, "snapshot.json", snapshot);
                Write(output, "schema.json", EvidenceJson.Schema(snapshot));
                var references = new EvidenceReferences(snapshot);
                Write(output, "reference-input.json", references.ModelInput);
                Write(output, "reference-schema.json", references.Schema());
                return 0;
            }

            var predictionJson = File.ReadAllText(Option("--predictions"));
            var evidence = args.Contains("--reference-format") ? new EvidenceReferences(snapshot).Decode(predictionJson) : EvidenceJson.Parse(predictionJson);
            if (command == "score")
            {
                var expected = EvidenceJson.Parse(File.ReadAllText(Option("--gold")));
                var evaluation = EvidenceEvaluator.Score(snapshot, expected, evidence, args.Contains("--exhaustive-gold"));
                Directory.CreateDirectory(output);
                Write(output, "evaluation.json", evaluation);
                log.WriteLine(FormattableString.Invariant($"{EvidenceEvaluator.Version}: {evaluation.Errors.Count(e => e.Critical)} critical differences; complete={evaluation.Complete}; exhaustive gold={evaluation.ExhaustiveGold}. Annotation review and independent qualification are separate gates."));
                return evaluation.HasCritical ? 1 : 0;
            }
            if (command == "check")
            {
                var validation = EvidenceValidator.Check(snapshot, evidence);
                Directory.CreateDirectory(output);
                Write(output, "check.json", validation);
                log.WriteLine(FormattableString.Invariant($"{snapshot.CaseId}: {validation.Issues.Length} structural issues; complete coverage={validation.CompleteCoverage}; semantic meaning remains unassessed."));
                return validation.StructurallyValid ? 0 : 1;
            }
            var findings = Option("--findings") is { Length: > 0 } findingsPath ? Read<ImmutableArray<SemanticFinding>>(findingsPath) : [];
            var rules = Option("--rules") is { Length: > 0 } rulesPath ? Read<ImmutableArray<EvidenceRule>>(rulesPath) : [];
            var identity = new AnalysisIdentity(snapshot.Fingerprint, CaseEvidence.CurrentSchema, Option("--reader", "external-unqualified"), RunManifest.FolderHash(Option("--predictions")),
                Option("--corpus-version", "unqualified") + ":" + RunManifest.FolderHash(Option("--law-root", Path.Combine(AppContext.BaseDirectory, "data", "law"))),
                Option("--rule-version", "unreviewed") + ":" + RunManifest.FolderHash(Option("--rules")) + ":" + RunManifest.FolderHash(typeof(CaseEvidence).Assembly.Location),
                RunManifest.FolderHash(Option("--findings")));
            var pipeline = new EvidencePipeline(new SuppliedReader(evidence), new SuppliedAssessor(findings));
            var resultRun = await pipeline.RunAsync(snapshot, identity, () => identity, rules, cancellationToken);
            Directory.CreateDirectory(output);
            Write(output, "result.json", resultRun);
            var L = new Loc(); // The document remains Arabic even if the app's interface is English.
            File.WriteAllText(Path.Combine(output, "draft.ar.txt"), EvidenceDraftRenderer.Render(snapshot, resultRun.DraftPlan, identity, key => L[key], (key, values) => L.Format(key, values)));
            var definition = DefinitionLoader.Load(Option("--definition", Path.Combine(AppContext.BaseDirectory, "data", "document-types", "requete-introductive")));
            var keep = definition.Sections.Where(s => s.Kind == SectionKind.Narrative).ToDictionary(s => s.Id,
                s => s.Id == "grounds" ? resultRun.DraftPlan.Authorities.IsEmpty ? L["EvidenceGroundsGap"] : string.Join(Environment.NewLine, resultRun.DraftPlan.Authorities.Select(a => a.Quote)) : input.Inputs.GetValueOrDefault(s.SourceField ?? string.Empty) ?? string.Empty);
            var draft = await new DraftingService(new StubModelProvider()).DraftAsync(definition, input.Inputs, new DraftOptions { KeepNarrative = keep }, cancellationToken);
            if (draft.FindSection("facts") is { } factsSection)
            {
                var factPlan = resultRun.DraftPlan with { Clauses = [.. resultRun.DraftPlan.Clauses.Where(c => c.Category == "notes.facts")], Authorities = [] };
                factsSection.Text = EvidenceDraftRenderer.Render(snapshot, factPlan, identity, key => L[key], (key, values) => L.Format(key, values));
            }

            foreach (var section in draft.Sections.Where(s => s.Id is "facts" or "grounds")) { section.AiWritten = true; section.Reviewed = false; }
            Write(output, "petition.json", draft);
            File.WriteAllText(Path.Combine(output, "petition.ar.txt"), string.Join(Environment.NewLine + Environment.NewLine, draft.Sections.Select(s => s.Text)));
            Write(output, "formal-checks.json", ComplianceChecker.Check(definition, draft));
            Write(output, "review-state.json", new { appIntegrationQualified = false, unreviewedSections = draft.Sections.Where(s => s.AiWritten && !s.Reviewed).Select(s => s.Id), containsUnfilledGrounds = resultRun.DraftPlan.Authorities.IsEmpty, note = "Existing app review/export behavior is unchanged; no export authorization is granted by this harness." });
            Write(output, "snapshot.json", snapshot);
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            Write(output, "end-to-end.json", new { scope = "supplied evidence through draft and formal checks; excludes model inference, human review and physical 8 GB qualification", seconds = totalWatch.Elapsed.TotalSeconds, pipelineSeconds = resultRun.Elapsed.TotalSeconds, peakWorkingSetBytes = process.PeakWorkingSet64, pipelinePeakWorkingSetBytes = resultRun.PeakWorkingSetBytes, inputFingerprint = snapshot.Fingerprint, identity });
            log.WriteLine(FormattableString.Invariant($"{snapshot.CaseId}: {resultRun.Evidence.Checked.Issues.Length} structural issues; {resultRun.Decisions.Count(d => d.Qualified)} reviewed rule decisions; {resultRun.Elapsed.TotalSeconds:F3}s; peak process working set {resultRun.PeakWorkingSetBytes} bytes. This harness result is not app qualification."));
            return resultRun.Evidence.Checked.StructurallyValid ? 0 : 1;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or JsonException or InvalidOperationException)
        {
            log.WriteLine("evidence: " + ex.Message);
            return 2;
        }
    }

    private static EvidenceAuditCase Audit(FactCase c, string reader, CaseFacts facts, double? seconds, IReadOnlyList<FactError> additional)
    {
        var dates = StrictFactsEvaluator.Dates(c.Gold, facts);
        var polarities = StrictFactsEvaluator.Polarities(c.Gold, facts);
        return new(c.Name, c.Partition, reader, seconds, [.. StrictFactsEvaluator.Score(c.Gold, facts), .. additional], facts.Rejected.Count, dates.Correct, dates.Total, polarities.Correct, polarities.Total);
    }

    public static string LegacyAnswer(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        var header = markdown.IndexOf("## Answer", StringComparison.Ordinal);
        var start = header < 0 ? -1 : markdown.IndexOf("```", header, StringComparison.Ordinal);
        var end = start < 0 ? -1 : markdown.IndexOf("```", start + 3, StringComparison.Ordinal);
        return start < 0 || end < 0 ? string.Empty : markdown[(start + 3)..end].Trim();
    }

    private sealed class SuppliedReader(CaseEvidence evidence) : IEvidenceReader
    {
        public Task<CaseEvidence> ReadAsync(CaseSnapshot snapshot, CancellationToken cancellationToken) => Task.FromResult(evidence);
    }

    private sealed class SuppliedAssessor(ImmutableArray<SemanticFinding> findings) : IEvidenceAssessor
    {
        public Task<ImmutableArray<SemanticFinding>> AssessAsync(CheckedEvidence evidence, CancellationToken cancellationToken) => Task.FromResult(findings);
    }
}
