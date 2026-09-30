using System.Text.RegularExpressions;
using DzLegal.Domain;

namespace DzLegal.FormEngine;

/// <summary>
/// Checks a draft against every required element of its document type and reports, per element,
/// whether it is present, missing (with the missing parts) or not applicable to this case.
/// Pure and deterministic: the same draft always yields the same report.
/// </summary>
public static class ComplianceChecker
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public static ComplianceReport Check(DocumentTypeDefinition definition, DraftDocument draft)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(draft);
        if (!string.Equals(definition.Id, draft.DocumentTypeId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Draft is of type '{draft.DocumentTypeId}' but definition is '{definition.Id}'.", nameof(draft));
        }

        var findings = definition.Elements.Select(e => CheckElement(e, draft)).ToList();
        return new ComplianceReport(definition.Id, findings);
    }

    private static Finding CheckElement(RequiredElement element, DraftDocument draft)
    {
        if (!Condition.All(element.AppliesWhen, draft.Inputs))
        {
            return new Finding(element, ElementStatus.NotApplicable, []);
        }

        var missing = Evaluate(element.Check, draft);
        return new Finding(element, missing.Count == 0 ? ElementStatus.Present : ElementStatus.Missing, missing);
    }

    /// <summary>Returns the missing parts; an empty list means the check passed.</summary>
    internal static List<string> Evaluate(RuleCheck check, DraftDocument draft) => check switch
    {
        FieldsPresentCheck c => c.Fields.Where(f => IsBlank(draft.Inputs.GetValueOrDefault(f))).ToList(),
        SectionNotEmptyCheck c => IsBlank(draft.FindSection(c.Section)?.Text) ? [c.Section] : [],
        SectionMatchesCheck c => Regex.IsMatch(draft.FindSection(c.Section)?.Text ?? string.Empty, c.Pattern, RegexOptions.None, RegexTimeout)
            ? []
            : [c.Section],
        SectionOrderCheck c => EvaluateOrder(c, draft),
        PredominantlyArabicCheck c => c.Sections
            .Where(s => ArabicScript.Ratio(draft.FindSection(s)?.Text) < c.MinRatio)
            .ToList(),
        AllOfCheck c => c.Checks.SelectMany(inner => Evaluate(inner, draft)).Distinct().ToList(),
        AnyOfCheck c => EvaluateAnyOf(c, draft),
        _ => throw new NotSupportedException($"Unsupported check {check.GetType().Name}."),
    };

    private static List<string> EvaluateAnyOf(AnyOfCheck check, DraftDocument draft)
    {
        var alternatives = new List<List<string>>();
        foreach (var inner in check.Checks)
        {
            var missing = Evaluate(inner, draft);
            if (missing.Count == 0)
            {
                return [];
            }

            alternatives.Add(missing);
        }

        // Report the alternatives together, e.g. "defendant.domicile|defendant.lastKnownDomicile".
        return [string.Join('|', alternatives.SelectMany(a => a).Distinct())];
    }

    private static List<string> EvaluateOrder(SectionOrderCheck check, DraftDocument draft)
    {
        var positions = check.Sections
            .Select(id => (id, index: draft.Sections.FindIndex(s => s.Id == id)))
            .Where(p => p.index >= 0)
            .ToList();

        var misplaced = new List<string>();
        for (var i = 1; i < positions.Count; i++)
        {
            if (positions[i].index < positions[i - 1].index)
            {
                // "a<b": a must come before b.
                misplaced.Add($"{positions[i - 1].id}<{positions[i].id}");
            }
        }

        return misplaced;
    }

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);
}
