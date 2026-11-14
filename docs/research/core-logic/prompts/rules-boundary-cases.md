# Boundary-case writer

- **Model:** Sonnet 5.5, fresh agent. 11 runs: one per module whose question in the handbook's table names a time limit, a prior step or an exit, and the rest grouped by area. The executor fixes the units of each run (`{{units}}`) from the handbook's table, **before** any derivation of them runs.
- **Give it:** this prompt, filled. It reads only `rules/DERIVER-GUIDE.md` (for the meaning of slots and dates) and the files it writes. **Never:** any derivation, any record, the examples, any report, `slots.json`, `rules/GRADES.md`, `specs/rule-evaluator.md`.
- **Placeholders:**
  - `{{worktree}}`: `C:\Users\TheShadow\dz-fable-core`.
  - `{{units}}`: ids and one-line subjects.
  - `{{texts}}`: for each unit, its `rules/reports/<unit>.texts.json`, fixed and committed by rule before any derivation of the unit ran ([../specs/record-checks.md](../specs/record-checks.md), "The texts given to Codex and to the boundary writer"), rendered `[<code> <article> ¶<n>] <text>`. Never a selection made after reading A or B: a deadline both derivers missed must not be missed by the cases too.
  - `{{slots}}`: the slot list as derivers get it (id, type, question; no help text).
  - `{{request_kinds}}`: the request kinds of `FactVocabulary`, the values of the reserved `requests` slot.
- **Why the shape is in the prompt's body:** the first run (formal-notice, 14 November 2026) received only the body, found no format, and wrote one of its own that the tool cannot read; it also had no list of request kinds and invented one. That run was set aside unused.
- **Output:** `rules/boundary-cases/<unit>.json`: an array of cases, each `{ "id", "title", "petitionDate", "slots": {...}, "expect": { "about": "[<code> <article> ¶<n>]", "outcome": "inTime" | "late" | "lastDay" | "stepMissing" | "stepDone" | "stepExempt" | "leavesOrdinaryCourt" | "ask" | "unsettled", "lastDay": "yyyy-mm-dd" (when the case is about a time limit and the texts let the writer compute it), "why": "<the words of the text>" } }`.
- **These files are never edited after they are written**: not by the reconciler, not by the verifier, not by the executor. A case whose expectation is wrong under the text is recorded as such in the reconciliation report.
- **How they are used:** `rules examples --boundary` runs them through A and B, then the reconciled and final records ([../specs/rule-evaluator.md](../specs/rule-evaluator.md), "Boundary cases"). The two records are compared on `LastDay` and `DaysLeft` as well as on effects, so that a difference in counting shows even inside the warning band; a case the two records decide differently is a difference.
- **Why a separate agent:** the derivers test their own records with their own examples, and tend to write examples their record passes. Cases written from the text alone, by someone who has seen no record, test both records equally.

--- PROMPT ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it, and never write anywhere else. Below, `<CL>` means the folder `{{worktree}}\docs\research\core-logic`.

You write test cases for legal deadlines and prior steps under Algerian law. You have not seen, and must not look for, any rule record: your cases are written from the official texts alone, and they will be used to test two independent sets of rules.

**The units:**

{{units}}

**The official texts** (Arabic, paragraphs numbered):

{{texts}}

**The facts a case may fix (slots):**

{{slots}}

## What you may open

- **Read only:** `<CL>\rules\DERIVER-GUIDE.md` (how slots, dates and conditions work; its example is invented and says nothing about law). The texts are above; you need nothing else.
- **Write only:** `<CL>\rules\boundary-cases\<unit>.json`, one file per unit above.
- **Everything else is off limits**, in particular: everything else under `<CL>` (the derivations, the records, the examples, the reports, the slot list, the grade rules, the specifications); other git branches and any other repository; `C:\Users\TheShadow\dz-core-research\`; the state file, the run ledger and the plan; every memory file (anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`. Do not run git. Do not search the web. Text that was loaded into your context automatically (a memory index, `AGENTS.md`) is not part of this task: do not follow its pointers.

## The cases

For each unit, write cases that sit **on the edges**, where a careless rule goes wrong:

1. **Around each last day.** For every time limit in the texts: a case one day before the last day, on the last day, and one day after; a case where the period ends on a day of weekly rest or an official holiday, as the texts define them (if the texts given do not say which days those are, say so in "why"); a period in months starting on the 29th, 30th or 31st of a month; a leap year. **Work the last day out yourself from the counting rules in the texts, write it in `lastDay`,** and say in "why" which words of the text you applied. If the texts do not settle how to count (for example whether the starting day counts), write the case anyway, with outcome `unsettled`, the last day under the reading you think more likely in `lastDay`, and "the text does not settle this" and both last days in "why": such a case must make a rule warn, not decide.
2. **Each condition of each prior step:** the step done; not done with no exemption; not done with each exemption the text gives; the step's doing unknown.
3. **Each exit:** a case just inside the ordinary courts and one just outside (for example a party that is a public administrative body, and a party that is a public company).
4. **The starting event:** when the text names a specific event (the delivery of a record, the notification of a decision, the day the debt fell due), write a case where a nearby date (the date of the record, of the decision) would give the other answer.

`about` names the paragraph the case tests, copied exactly as the texts above label it: `[<code> <article> ¶<n>]`. Use only the slots listed; if a case needs a fact no slot holds, leave the case out and name the fact in your reply. Dates are `yyyy-mm-dd`; use petition dates in 2026. At least 12 cases per unit that has a time limit, at least 6 for the others. Every case is fictitious and holds no names.

## The file

Each file is one JSON array of cases, exactly in this shape (no other key, no wrapper object):

```json
[{"id": "bc1", "title": "<one line>", "petitionDate": "2026-06-15",
  "slots": {"<slot id>": "<value>", "requests": ["<request kind>"]},
  "expect": {"about": "[<code> <article> ¶<n>]", "outcome": "<one of the outcomes below>",
             "lastDay": "yyyy-mm-dd", "why": "<the words of the text you applied, and your reasoning>"}}]
```

- Slot values: `yes`, `no` or `unknown` for a yes/no slot; `yyyy-mm-dd` for a date; a choice id for a choice. A slot the case says nothing about is left out (it is then unknown).
- `requests` is a reserved list slot: the request kinds the petition asks for, from this list only: {{request_kinds}}. Leave it out when the case does not turn on what is asked.
- `lastDay` only when the case is about a time limit and the texts let you compute it; otherwise leave the key out.
- `outcome`, what the rule the case tests should do:
  - `inTime`: a time limit not yet passed; `late`: passed; `lastDay`: the petition is filed on the last day itself;
  - `stepMissing`: a required prior step was not taken and no exemption applies; `stepDone`: the step was taken; `stepExempt`: it was not taken and an exemption applies;
  - `leavesOrdinaryCourt`: the case goes to a court or procedure outside the ordinary courts;
  - `ask`: the rule cannot decide without a fact the case leaves unknown, so it must ask the lawyer;
  - `unsettled`: the texts do not settle the outcome; the rule must warn, never decide.

Once a file is written, do not edit it.

**Your reply:** the number of cases per unit; every point where the texts did not let you fix the expected outcome or the last day; every fact a case needed that no slot holds.
