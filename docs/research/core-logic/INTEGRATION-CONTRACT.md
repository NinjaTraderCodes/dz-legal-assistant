# Integration contract

**For the reviewer who integrates this work into the app, and for the executor, who must not drift from it.** Everything here exists as compiled code in `research/core-logic/src/DzLegal.CoreLogic.Contracts` and is exercised by the frame's tests. If this file and the code disagree, the code is right and this file has a defect: fix the file.

The research changes nothing in the app. This contract says where each product would plug in, so that integration needs no rework. Facts about the app were read on `fable/core-logic` at `c7c9488` (the default branch's head on 12 November 2026); re-check them if the app has moved.

## 1. Four products, four seams

| Product | Where it lives in the research | What it becomes in the app |
|---|---|---|
| Rule records (21 claim types, 12 modules flattened in) | `docs/research/core-logic/rules/final/*.json` | data under `data/law/` (a new folder, for example `data/law/rules/`), loaded next to the claim catalogue |
| The rule evaluator | `research/core-logic/src/DzLegal.CoreLogic.Rules` (`IRuleEvaluator`) | a library the presentation layer calls after the lawyer confirms the claim type |
| The slot list | `docs/research/core-logic/slots.json` | the questions the app asks the lawyer |
| The note reader | `research/core-logic/src/DzLegal.CoreLogic.Reader` (`INoteReader`) with its model files | a library that proposes the claim type, the events and the slot answers from the notes |

## 2. A rule record and `data/law/claims`

The app's `ClaimType` (`src/DzLegal.Law/ClaimCatalog.cs`) has: `id, area, label, description, verified, contractual, articles[{code, number, role, required, why}], prerequisites, suggestedRequests, requiredDocuments, factsToState`. **The app's loaders ignore JSON keys they do not know. They refuse: an enum value they do not know; a code outside the library's own; an article the library does not hold; a repealed article; an article number that appears twice in a code file.** So a record's content can be mapped onto the existing fields only after the law delta has been integrated (the cited articles must exist in the library first, and the mapped `articles[]` must still pass the app's own validation of the catalogue against the library), and the new content needs new fields.

| Rule record | Existing field of `ClaimType` | Mapping |
|---|---|---|
| `id`, `area`, `label` | `id`, `area`, `label` | the same values; the record never renames a claim |
| `verified` | `verified` | always `false` |
| `basis[]` (`cite.code`, `cite.article`, `role`, `required`, `says`) | `articles[]` (`code`, `number`, `role`, `required`, `why`) | one catalogue article per distinct (code, article); `role` uses the same six values as the app's `ArticleRole` on purpose; `why` takes `says`. **Lost in this mapping:** the paragraph numbers, the version, the quote, the grade. |
| `priorSteps[].message` | `prerequisites[]` | the message text, as today's free-text line |
| `requests[].text.ar` | `suggestedRequests[]` | the Arabic request line |
| `documents[].name.ar` | `requiredDocuments[]` | the name |
| `elements[].factsToState[].ar` | `factsToState[]` | the lines, flattened |
| — | `description`, `contractual` | not produced by a record; they stay as they are |

**What needs new fields or a new file** (recommended: keep the whole record as its own file, `data/law/rules/<claim id>.json`, loaded through the contract's types, and leave `data/law/claims/*.json` as the catalogue): `cite.paragraphs` and `cite.version`; `quote`, `source`, `grading`; `elements[]` with their events, burden and proof; `court` (section, territory, exits); `deadlines[]`; `priorSteps[]` as conditions; `boundaries[]`; `slots`; `examples`; `openPoints`; `checklist`; `history`.

**Articles the records cite that the library lacks** come in `docs/research/core-logic/law/law-delta.json`, in the library's own article format. The instructions per article (which generator input it goes through, which tests guard the library) are in `law/REPORT.md` and [law/README.md](law/README.md). An entry with `supersededWording: true` (an older wording that still governs some cases) is **not** merged into the app's code file, whose loader refuses a number that appears twice: the reviewer decides how the library holds two wordings.

**A consistency rule for the integrator:** after integration, every (code, article) of a record's `basis` must resolve in the app's law library to a non-repealed article whose text contains each quoted paragraph (level 1 of the text comparison). That is one test, and it would have caught the six repealed CPCA articles the library carries today.

## 3. The rule evaluator

```csharp
public interface IRuleEvaluator
{
    RuleOutcome Evaluate(IReadOnlyList<RuleRecord> records, CaseSlots slots, DateOnly petitionDate);
}
```

- **Pure:** no file, no clock, no network. `petitionDate` is the form's `document.date` (the form writes dates as `dd/MM/yyyy`; the caller parses it).
- **Input `CaseSlots`:** the claim ids **the lawyer confirmed**; the slot values with their origin (`Confirmed` or `Proposed`); the request kinds; the section the lawyer wrote, mapped to `CourtSection`.
- **Output `RuleOutcome`:** `Findings`, `Questions`, and the proposed `Section`.
- **A finding** carries: `Ref` and `RecordId` (which item of which record); `Kind` (deadline, prior step, court section, exit, document, boundary); `DeclaredEffect` and `Effect` (one of four: `BlockCleanExport`, `Warn`, `Ask`, `None`); `Grade`; `ToCheck` (true for grade D: the warning is shown with the mark "to check"); `Message` (Arabic and English, from the record); `Basis` (the articles with their paragraphs, to show with the message); `SlotsUsed`; for a deadline `LastDay` and `DaysLeft`; a `Trace` for logs.
- **A question** is a slot the rules need and do not have, with the items that wait for it.

Full behaviour: [specs/rule-evaluator.md](specs/rule-evaluator.md). Grades and effects: [rules/GRADES.md](rules/GRADES.md).

### What "block" means (owner's decision, 12 November 2026)

**The only blocking effect is `BlockCleanExport`, and it means what the app's `BlockerMandatory` means today:** the clean export is blocked, and the export with the «مسودة» mark stays allowed. In `MainWindowViewModel.UpdateExportBlockers`, the lines for missing mandatory elements are added only when the draft mark is off; a rule finding with `Effect == BlockCleanExport` belongs **in that same branch**. It never joins the four kinds that stop even a marked draft (unreviewed AI text, a placeholder left in a section, a blocking problem in the notes, over-long text). A test of the frame fixes the four effects; adding a harder one is forbidden.

### Findings and the app's existing checks

| Finding | `Effect` | Where it belongs in the app |
|---|---|---|
| a forfeiture that has expired, or a required prior step that is missing and whose record declares a block: grade A, every slot used confirmed | `BlockCleanExport` | an export blocker lifted by the draft mark, like `BlockerMandatory`; also shown in the checks panel as `Severity.Inadmissible` |
| a deadline inside its band; a limitation expired; a missing prior step that declares a warning, or any block below grade A; a section that differs; an exit; a boundary | `Warn` | the checks panel, as `Severity.Warning`, with the articles of `Basis`; never an export blocker |
| a finding of a grade D item (`ToCheck = true`): the rule rests on no official text, or on a text not yet verified | `Warn`, with the mark | the checks panel, **as a warning** (`Severity.Warning`) that also carries the mark "to check" (a new label in both languages). Never weaker than a warning, never hidden, never an export blocker. Only an item that declares a block or a warning becomes `Warn`; a question (`Ask`) and a listed document (`None`) of a grade D item keep their effect and carry the mark. |
| a slot the rules need | `Ask` | a question to the lawyer (section 5) |
| a required document | `None` | the app lists the document (with the mark "to check" when `ToCheck` is true) |
| any other finding with `None` | `None` | nothing is shown; kept for tests and logs |

The app's form conditions test form fields only, and `SubstanceChecks` reads the notes' text with word lists. The one prior-step check it has today is the formal-notice word list (it looks for notice words when damages are asked); it has no check for a conciliation record (in the app today the conciliation record is a free-text prerequisite line of the claim catalogue). Where a rule finding and that check speak of the same thing, **the rule finding replaces the word-list check once the slot is confirmed**; until then both may show. The integrator decides the order of replacement; the research does not touch `SubstanceChecks`.

**Two differences from the app today, for the reviewer:** the app holds at most two claim types with one confirmation flag for both, while the reader may answer up to three, each to be confirmed; and a claim answer's `Evidence` may be empty (a classifier does not always point at words), while an event's never is.

**Flag for the reviewer:** today the app lets claim ids that the lawyer has **not** confirmed drive some warnings (`GroundsAnalysis.ClaimIds`, at most two, no confidence; empty means the analysis abstained). The rules of this work must **never** run on an unconfirmed claim type: `CaseSlots.ClaimIds` is the confirmed list. Whether to change the existing behaviour is the reviewer's call; it is noted here so that the two are not mixed by accident.

## 4. The classifier and the reader

```csharp
public interface INoteReader
{
    ReaderInfo Info { get; }
    NoteReading Read(NoteInput note, IReadOnlyList<string> confirmedClaimIds);
}
```

- **Input `NoteInput`:** the four fields `CaseText.From` reads (`claim.subject`, `notes.facts`, `notes.requests`, `documents.list`) and the section when given. Only the facts are required.
- **`confirmedClaimIds`:** empty → the reader classifies. Given → it does not classify; it echoes the given ids in `Claim.Answers` and reads the elements of those types (the app's real case once the lawyer has chosen, and the "type given" setting of the scorer; a note outside the catalogue is never "given").
- **Runs** on CPU, in process, with no network; everything it loads is a local file listed with its SHA-256 in `ReaderInfo.Files`.

**Output `NoteReading`:**

| Part | Content |
|---|---|
| `Claim` | `Answers`: the claim types it answers with (one to three; each with a calibrated `Confidence` and the `Evidence` spans), or `outside`; `Others`: further candidates for a pick list; `Abstained`: true when it does not answer. **The app then asks the lawyer to choose; it never guesses.** |
| `Events` | one entry for **every** element of the answered claim types: `Claim` (the claim type the element belongs to: several claim types share event kinds), `Kind` (an event of `FactVocabulary`), `Polarity` (`NotMentioned`, `Stated`, `Denied`, `Disputed`), `Confidence`, `Abstained`, `Actor`, `DisputedBy`, `Date`, `Amount`, `Evidence` |
| `Slots` | one entry for each slot it reads from the note: `Value`, `Confidence`, `Abstained`, `Evidence` |
| `Requests` | the request kinds asked, each with `Confidence`, `Amount`, `Evidence` |
| `Values` | every date and amount found, normalised (a date as `yyyy-mm-dd`, `yyyy-mm` or `yyyy`; an amount as digits, in dinars), with its span and its `Roles`: what it is for (one value may serve two) |
| `Info` | name, version, file hashes |

**Polarity names.** `Stated` is the app's `affirmed` (`FactPolarity` in `DzLegal.Drafting`); `Denied` and `Disputed` keep their names; `NotMentioned` is the absence of the three. The mapping is one line each way.

**The quoted words behind each answer.** `TextSpan(Field, Start, Length)`: a stretch of one field of the note **as the lawyer typed it**, in UTF-16 code units, before any normalisation, so that the app can highlight it without re-finding it. Rules the reader enforces on itself: an answer other than `NotMentioned` or `unknown` with no span becomes an abstention; `NotMentioned` and `unknown` carry no span.

**Confidence** is calibrated on development notes: among answers given with confidence c, a share c are right. It is for ordering and for thresholds; the app should show the words, not the number.

**Abstention** exists at three levels (the claim, each event, each slot). An abstained event still carries the reader's best guess in `Polarity`, to be shown as a question, never as a fact.

**Nothing the reader returns is a confirmed fact.** Every value enters `CaseSlots` with `Origin = Proposed` until the lawyer confirms it, and a proposed value never blocks (the evaluator lowers `BlockCleanExport` to `Warn`).

## 5. The slots, as the questions the app asks the lawyer

`slots.json` (`SlotCatalogue`): each slot has an `Id`, a `Type` (`yesNoUnknown`, `date`, `amount`, `choice` with its `Choices`), the `Question` in Arabic and English, an optional `Help`, `AskedFor` (the claim types whose records read it; rewritten by script from the final records), `AskedWhen` (a condition on other slots), `FromEvent` (the event whose polarity or date the reader uses to propose an answer), `ReadFromNote` (whether the reader proposes one), and `Origin` (the brief, or the record that needs it).

How the app uses them (the app has no question model today; this is new presentation work):

1. The lawyer confirms the claim type.
2. The reader's proposals fill slots as `Proposed`, each with its words highlighted.
3. The evaluator runs. `RuleOutcome.Questions` lists the slots still needed, in order; the app asks each one with its `Question`, shows the proposal if there is one, and stores the lawyer's answer as `Confirmed`.
4. The evaluator runs again after each answer. A question never blocks anything: an unanswered slot leaves its rule at `Ask`.

For the app, each `Question` and `Help` and each rule `Message` is text that lives in data, in both languages, like the form definition's labels; nothing here is hard-coded in XAML or C#. The one reserved slot name, `requests`, is not a question: it is the list of request kinds, which the app already holds.

## 6. JSON

`ContractJson.Options`: the app's `DefinitionJson.Options` (camelCase names, enums as camelCase strings, comments allowed, Arabic kept readable), made strict in three ways: **an unknown member is an error; a property name must have the exact case; an enum must be written by name, never as a number.** A test of the frame holds all three. The research files are strict so that a record and the contract cannot drift; the app's own loaders stay tolerant.

## 7. What the integrator must not assume

- That a record's rule is legally verified. Every record says `"verified": false`, and every item has a grade that says how far it can be trusted and why.
- That the reader's scores on synthetic notes hold on real ones. The report gives the scores on the app's own fictitious cases beside them.
- That a finding's `Effect` may be raised, or a warning removed. A product decision may lower a block to a warning. It may **not** hide a warning, show it as something weaker, or drop the mark "to check": a rule that fires is never silent (owner's decision, 13 November 2026).
