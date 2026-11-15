using System.Text.Json;

namespace DzLegal.CoreLogic.Contracts.Tests;

/// <summary>The reading's optional role (scheme 1.0, section 3; INTEGRATION-CONTRACT.md, section 4).</summary>
public sealed class NoteReadingTests
{
    [Fact]
    public void AClaimAnswerCarriesAnOptionalRoleAndAnAnswerWithoutOneStillLoads()
    {
        const string WithRole = """
            { "claimId": "contract-damages", "confidence": 0.9,
              "role": { "relation": "works_contract", "position": "contractor", "confidence": 0.8,
                        "evidence": [ { "field": "facts", "start": 0, "length": 12 } ] } }
            """;
        var guess = JsonSerializer.Deserialize<ClaimGuess>(WithRole, ContractJson.Options)!;
        Assert.Equal(("works_contract", "contractor", false), (guess.Role!.Relation, guess.Role.Position, guess.Role.Abstained));
        Assert.Equal(new TextSpan(NoteField.Facts, 0, 12), Assert.Single(guess.Role.Evidence));

        const string Without = """{ "claimId": "loan-repayment", "confidence": 0.95 }""";
        Assert.Null(JsonSerializer.Deserialize<ClaimGuess>(Without, ContractJson.Options)!.Role);
    }

    [Fact]
    public void ARoleWithoutItsPositionIsRefused() =>
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<ClaimGuess>(
            """{ "claimId": "custody", "confidence": 0.9, "role": { "relation": "marriage", "confidence": 0.7 } }""", ContractJson.Options));
}
