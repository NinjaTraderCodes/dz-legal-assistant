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

    /// <summary>A line of the lawyer's own argument that cites no article: the model writes the whole paragraph.</summary>
    public bool IsLawyersLine => Texts.Count == 0 && Unloaded.Count == 0;

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
/// application that cites (a clause after its facts that names an article is cut first) or quotes an article, has a
/// number found nowhere in the notes, or fails the other checks is
/// asked once more, then left as «(...)» for the lawyer.
/// </summary>
public sealed partial class ArguedGrounds(IModelProvider provider)
{
    /// <summary>Request id for the model call (used by recording providers and logs).</summary>
    public const string RequestId = "grounds-argued";

    private const string Placeholder = "(...)";

    /// <summary>The codes whose articles the grounds may cite by number when their text is not loaded.</summary>
    private static readonly HashSet<string> CoveredCodes = new(StringComparer.Ordinal)
    {
        "القانون المدني", "القانون التجاري", "قانون الأسرة", "قانون الإجراءات المدنية والإدارية",
        NarrativeChecks.LabourRelations, NarrativeChecks.LabourDisputes,
    };

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
        // A request the lawyer wrote in French is not handed to the model: it would copy it into the Arabic grounds.
        var requests = (requestsNotes ?? string.Empty).Split('\n').Select(l => l.Trim())
            .Where(l => l.Length > 0 && ArabicScript.Ratio(l) >= NarrativeChecks.MinArabicRatio).ToList();
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
        // An article the library does not hold is cited by number only, and only from a code this app covers: the
        // code must never write «تستند إلى المادة 1 من الدستور» because the notes named it (hostile case a1).
        var unloaded = cited.SelectMany(c => c.Refs.Select(r => (c.Line, Ref: r)))
            .Where(x => !legalTexts.Any(t => Matches(t, x.Ref)) && (x.Ref.CodeName is null || CoveredCodes.Contains(x.Ref.CodeName)))
            .Select(x => $"المادة {x.Ref.Number} من {x.Ref.CodeName ?? LawNamedInLine(x.Line, x.Ref.Number) ?? "القانون المدني"}")
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

        // No basis article loaded (a Family Code or labour article cited by number only): the first paragraph founds
        // the first request, so the code still restates it in the lawyer's words.
        if (requests.Count > 0 && groups.Count > 0 && groups.All(g => g.Request is null) && (groups[0].Texts.Count > 0 || groups[0].Unloaded.Count > 0))
        {
            groups[0] = groups[0] with { Request = requests[0] };
        }

        return groups;
    }

    /// <summary>
    /// The law a line names after an article the reference reader does not know («المادة 80 من القانون المتعلق بعلاقات
    /// العمل»), as the lawyer wrote it; null when the line names none, so the article is the Civil Code's. Without it
    /// the code wrote «المادة 80 من القانون المدني» (case 07, 16 Oct).
    /// </summary>
    internal static string? LawNamedInLine(string line, string number)
    {
        foreach (Match m in NamedLaw().Matches(line ?? string.Empty))
        {
            if (ArticleNumbers.Canonical(ArabicScript.Normalize(m.Groups["n"].Value)) == number)
            {
                return m.Groups["law"].Value.Trim();
            }
        }

        return null;
    }

    [GeneratedRegex(@"ماد[ةه]\s+(?<n>[\d\u0660-\u0669]+(?:\s+مكرر(?:\s*\d+)?)?)\s+من\s+(?<law>(?:ال)?قانون[^:،,.;؛\n]*)")]
    private static partial Regex NamedLaw();

    public ModelRequest BuildRequest(DocumentTypeDefinition definition, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<ArguedGroup> groups, string promptPath)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(groups);
        var plan = new StringBuilder();
        for (var i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            plan.AppendLine(CultureInfo.InvariantCulture, $"{i + 1}- الموضوع: {g.Topic}");
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
                plan.AppendLine("    الطلب الذي تؤسسه (يضيفه البرنامج في آخر الفقرة): " + g.Request);
            }
        }

        var user = _renderer.RenderFile(definition, promptPath, inputs, new Dictionary<string, object?>
        {
            ["plan"] = plan.ToString().TrimEnd(),
            ["requests_for_model"] = RequestsForModel(inputs.GetValueOrDefault("notes.requests", string.Empty)),
            ["paragraph_count"] = groups.Count,
        });
        var system = definition.SystemPrompt is null
            ? string.Empty
            : File.ReadAllText(Path.Combine(definition.SourceDirectory, definition.SystemPrompt)).Trim();
        var notes = string.Join('\n', groups.Select(g => g.Note).Where(n => n.Length > 0));
        return new ModelRequest(RequestId, system, TemplateRenderer.Normalize(user), notes) { MaxTokens = 400 + (250 * groups.Count) };
    }

    /// <summary>
    /// The request lines the model may read: formal Arabic only. A line in dialect or French is replaced by a mention,
    /// since the model copied «يخلص 240000 دج تاع الكرا…» into its sentence (Gemma 3 4B, case 03, 16 Oct).
    /// </summary>
    internal static string RequestsForModel(string requests)
    {
        var lines = (requests ?? string.Empty).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var formal = lines.Where(l => !NotFormal(l)).ToList();
        return formal.Count == lines.Count ? string.Join('\n', formal)
            : string.Join('\n', formal.Append("(طلبات أخرى كتبت بغير العربية الفصحى، لا تنقلها)"));
    }

    private static bool NotFormal(string line) =>
        ArabicScript.Ratio(line) < NarrativeChecks.MinArabicRatio || SubstanceChecks.IsDialect(line) || NarrativeChecks.LatinWords(line).Any();

    /// <summary>True when the sentence repeats three words in a row of a request line written in dialect or French.</summary>
    internal static bool CopiesInformalRequest(string argument, string requests)
    {
        var said = " " + string.Join(' ', ArabicScript.Tokens(argument).Select(ArabicScript.Normalize)) + " ";
        foreach (var line in (requests ?? string.Empty).Split('\n').Where(l => l.Trim().Length > 0 && NotFormal(l)))
        {
            var words = ArabicScript.Tokens(line).Select(ArabicScript.Normalize).Where(w => !w.Any(char.IsDigit)).ToList();
            for (var k = 0; k + 3 <= words.Count; k++)
            {
                if (said.Contains(" " + string.Join(' ', words.Skip(k).Take(3)) + " ", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
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
        var raw = new List<string> { await AskAsync(definition, request, groups.Count, cancellationToken) };
        var arguments = new Dictionary<int, string>(WithoutOwnCitations(Parse(raw[0], groups.Count), groups));
        var failed = Enumerable.Range(1, groups.Count).Where(n => Problem(definition, arguments.GetValueOrDefault(n), groups, inputs, legalTexts) is not null).ToList();

        // Each paragraph still missing or refused is asked on its own, as one sentence. Asking the whole plan again
        // failed the same way: Gemma 3 4B (local, 18 Oct) wrote 4, then 3, then only 1 of 4 numbered lines, on both tries.
        // The question is added after the same prompt, so Ollama reuses what it already read and only the end is new.
        foreach (var n in failed)
        {
            var answer = await AskOneAsync(definition, request, n, groups[n - 1], cancellationToken);
            raw.Add(answer);
            if (OneParagraph(answer, groups[n - 1]) is { } one)
            {
                var application = Application(one, groups[n - 1]);
                if (Problem(definition, application, groups, inputs, legalTexts) is null || !arguments.ContainsKey(n))
                {
                    arguments[n] = application;
                }
            }
        }

        return Assemble(definition, groups, arguments, inputs, legalTexts) with { Retried = raw.Count > 1, RawAnswers = raw };
    }

    /// <summary>The question for paragraph <paramref name="number"/> alone, after the same prompt.</summary>
    internal static ModelRequest OneParagraphRequest(ModelRequest request, int number, ArguedGroup group)
    {
        var start = group.RuleSentence.Length > 0
            ? $"مكان «…» في الفقرة {number} ({group.Topic}): جملة واحدة تبدأ بـ«وحيث إن» تبين كيف تنطبق القاعدة على الوقائع"
            : $"الفقرة {number}: جملة واحدة تبدأ بـ«وحيث إن» تصوغ ملاحظة المحامي بلغة قانونية من الوقائع: {group.Note.ReplaceLineEndings(" / ")}";
        return request with
        {
            UserPrompt = request.UserPrompt + "\n\nاكتب الآن " + start + ". سطر واحد فقط، دون رقم، ودون ذكر أي مادة أو قانون.",
            MaxTokens = 250,
        };
    }

    /// <summary>The first line of a one-paragraph answer, without a number and without the opening the code writes.</summary>
    internal static string? OneParagraph(string answer, ArguedGroup group)
    {
        var line = (answer ?? string.Empty).Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Any(ArabicScript.IsArabicLetter));
        if (line is null)
        {
            return null;
        }

        line = LeadingNumber().Replace(line, string.Empty).Trim();
        // The model sometimes copies the paragraph's opening, article included: only what follows it is the answer.
        var rule = group.RuleSentence;
        if (rule.Length > 0 && line.IndexOf(rule, StringComparison.Ordinal) is var at and >= 0)
        {
            var rest = line[(at + rule.Length)..].TrimStart('،', ',', ' ');
            line = rest.StartsWith("وحيث", StringComparison.Ordinal) || rest.StartsWith("حيث", StringComparison.Ordinal) ? rest : "وحيث إن " + rest;
        }

        return line.Length > "وحيث إن ".Length ? line : null;
    }

    [GeneratedRegex(@"^[\[(]?[\d\u0660-\u0669]{1,2}[\])\.:\-]\s*")]
    private static partial Regex LeadingNumber();

    /// <summary>One sentence: reading stops at the end of the first line.</summary>
    private async Task<string> AskOneAsync(DocumentTypeDefinition definition, ModelRequest request, int number, ArguedGroup group, CancellationToken cancellationToken)
    {
        var answer = new StringBuilder();
        try
        {
            await foreach (var piece in _provider.StreamAsync(OneParagraphRequest(request, number, group), cancellationToken))
            {
                answer.Append(piece);
                var text = answer.ToString();
                if (text.TrimStart().IndexOf('\n', StringComparison.Ordinal) > 0)
                {
                    break;
                }
            }
        }
        catch (ModelProviderException ex) when (ex.IsCutOff)
        {
            return CutOffMarker;
        }

        return DraftingService.ApplyCorrections(definition, answer.ToString());
    }

    /// <summary>
    /// Arguments by paragraph number («1- …», or «[1] …» as earlier prompts asked). The prompt avoids «[1]»: behind
    /// OpenRouter, DeepInfra's server took an answer starting with «[» for a function call and returned no text. A small model sometimes drops the numbers: when it wrote exactly one
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
        ArgumentNullException.ThrowIfNull(inputs);
        var problems = new List<string>();
        var rejected = new List<string>();
        var unwritten = new List<string>();
        var lines = new List<string>();
        var applications = Enumerable.Range(0, groups.Count)
            .Select(i => arguments.GetValueOrDefault(i + 1) is { } given ? WithRolesForNames(Application(given, groups[i]), inputs) : null)
            .ToList();
        var accepted = applications.Select(a => Problem(definition, a, groups, inputs, legalTexts) is null).ToList();
        var closings = Closings(groups, inputs, accepted);
        for (var i = 0; i < groups.Count; i++)
        {
            var argument = applications[i];
            if (Problem(definition, argument, groups, inputs, legalTexts) is { } problem)
            {
                problems.Add($"[{i + 1}] {groups[i].Topic}: {problem}, left as {Placeholder}");
                if (argument is not null)
                {
                    rejected.Add(argument);
                }
                else if (groups[i].IsLawyersLine)
                {
                    unwritten.Add(groups[i].Note);
                }

                argument = "وحيث إن " + Placeholder;
            }

            var rule = groups[i].RuleSentence;
            if (rule.Length == 0)
            {
                lines.Add(WithClosing(GroundsSkeleton.Reopen(argument!, i), closings[i]));
                continue;
            }

            var application = WithoutRestatedRule(argument!.Trim().TrimStart('،', ',', '-', ' ').TrimEnd(), groups[i]);
            lines.Add(WithClosing(Opening(i) + rule + "، " + application, closings[i]));
        }

        return new SkeletonDraft(string.Join('\n', lines), [], problems) { Rejected = rejected, Unwritten = unwritten };
    }

    private static string Opening(int index) => index == 0 ? "حيث إن " : "وحيث إن ";

    private static string WithClosing(string paragraph, string? closing)
    {
        var text = paragraph.TrimEnd().TrimEnd('.', '،', ',').TrimEnd();
        return closing is null ? text + "." : text + "، " + closing + ".";
    }

    private static Dictionary<int, string> WithoutOwnCitations(IReadOnlyDictionary<int, string> arguments, IReadOnlyList<ArguedGroup> groups) =>
        arguments.ToDictionary(a => a.Key, a => a.Key >= 1 && a.Key <= groups.Count ? Application(a.Value, groups[a.Key - 1]) : a.Value);

    /// <summary>The model's sentence without a reference to the paragraph's own article and without a closing of its own.</summary>
    private static string Application(string given, ArguedGroup group) => WithoutModelClosing(WithoutCitingClauses(WithoutOwnCitation(given, group)));

    /// <summary>
    /// Puts the parties' standing in place of their names («المدعى عليه حمداني سمير» → «المدعى عليه»), as the prompt asks
    /// and Gemma 3 4B does not (case 12, 18 Oct). Petitions name the parties in the heading only; with roles a reversal
    /// such as «وحيث إن بن علي كريم ملتزم بتنفيذ عقد الاعتراف بالدين» reads plainly as «وحيث إن المدعي ملتزم…».
    /// </summary>
    internal static string WithRolesForNames(string application, IReadOnlyDictionary<string, string> inputs)
    {
        var text = application;
        foreach (var (party, role) in new[] { ("plaintiff", "المدعي"), ("defendant", "المدعى عليه") })
        {
            var entity = inputs.GetValueOrDefault(party + ".isLegalEntity", string.Empty) == "true";
            var female = entity || inputs.GetValueOrDefault(party + ".gender", string.Empty) == "female";
            var standing = female ? (party == "plaintiff" ? "المدعية" : "المدعى عليها") : role;
            var last = inputs.GetValueOrDefault(party + ".lastName", string.Empty).Trim();
            var first = inputs.GetValueOrDefault(party + ".firstName", string.Empty).Trim();
            // A full name only: a surname alone may also be a word of the facts.
            string[] names = entity ? [inputs.GetValueOrDefault(party + ".entityName", string.Empty).Trim()]
                : last.Length > 0 && first.Length > 0 ? [$"{last} {first}", $"{first} {last}"]
                : [];
            foreach (var name in names.Where(n => n.Length > 2))
            {
                var pattern = @"(?:(?:ال)?مدع(?:ى\s+عليها|ى\s+عليه|ية|ي)\s+)?(?:\(\s*)?" + Regex.Escape(name).Replace(@"\ ", @"\s+", StringComparison.Ordinal) + @"(?:\s*\))?";
                text = Regex.Replace(text, pattern, standing);
            }
        }

        return text;
    }

    /// <summary>
    /// Drops a clause after the first that brings in an article («، وبموجب المادة 450 من القانون المدني الذي يلزم المقترض
    /// برد نظير ما اقترضه»). The code writes every rule and citation, so such a clause is a repeat or the model's own law,
    /// sometimes wrong: Gemma 3 4B gave art. 106 the loan rule of art. 450 (local, 18 Oct). Two such clauses cost two
    /// paragraphs and a second call of about a minute, although the facts before them were right. An opening clause
    /// that cites is still rejected.
    /// </summary>
    internal static string WithoutCitingClauses(string application)
    {
        var clauses = application.Split('،');
        var kept = clauses.Where((c, k) => k == 0 || !CitingClause().IsMatch(c.Trim())).ToList();
        return kept.Count == clauses.Length ? application : string.Join('،', kept).TrimEnd();
    }

    [GeneratedRegex(@"^و?(?:(?:ب?موجب|بمقتضى|طبقا|وفقا|وفق|عملا|استنادا|حسب|كما\s+(?:تنص|نصت|تقضي|قضت))\s+(?:\S+\s+){0,2}?)?(?:ال|لل|بال)?ماد(?:[ةه]|تين|تان)\s+[\d\u0660-\u0669]")]
    private static partial Regex CitingClause();

    /// <summary>
    /// Cuts a closing the model wrote although the code writes it («، فإن طلب المدعي … يكون مؤسسا قانونا», «وعليه يكون
    /// طلب…»), and a closing filler with nothing after it («، وثبت بذلك»). The opening clause always stays.
    /// </summary>
    internal static string WithoutModelClosing(string application)
    {
        var text = ModelClosing().Replace(application, string.Empty);
        text = FillerEnding().Replace(text, string.Empty).TrimEnd();
        return text.Length >= 10 ? text : application;
    }

    [GeneratedRegex(@"\s*[،,]?\s*(?:(?:و?عليه|و?لذا|و?لذلك|و?بالتالي|و?من\s+ثم|و?بناء\s+على\s+ذلك)\s*[،,]?\s*)?(?:(?:ف|و)?[إا]ن\s+طلب|(?:ف|و)?يكون\s+طلب|مما\s+يجعل\s+طلب|يكون\s+مؤسسا|تكون\s+مؤسسة)[^\n]*$")]
    private static partial Regex ModelClosing();

    [GeneratedRegex(@"\s*[،,]?\s*و?(?:قد\s+)?ثبت\s*(?:ب?ذلك)?\s*\.?\s*$")]
    private static partial Regex FillerEnding();

    private static readonly string[] RequestOpeners =
    [
        "الزام", "حكم", "قضاء", "ابطال", "بطلان", "فسخ", "تصريح", "تثبيت", "طرد", "اخلاء", "تطليق", "اسناد", "منح", "تمكين",
        "ارجاع", "رد", "تعويض", "استرداد", "تسليم", "دفع", "قسمه", "اثبات", "انهاء", "نفقه", "حضانه", "زياره", "اعاده", "ابقاء",
    ];

    /// <summary>
    /// A request line the closing can repeat as written: formal Arabic (no dialect, no French) that reads after «طلب
    /// المدعي», i.e. opens with a verbal noun such as «إلزام», «الحكم», «إبطال», «فسخ». Anything else gets the general closing.
    /// </summary>
    internal static bool IsFormalRequest(string? request)
    {
        if (string.IsNullOrWhiteSpace(request) || ArabicScript.Ratio(request) < NarrativeChecks.MinArabicRatio || SubstanceChecks.IsDialect(request)
            || NarrativeChecks.LatinWords(request).Any())
        {
            return false;
        }

        var first = ArabicScript.Normalize(RequestText(request)).Split(' ')[0];
        first = first.StartsWith("وال", StringComparison.Ordinal) ? first[3..] : first.StartsWith("ال", StringComparison.Ordinal) ? first[2..] : first.StartsWith('و') ? first[1..] : first;
        return RequestOpeners.Any(o => first.StartsWith(o, StringComparison.Ordinal) || ("ال" + first).StartsWith(o, StringComparison.Ordinal));
    }

    /// <summary>The request line without a list marker, a leading «و» or its final stop.</summary>
    private static string RequestText(string request)
    {
        var text = ListMarker().Replace(request.Trim(), string.Empty).Trim().TrimEnd('.', '،', ';', '؛').Trim();
        text = AskingTheCourt().Replace(text, string.Empty);
        return text.StartsWith('و') && text.Length > 1 && !text.StartsWith("وقف", StringComparison.Ordinal) ? text[1..] : text;
    }

    /// <summary>«نلتمس من المحكمة الموقرة …», «يلتمس المدعي من هيئة المحكمة الحكم بـ…»: the frame, not the request.</summary>
    [GeneratedRegex(@"^(?:ن|ي|ت)(?:لتمس|طلب)\S*\s+(?:\S+\s+){0,2}?من\s+(?:هيئة\s+)?(?:ال)?محكمة(?:\s+الموقرة)?\s*[:،,]?\s*")]
    private static partial Regex AskingTheCourt();

    [GeneratedRegex(@"^(?:[-–•*]|\(?\d{1,2}[\)\.\-]|[\u0660-\u0669]{1,2}[\)\.\-])\s*")]
    private static partial Regex ListMarker();

    /// <summary>
    /// The closing the code writes after each application, so the request in the grounds is the lawyer's own line, word
    /// for word (Gemma 3 4B copied dialect, changed amounts and dropped «أو» when it wrote the closing: 13 of 89 closings,
    /// 16 Oct). A formal request line is restated; a request in dialect or French gets «فإن طلبات المدعي تكون مؤسسة
    /// قانونا», which the last paragraph also takes when no paragraph has a closing, and which is written once.
    /// </summary>
    internal static IReadOnlyList<string?> Closings(IReadOnlyList<ArguedGroup> groups, IReadOnlyDictionary<string, string> inputs, IReadOnlyList<bool> written)
    {
        var entity = inputs.GetValueOrDefault("plaintiff.isLegalEntity", string.Empty) == "true";
        var female = entity || inputs.GetValueOrDefault("plaintiff.gender", string.Empty) == "female";
        var plaintiff = female ? "المدعية" : "المدعي";
        var general = $"فإن طلبات {plaintiff} تكون مؤسسة قانونا";
        var closings = new string?[groups.Count];
        var generalWritten = false;
        for (var i = 0; i < groups.Count; i++)
        {
            if (!written[i] || groups[i].Request is not { } request)
            {
                continue;
            }

            if (IsFormalRequest(request))
            {
                closings[i] = $"فإن طلب {plaintiff} {RequestText(request)} يكون مؤسسا قانونا";
            }
            else if (!generalWritten)
            {
                closings[i] = general;
                generalWritten = true;
            }
        }

        var hasRequests = !string.IsNullOrWhiteSpace(inputs.GetValueOrDefault("notes.requests", string.Empty));
        if (hasRequests && closings.All(c => c is null) && Enumerable.Range(0, groups.Count).LastOrDefault(i => written[i], -1) is var last and >= 0)
        {
            closings[last] = general;
        }

        return closings;
    }

    /// <summary>
    /// Removes a bare reference to one of the paragraph's own articles («، كما تنص عليه المادة 498 من القانون المدني»,
    /// «طبقا للمادة 498 من القانون المدني»): the code already cited it in the rule sentence, so it adds nothing and need
    /// not cost the paragraph (Gemma 3 4B, 14 Oct, case 03). Any other reference stays, and is rejected as before.
    /// </summary>
    internal static string WithoutOwnCitation(string application, ArguedGroup group)
    {
        var own = group.Texts.Select(t => ArticleNumbers.Canonical(t.Number)).ToHashSet(StringComparer.Ordinal);
        return OwnCitation().Replace(application, m =>
            own.Contains(ArticleNumbers.Canonical(m.Groups["n"].Value)) ? string.Empty : m.Value);
    }

    [GeneratedRegex(@"\s*[،,]?\s*(?:كما\s+(?:تنص|نصت|تقضي|قضت)\s+(?:عليه|على\s+ذلك|به)\s+|طبقا\s+|وفقا\s+|عملا\s+|استنادا\s+(?:إلى|الى)\s+)(?:ال|لل|بال)?ماد[ةه]\s+(?<n>\d+(?:\s+مكرر)?)\s+من\s+(?:ال)?قانون\s+(?:ال)?\p{L}+")]
    private static partial Regex OwnCitation();

    /// <summary>
    /// Drops a clause of the application that repeats the paragraph's rule, which the code already wrote before it
    /// (Gemma 3 4B: «…، فإن ذلك يثبت إخلاله بالتزامه، وبتعويض الدائن عن الضرر اللاحق من تأخر المدين…، فإنه…»). Only a
    /// clause with no number whose words are the rule's is removed; the opening clause always stays.
    /// </summary>
    internal static string WithoutRestatedRule(string application, ArguedGroup group)
    {
        var rules = group.Texts.Where(t => !string.IsNullOrWhiteSpace(t.Gist)).Select(t => Core(ArguedGroup.RuleClause(t.Gist!))).Where(r => r.Length > 0).ToList();
        if (rules.Count == 0)
        {
            return application;
        }

        var clauses = application.Split('،');
        var kept = clauses.Where((c, k) => k == 0 || c.Any(char.IsDigit) || Core(c) is not { Length: >= 12 } core
            || !rules.Any(r => r.Contains(core, StringComparison.Ordinal) || core.Contains(r, StringComparison.Ordinal))).ToList();
        return kept.Count == clauses.Length ? application : string.Join('،', kept);

        static string Core(string clause)
        {
            var text = ArabicScript.Normalize(clause).Trim().TrimEnd('.');
            text = text.StartsWith('و') ? text[1..] : text;
            foreach (var prefix in new[] { "بانه ", "بان ", "ب" })
            {
                if (text.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return text[prefix.Length..].Trim();
                }
            }

            return text;
        }
    }

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

        // French has no place in the Arabic grounds («Condamner le défendeur…», «1.500.000 DA au principal» copied from
        // French requests), except a term the facts or documents themselves use (reconnaissance de dette).
        var ownTerms = NarrativeChecks.LatinWords(inputs.GetValueOrDefault("notes.facts", string.Empty) + "\n" + inputs.GetValueOrDefault("documents.list", string.Empty))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (NarrativeChecks.LatinWords(argument).FirstOrDefault(w => !ownTerms.Contains(w)) is { } french)
        {
            return $"writes in French ({french})";
        }

        if (CopiesInformalRequest(argument, inputs.GetValueOrDefault("notes.requests", string.Empty)))
        {
            return "copies a request not in formal Arabic";
        }

        var sources = string.Join('\n', groups.Select(g => g.Note)
            .Append(inputs.GetValueOrDefault("notes.facts", string.Empty))
            .Append(inputs.GetValueOrDefault("notes.requests", string.Empty))
            .Append(inputs.GetValueOrDefault("documents.list", string.Empty)));
        // «مليون وخمائة وخمسين ألف دينار», «خمس عشرة جانفي ألف وخمائة وخمسة وسبعين» (Gemma 3 4B, a4, 16 Oct): a number
        // written in words escapes the number checks, so it is refused unless the lawyer wrote it so.
        if (NumberInWords().Match(argument) is { Success: true } words
            && !ArabicScript.Normalize(sources).Contains(ArabicScript.Normalize(words.Value), StringComparison.Ordinal))
        {
            return $"writes a number in words ({words.Value})";
        }

        return NarrativeChecks.Check(argument, sources, inputs, definition?.GroundedTerms, legalTexts)
            .FirstOrDefault(x => x.IsHard || x.IsWrongArticleContent || x.Kind is NarrativeIssueKind.UnknownNumber or NarrativeIssueKind.UncitedArticle) is { } issue
            ? $"{issue.Kind} {issue.Detail}"
            : null;
    }

    private const string NumberWord = @"(?:واحد\S*|[اأإ]ثن\S*|ثلاث\S*|[اأ]ربع\S*|خمس\S*|ست\S*|سبع\S*|ثمان\S*|تسع\S*|عشر\S*|\S*م(?:ائ|ئ|اي)ة|مئتين|مائتين|[اأآ]لف\S*|[اآ]لاف|مليون\S*|ملايين|مليار\S*)";

    [GeneratedRegex(@"(?<!\S)" + NumberWord + @"\s+و" + NumberWord + @"(?!\S)")]
    private static partial Regex NumberInWords();

    /// <summary>
    /// «- المادة 450 من القانون المدني» lines are the ticked articles the app lists before the lawyer's own notes;
    /// every other line is the lawyer's.
    /// </summary>
    /// <remarks>
    /// Only a bare citation counts as the app's: a lawyer's «- المادة 124 من القانون المدني الجزائري (الالتزام بالتعويض…)»
    /// was taken for a ticked article and its argument lost (app run, 18 Oct).
    /// </remarks>
    public static bool IsOwnLine(string line) => !TickedLine().IsMatch((line ?? string.Empty).Trim());

    [GeneratedRegex(@"^-\s*المادة\s+[\d\u0660-\u0669]+(?:\s+مكرر(?:\s*[\d\u0660-\u0669]+)?)?\s+من\s+[^\n:()،,.؛;«»]+$")]
    private static partial Regex TickedLine();

    /// <summary>True when the notes cite an article of a code this app covers, loaded or not.</summary>
    public static bool CitesCoveredArticle(string notes) =>
        References(notes ?? string.Empty).Any(r => r.CodeName is null || CoveredCodes.Contains(r.CodeName));

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

    /// <summary>
    /// The model's answer. One cut off at the length limit (a small model sometimes rambles on French or dialect notes)
    /// counts as no answer for this try, so the paragraphs are asked once more and then left as «(...)», rather than the
    /// whole draft failing.
    /// </summary>
    /// <remarks>
    /// Reading stops as soon as the model starts a line numbered past the plan: lines beyond it are never used, and a
    /// small model sometimes writes five lines for one planned (about 80 s of CPU writing thrown away, measured).
    /// </remarks>
    private async Task<string> AskAsync(DocumentTypeDefinition definition, ModelRequest request, int paragraphs, CancellationToken cancellationToken)
    {
        var answer = new StringBuilder();
        var sawLineBreak = false;
        try
        {
            await foreach (var piece in _provider.StreamAsync(request, cancellationToken))
            {
                answer.Append(piece);
                sawLineBreak |= piece.Contains('\n', StringComparison.Ordinal);
                if (sawLineBreak && PastThePlan(answer.ToString(), paragraphs) is { } end)
                {
                    answer.Length = end;
                    break;
                }
            }
        }
        catch (ModelProviderException ex) when (ex.IsCutOff)
        {
            return CutOffMarker;
        }

        return DraftingService.ApplyCorrections(definition, answer.ToString());
    }

    /// <summary>
    /// Where a line numbered beyond <paramref name="paragraphs"/> starts, once every planned line 1..N is present; null
    /// while the plan is not complete. Only whole lines count, so a planned line is never cut.
    /// </summary>
    internal static int? PastThePlan(string answer, int paragraphs)
    {
        var seen = new HashSet<int>();
        foreach (Match m in NumberedLineStart().Matches(answer))
        {
            if (!int.TryParse(ArabicScript.Normalize(m.Groups["n"].Value), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            {
                continue;
            }

            if (n > paragraphs && Enumerable.Range(1, paragraphs).All(seen.Contains))
            {
                return m.Index;
            }

            seen.Add(n);
        }

        return null;
    }

    [GeneratedRegex(@"(?<=^|\n)[ \t]*[\[(]?(?<n>\d{1,2})[\])\.:\-]", RegexOptions.CultureInvariant)]
    private static partial Regex NumberedLineStart();

    /// <summary>Stands for a cut-off answer in <see cref="SkeletonDraft.RawAnswers"/>; it parses to no paragraph.</summary>
    internal const string CutOffMarker = "(answer cut off at the length limit)";
}
