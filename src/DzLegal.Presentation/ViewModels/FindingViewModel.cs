using DzLegal.Domain;

namespace DzLegal.Presentation.ViewModels;

/// <summary>One line of the compliance report, already localized.</summary>
public sealed class FindingViewModel
{
    public FindingViewModel(Finding finding, DocumentTypeDefinition definition, Loc loc)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(finding);
        var element = finding.Element;
        var lang = loc.Language;
        ElementId = element.Id;
        Status = finding.Status;
        Label = element.Label.Get(lang);
        Article = element.Article;
        IsWarning = element.Severity == Severity.Warning;
        IsVerified = element.Verified;
        UnverifiedText = loc["Unverified"];
        StatusText = finding.Status switch
        {
            ElementStatus.Present => loc["StatusPresent"],
            ElementStatus.Missing => IsWarning ? loc["SeverityWarning"] : loc["StatusMissing"],
            _ => loc["StatusNotApplicable"],
        };
        Message = IsMissing ? element.MissingMessage.Get(lang) : string.Empty;
        TargetGroupId = finding.MissingParts
            .SelectMany(p => p.Split('|', '<'))
            .Select(id => definition.FindField(id)?.Group ?? definition.FindSection(id)?.FormGroup)
            .FirstOrDefault(g => g is not null);
        MissingPartsText = IsMissing && finding.MissingParts.Count > 0
            ? loc.Format("MissingParts", string.Join("، ", finding.MissingParts.Select(p => DescribePart(p, definition, loc))))
            : string.Empty;
    }

    public string ElementId { get; }

    /// <summary>The form step where the lawyer can fix this finding.</summary>
    public string? TargetGroupId { get; }

    public ElementStatus Status { get; }

    public string Label { get; }

    public string Article { get; }

    public string StatusText { get; }

    public string Message { get; }

    public string MissingPartsText { get; }

    public bool IsWarning { get; }

    public bool IsVerified { get; }

    public bool IsUnverified => !IsVerified;

    public string UnverifiedText { get; }

    public bool IsMissing => Status == ElementStatus.Missing;

    public bool IsMissingMandatory => IsMissing && !IsWarning;

    public bool IsMissingWarning => IsMissing && IsWarning;

    public bool IsPresent => Status == ElementStatus.Present;

    public bool IsNotApplicable => Status == ElementStatus.NotApplicable;

    public bool HasMessage => Message.Length > 0;

    public bool HasMissingParts => MissingPartsText.Length > 0;

    /// <summary>Turns a Form Engine part id ("plaintiff.domicile", "a|b", "a&lt;b") into readable labels.</summary>
    internal static string DescribePart(string part, DocumentTypeDefinition definition, Loc loc)
    {
        if (part.Contains('<', StringComparison.Ordinal))
        {
            var pair = part.Split('<', 2);
            return loc.Format("OrderPart", Name(pair[0], definition, loc), Name(pair[1], definition, loc));
        }

        return string.Join(loc["Or"], part.Split('|').Select(p => Name(p, definition, loc)));
    }

    private static string Name(string id, DocumentTypeDefinition definition, Loc loc) =>
        definition.FindField(id)?.Label.Get(loc.Language)
        ?? definition.FindSection(id)?.Title.Get(loc.Language)
        ?? id;
}
