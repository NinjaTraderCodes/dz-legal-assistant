using System.Globalization;
using DzLegal.Domain;

namespace DzLegal.Drafting;

public static class CaseInputs
{
    public const string DateFormat = "dd/MM/yyyy";

    /// <summary>A fresh set of inputs with every field at its default value.</summary>
    public static Dictionary<string, string> CreateDefaults(DocumentTypeDefinition definition, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.Fields.ToDictionary(
            f => f.Id,
            f => f.Kind == FieldKind.Date && string.Equals(f.Default, "today", StringComparison.OrdinalIgnoreCase)
                ? today.ToString(DateFormat, CultureInfo.InvariantCulture)
                : f.Default ?? string.Empty,
            StringComparer.Ordinal);
    }
}
