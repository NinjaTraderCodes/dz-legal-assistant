# Brief: the core logic and the classifier, built to be right

*For Fable, 12 November 2026. Branch `fable/core-logic`, from `claude/stoic-fermat-mi1gko`. Part 7 of the architecture report closes first (step 0).*

## The goal

The app must draft a statement of claim that is legally right for the lawyer's case, on an ordinary laptop, offline. Part 7 measured that no ready-made model under 1 GB reads a lawyer's notes well enough, and the audit (Part 1) showed that the decisions that can hurt a lawyer are rules, not text generation: the claim type, the articles, the deadlines, the prior steps, the competent court and section.

So this brief asks for the **core logic** of the app: legal rules that are complete and sourced, a body of high-quality test notes, and a classifier and reader that turn a lawyer's notes into the structured facts those rules need. Be inventive. If the best answer is a model designed from scratch for this project, build it.

**There is no human reviewer in this work.** No lawyer will check your rules or your notes. Everything you produce must therefore carry its own proof: the official text it rests on, an independent second derivation, and an adversarial check. Where the law is unclear, say so and choose the reading that protects the lawyer (warn rather than stay silent).

## Step 0. Close Part 7

Do not run sessions 2 and 3. Write sections 7.3 to 7.6 from what is measured (the encoders and the hidden v2 table), state that the analysis and drafting stages of Jais-3-3.7B were not run, and answer plainly in 7.5: no ready-made model under 1 GB is good enough today; Jais-3-3.7B (50 of 90) is the candidate to replace Gemma 3 4B as the default; a small classifier could make the app useful without the AI. Push to `fable/architecture-review`.

## Step 1. The legal rules for the 21 claim types

For each claim type in `data/law/claims/*.json`, write a complete, machine-readable rule record:

- **Basis:** the articles the claim rests on, and for each one the exact part that applies (paragraph or numbered item: Family Code art. 53 item 3, Law 90-11 art. 73-4 para. 2).
- **Elements to prove** and the facts the lawyer must state for each.
- **Usual requests**, including the accessory ones, each tied to the article that grounds it.
- **Competent court:** section (CPCA arts. 32, 512, 515, 536 مكرر and the others), territory (arts. 37 to 40, 426), and the cases that leave the ordinary courts (administrative courts, CPCA arts. 800 and 801, and the specialised commercial courts).
- **Deadlines:** every limitation or forfeiture period, the event it runs from, and whether the court applies it of its own motion (Civil Code art. 321) or only on a plea. A forfeiture blocks export; a limitation warns.
- **Prior steps:** formal notice (Civil Code arts. 119, 179, 180, 181), labour conciliation (Law 90-04 art. 19 and its exceptions), conciliation before the specialised commercial court (CPCA art. 536 مكرر 4), publication at the land registry (CPCA art. 17), notice to quit (Commercial Code arts. 173, 187 مكرر), and any other.
- **Required documents.**
- **Boundaries:** the near-miss cases that look like this type but are not (and what they are).

**Method, because no lawyer will check it:**
1. Two independent derivations of every record (separate agents that do not see each other's work), each citing the official text verbatim.
2. A reconciliation: every disagreement listed and resolved from the text, or marked uncertain.
3. An adversarial pass: an agent whose only job is to find an amendment, a special law, a Supreme Court decision or a CPCA rule that breaks the record.
4. Every record keeps `"verified": false` (no lawyer has verified it) and carries its sources and a confidence for each field.

Also answer the 22 questions of the owner's Legal Review page, copied in `docs/briefs/LEGAL-REVIEW-QUESTIONS.md` (write the answers in `docs/research/core-logic/legal-review-answers.md`), each with the text it rests on and a confidence. They are the open points of the current app.

Write the records under `docs/research/core-logic/rules/` with a JSON schema. Do not edit `data/`: the records are integrated into the app later, by the reviewer.

## Step 2. The missing articles

Every article a rule record cites must exist in the app's library with its official text. Start from what exists: `data/law/codes/` and the corpus being built on `codex/law-corpus` (`data/law/corpus/`, read it, do not change it). List what is missing (at least Civil Code arts. 101, 133, 180, 181, 308, 309, 312, 321; the Commercial Code articles on commercial leases and their deadlines; Law 90-04; CPCA arts. 37 to 40, 800, 801) and collect the official texts from the Official Journal (JORADP) or the Supreme Court's publications, with the source, the date and the amending texts. Write them under `docs/research/core-logic/law/`, in the format of `data/law/codes/`.

## Step 3. High-quality lawyer notes, and hidden set v3

Build the notes the classifier and the reader are trained and tested on: what an Algerian lawyer really writes before drafting, in formal Arabic, Darija in Arabic script, French, and mixtures; short and long; tidy and hurried; with negations ("no notice was served"), corrections, irrelevant details, several claims in one note, figures and dates in every format, and near-miss cases outside the 21 types (administrative, criminal, family matters not covered).

- Every note carries its gold answer: the claim type and every structured slot of step 4, derived from the rule records.
- **Quality without humans:** each note is labelled independently by at least two judges of different model families; a note whose labels disagree is rewritten or discarded. A sample is re-read adversarially for realism and for leakage of the label into the wording.
- **Training licence:** notes used to train anything the app ships must come from a teacher whose terms allow training on its outputs (Part 4 of the report). Notes from other models may be used for testing only.
- **Hidden set v3:** a separate agent writes it, from its own plan, after the generation method is frozen. It goes to `C:\dz-hidden-v3` and is never opened by the agent that builds the classifier, never committed, and read only by the owner's command; only `summary.md` leaves it, as for v2. Aim for at least 10 notes per claim type and 30 near-miss notes.

## Step 4. The classifier and the reader: be inventive

From a note, produce what the rules need:

- the **claim type** (21 types plus "outside the catalogue"), with a calibrated confidence and the right to abstain;
- the **structured slots** the rules check: formal notice served (yes, no, unknown) and its date and means; non-conciliation record and its delivery date; contract date (before or after the 2005 commercial-lease reform); whether a party is a public body; whether the property is registered; amounts and what each one is for; the requests asked; the events with their dates and their polarity (stated, denied, disputed);
- for every answer, the words of the note it comes from, so the lawyer sees why.

You are free in the method: a fine-tuned encoder, a model designed and trained from scratch for Algerian legal notes (its own tokenizer for Arabic, Darija and French; morphology; a legal lexicon), a hybrid of rules and learning, distillation from a large model, an ensemble. Polarity is the hardest part today (23 of Jais-3-3.7B's 40 critical errors on hidden v2): treat it as a first-class problem. Measure each idea; keep what wins.

**Constraints:** runs on CPU in .NET (ONNX Runtime, MIT licence) or in plain code; at most about 150 MB on disk; under one second per note on the laptop; every licence allows commercial redistribution; nothing from the hidden sets in training.

**Targets on hidden v3:** claim type 95 % or more; outside the catalogue recognised 95 % or more; polarity 95 % or more; each other slot 90 % or more; abstentions counted separately.

## Step 5. Do we need to fine-tune a generative model?

With steps 1 to 4 in place, the generative model only writes the sentence that applies the law to the confirmed facts. Measure whether a template over the confirmed facts, Gemma 4 E4B, Jais-3-3.7B or a small model does that well enough, and conclude whether any fine-tuning is needed. Do not fine-tune anything without the owner's word.

## Step 6. Freeze

Commit the final scores with each model's digest, the data versions, the notes' generation method and the exact commands, so every figure can be reproduced.

## Rules

- **Branch:** `fable/core-logic` only, in a new worktree. Push nothing to any other branch; merge nothing.
- **What you may write:** new files under `docs/research/core-logic/` and new projects under `research/core-logic/` (code, tests, training scripts). Do not change the app, `src/`, `data/`, `tests/`, the prompts or the existing harness; the reviewer integrates later.
- **Builds and tests:** after every commit, `dotnet build -c Release` with 0 warnings and `dotnet test -c Release` green.
- **Hidden sets:** never opened by you or your agents; only the owner's command reads them; only `summary.md` leaves. Nothing is tuned on v1, v2 or v3.
- **Data:** fictitious only. No real names, no real cases, no personal data.
- **Money and keys:** ask the owner before any spending or any API key; never print, log or commit a key.
- **Sources:** every legal statement with its official source and date; every claim about a model with its link and date.
- **Reporting:** after each step, push and give the owner the commit id and a short plain-language summary: what is done, what is uncertain, what is next. Then continue unless the owner says otherwise.
