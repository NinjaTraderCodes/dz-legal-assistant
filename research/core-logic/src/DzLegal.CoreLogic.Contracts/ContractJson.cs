using System.Text.Json;
using System.Text.Json.Serialization;
using DzLegal.Domain.Serialization;

namespace DzLegal.CoreLogic.Contracts;

/// <summary>
/// JSON options for rule records and the slot catalogue: the app's own options (camelCase names, enums as camelCase
/// strings, Arabic kept readable), made strict in three ways so that a record and the contract cannot drift apart
/// unnoticed: a member the contract does not know is an error; a property name must have the exact case; an enum must be
/// written by name, never as a number.
/// </summary>
public static class ContractJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(DefinitionJson.Options)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            PropertyNameCaseInsensitive = false,
        };

        // First in the list wins: this one refuses integers, which the app's own converter accepts.
        options.Converters.Insert(0, new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
