using System.Collections.Immutable;
using System.Text.Json;
using DzLegal.Drafting.Evidence;

namespace DzLegal.Drafting.Tests;

public sealed class EvidenceTests
{
    private static CaseSnapshot Snapshot(string facts = "وقع العقد يوم 15/02/2024. بدأ الإيجار يوم 01/03/2024.") => CaseSnapshot.Create("fictional-lease", 0,
        new Dictionary<string, string> { ["notes.facts"] = facts, ["notes.requests"] = "طلب دفع 35000 دج.", ["documents.list"] = "نسخة عقد.", ["plaintiff.firstName"] = "اسم وهمي", ["court.section"] = "القسم المدني" });

    private static CaseEvidence Empty(CaseSnapshot snapshot) => new()
    {
        SnapshotFingerprint = snapshot.Fingerprint,
        Coverage = [.. snapshot.Sentences.Select(s => new EvidenceCoverage(s.Id, "read"))],
    };

    private static EvidenceSpan Span(CaseSnapshot snapshot, string text) => new("notes.facts", snapshot.Fields["notes.facts"].IndexOf(text, StringComparison.Ordinal), text.Length);

    private static EvidenceEvent Event(CaseSnapshot snapshot, string kind = "contract_concluded", string polarity = "affirmed") =>
        new("event1", kind, "plaintiff", polarity, null, [], [], [Span(snapshot, snapshot.Sentences.First(s => s.FieldId == "notes.facts").Text)]);

    [Fact]
    public void SnapshotOwnsEveryFieldAndNeverNormalizesTheOriginal()
    {
        var fields = new Dictionary<string, string> { ["notes.facts"] = "نص\r\nثانٍ.", ["custom.future-field"] = "سري وهمي" };
        var snapshot = CaseSnapshot.Create("fictional", 2, fields);
        fields["notes.facts"] = "changed";
        Assert.Equal("نص\r\nثانٍ.", snapshot.Fields["notes.facts"]);
        Assert.Contains(snapshot.Sentences, s => s.FieldId == "custom.future-field");
        Assert.All(snapshot.Sentences, s => Assert.Equal(s.Text, snapshot.Read(new(s.FieldId, s.Start, s.Text.Length))));
    }

    [Fact]
    public void EveryInputAndRevisionParticipatesInFreshness()
    {
        var snapshot = Snapshot();
        foreach (var key in snapshot.Fields.Keys)
        {
            var fields = snapshot.Fields.ToDictionary();
            fields[key] += " جديد";
            Assert.NotEqual(snapshot.Fingerprint, CaseSnapshot.Create(snapshot.CaseId, 0, fields).Fingerprint);
        }

        Assert.NotEqual(snapshot.Fingerprint, CaseSnapshot.Create(snapshot.CaseId, 1, snapshot.Fields).Fingerprint);
        var id = new AnalysisIdentity(snapshot.Fingerprint, "v1", "model", "options", "corpus", "rules");
        Assert.False(id.IsCurrent(snapshot, id with { CorpusVersion = "new" }));
        Assert.False(id.IsCurrent(snapshot, id with { ReaderDigest = "new" }));
        Assert.False(id.IsCurrent(snapshot, id with { RuleVersion = "new" }));
    }

    [Fact]
    public void SourceSpansRejectOverflowAndHalfOfASurrogatePair()
    {
        var snapshot = Snapshot("نص 😀 آخر");
        Assert.Null(snapshot.Read(new("notes.facts", int.MaxValue, int.MaxValue)));
        Assert.Null(snapshot.Read(new("notes.facts", -1, 2)));
        Assert.Null(snapshot.Read(new("notes.facts", 3, 1)));
        Assert.Equal("😀", snapshot.Read(new("notes.facts", 3, 2)));
    }

    [Fact]
    public void NormalizedSearchMatchesMapBackToOriginalDiacriticsDigitsAndLigatures()
    {
        const string original = "  أَجَّرَ  ﷲ مبلغ ٥٠٠٠ دج  ";
        var normalized = NormalizedEvidenceText.From("notes.facts", original);
        Assert.Equal(DzLegal.Domain.ArabicScript.Normalize(original), normalized.Text);
        var start = normalized.Text.IndexOf("5000", StringComparison.Ordinal);
        var span = normalized.Map(start, 4);
        Assert.Equal("٥٠٠٠", original.Substring(span.Start, span.Length));
        var god = normalized.Text.IndexOf("الله", StringComparison.Ordinal);
        var ligature = normalized.Map(god, 4);
        Assert.Equal("ﷲ", original.Substring(ligature.Start, ligature.Length));
    }

    [Theory]
    [InlineData("29/02/2024", "date", "29/02/2024")]
    [InlineData("29/02/2023", "date", null)]
    [InlineData("31/13/2024", "date", null)]
    [InlineData("2024", "amount", null)]
    [InlineData("15/02/2024", "amount", null)]
    [InlineData("1.500,50", "amount", null)]
    [InlineData("20 دج", "amount", "20")]
    [InlineData("2 مليون", "amount", "2000000")]
    [InlineData("٥٠٠.٠٠٠ دج", "amount", "500000")]
    [InlineData("9223372036854775807 مليون", "amount", null)]
    public void ValuesRejectImpossibleDatesAmbiguousMoneyAndOverflow(string text, string kind, string? expected) => Assert.Equal(expected, EvidenceValidator.NormalizeValue(text, kind));

    [Fact]
    public void CoverageAndCitationsAreNecessaryButDoNotProveDateMeaning()
    {
        var snapshot = Snapshot();
        var ev = Event(snapshot) with { Values = [new("date", "signature_date", Span(snapshot, "01/03/2024"))] };
        var check = EvidenceValidator.Check(snapshot, Empty(snapshot) with { Events = [ev] });
        Assert.True(check.StructurallyValid); // The wrong semantic binding is deliberately NOT called verified.
        Assert.Equal(EvidenceState.Unknown, new AssessedEvidence(check, []).StateOf(ev.Id));
        Assert.Contains(EvidenceValidator.Check(snapshot, Empty(snapshot) with { Coverage = [] }).Issues, i => i.Code == "unread_passage");
    }

    [Fact]
    public void CourtHintUnknownParticipantsAndDuplicateIdsCannotBecomeEvidence()
    {
        var snapshot = Snapshot();
        var ev = Event(snapshot) with { Sources = [new("court.section", 0, 5)], Participants = [new("actor", "invented_person", [Span(snapshot, "العقد")])] };
        var check = EvidenceValidator.Check(snapshot, Empty(snapshot) with { Events = [ev, ev] });
        Assert.Contains(check.Issues, i => i.Code == "hint_as_fact");
        Assert.Contains(check.Issues, i => i.Code == "party");
        Assert.Contains(check.Issues, i => i.Code == "id");
    }

    [Fact]
    public void ARefutedExtractionDoesNotProveThatADeniedEventHappened()
    {
        var snapshot = Snapshot();
        var ev = Event(snapshot, "notice_served", "denied");
        var evidence = new AssessedEvidence(EvidenceValidator.Check(snapshot, Empty(snapshot) with { Events = [ev] }), [new(ev.Id, EvidenceState.Refuted, "gold-fixture", "1")]);
        Assert.Equal(EvidenceState.Unknown, CivilLeaseConditions.Evaluate(evidence)["lease.notice_served"].State);
        evidence = evidence with { Findings = [new(ev.Id, EvidenceState.Supported, "gold-fixture", "1")] };
        Assert.Equal(EvidenceState.Refuted, CivilLeaseConditions.Evaluate(evidence)["lease.notice_served"].State);
    }

    [Fact]
    public void DeclaredUnresolvedPassagesCannotSilentlyBecomeCompleteAnalysis()
    {
        var snapshot = Snapshot();
        var ev = Event(snapshot);
        var proposed = Empty(snapshot) with { Events = [ev], Coverage = [.. snapshot.Sentences.Select(s => new EvidenceCoverage(s.Id, "unresolved"))] };
        var checkedEvidence = EvidenceValidator.Check(snapshot, proposed);
        Assert.True(checkedEvidence.StructurallyValid);
        Assert.False(checkedEvidence.CompleteCoverage);
        Assert.Equal(EvidenceState.Unknown, new AssessedEvidence(checkedEvidence, [new(ev.Id, EvidenceState.Supported, "gold-fixture", "1")]).StateOf(ev.Id));
    }

    [Fact]
    public void OpposingAttributedAssertionsRemainAConflict()
    {
        var snapshot = Snapshot();
        var asserted = Event(snapshot, "notice_served");
        var denied = asserted with { Id = "event2", AssertedBy = "defendant", Polarity = "denied", DisputeGroup = "notice" };
        var evidence = new AssessedEvidence(EvidenceValidator.Check(snapshot, Empty(snapshot) with { Events = [asserted, denied] }),
            [new(asserted.Id, EvidenceState.Supported, "gold-fixture", "1"), new(denied.Id, EvidenceState.Supported, "gold-fixture", "1")]);
        Assert.Equal(EvidenceState.Conflicting, CivilLeaseConditions.Evaluate(evidence)["lease.notice_served"].State);
    }

    [Theory]
    [InlineData(EvidenceState.Unknown)]
    [InlineData(EvidenceState.Conflicting)]
    public void NegationPreservesUncertainty(EvidenceState state) => Assert.Equal(state, EvidenceLogic.Not(state));

    [Fact]
    public void RulesKeepMissingExceptionsUnknownAndRequireReviewedAuthorities()
    {
        var rule = new EvidenceRule("lease-probe", ["lease"], ["exception"], [], "unreviewed");
        var conditions = new Dictionary<string, ConditionResult> { ["lease"] = new("lease", EvidenceState.Supported, ["event1"], []) };
        var result = rule.Evaluate(conditions);
        Assert.Equal(EvidenceState.Unknown, result.State);
        Assert.False(result.Qualified);
        Assert.False(new AuthorityClause("civil-code", "498", "unknown", "quoted test clause", new string('a', 64), "reviewed").ReviewedSource);
        Assert.False(new AuthorityClause("civil-code", "498", "2024-01-01", "quoted test clause", "missing", "reviewed").ReviewedSource);
    }

    [Fact]
    public void SchemaCannotAcceptModelSuppliedSemanticCertification()
    {
        Assert.Throws<JsonException>(() => EvidenceJson.Parse("""{"verified": true}"""));
        using var schema = JsonDocument.Parse(EvidenceJson.Schema(Snapshot()).GetRawText());
        Assert.False(schema.RootElement.GetProperty("additionalProperties").GetBoolean());
        Assert.Contains("notice_served", schema.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{\"schema\":\"a\",\"schema\":\"b\"}")]
    [InlineData("{\"events\":[null]}")]
    [InlineData("{\"events\":null}")]
    public void AmbiguousOrNullModelRecordsAreRejectedBeforeAssessment(string json) => Assert.Throws<JsonException>(() => EvidenceJson.Parse(json));

    [Fact]
    public async Task OversizedRequestsFailBeforeCallingAModelAndNothingIsSilentlyDropped()
    {
        var model = new FakeReaderProvider();
        var reader = new StructuredEvidenceReader(model, "Read evidence, not legal conclusions.", _ => 5000, 4096);
        await Assert.ThrowsAsync<ModelProviderException>(() => reader.ReadAsync(Snapshot(new string('س', 10000)), CancellationToken.None));
        Assert.False(model.Called);
        var mismatched = new StructuredEvidenceReader(model, "Read evidence.", _ => 1, 8192);
        await Assert.ThrowsAsync<ModelProviderException>(() => mismatched.ReadAsync(Snapshot(), CancellationToken.None));
        Assert.False(model.Called);
    }

    [Fact]
    public async Task LateResultsCannotBeUsedAsACurrentDraft()
    {
        var snapshot = Snapshot();
        var identity = new AnalysisIdentity(snapshot.Fingerprint, CaseEvidence.CurrentSchema, "reader", "options", "corpus", "rules");
        var pipeline = new EvidencePipeline(new FixedReader(Empty(snapshot)), new UnqualifiedEvidenceAssessor());
        var result = await pipeline.RunAsync(snapshot, identity, () => identity with { CorpusVersion = "changed" }, [], CancellationToken.None);
        Assert.False(result.Current);
        Assert.Throws<InvalidOperationException>(() => EvidenceDraftRenderer.Render(snapshot, result.DraftPlan, identity with { RuleVersion = "changed" }, key => key, (_, values) => (string)values[0]!));
        Assert.All(snapshot.Sentences.Where(s => s.FieldId is "notes.facts" or "notes.requests" or "documents.list"), s => Assert.Contains(result.DraftPlan.Clauses, c => c.Id == s.Id));
    }

    private sealed class FakeReaderProvider : IStructuredModelProvider
    {
        public bool Called { get; private set; }
        public string Name => "test";
        public int ContextTokenLimit => 4096;
        public int OutputTokenLimit => 1500;
        public Task<string> GenerateAsync(ModelRequest request, CancellationToken cancellationToken) { Called = true; return Task.FromResult("{}"); }
    }

    private sealed class FixedReader(CaseEvidence evidence) : IEvidenceReader
    {
        public Task<CaseEvidence> ReadAsync(CaseSnapshot snapshot, CancellationToken cancellationToken) => Task.FromResult(evidence);
    }
}
