# Brief: writing hidden set v2

**Written:** 6 November 2026.

**For:** an independent author, an AI agent or a person, who has **not** worked on the fact reader. That rules out Codex, which is building Alpha-03, and any session that has read the reader's prompts, its cases or its results.

**What it is for:** the new unseen set on which Alpha-03 (`docs/briefs/ALPHA-03-BRIEF.md`) and Alpha-01 are measured, once each. The first hidden set (100 cases, 30 Oct) has been seen at summary level, so under rule 7 of `docs/HIDDEN-SET.md` it can no longer measure a changed reader.

## 1. What you may read, and nothing else

You may read only:
- **this brief;**
- **`docs/HIDDEN-SET.md`:** the rules, the content and the exact file format. It is your main reference;
- **`data/law/claims/*.json`:** the 21 claim types and what each one is;
- **`data/law/claim-elements.json`:** for each claim type, its roles, the facts it is read for (with their actor, and whether they carry a date or an amount) and its requests;
- **`src/DzLegal.Drafting/CaseFacts.cs`,** the `FactVocabulary` class only: the closed lists of codes.

**You must not open, search or list anything else in the repository.** In particular:
- `data/law/prompts/` and `data/document-types/*/prompts/`;
- `tests/` (any case file);
- `docs/baselines/`, `docs/ALPHA-*.md`, `docs/FACT-READING.md`, `docs/LOCAL-READER-SEED.md`, `docs/briefs/ALPHA-0*-BRIEF.md`;
- the git history.

If a tool shows you one of them by accident, stop and tell the owner.

## 2. What you write

**One file:** `C:\dz-hidden-v2\hidden-cases.json`. It goes outside the repository, in the format of `docs/HIDDEN-SET.md` § Format: an object with a `cases` list. Keep no other `.json` file in that folder: the harness reads every one it finds there.

**Coverage, as in `docs/HIDDEN-SET.md` § Content:**
- **4 or 5 cases for each of the 21 claim types,** about 90 in all;
- **10 cases outside the catalogue** (`"claim": "out-of-scope"`).

**The cases:**
- **Names:** `n001` to `n100`, in a shuffled order, so the name says nothing about the claim type. The first set used `h…`, so the two summaries never mix.
- **`"partition": "hidden"`** on every case.
- **Fictitious data only:** invented names, places, dates and amounts; formal Arabic (الفصحى) only.
- **They read like a lawyer's notes, not like an exercise.** Follow every point of `docs/HIDDEN-SET.md` § "Make them read like a lawyer's notes". Spread the difficulties as a lawyer's files would: some cases are plain, some have several of the difficulties. **Do not write a case to trap one particular kind of reader**; you do not know how the reader works, and the set must measure the product, not a guess about it.
- **Vary the wording.** Use different verbs, word orders and ways of saying that something did not happen or is contested, for example:
  - one party asserts and the other denies;
  - a party contests the amount but not the fact;
  - a fact said not to have happened;
  - a fact that happened despite something else.

  Do not settle on a few stock phrases.

### Labelling polarity

Use exactly the definitions of `docs/HIDDEN-SET.md`:
- **`affirmed`:** the text says it happened;
- **`denied`:** the text says it did not happen;
- **`disputed`:** one side claims it, and the other denies or contests it.

**Rules the owner added after Codex's Report 1** (apply them exactly; the owner fills this in before handing you the brief):

> **Agreed by the owner on 6 November 2026** (Codex's Report 1, `docs/ALPHA-03.md` item 7, on `codex/alpha-03`). **Pending:** a legal review of the dismissal example in rule 2; the owner removes this line once it is confirmed.
>
> 1. **Affirmed:** the lawyer's notes assert the proposition, including a proposition that is itself negative: an assertion that wages were not paid affirms `wages_unpaid`. A document named in `documents.list` affirms the event it is named after. This records an assertion in the notes, **not judicial proof**.
> 2. **Disputed:** a party denies or contests the same fact the plaintiff relies on. The plaintiff's assertion may be implicit in bringing the claim; it need not be written out. So «ينكر المدعى عليه استلام القرض» is **disputed**, not denied or absent. A contest attaches to its target proposition, not to every event in the sentence or the case: a denial of the alleged reason for a dismissal does not, by itself, dispute that the dismissal occurred.
> 3. **Denied:** the notes themselves state that the proposition did not happen, rather than reporting a party's opposition to it. For a notice, «لم يوجه أي إعذار» is denied. Negation is read against the whole element: «لم يدفع» affirms non-payment and denies payment; in «لم يدفع رغم إعذاره» the notice is affirmed.
> 4. **Absent:** the notes contain no assertion, no notes-level denial, no party's contest and no named document for that proposition.
>
> Label each event at the granularity it describes. A named document does not override an explicit contest of that event. If a scenario seems to fit two rules, do not invent a tie rule: rewrite or replace the case (§ Check your own labels).

### Check your own labels

When all the cases are written:
1. **Re-read every case cold,** without looking at your labels, and label it again.
2. **Compare the two labellings.** Where they differ:
   - if the text can be made unambiguous **without making it artificial**, rewrite it;
   - otherwise, replace the case.

   An ambiguous case measures nothing.
3. **Run the project's check,** from the repository root:

   ```powershell
   dotnet run -c Release --project src/DzLegal.Harness -- facts check --cases C:\dz-hidden-v2
   ```

   It names problems by case name only, never by text: unknown codes, dates or amounts not written in the case, answers the reader is never asked for. Fix them until it prints «No problem found.»

## 3. What you hand over

- **To the owner only:** one short note with the number of cases per claim type, the number outside the catalogue, that the check prints «No problem found.», and how many cases you rewrote or replaced at the label check.
- **No case text, label or file** goes to anyone else, into the repository, into a commit, into a chat with a developer, or into any online service.

**Keep a copy** of `C:\dz-hidden-v2\` somewhere only the owner controls (an encrypted archive, a USB key).

## 4. For the owner

1. **When to hand this brief over:** the rule agreed from Codex's Report 1 is in the quoted block of § Labelling polarity. Once the legal review of its dismissal example is done, remove the «Pending» line (or correct the example), then start the author.
2. **Who:** a fresh session of a model or person that has not seen this project's reader. Give it read access to the repository on the laptop and the folder `C:\dz-hidden-v2\`, and nothing more.
3. **The run,** once each, on Codex's frozen commit, plugged in, one run at a time. Use these commands, and the command Codex gives in Report 2 for Alpha-03:

   ```powershell
   # Alpha-01, the reference, on the same set
   dotnet run -c Release --project src/DzLegal.Harness -- facts --alpha --cases C:\dz-hidden-v2 --partition hidden --models gemma3:4b --out C:\dz-hidden-v2-results\alpha-01
   ```

4. **What is shared:** only each run's `summary.md`. The `--out` folders hold every case's text: keep them with the set.
5. **Afterwards:** once anyone opens the failures to improve a reader, v2 has been seen too. Record it in `docs/HIDDEN-SET.md` § Runs, as the first set was.
