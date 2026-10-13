using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DzLegal.Domain;
using DzLegal.Law;

namespace DzLegal.Drafting;

/// <summary>
/// One paragraph of argued grounds: the articles that play the same part for the claim (the obligation and its breach,
/// its proof, the compensation), cited together, and the request they support.
/// </summary>
/// <param name="Topic">What the paragraph establishes, shown to the model («قيام الالتزام وإخلال المدعى عليه به»).</param>
/// <param name="Texts">The loaded articles cited in it.</param>
/// <param name="Unloaded">Articles the notes cite whose text is not loaded, cited by number only («المادة 179 من القانون المدني»).</param>
/// <param name="Note">The lawyer's own lines for these articles, or a line with no article (an argument of their own).</param>
/// <param name="Request">The request line it supports, when one is clear.</param>
public sealed partial record ArguedGroup(string Topic, IReadOnlyList<LegalText> Texts, IReadOnlyList<string> Unloaded, string Note, string? Request)
{
    private static readonly string[] SentenceSubjects = ["العقد ", "البيع ", "العقود ", "عقود ", "كل ", "على ", "ما ورد", "الإيجار ", "القرض "];
    private static readonly string[] ClauseOpeners = ["لا ", "إذا ", "عند ", "بعد ", "في "];

    /// <summary>«المادتين 106 و107 من القانون المدني», «المادة 323 من القانون المدني والمادة 15 من قانون …».</summary>
    public string Citation => string.Join(" و", Cite(Texts).Concat(Unloaded));

    /// <summary>
    /// The sentence the code writes to open the paragraph, from the catalog's one-line rules, so the model never states
    /// the law: «المادتين 106 و107 من القانون المدني تقضيان بأن العقد شريعة المتعاقدين، وبأنه يجب تنفيذ العقد…».
    /// Articles with no rule, and articles whose text is not loaded, are cited by number only. Empty for a paragraph
    /// that cites no article (a line of the lawyer's own argument).
    /// </summary>
    public string RuleSentence
    {
        get
        {
            var withRule = Texts.Where(t => !string.IsNullOrWhiteSpace(t.Gist)).ToList();
            var byNumber = Texts.Where(t => string.IsNullOrWhiteSpace(t.Gist)).Select(t => $"المادة {t.Number} من {t.CitationName}").Concat(Unloaded).ToList();
            if (withRule.Count == 0)
            {
                return byNumber.Count > 0 ? "الدعوى تستند إلى " + string.Join(" و", byNumber) : string.Empty;
            }

            var verb = withRule.Count == 2 ? "تقضيان" : "تقضي";
            var rules = withRule.Select(t => RuleClause(t.Gist!)).Distinct().ToList();
            var sentence = $"{string.Join(" و", Cite(withRule))} {verb} {string.Join("، و", rules)}";
            return byNumber.Count > 0 ? sentence + "، وذلك طبقا لل" + string.Join(" و", byNumber)[2..] : sentence;
        }
    }

    /// <summary>
    /// A one-line rule after «تقضي»: a sentence («العقد شريعة المتعاقدين») takes «بأن», a clause opening with a verb or
    /// a condition («يجب تنفيذ…», «لا يستحق…») takes «بأنه», and a noun phrase («تعويض الدائن عن…») takes «بـ».
    /// «تعريف قرض الاستهلاك: يلتزم المقترض…» keeps the rule after the colon; parentheses are dropped.
    /// </summary>
    public static string RuleClause(string gist)
    {
        ArgumentNullException.ThrowIfNull(gist);
        var text = Parenthesis().Replace(gist, string.Empty).Trim().TrimEnd('.', '،');
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        if (colon > 0)
        {
            var head = text[..colon].Trim();
            var tail = text[(colon + 1)..].Trim();
            if (IsClause(tail) || SentenceSubjects.Any(s => tail.StartsWith(s, StringComparison.Ordinal)) || head.StartsWith("بديلا", StringComparison.Ordinal))
            {
                text = tail;
            }
            else if (head.StartsWith("تعريف ", StringComparison.Ordinal))
            {
                return $"بأن {head["تعريف ".Length..]} هو {tail}";
            }
            else if (head.StartsWith("كيف يتم ", StringComparison.Ordinal))
            {
                return $"بأن {head["كيف يتم ".Length..]} يتم ب{tail}";
            }
            else
            {
                return $"ب{head}، أي {tail}";
            }
        }

        return IsClause(text) ? "بأنه " + text
            : SentenceSubjects.Any(s => text.StartsWith(s, StringComparison.Ordinal)) ? "بأن " + text
            : "ب" + text;
    }

    private static bool IsClause(string text) =>
        ClauseOpeners.Any(o => text.StartsWith(o, StringComparison.Ordinal)) || (text.StartsWith('ي') && text.Split(' ')[0].Length > 2);

    private static IEnumerable<string> Cite(IEnumerable<LegalText> texts) =>
        texts.GroupBy(t => t.CitationName)
            .Select(g => (g.Count() switch { 1 => "المادة ", 2 => "المادتين ", _ => "المواد " }) + string.Join(" و", g.Select(t => t.Number)) + " من " + g.Key);

    [GeneratedRegex(@"\s*\([^)]*\)")]
    private static partial Regex Parenthesis();
}

/// <summary>
/// Argued grounds, as Algerian petitions write them: articles cited by number with their rule in one line, not quoted
/// in full. The code plans the paragraphs from the catalog roles (the obligation and its breach, its proof, the
/// compensation; "limit" articles such as no interest on a private loan are warnings for the lawyer, not grounds) and
/// writes each paragraph's rule sentence from the catalog's one-line rules («المادتين 106 و107 من القانون المدني
/// تقضيان بأن…»), so the law in the draft is never the model's. The model writes only how the facts meet the rule. An
/// application that cites or quotes an article, has a number found nowhere in the notes, or fails the other checks is
/// asked once more, then left as «(...)» for the lawyer.
/// </summary>
public sealed class ArguedGrounds(IModelProvider provider)
{
    /// <summary>Request id for the model call (used by recording providers and logs).</summary>
    public const string RequestId = "grounds-argued";

    private const string Placeholder = "(...)";

    private static readonly (string Topic, string[] Roles)[] Order =
    [
        ("قيام الالتزام وإخلال المدعى عليه به", ["basis"]),
        ("إثبات الالتزام", ["proof"]),
        ("التعويض عن الضرر", ["prerequisite", "damages"]),
    ];

    private readonly IModelProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    private readonly TemplateRenderer _renderer = new();

    /// <summary>
    /// The paragraphs, in a lawyer's order: one per role group that has an article, then the articles with no catalog
    /// role and the unloaded ones the notes cite, then each line of the notes that cites no article. A "limit" or
    /// "procedure" article is left out unless the lawyer's notes cite it.
    /// </summary>
    public static IReadOnlyList<ArguedGroup> Plan(string groundsNotes, IReadOnlyList<LegalText> legalTexts, string requestsNotes)
    {
        ArgumentNullException.ThrowIfNull(legalTexts);
        var lines = (groundsNotes ?? string.Empty).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var requests = (requestsNotes ?? string.Empty).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var cited = lines.Select(l => (Line: l, Refs: References(l))).ToList();
        var own = cited.Where(c => IsOwnLine(c.Line)).ToList();
        bool InNotes(LegalText t) => own.Any(c => c.Refs.Any(r => Matches(t, r)));
        string NotesFor(IEnumerable<LegalText> texts) => string.Join('\n', own
            .Where(c => c.Refs.Any(r => texts.Any(t => Matches(t, r))))
            .Select(c => c.Line));

        var groups = new List<ArguedGroup>();
        var used = new HashSet<LegalText>();
        foreach (var (topic, roles) in Order)
        {
            // Within a paragraph, in the order of the code (179 before 186), as citations are written.
            var texts = legalTexts.Where(t => t.Role is { } role && roles.Contains(role) && used.Add(t))
                .OrderBy(t => int.TryParse(new string(t.Number.TakeWhile(char.IsDigit).ToArray()), out var n) ? n : int.MaxValue)
                .ToList();
            if (texts.Count > 0)
            {
                var request = roles.Contains("basis") ? requests.FirstOrDefault()
                    : roles.Contains("damages") ? requests.FirstOrDefault(r => r.Contains("تعويض", StringComparison.Ordinal))
                    : null;
                groups.Add(new ArguedGroup(topic, texts, [], NotesFor(texts), request));
            }
        }

        var others = legalTexts.Where(t => !used.Contains(t) && (t.Role is null or not ("limit" or "procedure") || InNotes(t))).ToList();
        var unloaded = cited.SelectMany(c => c.Refs)
            .Where(r => !legalTexts.Any(t => Matches(t, r)))
            .Select(r => $"المادة {r.Number} من {r.CodeName ?? "القانون المدني"}")
            .Distinct()
            .ToList();
        if (others.Count > 0 || unloaded.Count > 0)
        {
            groups.Add(new ArguedGroup("سند آخر للدعوى", others, unloaded, NotesFor(others), null));
        }

        foreach (var (line, _) in cited.Where(c => c.Refs.Count == 0))
        {
            groups.Add(new ArguedGroup("حجة أوردها المحامي", [], [], line, null));
        }

        return groups;
    }

    public ModelRequest BuildRequest(DocumentTypeDefinition definition, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<ArguedGroup> groups, string promptPath)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(groups);
        var plan = new StringBuilder();
        for (var i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            plan.AppendLine(CultureInfo.InvariantCulture, $"[{i + 1}] الموضوع: {g.Topic}");
            var rule = g.RuleSentence;
            plan.AppendLine(rule.Length > 0
                ? $"    بداية الفقرة (يكتبها البرنامج): {Opening(i)}{rule}، …"
                : "    بداية الفقرة: لا توجد؛ اكتب الفقرة كاملة من ملاحظة المحامي.");
            if (g.Note.Length > 0)
            {
                plan.AppendLine("    ملاحظة المحامي: " + g.Note.ReplaceLineEndings(" / "));
            }

            if (g.Request is not null)
            {
                plan.AppendLine("    الطلب الذي تؤسسه: " + g.Request);
            }
        }

        var user = _renderer.RenderFile(definition, promptPath, inputs, new Dictionary<string, object?>
        {
            ["plan"] = plan.ToString().TrimEnd(),
            ["paragraph_count"] = groups.Count,
        });
        var system = definition.SystemPrompt is null
            ? string.Empty
            : File.ReadAllText(Path.Combine(definition.SourceDirectory, definition.SystemPrompt)).Trim();
        var notes = string.Join('\n', groups.Select(g => g.Note).Where(n => n.Length > 0));
        return new ModelRequest(RequestId, system, TemplateRenderer.Normalize(user), notes) { MaxTokens = 250 + (200 * groups.Count) };
    }

    public async Task<SkeletonDraft> DraftAsync(DocumentTypeDefinition definition, SectionDefinition section, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<LegalText> legalTexts, string promptPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(inputs);
        var groups = Plan(inputs.GetValueOrDefault(section.SourceField ?? string.Empty, string.Empty), legalTexts, inputs.GetValueOrDefault("notes.requests", string.Empty));
        if (groups.Count == 0)
        {
            return new SkeletonDraft(string.Empty, [], ["no grounds in the notes"]);
        }

        var request = BuildRequest(definition, inputs, groups, promptPath);
        var raw = new List<string> { await AskAsync(definition, request, cancellationToken) };
        var arguments = new Dictionary<int, string>(Parse(raw[0], groups.Count));
        var failed = Enumerable.Range(1, groups.Count).Where(n => Problem(definition, arguments.GetValueOrDefault(n), groups, inputs, legalTexts) is not null).ToList();
        if (failed.Count > 0)
        {
            raw.Add(await AskAsync(definition, request, cancellationToken));
            var second = Parse(raw[^1], groups.Count);
            foreach (var n in failed.Where(n => second.TryGetValue(n, out var retry) && Problem(definition, retry, groups, inputs, legalTexts) is null))
            {
                arguments[n] = second[n];
            }
        }

        return Assemble(definition, groups, arguments, inputs, legalTexts) with { Retried = failed.Count > 0, RawAnswers = raw };
    }

    /// <summary>
    /// Arguments by paragraph number («[1] …»). A small model sometimes drops the numbers: when it wrote exactly one
    /// Arabic line per paragraph, the lines are taken in order.
    /// </summary>
    public static IReadOnlyDictionary<int, string> Parse(string answer, int paragraphs)
    {
        var numbered = GroundsSkeleton.ParseArguments(answer);
        if (numbered.Count > 0)
        {
            return numbered;
        }

        var lines = (answer ?? string.Empty).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && l.Any(ArabicScript.IsArabicLetter)).ToList();
        return lines.Count == paragraphs ? lines.Select((l, k) => (k, l)).ToDictionary(x => x.k + 1, x => x.l) : new Dictionary<int, string>();
    }

    /// <summary>
    /// Joins the paragraphs: «حيث إن» or «وحيث إن», the rule sentence the code wrote, then the model's application of
    /// it to the facts. An application that fails the checks is left as «(...)» after the rule, and kept aside in
    /// <see cref="SkeletonDraft.Rejected"/> for the lawyer to see.
    /// </summary>
    public static SkeletonDraft Assemble(DocumentTypeDefinition? definition, IReadOnlyList<ArguedGroup> groups, IReadOnlyDictionary<int, string> arguments, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<LegalText> legalTexts)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(arguments);
        var problems = new List<string>();
        var rejected = new List<string>();
        var lines = new List<string>();
        for (var i = 0; i < groups.Count; i++)
        {
            var argument = arguments.GetValueOrDefault(i + 1);
            if (Problem(definition, argument, groups, inputs, legalTexts) is { } problem)
            {
                problems.Add($"[{i + 1}] {groups[i].Topic}: {problem}, left as {Placeholder}");
                if (argument is not null)
                {
                    rejected.Add(argument);
                }

                argument = "وحيث إن " + Placeholder;
            }

            var rule = groups[i].RuleSentence;
            if (rule.Length == 0)
            {
                lines.Add(GroundsSkeleton.Reopen(argument!, i));
                continue;
            }

            var application = argument!.Trim().TrimStart('،', ',', '-', ' ').TrimEnd();
            lines.Add(Opening(i) + rule + "، " + application + (application.EndsWith('.') ? string.Empty : "."));
        }

        return new SkeletonDraft(string.Join('\n', lines), [], problems) { Rejected = rejected };
    }

    private static string Opening(int index) => index == 0 ? "حيث إن " : "وحيث إن ";

    /// <summary>Why an application cannot be used, or null.</summary>
    private static string? Problem(DocumentTypeDefinition? definition, string? argument, IReadOnlyList<ArguedGroup> groups, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<LegalText> legalTexts)
    {
        if (argument is null)
        {
            return "missing";
        }

        // The code wrote the rule and its citations: the application cites and quotes nothing.
        if (References(argument).FirstOrDefault() is { Number: { } cited })
        {
            return $"cites article {cited}";
        }

        if (argument.IndexOfAny(['«', '»', '"', '“', '”']) >= 0)
        {
            return "quotes a text";
        }

        var sources = string.Join('\n', groups.Select(g => g.Note)
            .Append(inputs.GetValueOrDefault("notes.facts", string.Empty))
            .Append(inputs.GetValueOrDefault("notes.requests", string.Empty))
            .Append(inputs.GetValueOrDefault("documents.list", string.Empty)));
        return NarrativeChecks.Check(argument, sources, inputs, definition?.GroundedTerms, legalTexts)
            .FirstOrDefault(x => x.IsHard || x.IsWrongArticleContent || x.Kind is NarrativeIssueKind.UnknownNumber or NarrativeIssueKind.UncitedArticle) is { } issue
            ? $"{issue.Kind} {issue.Detail}"
            : null;
    }

    /// <summary>
    /// «- المادة 450 من القانون المدني» lines are the ticked articles the app lists before the lawyer's own notes;
    /// every other line is the lawyer's.
    /// </summary>
    public static bool IsOwnLine(string line) => !(line ?? string.Empty).TrimStart().StartsWith("- المادة", StringComparison.Ordinal);

    /// <summary>The articles the lawyer's own lines of the notes cite: number, and the code's name when written.</summary>
    public static IReadOnlyList<(string Number, string? CodeName)> CitedInOwnLines(string notes) =>
        (notes ?? string.Empty).Split('\n').Where(IsOwnLine).SelectMany(References).Distinct().ToList();

    /// <summary>
    /// A ticked "limit" or "procedure" article that the lawyer's own lines don't cite: <see cref="Plan"/> leaves it out
    /// of the grounds on purpose.
    /// </summary>
    public static bool LeftOut(LegalText text, string notes) =>
        text.Role is "limit" or "procedure" && !CitedInOwnLines(notes).Any(r => Matches(text, r));

    private static List<(string Number, string? CodeName)> References(string text) =>
        NarrativeChecks.FullArticleReferences(NarrativeChecks.CanonicalForReferences(text))
            .Select(r => (r.Number, CodeName: r.Code is null ? null : NarrativeChecks.CodeName(r.Code)))
            .Concat(NarrativeChecks.FrenchArticleReferences(text).Select(r => (r.Number, CodeName: (string?)r.CodeName)))
            .Distinct()
            .ToList();

    private static bool Matches(LegalText text, (string Number, string? CodeName) reference) =>
        ArticleNumbers.Canonical(text.Number) == reference.Number
        && (reference.CodeName is null || ArabicScript.Normalize(text.CitationName).Contains(ArabicScript.Normalize(reference.CodeName), StringComparison.Ordinal));

    private async Task<string> AskAsync(DocumentTypeDefinition definition, ModelRequest request, CancellationToken cancellationToken)
    {
        var answer = new StringBuilder();
        await foreach (var piece in _provider.StreamAsync(request, cancellationToken))
        {
            answer.Append(piece);
        }

        return DraftingService.ApplyCorrections(definition, answer.ToString());
    }
}
