using System.Globalization;
using System.Text.Json;
using DzLegal.CoreLogic.Contracts;
using DzLegal.Domain;

namespace DzLegal.CoreLogic.Rules;

/// <summary>
/// The one rule evaluator (specs/rule-evaluator.md). Pure: no file, no clock, no network, no culture; the same inputs give
/// the same outcome. The worked examples, the boundary cases, the scenario sampler, the scorer and later the app all call it.
/// </summary>
public sealed class RuleEvaluator : IRuleEvaluator
{
    /// <summary>The band, in days before or after the last day, inside which a deadline warns when its record gives none.</summary>
    public const int DefaultWarnBandDays = 15;

    /// <summary>The <see cref="RuleFinding.Ref"/> of the finding that says the written section differs from the proposed one.</summary>
    public const string SectionRef = "section";

    public RuleOutcome Evaluate(IReadOnlyList<RuleRecord> records, CaseSlots slots, DateOnly petitionDate)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(slots);

        var reader = new CaseSlotReader(slots);
        var findings = new List<RuleFinding>();
        var questions = new QuestionList();
        SectionProposal? section = null;

        // The caller passes the records of the confirmed claim types only; every record given is run.
        foreach (var record in records)
        {
            var run = new RecordRun(record, reader, slots.Section, petitionDate, findings, questions);

            // The order of the questions: records as given; inside a record exits, section, prior steps, deadlines, boundaries.
            foreach (var exit in record.Court?.Exits ?? [])
            {
                run.Exit(exit);
            }

            section ??= run.Section();
            foreach (var step in record.PriorSteps)
            {
                run.PriorStep(step);
            }

            foreach (var deadline in record.Deadlines)
            {
                run.Deadline(deadline);
            }

            foreach (var boundary in record.Boundaries)
            {
                run.Boundary(boundary);
            }

            foreach (var document in record.Documents)
            {
                run.Document(document);
            }
        }

        return new RuleOutcome { Findings = findings, Questions = questions.ToList(), Section = section };
    }

    /// <summary>An effect as records write it: "blockCleanExport", "warn", "ask", "none".</summary>
    public static string Name(FindingEffect effect) => JsonNamingPolicy.CamelCase.ConvertName(effect.ToString());

    private sealed class QuestionList
    {
        private readonly List<(string Slot, List<string> NeededBy)> questions = [];

        public void Add(IEnumerable<string> slots, string neededBy)
        {
            foreach (var slot in slots)
            {
                var index = questions.FindIndex(question => question.Slot == slot);
                if (index < 0)
                {
                    questions.Add((slot, [neededBy]));
                }
                else if (!questions[index].NeededBy.Contains(neededBy))
                {
                    questions[index].NeededBy.Add(neededBy);
                }
            }
        }

        public List<SlotQuestion> ToList() =>
            [.. questions.Select(question => new SlotQuestion { Slot = question.Slot, NeededBy = [.. question.NeededBy] })];
    }

    /// <summary>One record applied to the case: one method per kind of item, each producing at most one finding.</summary>
    private sealed class RecordRun(
        RuleRecord record,
        CaseSlotReader slots,
        CourtSection? writtenSection,
        DateOnly petitionDate,
        List<RuleFinding> findings,
        QuestionList questions)
    {
        private readonly Dictionary<string, Cite> cites = CitesOf(record);

        public void Exit(CourtExit exit)
        {
            var used = new List<string>();
            var when = Conditions.Evaluate(exit.When, slots, used);
            var (declared, branch) = When(when, exit.Id, "when", FindingEffect.Warn);
            Add(FindingKind.Exit, exit.Id, declared, exit.Grading, exit.Message, exit.Basis, used, branch);
        }

        public SectionProposal? Section()
        {
            if (record.Court?.Section is not { } rule)
            {
                return null;
            }

            // The first alternative that is true wins; an unknown one is skipped and its slots asked, since it could still win.
            var used = new List<string>();
            SectionAlternative? chosen = null;
            foreach (var alternative in rule.Alternatives)
            {
                var result = Conditions.Evaluate(alternative.When, slots, used);
                if (result.Value == Truth.True)
                {
                    chosen = alternative;
                    break;
                }

                if (result.Value == Truth.Unknown)
                {
                    questions.Add(result.Ask, SectionRef);
                }
            }

            var proposed = chosen?.Value ?? rule.Value;
            var basis = chosen?.Basis ?? rule.Basis;
            if (writtenSection is { } written && written != proposed)
            {
                var branch = $"proposed {Word(proposed)}, the petition says {Word(written)}";
                Add(FindingKind.CourtSection, SectionRef, FindingEffect.Warn, rule.Grading, rule.Message, basis, used, branch);
            }

            return new SectionProposal { Section = proposed, Grade = GradeTable.GradeOf(rule.Grading), Basis = Cites(basis) };
        }

        public void PriorStep(PriorStep step)
        {
            if (step.Slots.Count == 0)
            {
                throw new RuleRecordException($"{record.Id}.{step.Id}: a prior step names its own slot first in slots, and this one has none.");
            }

            var used = new List<string>();
            void Finish(FindingEffect declared, string branch) =>
                Add(FindingKind.PriorStep, step.Id, declared, step.Grading, step.Message, step.Basis, used, branch);

            if (step.RequiredWhen is { } requiredWhen)
            {
                var required = Conditions.Evaluate(requiredWhen, slots, used);
                if (required.Value == Truth.False)
                {
                    Finish(FindingEffect.None, "requiredWhen false");
                    return;
                }

                if (required.Value == Truth.Unknown)
                {
                    questions.Add(required.Ask, step.Id);
                    Finish(FindingEffect.Ask, $"requiredWhen unknown, asks {string.Join(", ", required.Ask)}");
                    return;
                }
            }

            var own = step.Slots[0];
            used.Add(own);
            switch (slots.Value(own))
            {
                case "yes":
                    Finish(FindingEffect.None, $"{own} yes, the step was done");
                    return;
                case null:
                    // Whether ask or warn, the step's slot is asked; the exemptions are not, before the lawyer says the step was not done.
                    questions.Add([own], step.Id);
                    Finish(step.EffectWhenUnknown, $"{own} unknown, asks {own}");
                    return;
                case "no":
                    break;
                case var other:
                    throw new ArgumentException($"The slot {own} of the prior step {step.Id} is answered \"{other}\"; it must be yes, no or unknown.");
            }

            var exemptions = step.NotRequiredWhen.Select(exemption => Conditions.Evaluate(exemption.When, slots, used)).ToList();
            var exempt = exemptions.FindIndex(result => result.Value == Truth.True);
            if (exempt >= 0)
            {
                Finish(FindingEffect.None, $"{own} no, exempt by notRequiredWhen[{exempt}]");
                return;
            }

            var unknown = exemptions.Where(result => result.Value == Truth.Unknown).SelectMany(result => result.Ask).Distinct(StringComparer.Ordinal).ToList();
            if (unknown.Count > 0)
            {
                questions.Add(unknown, step.Id);
                Finish(FindingEffect.Ask, $"{own} no, exemptions unknown, asks {string.Join(", ", unknown)}");
                return;
            }

            Finish(step.EffectWhenMissing, $"{own} no, no exemption applies");
        }

        public void Deadline(Deadline deadline)
        {
            var used = new List<string>();
            void Finish(FindingEffect declared, string branch, DateOnly? lastDay = null, int? daysLeft = null) =>
                Add(FindingKind.Deadline, deadline.Id, declared, deadline.Grading, deadline.Message, deadline.Basis, used, branch, lastDay, daysLeft);

            if (deadline.When is { } whenCondition)
            {
                var when = Conditions.Evaluate(whenCondition, slots, used);
                if (when.Value != Truth.True)
                {
                    var (declared, branch) = When(when, deadline.Id, "when", FindingEffect.None);
                    Finish(declared, branch);
                    return;
                }
            }

            var startSlot = deadline.StartsFrom.Slot;
            used.Add(startSlot);
            if (slots.Value(startSlot) is null)
            {
                questions.Add([startSlot], deadline.Id);
                Finish(FindingEffect.Ask, $"start {startSlot} unknown, asks {startSlot}");
                return;
            }

            var start = slots.Date(startSlot);
            var lastDay = Periods.LastDay(start, deadline.Period, deadline.Counting);
            var daysLeft = lastDay.DayNumber - petitionDate.DayNumber;
            var band = deadline.WarnBandDays ?? DefaultWarnBandDays;

            // Inside the band the declared effect is a warning, whatever the counting gives: holidays and the act that started
            // the period are beyond what the app can see.
            var inBand = Math.Abs(daysLeft) <= band;
            var effect = inBand ? FindingEffect.Warn : daysLeft < 0 ? deadline.EffectWhenExpired : FindingEffect.None;
            var days = daysLeft switch
            {
                > 0 => string.Create(CultureInfo.InvariantCulture, $"{daysLeft} days left"),
                0 => "on the last day",
                _ => string.Create(CultureInfo.InvariantCulture, $"{-daysLeft} days late"),
            };
            var branchText = string.Create(
                CultureInfo.InvariantCulture,
                $"start {Dates.Write(start)}, last day {Dates.Write(lastDay)}, petition {Dates.Write(petitionDate)}, {days}, {(inBand ? "inside" : "outside")} band {band}");
            Finish(effect, branchText, lastDay, daysLeft);
        }

        public void Boundary(Boundary boundary)
        {
            // Without a condition, a boundary is documentation for the classifier and the sampler: no finding.
            if (boundary.When is not { } whenCondition)
            {
                return;
            }

            var used = new List<string>();
            var when = Conditions.Evaluate(whenCondition, slots, used);
            var (declared, branch) = When(when, boundary.Id, "when", FindingEffect.Warn);
            Add(FindingKind.Boundary, boundary.Id, declared, boundary.Grading, boundary.Message, boundary.Basis, used, branch);
        }

        public void Document(RequiredDocument document)
        {
            var used = new List<string>();
            var branch = "listed";
            if (document.RequiredWhen is { } requiredWhen)
            {
                var required = Conditions.Evaluate(requiredWhen, slots, used);
                if (required.Value == Truth.False)
                {
                    return;
                }

                // Whether a document is attached is the app's own form check; an unknown condition lists it and asks nothing.
                branch = required.Value == Truth.True ? "requiredWhen true, listed" : "requiredWhen unknown, listed";
            }

            Add(FindingKind.Document, document.Id, FindingEffect.None, document.Grading, document.Name, document.Basis, used, branch);
        }

        // A condition that decides an item: true gives the item's effect, false nothing, unknown a question.
        private (FindingEffect Declared, string Branch) When(ConditionResult result, string item, string name, FindingEffect whenTrue)
        {
            switch (result.Value)
            {
                case Truth.True:
                    return (whenTrue, $"{name} true");
                case Truth.False:
                    return (FindingEffect.None, $"{name} false");
                default:
                    questions.Add(result.Ask, item);
                    return (FindingEffect.Ask, $"{name} unknown, asks {string.Join(", ", result.Ask)}");
            }
        }

        private void Add(
            FindingKind kind,
            string reference,
            FindingEffect declared,
            Grading? grading,
            LocalizedText message,
            IReadOnlyList<string> basis,
            List<string> used,
            string branch,
            DateOnly? lastDay = null,
            int? daysLeft = null)
        {
            var grade = GradeTable.GradeOf(grading);
            var proposed = used.Any(slots.IsProposed);
            var (effect, toCheck) = GradeTable.Apply(declared, grade, proposed);
            var gradeEffect = GradeTable.Apply(declared, grade, usesProposedSlot: false).Effect;

            var trace = $"{reference}: {branch} -> {Name(declared)}; grade {grade} -> {Name(gradeEffect)}";
            if (gradeEffect != effect)
            {
                trace += $"; a proposed slot -> {Name(effect)}";
            }

            if (toCheck)
            {
                trace += ", to check";
            }

            findings.Add(new RuleFinding
            {
                Ref = reference,
                RecordId = record.Id,
                Kind = kind,
                DeclaredEffect = declared,
                Effect = effect,
                Grade = grade,
                ToCheck = toCheck,
                Message = message,
                Basis = Cites(basis),
                SlotsUsed = [.. used.Distinct(StringComparer.Ordinal)],
                LastDay = lastDay,
                DaysLeft = daysLeft,
                Trace = trace,
            });
        }

        // An id that resolves to no statement is skipped here: rules check refuses such a record, and Basis is only shown.
        private List<Cite> Cites(IReadOnlyList<string> ids) => [.. ids.Where(cites.ContainsKey).Select(id => cites[id])];

        private static Dictionary<string, Cite> CitesOf(RuleRecord record)
        {
            var cites = new Dictionary<string, Cite>(StringComparer.Ordinal);
            foreach (var statement in record.Basis)
            {
                cites.TryAdd(statement.Id, statement.Cite);
            }

            return cites;
        }

        private static string Word(CourtSection section) => JsonNamingPolicy.CamelCase.ConvertName(section.ToString());
    }
}
