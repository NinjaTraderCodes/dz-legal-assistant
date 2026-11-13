using System.Globalization;
using System.Text.Json;

namespace DzLegal.CoreLogic.Contracts.Tests;

/// <summary>
/// A validator for the small part of JSON Schema that the handbook's schemas use: type, properties, required,
/// additionalProperties, items, enum, const and $ref (to #/$defs/name only). Small on purpose: no package is needed, and a
/// schema that uses anything else fails <see cref="UnsupportedKeywords"/> instead of being half checked.
/// </summary>
internal static class MiniSchema
{
    private static readonly HashSet<string> Checked =
        ["type", "properties", "required", "additionalProperties", "items", "enum", "const", "$ref"];

    private static readonly HashSet<string> Ignored = ["description", "title", "$schema", "$id", "$defs"];

    /// <summary>Every keyword of the schema that this validator does not know, with its path. Must be empty.</summary>
    public static IReadOnlyList<string> UnsupportedKeywords(JsonElement schema)
    {
        var found = new List<string>();
        Walk(schema, "#", found);
        return found;
    }

    public static IReadOnlyList<string> Validate(JsonElement schema, JsonElement value)
    {
        var errors = new List<string>();
        Check(schema, schema, value, "$", errors);
        return errors;
    }

    private static void Walk(JsonElement node, string path, List<string> found)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var member in node.EnumerateObject())
        {
            if (!Checked.Contains(member.Name) && !Ignored.Contains(member.Name))
            {
                found.Add($"{path}/{member.Name}");
                continue;
            }

            switch (member.Name)
            {
                case "properties" or "$defs":
                    foreach (var child in member.Value.EnumerateObject())
                    {
                        Walk(child.Value, $"{path}/{member.Name}/{child.Name}", found);
                    }

                    break;
                case "items":
                    Walk(member.Value, $"{path}/items", found);
                    break;
                case "$ref" when !member.Value.GetString()!.StartsWith("#/$defs/", StringComparison.Ordinal):
                    found.Add($"{path}/$ref ({member.Value.GetString()})");
                    break;
            }
        }
    }

    private static void Check(JsonElement root, JsonElement schema, JsonElement value, string path, List<string> errors)
    {
        if (schema.TryGetProperty("$ref", out var reference))
        {
            var name = reference.GetString()!["#/$defs/".Length..];
            if (!root.GetProperty("$defs").TryGetProperty(name, out var target))
            {
                errors.Add($"{path}: the schema refers to {name}, which it does not define");
                return;
            }

            Check(root, target, value, path, errors);
        }

        if (schema.TryGetProperty("type", out var type) && !HasType(value, type.GetString()!))
        {
            errors.Add($"{path}: expected {type.GetString()}, found {value.ValueKind}");
            return;
        }

        if (schema.TryGetProperty("const", out var constant) && !Same(constant, value))
        {
            errors.Add($"{path}: must be {constant.GetRawText()}");
        }

        if (schema.TryGetProperty("enum", out var allowed) && !allowed.EnumerateArray().Any(option => Same(option, value)))
        {
            errors.Add($"{path}: {value.GetRawText()} is not one of {allowed.GetRawText()}");
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            CheckObject(root, schema, value, path, errors);
        }

        if (value.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items))
        {
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                Check(root, items, item, string.Create(CultureInfo.InvariantCulture, $"{path}[{index}]"), errors);
                index++;
            }
        }
    }

    private static void CheckObject(JsonElement root, JsonElement schema, JsonElement value, string path, List<string> errors)
    {
        var hasProperties = schema.TryGetProperty("properties", out var properties);
        if (schema.TryGetProperty("required", out var required))
        {
            foreach (var name in required.EnumerateArray())
            {
                if (!value.TryGetProperty(name.GetString()!, out _))
                {
                    errors.Add($"{path}: {name.GetString()} is missing");
                }
            }
        }

        var closed = schema.TryGetProperty("additionalProperties", out var additional) && additional.ValueKind == JsonValueKind.False;
        foreach (var member in value.EnumerateObject())
        {
            if (hasProperties && properties.TryGetProperty(member.Name, out var memberSchema))
            {
                Check(root, memberSchema, member.Value, $"{path}.{member.Name}", errors);
            }
            else if (closed)
            {
                errors.Add($"{path}: {member.Name} is not a known field");
            }
        }
    }

    private static bool HasType(JsonElement value, string type) => type switch
    {
        "object" => value.ValueKind == JsonValueKind.Object,
        "array" => value.ValueKind == JsonValueKind.Array,
        "string" => value.ValueKind == JsonValueKind.String,
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
        "number" => value.ValueKind == JsonValueKind.Number,
        _ => false,
    };

    private static bool Same(JsonElement expected, JsonElement actual) =>
        expected.ValueKind == actual.ValueKind && expected.GetRawText() == actual.GetRawText();
}
