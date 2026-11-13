# Specification: the article store and the comparison of texts

**Why.** A rule may quote only a text that exists, is in force (or is the older wording the rule names) and was verified. Several places hold texts, in different formats, with different guarantees. The article store is **one read-only view over them**, built by script, so that every other script (the quoting script, the inventory, the grading) asks one question: "give me article N of code C, in this version or in force, with its paragraphs, and tell me how far it can be trusted".

Nothing here changes `data/`, `tools/law-corpus/` or the `codex/law-corpus` branch. The store is a generated file outside the repository (`C:\Users\TheShadow\dz-core-research\store\articles.json`); its inputs are pinned by commit id and hash, so it can be rebuilt.

## The inputs

| Input | Where | Read how | What it gives |
|---|---|---|---|
| **Library** | `data/law/codes/<code>.json` on `fable/core-logic` | file | Civil and Commercial Codes: every article proved letter by letter against the official consolidated PDF of 2007 (`sourceExtract`). Labour laws 90-11 and 90-04: transcribed from the Official Journal page image and re-read by the same model family. CPCA and Family Code: **imported from the corpus**, so they add no independent proof. One wording per article. |
| **Corpus** | `data/law/corpus/<code>/articles.json`, `ledger.json` and `report.json` at commit `9b60990` of `codex/law-corpus` | `git show 9b60990:<path>` (never check the branch out, never write to it) | Each article with its versions, the amending text and a verification state (`checked`, `partly-checked`, `unsettled`), with Codex's check records; the report's `unsettled` list with the reason for each version. |
| **Delta** | `docs/research/core-logic/law/law-delta.json`, and after the second pass also `law/unused-articles.json` (same article format) | file | The articles this work adds, each read twice from the page image by two model families, and the corrections ([../law/README.md](../law/README.md)). It exists from the first pass and grows; the second pass trims it. |
| **Readings** | `docs/research/core-logic/law/readings.jsonl` | file | One line per fresh page reading of an article that the library or the corpus already holds (line format in [../law/README.md](../law/README.md), "Re-readings"). `verified` is computed from it. |
| **Ledger** | `docs/research/core-logic/law/amendment-ledger.json` | file | What the Official Journal scan found ([official-journal-scan.md](official-journal-scan.md)). Gives `textAsOf`, `ledgerRefs` and `ledgerClean`. |

The store records each input's commit or SHA-256 in `inputs`. Reading `unused-articles.json` as well as `law-delta.json` means that trimming the delta in the second pass changes no entry of the store.

## Reading the corpus correctly (three traps, all met in practice)

1. **The version in force is the last one.** `versions[-1]` is the version whose `introducedBy` has the latest ledger date. The article is repealed if and only if that version's `status` is `"repealed"`. **Never filter on `inForceTo` or on a status of "in force"**: the app's own importer did, and so the library still carries six CPCA articles that Law 22-13 repealed (826, 835, 836, 912, 913, 914).
2. **`VersionOn()` of the corpus code is not usable** (it answers "unsettled" for most dates). Do not call it; use rule 1.
3. **A later publication may touch an article without a new version being recorded.** Guard: flag the article when a ledger publication that is not a rectification, dated after the version's `introducedBy`, lists the article's `canonicalNumber` in `articlesTouched`. Known hits: Civil Code 18, 19, 22 to 25. A flagged article is not `verified` until the amending text is read on the Official Journal page (a line of `readings.jsonl` with `why: "guard"`).

Two more facts of the corpus:

- **An article held as a reference, without text** (`availability: "reference-only"`, no versions: Civil Code 40, 42, 43, 170, 182, 182 مكرر, 184, 212; Civil Status art. 64) gives no text and no status. Its text is taken from the library when the library has it with its proof (Civil Code 182 and 182 مكرر are cited by the claims and are in the library); otherwise it is to fetch (a delta article).
- **The corpus's "unsettled" counts for CPCA and the Family Code concern dates of entry into force, not the texts.** Every CPCA and Family Code version is `checked`. The reasons are in `report.json` (`unsettled[].reason`). For CPCA they record that **Law 22-13 art. 13 delays the new court rules (CPCA 32, 33, 531, 536 مكرر, 536 مكرر 4, 800, 801 and the other articles Law 22-13 touched) until the new courts are installed**, and keeps cases filed earlier before the former courts. That is a lead, not a text: see `inForceNote` below.

Compare article numbers on `canonicalNumber` (the library's `number` is already canonical: `187 مكرر`, `73-4`).

## The store's entry

The store is **keyed by (code, article, version)**. Every wording the inputs hold is an entry: the corpus's earlier versions, the library's one wording, the delta's entries (two wordings of one article are two delta entries). A lookup without a version returns the wording in force; a `cite.version` selects another, and `quote-records` honours it (it fails when that version is not in the store).

```json
{
  "code": "cpca", "article": "504", "version": "law-08-09",
  "inForce": true,
  "repealed": false,
  "statusFrom": "corpus",
  "store": "library",
  "tier": 1,
  "path": ["<book>", "<title>", "<chapter>", "<section>"],
  "paragraphs": ["يجب رفع الدعوى أمام القسم الاجتماعي ..."],
  "alsoIn": ["corpus"],
  "agreement": "identical",
  "corpusState": "checked",
  "guardFlag": false,
  "verified": true,
  "verifiedBy": "ojPageReread",
  "inForceNote": null,
  "frenchCheck": null,
  "textAsOf": "2026-11-20", "ledgerRefs": [], "ledgerClean": true,
  "inputs": { "library": "<sha256>", "corpus": "9b60990", "delta": "<sha256>", "unused": "<sha256>", "readings": "<sha256>", "ledger": "<sha256>" }
}
```

- `path`: the article's place in its code (the library's `path`, or the corpus's), so that "the whole chapter" of an article can be listed by script: the texts given to Codex and to the boundary writer, and a deriver's reading of neighbours, depend on it.
- `paragraphs`: the lines of the text (the library's `text` split on line breaks; the corpus's `paragraphs`; the delta's `text` split on line breaks). A numbered item of a list is a paragraph. A `cite` counts them from 1.
- **Status, and which version is in force, always come from the corpus** when the code is in the corpus (repealed or not; which version is last). A library article that the corpus says is repealed is served as repealed: the six CPCA articles above come out `repealed: true`, with no paragraphs to quote, whatever the library holds (the delta carries the `libraryCorrection`). `statusFrom` says where the status came from: `corpus`; `library` for a code or an article the corpus does not have (the labour laws; library-only Civil and Commercial articles; reference-only entries); `delta` for a delta article. A delta correction of kind `wrongStatus` overrides the corpus.
- **Text: the delta, then the library, then the corpus**, for the version asked. The library holds one wording; it is used only for the version it is. For CPCA and the Family Code that is the corpus's last version (the library is the corpus's import). For a library-only Civil or Commercial article (among those the claims cite: Civil 164, 174, 176, 179, 186, 323, 324 مكرر 1, 324 مكرر 5, 333, 674; Commercial 30, 173, 176), its version is the latest text of the merged ledger (Codex's ledger and the scan's) that touches the article and is dated on or before the consolidated edition (Civil Code: `consolidatedAsOf` 2007-05-13); otherwise the original ordinance. When the edition gives no date (the Commercial Code's file has none), the version is the latest text touching the article before 2007 and `ledgerClean` stays false until a page reading confirms it. When the library's wording is older than the corpus's last version, the corpus's text is served and the delta carries a `libraryCorrection` of kind `outdatedVersion`.
- `store`: the input the text was taken from. `tier`: 1 for a text read from an Official Journal page (corpus; the library's labour laws, CPCA and Family Code; the delta), 2 for the library's Civil and Commercial Codes (official consolidated PDF). A tier-3 text never enters the store.
- `inForceNote`: what is known about the date from which the wording applies, when that is not plain. Filled from the corpus report's `unsettled` reason for the version (for CPCA, the Law 22-13 art. 13 lead; for Commercial 187 مكرر and 187 مكرر 1, the reason the report gives), marked as a lead, until the provision is read on its Official Journal page (Law 22-13 art. 13: JO 48 of 17 July 2022); then the note quotes that reading. The modules on court sections and on exits from the ordinary courts must answer it: it is a line of their checklist ([../law/README.md](../law/README.md), "Entry into force").
- `frenchCheck` (delta entries only; null elsewhere): `equal` when `proof.frenchChecked` is true; `mismatch` when it is false and `proof.french` is present; `noFrench` when the French edition does not hold the article (`note` says why). `grade-records` treats a deadline resting on a `mismatch` or `noFrench` entry as interpretive ([../law/README.md](../law/README.md), step 6).
- **`verified` is about the text only.** It says that the words were checked by the method below. It never means that a lawyer checked anything, and it is never copied into the library's own `verified` field (the app's field for a lawyer's check).

## When is an article `verified`?

`verified` belongs to an entry (one wording). The rule differs per code, because the sources' independence differs. `verifiedBy` records which line applied. A **fresh page reading** is a line of `readings.jsonl` for that (code, article, version), made against the store's current text (`storeTextSha256`), with `result: "agrees"`.

| Code | Verified when | `verifiedBy` |
|---|---|---|
| Civil Code, Commercial Code, in the library | The library's `sourceExtract` proof holds, **and** either the corpus has no text of the same version (absent, or reference-only) or the two texts agree at level 2 (or level 2o, below), or a fresh page reading settled their difference. A corpus state other than `checked` does not count as agreement. | `libraryProof`, `libraryProofAndCorpus` or `libraryProofAndReread` |
| Civil or Commercial Code, in the corpus only | Corpus state `checked`, no guard flag, and **one** fresh page reading by a family other than the corpus's checker. Corpus state `partly-checked` or `unsettled`: **two** fresh page readings, one by each family. | `corpusAndReread` or `corpusAndTwoReadings` |
| CPCA, Family Code | The corpus and the library are the same text, so their agreement proves nothing. Verified when the corpus state is `checked` and there is no guard flag (`corpusChecked`). With a fresh page reading by a family other than the corpus's checker, `verifiedBy` becomes `ojPageReread`. | `corpusChecked` or `ojPageReread` |
| Laws 90-11 and 90-04 | The library's text, plus one fresh page reading by Codex (the library's two readings were both Claude's). | `libraryAndCodexReread` |
| Anything in the delta | Two readings by two families agree at level 2 (or 2o), or a third reading settled each difference; the French check was done (`proof.french` present, or `note` says why there is no French text); for a Civil or Commercial Code article, `proof.consolidatedPdf` is present. | `twoFamilies` |
| A code in neither the library nor the corpus (a special law), or a corpus code the library lacks (Civil Status) | Only as a delta article, with `newCode`. Its `ledgerClean` is false (`scanNotRun`) until the scan's patterns cover the instrument ([official-journal-scan.md](official-journal-scan.md)). | `twoFamilies` |

**The corpus's checker** is taken to be Codex (the corpus is Codex's work), so the fresh reading of a corpus text is by Sonnet 5.5; when a version's `checks` name a Claude reading, the fresh reading is by Codex instead.

**The critical use rule (the only place it is stated).** `verified` is necessary but not always enough. For an item that is **a deadline, a prior step, a section rule or an exit**, `corpusChecked` alone does not satisfy condition 2 of the grades ([../rules/GRADES.md](../rules/GRADES.md)): every article in the item's basis needs a fresh page reading recorded in `readings.jsonl`, that is, any `verifiedBy` other than `corpusChecked`. For every other item, `corpusChecked` is enough. `grade-records` applies this; the store only reports `verifiedBy`. The reason: for CPCA and the Family Code, "checked" is Codex's check of its own copy, and the items that can block or send a case elsewhere must not rest on one family's reading. In practice the first pass re-reads every CPCA and Family Code seed article, so the rule rarely has to wait.

The first probe of a page reading (12 November): Codex read art. 19 of Law 90-04 from the page image and gave 65 of 66 words as the library has them; the one difference («يقدم» for «يقيم») was Codex's misreading, settled by looking at the image. Expect that order of error, and expect it in both directions.

## Comparing two Arabic texts: three levels

`text-compare` (a script with tests; used by the store, by the page-reading check and by the corpus comparison):

| Level | What is ignored | Use |
|---|---|---|
| 1. Identical | nothing but leading and trailing spaces | proof that a quote was not altered |
| 2. Same letters | Unicode form (NFC), tatweel, diacritics, runs of spaces, the hamza forms of alef (أ إ آ ٱ → ا), alef maqsura and ya in any position (ى → ي: the Journal of 1975 prints «شىء» where the consolidated edition prints «شيء»), ta marbuta and ha at the end of a word (ة → ه), Arabic and Western digits (٠-٩ → 0-9), and punctuation with the spaces around it | "the same text" for verification |
| 2o. Same letters but for hamza seats | everything of level 2, and the seat of a hamza (ء ئ ؤ, or ي و written for them) | an **orthographic** difference: recorded, never a correction, and it does not stop `verified` (the library's labour texts use modern spelling by the owner's decision of 16 October 2026; the page prints the old one) |
| 3. Same words in order | everything of level 2, and line breaks (paragraph division) | finding where two witnesses differ only in layout |

Rules: a difference at level 2 (that is not orthographic) is a **real difference** until a model has looked at the page image and said which witness is right, and the decision is recorded (in `readings.jsonl`, and in the delta's corrections when the store's text was wrong). **Digits are never normalised away**: «ستة (6)» against «ستة (8)» is a difference at every level. **Negation words and exception words are never ignored**, whatever their spelling: a comparison that reports "same" must keep every لا، لم، لن، ليس، غير، إلا، ما لم، دون. A difference of paragraph division (level 3 holds, level 2 does not) is a difference too: `cite.paragraphs` counts lines.

The output for each pair: the level reached, and when below level 2 the list of differing words with ten words of context on each side.

## The inventory (step 2)

`inventory-cited` reads every `cite` of every record (both derivations, then the final ones) and writes, per (code, article, version): present where, state, `verified`, `verifiedBy`, whether a critical item cites it, and what must be done (`nothing`, `compare`, `rereadPage`, `fetch`). Its totals are the figures the brief asks for: how many cited articles were checked by script, how many by a page reading, how many corrections were found.

## Done when

- The store builds from pinned inputs, twice, to the same hash.
- Tests cover: the three corpus traps (with the six repealed CPCA articles served as repealed, and Civil Code 18, as fixtures); a reference-only article served from the library (Civil Code 182); an older version returned for a `cite.version` and the in-force one without it; each level of the comparison on pairs that must pass and pairs that must fail (a changed digit, a dropped لا, a swapped إلا, «شىء» against «شيء» passing level 2); `verified` computed from a small `readings.jsonl` fixture, and `corpusChecked` changed to `ojPageReread` by a reading line; `paragraphs` of the library and of the corpus for one article of each code.
- Every article cited by a final record is `verified` (with a fresh page reading where the critical use rule asks for one), or the item that cites it is graded D and listed in the report.
