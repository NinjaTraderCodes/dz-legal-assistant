using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using DzLegal.CoreLogic.Contracts;
using DzLegal.CoreLogic.Rules;

namespace DzLegal.CoreLogic.Notes.Sampler;

/// <summary>What one <c>notes sample</c> draws: <paramref name="Groups"/> polarity groups of 3 to 4 count inside <paramref name="Count"/>.</summary>
public sealed record SampleRequest(string Set, int Count, int Seed, int Groups = 0);

/// <summary>
/// The scenario sampler, version 1 (specs/scenario-sampler.md): code decides the case, the writer only words it. Every draw
/// comes from a substream fixed by (seed, scenario, aspect), and each element draws its own numbers whatever its polarity,
/// so that the members of a polarity group differ in that one element and in what follows from it, nothing else.
/// </summary>
public sealed class ScenarioSampler
{
    public const string Version = "1.1";  // 1.1 (16 November 2026): a later claim's role keeps the plaintiff's sex

    private static readonly string[] Polarities = ["stated", "denied", "disputed", "notMentioned"];
    private readonly SamplerData data;
    private readonly IReadOnlyList<RuleRecord> finals;
    private readonly RuleEvaluator evaluator = new();
    private readonly IReadOnlyList<string> familyNames;

    public ScenarioSampler(SamplerData data, IReadOnlyList<RuleRecord> finalRecords)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        finals = finalRecords ?? [];
        if (data.Cards.Count == 0)
        {
            throw new InvalidDataException("notes/sampler/style-cards.json is missing: run `notes style-cards --write` first.");
        }

        familyNames = [.. SamplerData.Strings(data.Cast["family"]), .. SamplerData.Strings(data.Cast["maleGiven"]).Select(n => "بن " + n)];
    }

    /// <summary>Draws that failed a coherence check, by reason; a failed draw is replaced by the next sub-seed.</summary>
    public Dictionary<string, int> Rejected { get; } = new(StringComparer.Ordinal);

    public IReadOnlyList<JsonObject> Draw(SampleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var output = new List<JsonObject>();
        var groupsMade = 0;
        var draw = 0;
        var tag = request.Set.ToLowerInvariant();
        while (output.Count < request.Count)
        {
            var family = $"f-{tag}-{output.Count + 1:00000}";
            if (groupsMade < request.Groups && output.Count + 4 <= request.Count && draw % 3 == 0)
            {
                var group = DrawGroup(request.Seed, groupsMade, family, tag);
                if (group is not null)
                {
                    groupsMade++;
                    foreach (var member in group)
                    {
                        member["id"] = $"s-{tag}-{output.Count + 1:00000}";
                        output.Add(Ordered(member, request.Set));
                    }
                }

                draw++;
                continue;
            }

            var scenario = DrawCoherent(Rng.Derive(request.Seed, draw), null);
            draw++;
            scenario["id"] = $"s-{tag}-{output.Count + 1:00000}";
            scenario["family"] = family;
            output.Add(Ordered(scenario, request.Set));
        }

        return output;
    }

    // --- one scenario ------------------------------------------------------------------------------------------------

    /// <summary>A forced element: the polarity group's element and the polarity of this member.</summary>
    private sealed record Force(string Claim, string Kind, string Polarity);

    private JsonObject DrawCoherent(int seed, Force? force)
    {
        for (var attempt = 0; ; attempt++)
        {
            var s = Rng.Derive(seed, attempt);
            var (scenario, problem) = DrawOne(s, force);
            if (problem is null)
            {
                return scenario;
            }

            Rejected[problem] = Rejected.GetValueOrDefault(problem) + 1;
            if (attempt > 50)
            {
                throw new InvalidOperationException($"the sampler cannot draw a coherent scenario: {problem}");
            }
        }
    }

    private List<JsonObject>? DrawGroup(int seed, int groupIndex, string family, string tag)
    {
        var s = Rng.Derive(seed, 1_000_000 + groupIndex);
        var shape = new Rng(Rng.Derive(s, 0));
        // A claim type with one element cannot hold a group: its "notMentioned" member would say nothing of the claim.
        var claims = data.Claims.Where(c => c.Elements.Count >= 2).ToList();
        var claim = claims[shape.Between(0, claims.Count - 1)];
        var candidates = claim.Elements.Where(e => DrawnPolarities(e).Length >= 3).ToList();
        var element = shape.Pick(candidates);
        var group = $"g-{tag}-{groupIndex + 1:0000}";
        var members = new List<JsonObject>();
        foreach (var polarity in DrawnPolarities(element))
        {
            var (scenario, problem) = DrawOne(s, new Force(claim.Id, element.Kind, polarity));
            if (problem is not null)
            {
                Rejected["group: " + problem] = Rejected.GetValueOrDefault("group: " + problem) + 1;
                return null;
            }

            scenario["family"] = family;
            scenario["group"] = group;
            members.Add(scenario);
        }

        return members;
    }

    private string[] DrawnPolarities(ElementDef e) =>
        data.DeniedDrawn.GetValueOrDefault(e.Claim + "/" + e.Kind, true) ? Polarities : ["stated", "disputed", "notMentioned"];

    private sealed class EventDraft
    {
        public required ElementDef Element { get; init; }
        public required string Polarity { get; set; }
        public required string Actor { get; init; }
        public string? DisputedBy { get; set; }
        public DateOnly? Date { get; set; }
        public long? Amount { get; set; }
        public string? How { get; set; }
        public string? Concept { get; set; }
        public JsonObject? Contest { get; set; }
        public string? Form { get; set; }
        public required double[] U { get; init; }  // the element's own numbers, drawn whatever its polarity

        public bool Mentioned => Polarity is "stated" or "disputed";
    }

    private (JsonObject Scenario, string? Problem) DrawOne(int s, Force? force)
    {
        var shape = new Rng(Rng.Derive(s, 0));
        var card = data.Cards[shape.Between(0, data.Cards.Count - 1)];
        var petition = new DateOnly(2025, 1, 1).AddDays(shape.Between(0, 729));
        var kindRoll = shape.Next();
        List<string> claims;
        if (force is not null)
        {
            claims = [force.Claim];
        }
        else if (kindRoll < 0.12)
        {
            return DrawOutside(s, shape, card, petition);
        }
        else if (kindRoll < 0.24)
        {
            var pair = data.Tables["pairs"]!.AsArray()[shape.Between(0, data.Tables["pairs"]!.AsArray().Count - 1)]!.AsArray();
            claims = [.. pair.Select(c => (string)c!)];
        }
        else
        {
            claims = [data.Claims[shape.Between(0, data.Claims.Count - 1)].Id];
        }

        var draft = DrawClaims(s, claims, card, petition, force);
        return (draft, Check(draft));
    }

    /// <summary>
    /// The sex a family claim's position fixes for the plaintiff ("other" in the custody claims is the maternal
    /// grandmother); null for any other claim or position.
    /// </summary>
    public static string? SexOf(string claim, string position) =>
        claim is not ("divorce-by-husband" or "tatliq" or "khul" or "nafaqa" or "custody" or "custody-forfeiture" or "visitation")
            ? null
            : position switch
            {
                "wife" or "mother" or "other" => "f",
                "husband" or "father" => "m",
                _ => null,
            };

    private JsonObject DrawClaims(int s, IReadOnlyList<string> claims, StyleCard card, DateOnly petition, Force? force)
    {
        var shape = new Rng(Rng.Derive(s, 1));
        var roles = new JsonObject();
        var chosen = new Dictionary<string, RoleDef>(StringComparer.Ordinal);
        string? sex = null;
        foreach (var claim in claims)
        {
            var def = data.ClaimById[claim];
            // A later claim keeps the plaintiff's sex that the first one fixed (a wife asking for khul and custody is the
            // mother, not the father: version 1, 16 November 2026, 6 of the pilot's 500 scenarios).
            var offered = sex is null ? def.Roles : [.. def.Roles.Where(r => SexOf(claim, r.Position) is null || SexOf(claim, r.Position) == sex)];
            if (offered.Count == 0)
            {
                offered = def.Roles;
            }

            var role = offered[shape.Between(0, offered.Count - 1)];
            sex ??= SexOf(claim, role.Position);
            chosen[claim] = role;
            if (def.Roles.Count > 1)
            {
                roles[claim] = new JsonObject { ["relation"] = role.Relation, ["position"] = role.Position };
            }
        }

        // Events: every element of every claim, in the catalogue's order; a kind two claims share is one event.
        var events = new List<EventDraft>();
        var byKind = new Dictionary<string, EventDraft>(StringComparer.Ordinal);
        var index = 0;
        foreach (var claim in claims)
        {
            foreach (var e in data.ClaimById[claim].Elements)
            {
                var rng = new Rng(Rng.Derive(s, 100 + index++));
                var u = Enumerable.Range(0, 12).Select(_ => rng.Next()).ToArray();
                var polarity = force is not null && force.Claim == claim && force.Kind == e.Kind
                    ? force.Polarity
                    : DrawPolarity(e, u[0]);
                if (byKind.TryGetValue(e.Kind, out var shared))
                {
                    polarity = shared.Polarity;
                }

                var ev = new EventDraft { Element = e, Polarity = polarity, Actor = Actor(e, chosen[claim]), U = u };
                events.Add(ev);
                byKind.TryAdd(e.Kind, ev);
            }
        }

        // A claim type of several elements keeps at least one mentioned (its note says something of the claim besides the
        // request); a type of one element may leave it unmentioned, its request alone naming the claim. A kind two claims
        // share stays one event with one polarity.
        foreach (var claim in claims)
        {
            var own = events.Where(ev => ev.Element.Claim == claim).ToList();
            if (own.Count > 1 && own.All(ev => ev.Polarity == "notMentioned"))
            {
                var kind = own.First(ev => !(force is not null && force.Claim == claim && force.Kind == ev.Element.Kind)).Element.Kind;
                events.Where(ev => ev.Element.Kind == kind).ToList().ForEach(ev => ev.Polarity = "stated");
            }
        }

        // The manner of each event: how it is denied or disputed, a contest of a stated one, the concept to name it by.
        var synonyms = card.Parameters.GetValueOrDefault("terms") == "synonym";
        foreach (var ev in events)
        {
            var seeds = data.Seeds(ev.Element.Claim, ev.Element.Kind);
            var by = DisputedBy(seeds.DisputedBy, ev.Actor);
            ev.Concept = synonyms && seeds.Concepts.Count > 0 ? seeds.Concepts[(int)(ev.U[1] * seeds.Concepts.Count)] : null;
            if (ev.Polarity == "denied")
            {
                ev.How = seeds.Denied[(int)(ev.U[2] * seeds.Denied.Count)];
            }
            else if (ev.Polarity == "disputed")
            {
                ev.How = seeds.Disputed[(int)(ev.U[2] * seeds.Disputed.Count)];
                ev.DisputedBy = by;
            }
        }

        Dates(events, petition, s);
        var amounts = Amounts(claims, events, s);
        var slots = Slots(claims, events, s, petition);
        Contests(events);
        var requests = Requests(claims, amounts, s);
        var extras = Extras(claims, amounts, s);
        var cast = Cast(claims, chosen, slots, s);
        var distractors = Distractors(claims, s);

        var o = new JsonObject
        {
            ["claims"] = new JsonArray([.. claims.Select(c => (JsonNode)c)]),
            ["outsideKind"] = null,
        };
        if (roles.Count > 0)
        {
            o["roles"] = roles;
        }

        o["events"] = new JsonArray([.. events.Select(ToJson)]);
        o["slots"] = slots;
        o["requests"] = requests;
        o["extraValues"] = extras;
        o["cast"] = cast;
        o["distractors"] = distractors;
        o["petitionDate"] = Rng.Day(petition);
        var outcome = Outcome(claims, slots, requests, petition);
        if (outcome is not null)
        {
            o["outcome"] = outcome;
        }

        o["style"] = card.Id;
        return o;
    }

    private string DrawPolarity(ElementDef e, double u)
    {
        if (!data.DeniedDrawn.GetValueOrDefault(e.Claim + "/" + e.Kind, true))
        {
            // The element the claim rests on (its denial would end the claim): mostly stated, often disputed.
            return u < 0.74 ? "stated" : u < 0.88 ? "disputed" : "notMentioned";
        }

        return u < 0.42 ? "stated" : u < 0.54 ? "denied" : u < 0.66 ? "disputed" : "notMentioned";
    }

    private static string Actor(ElementDef e, RoleDef role) => e.Actor switch
    {
        null => "unknown",
        "plaintiff" => "plaintiff",
        "defendant" => "defendant",
        var position => position == role.Position ? "plaintiff" : "defendant",
    };

    private static string DisputedBy(string mode, string actor) => mode switch
    {
        "actor" => actor == "unknown" ? "defendant" : actor,
        "notActor" => actor == "defendant" ? "plaintiff" : "defendant",
        _ => "defendant",
    };

    /// <summary>
    /// Element k of n is dated at start + span × (k + its own fraction) / n: in the catalogue's order, and unmoved by the
    /// polarity of any other element. A date is given for a stated or disputed element that takes one, three times in four.
    /// </summary>
    private static void Dates(List<EventDraft> events, DateOnly petition, int s)
    {
        var rng = new Rng(Rng.Derive(s, 2));
        var span = rng.Between(90, 2400);
        var gap = rng.Between(10, 150);
        var start = petition.AddDays(-(span + gap));
        for (var k = 0; k < events.Count; k++)
        {
            var ev = events[k];
            if (ev.Element.HasDate && ev.Mentioned && ev.U[3] < 0.75)
            {
                ev.Date = start.AddDays((int)(span * (k + ev.U[4] * 0.9) / events.Count));
            }
        }

        // A kind two claims share keeps one date.
        foreach (var same in events.GroupBy(ev => ev.Element.Kind).Where(g => g.Count() > 1))
        {
            var first = same.First();
            foreach (var ev in same.Skip(1))
            {
                ev.Date = first.Date;
            }
        }
    }

    /// <summary>
    /// The sums of the case, drawn in a fixed order whatever the polarities (so that a group's members share them), then
    /// set on the events and kept by role for the requests: paid + owed = total when a part payment is stated.
    /// </summary>
    private Dictionary<string, long> Amounts(IReadOnlyList<string> claims, List<EventDraft> events, int s)
    {
        var rng = new Rng(Rng.Derive(s, 3));
        (long, long) Range(string name)
        {
            var r = data.Tables["amounts"]![name]!.AsArray();
            return ((long)r[0]!, (long)r[1]!);
        }

        long Draw(string name)
        {
            var (lo, hi) = Range(name);
            return rng.Amount(lo, hi);
        }

        var first = claims[0];
        var range = data.Tables["amounts"]![first] is not null ? first : "damages";
        var total = Draw(range);
        var paidShare = 0.1 + rng.Next() * 0.5;
        var months = rng.Between(2, 24);
        var damages = Draw("damages");
        var monthlyRent = Draw(claims.Contains("commercial-lease-eviction") ? "commercial-rent-monthly" : "unpaid-rent-monthly");
        var wage = Draw("wage-monthly");
        var maintenance = Draw("maintenance-monthly");
        var housing = Draw("housing-monthly");
        var khul = Draw("khul");
        var restitutionFull = rng.Next() < 0.5;

        EventDraft? Get(string kind) => events.FirstOrDefault(ev => ev.Element.Kind == kind);
        var payment = Get("payment_made");
        var paid = RoundLike(total * paidShare);
        var a = new Dictionary<string, long>(StringComparer.Ordinal) { ["damages"] = damages };
        if (claims.Contains("unpaid-rent") || claims.Contains("commercial-lease-eviction"))
        {
            a["contract_concluded"] = monthlyRent;
            a["rent_unpaid"] = monthlyRent * months;
            a["payment"] = monthlyRent * months;
        }
        else if (claims.Contains("unpaid-wages") || claims.Contains("abusive-dismissal"))
        {
            a["wages_unpaid"] = wage * months;
            a["wages"] = wage * months;
            a["wage-monthly"] = wage;
        }
        else
        {
            var owed = payment is { Polarity: "stated" } ? total - paid : total;
            foreach (var kind in new[] { "money_lent", "debt_acknowledged", "contract_concluded", "goods_delivered" })
            {
                a[kind] = total;
            }

            a["payment_made"] = paid;
            foreach (var kind in new[] { "debt_unpaid", "price_unpaid", "payment" })
            {
                a[kind] = owed;
            }

            // In rescission, annulment and non-delivery the plaintiff paid: restitution is what he paid.
            var plaintiffPaid = claims.Any(c => c is "contract-rescission" or "sale-annulment-fraud" or "sale-non-delivery");
            if (plaintiffPaid)
            {
                a["payment_made"] = restitutionFull && claims.Contains("sale-annulment-fraud") ? total : paid;
                a["restitution"] = payment is { Polarity: "stated" or "disputed" } ? a["payment_made"] : total;
            }
        }

        a["maintenance"] = maintenance;
        a["housing"] = housing;
        a["khul"] = khul;
        foreach (var ev in events)
        {
            if (ev.Element.HasAmount && ev.Mentioned && ev.U[5] < 0.85 && a.TryGetValue(ev.Element.Kind, out var value))
            {
                ev.Amount = value;
            }
        }

        return a;
    }

    private static long RoundLike(double x)
    {
        var step = (long)Math.Pow(10, Math.Max(0, Math.Floor(Math.Log10(Math.Max(x, 1))) - 2));
        return Math.Max(step, (long)x / step * step);
    }

    /// <summary>A contest of a stated event (the guide's cases 5 and 31): of its amount, or of its cause; about one in twelve.</summary>
    private void Contests(List<EventDraft> events)
    {
        foreach (var ev in events.Where(ev => ev.Polarity == "stated" && ev.U[6] < 0.08 && ev.Form is null))
        {
            var seeds = data.Seeds(ev.Element.Claim, ev.Element.Kind);
            var by = DisputedBy(seeds.DisputedBy, ev.Actor);
            if (ev.Amount is long amount && (seeds.Cause.Count == 0 || ev.U[7] < 0.5))
            {
                ev.Contest = new JsonObject
                {
                    ["of"] = "amount", ["by"] = by,
                    ["how"] = "the other side admits it but gives a smaller figure",
                    ["amount"] = RoundLike(amount * (0.3 + ev.U[8] * 0.5)),
                };
            }
            else if (seeds.Cause.Count > 0)
            {
                ev.Contest = new JsonObject
                {
                    ["of"] = "cause", ["by"] = by, ["how"] = seeds.Cause[(int)(ev.U[8] * seeds.Cause.Count)],
                };
            }
        }
    }

    /// <summary>
    /// The eight slots read from the note, by the guide's section 6 applied in reverse: the notice slots from the notice
    /// event (a stated demand in a type whose element includes «مطالبة» may be informal: the slot is then unknown), the two
    /// labour slots from the record, the contract's date from its event, and the two slots no event carries drawn apart.
    /// </summary>
    private JsonObject Slots(IReadOnlyList<string> claims, List<EventDraft> events, int s, DateOnly petition)
    {
        var rng = new Rng(Rng.Derive(s, 4));
        var formalRoll = rng.Next();
        var meansRoll = rng.Next();
        var deliveryRoll = rng.Next();
        var sameDayRoll = rng.Next();
        var deliveryGap = rng.Between(1, 30);
        var publicRoll = rng.Next();
        var propertyRoll = rng.Next();

        var slots = new JsonObject();
        foreach (var slot in data.Slots.Where(sl => claims.Any(sl.AskedForClaim)))
        {
            slots[slot.Id] = "unknown";
        }

        // The notice.
        var notice = events.FirstOrDefault(ev => ev.Element.Kind == "notice_served");
        var eviction = events.FirstOrDefault(ev => ev.Element.Kind == "eviction_notice");
        string Means() => meansRoll < 0.45 ? "bailiff" : meansRoll < 0.7 ? "registered_letter" : meansRoll < 0.8 ? "other" : "unknown";
        void Formal(EventDraft ev, string means)
        {
            slots["notice_served"] = "yes";
            slots["notice_means"] = means;
            if (ev.Date is DateOnly d)
            {
                slots["notice_date"] = Rng.Day(d);
            }

            ev.Form = (string)data.Tables["noticeForm"]![means]!;
        }

        if (notice is not null)
        {
            var mayBeInformal = notice.Element.Label.Contains("مطالبة", StringComparison.Ordinal);
            switch (notice.Polarity)
            {
                case "stated" when mayBeInformal && formalRoll >= 0.6:
                    notice.Form = "only an informal demand, never called a notice: " +
                                  SamplerData.Strings(data.Tables["informalDemand"])[(int)(notice.U[9] * 4) % 4];
                    break;
                case "stated" or "disputed":
                    Formal(notice, Means());
                    break;
                case "denied":
                    slots["notice_served"] = "no";
                    break;
            }
        }
        else if (eviction is not null)
        {
            if (eviction.Mentioned)
            {
                Formal(eviction, "bailiff");
            }
            else if (eviction.Polarity == "denied")
            {
                slots["notice_served"] = "no";
            }
        }

        // The record of non-conciliation (the two labour types).
        var record = events.FirstOrDefault(ev => ev.Element.Kind == "conciliation_failed");
        if (record is not null && slots.ContainsKey("nonconciliation_record"))
        {
            if (record.Mentioned)
            {
                slots["nonconciliation_record"] = "yes";
                if (deliveryRoll < 0.65)
                {
                    var from = record.Date ?? petition.AddDays(-(deliveryGap + 20));
                    var delivered = record.Date is not null && sameDayRoll < 0.15 ? from : from.AddDays(deliveryGap);
                    slots["nonconciliation_record_delivered_on"] = Rng.Day(delivered < petition ? delivered : petition.AddDays(-1));
                }
            }
            else if (record.Polarity == "denied")
            {
                slots["nonconciliation_record"] = "no";
            }
        }

        // The contract's date: only a claim type with a contract_concluded element, from that event.
        var contract = events.FirstOrDefault(ev => ev.Element.Kind == "contract_concluded");
        if (contract?.Date is DateOnly cd && contract.Mentioned)
        {
            slots["contract_date"] = Rng.Day(cd);
        }

        // Two slots no event carries.
        var family = claims.All(c => Area(c) == "family");
        var bodyPlaintiff = claims.All(c => c is "unpaid-rent" or "eviction-occupier-without-title" or "commercial-debt"
                                                or "contract-damages" or "contract-rescission");
        slots["public_body_party"] = family ? "no"
            : publicRoll < 0.08 && bodyPlaintiff ? "yes"
            : publicRoll < 0.16 ? "unknown"
            : "no";
        var property = claims.Any(c => SamplerData.Strings(data.Tables["propertyClaims"]).Contains(c));
        slots["property_registered"] = !property ? "unknown" : propertyRoll < 0.25 ? "yes" : propertyRoll < 0.45 ? "no" : "unknown";
        return slots;
    }

    private JsonArray Requests(IReadOnlyList<string> claims, Dictionary<string, long> amounts, int s)
    {
        var rng = new Rng(Rng.Derive(s, 5));
        var kinds = new List<RequestDef>();
        foreach (var claim in claims)
        {
            var list = data.ClaimById[claim].Requests;
            for (var i = 0; i < list.Count; i++)
            {
                var keep = rng.Next() < 0.45;
                if ((i == 0 || keep) && kinds.All(k => k.Kind != list[i].Kind))
                {
                    kinds.Add(list[i]);
                }
            }
        }

        var array = new JsonArray();
        foreach (var r in kinds)
        {
            var o = new JsonObject { ["kind"] = r.Kind };
            if (r.HasAmount && rng.Next() < 0.8 && amounts.TryGetValue(r.Kind, out var value))
            {
                o["amount"] = value;
            }

            array.Add(o);
        }

        return array;
    }

    private JsonArray Extras(IReadOnlyList<string> claims, Dictionary<string, long> amounts, int s)
    {
        var rng = new Rng(Rng.Derive(s, 6));
        var wageRoll = rng.Next();
        var unrelatedRoll = rng.Next();
        var optionalRoll = rng.Next();
        var what = rng.Pick(SamplerData.Strings(data.Tables["unrelatedAmounts"]));
        var unrelated = rng.Amount(5000, 900000);
        var array = new JsonArray();
        if (amounts.TryGetValue("wage-monthly", out var wage) && wageRoll < 0.6)
        {
            array.Add(new JsonObject { ["kind"] = "amount", ["value"] = wage, ["what"] = "the monthly wage", ["optional"] = true });
        }

        if (unrelatedRoll < 1.0 / 3 && array.All(x => (long)x!["value"]! != unrelated))
        {
            array.Add(new JsonObject { ["kind"] = "amount", ["value"] = unrelated, ["what"] = what, ["optional"] = optionalRoll < 0.5 });
        }

        return array;
    }

    private JsonArray Distractors(IReadOnlyList<string> claims, int s)
    {
        var rng = new Rng(Rng.Derive(s, 7));
        var pool = SamplerData.Strings(data.Tables["distractors"]!["any"]).Concat(SamplerData.Strings(data.Tables["distractors"]![Area(claims[0])])).ToList();
        var count = rng.Between(0, 2);
        var array = new JsonArray();
        for (var i = 0; i < count; i++)
        {
            var line = rng.Pick(pool);
            if (array.All(x => (string)x! != line))
            {
                array.Add(line);
            }
        }

        return array;
    }

    private string Area(string claim) =>
        data.Tables["areas"]!.AsObject().First(a => SamplerData.Strings(a.Value).Contains(claim)).Key;

    // --- the cast ----------------------------------------------------------------------------------------------------

    private JsonObject Cast(IReadOnlyList<string> claims, Dictionary<string, RoleDef> roles, JsonObject slots, int s)
    {
        var rng = new Rng(Rng.Derive(s, 8));
        var place = rng.Pick(SamplerData.Strings(data.Cast["wilayas"]));
        var claim = claims[0];
        var role = roles[claim];
        var companyRoll = rng.Next();
        var genderRoll = rng.Next();
        var publicLine = rng.Pick(SamplerData.Strings(data.Tables["publicBodies"])).Replace("{place}", place, StringComparison.Ordinal);
        var unclearLine = rng.Pick(SamplerData.Strings(data.Tables["publicBodyUnclear"]));
        var form = rng.Pick(SamplerData.Strings(data.Cast["companyForms"]));
        var activity = rng.Pick(SamplerData.Strings(data.Cast["activities"]));
        var root = CompanyRoot(rng);
        var root2 = CompanyRoot(rng);

        JsonObject Person(string gender) => new() { ["kind"] = "person", ["name"] = FullName(rng, gender), ["gender"] = gender };
        JsonObject Company(string r) => new() { ["kind"] = "company", ["name"] = r, ["describe"] = $"{form} in {activity}" };

        JsonObject plaintiff, defendant;
        switch (claim)
        {
            case "divorce-by-husband" or "tatliq" or "khul" or "nafaqa" or "custody" or "custody-forfeiture" or "visitation":
            {
                var p = role.Position is "wife" or "mother" or "other" ? "f" : "m";
                plaintiff = Person(p);
                defendant = Person(role.Position == "other" ? "f" : p == "f" ? "m" : "f");
                if (role.Position == "other")
                {
                    plaintiff["describe"] = "the children's maternal grandmother";
                }

                break;
            }

            case "commercial-debt":
                plaintiff = Company(root);
                defendant = companyRoll < 0.7 ? Company(root2) : Person(genderRoll < 0.8 ? "m" : "f");
                break;
            case "unpaid-wages" or "abusive-dismissal":
                plaintiff = Person(genderRoll < 0.7 ? "m" : "f");
                defendant = companyRoll < 0.75 ? Company(root) : Person("m");
                break;
            default:
                plaintiff = companyRoll < 0.2 ? Company(root) : Person(genderRoll < 0.65 ? "m" : "f");
                defendant = companyRoll is >= 0.2 and < 0.55 ? Company(root2) : Person(genderRoll < 0.5 ? "f" : "m");
                break;
        }

        switch ((string?)slots["public_body_party"])
        {
            case "yes":
                plaintiff = new JsonObject { ["kind"] = "publicBody", ["describe"] = publicLine };
                break;
            case "unknown":
                defendant = new JsonObject { ["kind"] = "unclearBody", ["name"] = root2, ["describe"] = unclearLine };
                break;
        }

        var cast = new JsonObject { ["plaintiff"] = plaintiff, ["defendant"] = defendant, ["place"] = place };
        if (claims.Any(c => c is "custody" or "custody-forfeiture" or "visitation" or "nafaqa" or "tatliq" or "khul"))
        {
            var count = rng.Between(1, 3);
            cast["children"] = new JsonArray([.. Enumerable.Range(0, count).Select(_ =>
                (JsonNode)rng.Pick(SamplerData.Strings(data.Cast[rng.Next() < 0.5 ? "maleGiven" : "femaleGiven"])))]);
        }

        return cast;
    }

    private string FullName(Rng rng, string gender)
    {
        var refused = SamplerData.Strings(data.Cast["refused"]);
        while (true)
        {
            var name = rng.Pick(SamplerData.Strings(data.Cast[gender == "f" ? "femaleGiven" : "maleGiven"])) + " " + rng.Pick(familyNames);
            if (!refused.Contains(name))
            {
                return name;
            }
        }
    }

    private string CompanyRoot(Rng rng)
    {
        var refused = SamplerData.Strings(data.Cast["refused"]);
        while (true)
        {
            var root = rng.Pick(SamplerData.Strings(data.Cast["companyFirst"])) + rng.Pick(SamplerData.Strings(data.Cast["companySecond"]));
            if (!refused.Contains(root))
            {
                return root;
            }
        }
    }

    /// <summary>Every full name and company root the cast lists can produce (for the refusal test).</summary>
    public IEnumerable<string> AllNames()
    {
        foreach (var g in SamplerData.Strings(data.Cast["maleGiven"]).Concat(SamplerData.Strings(data.Cast["femaleGiven"])))
        {
            foreach (var f in familyNames)
            {
                yield return g + " " + f;
            }
        }

        foreach (var a in SamplerData.Strings(data.Cast["companyFirst"]))
        {
            foreach (var b in SamplerData.Strings(data.Cast["companySecond"]))
            {
                yield return a + b;
            }
        }
    }

    // --- outside the catalogue ---------------------------------------------------------------------------------------

    /// <summary>
    /// An `outside` scenario: in about six of ten a near-miss built from a real type (its stated story, the values of that
    /// story as extra values, and the one telling fact), otherwise the telling fact alone. No event and no slot is labelled.
    /// </summary>
    private (JsonObject, string?) DrawOutside(int s, Rng shape, StyleCard card, DateOnly petition)
    {
        var table = data.Tables["nearMiss"]!.AsObject();
        var kinds = table.Select(k => k.Key).ToList();
        var kind = kinds[shape.Between(0, kinds.Count - 1)];
        var row = table[kind]!.AsObject();
        var nearMiss = shape.Next() < 0.6;
        var near = Near(row["near"]!);
        var resembled = near[shape.Between(0, near.Count - 1)];
        var place = SamplerData.Strings(data.Cast["wilayas"])[shape.Between(0, SamplerData.Strings(data.Cast["wilayas"]).Count - 1)];
        var fact = SamplerData.Strings(row["facts"])[shape.Between(0, SamplerData.Strings(row["facts"]).Count - 1)];
        var requestKind = SamplerData.Strings(row["requests"])[shape.Between(0, SamplerData.Strings(row["requests"]).Count - 1)];

        var o = new JsonObject { ["claims"] = new JsonArray("outside"), ["outsideKind"] = kind };
        var extras = new JsonArray();
        long? requestAmount = null;
        JsonObject cast;
        if (nearMiss)
        {
            var story = DrawClaims(s, [resembled], card, petition, null);
            var lines = new JsonArray();
            foreach (var ev in story["events"]!.AsArray().Where(e => (string)e!["polarity"]! == "stated"))
            {
                var seeds = data.Seeds(resembled, (string)ev!["kind"]!);
                var concept = seeds.Concepts.Count > 0 ? seeds.Concepts[0] : (string)ev["kind"]!;
                lines.Add(concept);
                if (ev["date"] is JsonNode d)
                {
                    extras.Add(new JsonObject { ["kind"] = "date", ["value"] = (string)d!, ["what"] = $"the date of {concept}", ["optional"] = false });
                }

                if (ev["amount"] is JsonNode a && extras.All(x => (string?)x!["what"] != $"the sum of {concept}"))
                {
                    extras.Add(new JsonObject { ["kind"] = "amount", ["value"] = (long)a!, ["what"] = $"the sum of {concept}", ["optional"] = false });
                    requestAmount ??= (long)a;
                }
            }

            o["resembles"] = resembled;
            o["story"] = lines;
            cast = story["cast"]!.AsObject().DeepClone().AsObject();
        }
        else
        {
            var rng = new Rng(Rng.Derive(s, 8));
            cast = new JsonObject
            {
                ["plaintiff"] = new JsonObject { ["kind"] = "person", ["name"] = FullName(rng, "m"), ["gender"] = "m" },
                ["defendant"] = new JsonObject { ["kind"] = "person", ["name"] = FullName(rng, "f"), ["gender"] = "f" },
                ["place"] = place,
            };
        }

        if (kind == "administrative")
        {
            cast["defendant"] = new JsonObject { ["kind"] = "publicBody", ["describe"] = fact.Replace("{place}", (string)cast["place"]!, StringComparison.Ordinal) };
        }

        o["outsideFact"] = fact;
        o["slots"] = new JsonObject([.. data.Slots.Where(sl => sl.AskedFor.Contains("*"))
            .Select(sl => new KeyValuePair<string, JsonNode?>(sl.Id, "unknown"))]);
        var request = new JsonObject { ["kind"] = requestKind };
        if (requestKind is "payment" or "damages" && requestAmount is long ra)
        {
            request["amount"] = ra;
        }

        o["requests"] = new JsonArray(request);
        o["extraValues"] = extras;
        o["cast"] = cast;
        o["distractors"] = new JsonArray();
        o["petitionDate"] = Rng.Day(petition);
        o["style"] = card.Id;
        return (o, null);
    }

    private List<string> Near(JsonNode near)
    {
        if (near is JsonArray list)
        {
            return [.. list.Select(x => (string)x!)];
        }

        var text = (string)near!;
        return text == "any"
            ? [.. data.Claims.Select(c => c.Id)]
            : [.. text.Split('+').SelectMany(area => SamplerData.Strings(data.Tables["areas"]![area]))];
    }

    // --- coherence and outcome ---------------------------------------------------------------------------------------

    /// <summary>The scenario's own consistency (the guide's slot table in reverse, dates before the petition); null when coherent.</summary>
    private static string? Check(JsonObject o)
    {
        var slots = o["slots"]!.AsObject();
        var petition = (string)o["petitionDate"]!;
        string? Slot(string id) => (string?)slots[id];
        if (Slot("notice_served") != "yes" && (Slot("notice_date") is not null and not "unknown" ||
                                              Slot("notice_means") is not null and not "unknown"))
        {
            return "notice date or means without a formal notice";
        }

        if (Slot("nonconciliation_record") != "yes" && Slot("nonconciliation_record_delivered_on") is not null and not "unknown")
        {
            return "delivery date without the record";
        }

        var record = o["events"]!.AsArray().FirstOrDefault(e => (string)e!["kind"]! == "conciliation_failed")?["date"];
        if (record is not null && Slot("nonconciliation_record_delivered_on") is string delivered && delivered != "unknown" &&
            string.CompareOrdinal(delivered, (string)record!) < 0)
        {
            return "record delivered before it was drawn up";
        }

        foreach (var e in o["events"]!.AsArray())
        {
            if (e!["date"] is JsonNode d && string.CompareOrdinal((string)d!, petition) >= 0)
            {
                return "an event dated on or after the petition";
            }

            if (e["amount"] is JsonNode a && (long)a! <= 0)
            {
                return "an amount that is not positive";
            }
        }

        return null;
    }

    /// <summary>The rule evaluator on the final records that apply (version 1: formal-notice at first); null when none applies.</summary>
    private JsonObject? Outcome(IReadOnlyList<string> claims, JsonObject slots, JsonArray requests, DateOnly petition)
    {
        var records = finals.Where(r => r.AppliesTo.Contains("*") || r.AppliesTo.Any(claims.Contains)).ToList();
        if (records.Count == 0)
        {
            return null;
        }

        var values = slots.Where(kv => (string?)kv.Value is not null and not "unknown")
            .Select(kv => new SlotValue { Slot = kv.Key, Value = (string)kv.Value! }).ToList();
        var result = evaluator.Evaluate(records, new CaseSlots
        {
            ClaimIds = claims,
            Values = values,
            Requests = [.. requests.Select(r => (string)r!["kind"]!)],
        }, petition);
        return new JsonObject
        {
            ["findings"] = new JsonArray([.. result.Findings.Select(f => (JsonNode)new JsonObject
            {
                ["ref"] = f.Ref,
                ["effect"] = JsonNamingPolicy.CamelCase.ConvertName(f.DeclaredEffect.ToString()),
            })]),
            ["questions"] = new JsonArray([.. result.Questions.Select(q => (JsonNode)q.Slot)]),
        };
    }

    private static JsonNode ToJson(EventDraft ev)
    {
        var o = new JsonObject
        {
            ["claim"] = ev.Element.Claim,
            ["kind"] = ev.Element.Kind,
            ["polarity"] = ev.Polarity,
            ["actor"] = ev.Actor,
        };
        if (ev.DisputedBy is not null)
        {
            o["disputedBy"] = ev.DisputedBy;
        }

        if (ev.Date is DateOnly d)
        {
            o["date"] = Rng.Day(d);
        }

        if (ev.Amount is long a)
        {
            o["amount"] = a;
        }

        if (ev.How is not null)
        {
            o["how"] = ev.How;
        }

        if (ev.Form is not null && ev.Mentioned)
        {
            o["form"] = ev.Form;
        }

        if (ev.Contest is not null)
        {
            o["contest"] = ev.Contest;
        }

        if (ev.Concept is not null && ev.Polarity != "notMentioned")
        {
            o["concept"] = ev.Concept;
        }

        return o;
    }

    /// <summary>The scenario's keys in the order of specs/scenario-sampler.md, "A scenario", with its set.</summary>
    private static JsonObject Ordered(JsonObject o, string set)
    {
        var keys = new[]
        {
            "id", "set", "family", "group", "claims", "outsideKind", "resembles", "outsideFact", "story", "roles", "events",
            "slots", "requests", "extraValues", "cast", "distractors", "petitionDate", "outcome", "style", "difficulty",
        };
        o["set"] = set;
        o["difficulty"] ??= null;
        var ordered = new JsonObject();
        foreach (var key in keys.Where(o.ContainsKey))
        {
            ordered[key] = o[key]?.DeepClone();
        }

        foreach (var (key, value) in o.Where(kv => !keys.Contains(kv.Key)))
        {
            ordered[key] = value?.DeepClone();
        }

        return ordered;
    }

    public static string Format(long value) => value.ToString(CultureInfo.InvariantCulture);
}
