namespace DzLegal.Domain;

public enum ElementStatus
{
    Present,
    Missing,
    NotApplicable,
}

/// <summary>The outcome of checking one required element.</summary>
public sealed record Finding(
    RequiredElement Element,
    ElementStatus Status,
    IReadOnlyList<string> MissingParts)
{
    public string ElementId => Element.Id;
}

public sealed record ComplianceReport(string DocumentTypeId, IReadOnlyList<Finding> Findings)
{
    public IEnumerable<Finding> Missing => Findings.Where(f => f.Status == ElementStatus.Missing);

    /// <summary>True when no element whose omission causes inadmissibility is missing.</summary>
    public bool IsAdmissibleInForm =>
        !Missing.Any(f => f.Element.Severity == Severity.Inadmissible);

    public Finding? Find(string elementId) => Findings.FirstOrDefault(f => f.ElementId == elementId);
}
