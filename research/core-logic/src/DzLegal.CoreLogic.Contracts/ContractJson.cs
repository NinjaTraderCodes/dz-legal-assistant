using System.Text.Json;
using System.Text.Json.Serialization;
using DzLegal.Domain.Serialization;

namespace DzLegal.CoreLogic.Contracts;

/// <summary>
/// JSON options for rule records and the slot catalogue: the app's own options (camelCase names, enums as camelCase
/// strings, Arabic kept readable), with one difference: a member the contract does not know is an error, so that a
/// record and the contract cannot drift apart unnoticed.
/// </summary>
public static class ContractJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(DefinitionJson.Options)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
