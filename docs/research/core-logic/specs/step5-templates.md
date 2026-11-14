# Specification: step 5, how much of the petition templates can write, and whether the app can work under 1 GB

**The two questions** (owner, 14 November 2026). With the reader and the rules in place, a generative model no longer chooses the claim, the articles, the deadlines or the facts. Step 5 measures what is left for it:

1. **How much of the petition can templates write?**
2. **Can the app work with less than 1 GB of model files?**

**When.** As soon as six claim types are final: `unpaid-wages`, `abusive-dismissal`, `loan-repayment`, `unpaid-rent`, `commercial-debt`, `nafaqa` (HANDBOOK section 7). It needs their final records (for the articles) and the sampler; it does not need the finished reader.

**Nothing is fine-tuned without the owner's word,** and nothing in the app changes: this is a measurement, and its result is a recommendation for the reviewer.

## 1. What the app writes today (read on 14 November 2026; read again on the day)

`data/document-types/requete-introductive/definition.json` lists the petition's sections. **Thirteen are written by fixed templates today, and one by the model:**

| Section (id) | Written today by | Step 5 |
|---|---|---|
| `header`, `plaintiff`, `defendant`, `subject`, `publication`, `opening`, `form` (في الشكل), `requestsIntro`, `documents`, `dateplace`, `signature` | a template, from the form's fields | **not measured again**: counted as "template" in the result. (The rules' findings on steps and time limits are shown to the lawyer as findings; step 5 does not write them into the petition.) |
| `requests` | a template that numbers **the lawyer's own lines**. The definition records why: a model rewriting them changed the relief asked of the court in 9 of 15 audited drafts | **not measured again**: counted as "template, by the app's own audit". No model writes requests in step 5 |
| `facts` (الوقائع) | a template around the lawyer's own text | measured (part B): can a clean facts section be written from the reader's facts? |
| `grounds` (المناقشة القانونية) | **the model** (the one narrative section, up to 900 tokens) | **the heart of step 5** (part A) |

If the definition has changed when step 5 starts, redo this table from the file and say so in the report. Read the file; never change it.

## 2. The fact sheets

**120 sheets, 20 per claim type**, drawn by the sampler (`notes sample --set step5 --seed 5`), with these constraints: one claim type per sheet; no `outside`; every element `stated`, `denied` or `disputed` in a way a petition would assert (the cells of [../notes/SCHEME.md](../notes/SCHEME.md), section 8); the cast from the fictitious lists, with a court, a section and a lawyer added from lists of the same kind.

`notes step5-sheets` then runs the rule evaluator on each scenario with the six final records and writes `docs/research/core-logic/results/step-5/sheets/<nnn>.json`:

```json
{
  "n": 17, "claim": "unpaid-wages",
  "cast": { "plaintiff": "…", "defendant": "…", "lawyer": "…", "court": "…", "section": "…", "place": "…", "date": "2026-03-10" },
  "facts": [ { "id": "f1", "element": "unpaid-wages/employment_started", "proposition": "<the element's Arabic proposition>",
               "polarity": "stated", "actor": "plaintiff", "date": "2019-09-03", "amount": null, "disputedHow": null } ],
  "requests": [ { "kind": "wages", "label": "<Arabic label>", "amount": 384000 } ],
  "grounds": [ { "g": 1, "element": "unpaid-wages/wages_unpaid", "facts": ["f2"],
                 "articles": [ { "name": "<the code's Arabic citation name>", "code": "law-90-11", "article": "…", "paragraphs": [1],
                                 "quote": "<the quoted paragraphs, from the final record>", "says": "<the record's English line>" } ] } ]
}
```

- **`grounds` holds two elements per sheet:** the first element of the claim type in the record's order that has a `basis`, and one more drawn at random among the others that have one. 240 paragraphs in all. An element whose record gives it no article is never drawn (there would be nothing to apply).
- Dates, amounts and names appear on the sheet exactly once each, in normal form; the rendering to Arabic (below) is by code.
- **The sheets are frozen (committed) before any writer runs,** with the templates and the phrase tables. Nothing of them changes after the first output exists.

## 3. The writers

| Writer | What it is | Model files (measure the real sizes on the day) |
|---|---|---|
| **T** | templates in code, with the phrase tables below | none |
| **J1** | Jais-3-0.9B (`jais3-0.9b-low`) | under 1 GB |
| **Q2** | Qwen3.5-2B (`hf.co/unsloth/Qwen3.5-2B-GGUF:Q4_K_M`, think off: the value is the string "false") | over 1 GB |
| **J4** | Jais-3-3.7B (`jais3-3.7b-low`) | over 1 GB |
| **G4** | Gemma 4 E4B (`gemma4:e4b`, think off) | over 1 GB; the app's model today |

All local, on loopback, one model loaded at a time, with the digests recorded in Part 7 (`git show fable/architecture-review:docs/research/fable-part7/run/models.json`). One sheet per call, temperature 0.2, a fixed seed. A tested script strips the marker lines a Jais 3 copy may put at the start of its answer before anything reads an output.

### Part A. The legal discussion (`grounds`): one paragraph per element

- **Models:** [../prompts/gen-apply-law.md](../prompts/gen-apply-law.md), one call per (sheet, element): 240 calls per model, at most about 220 output tokens.
- **Template T-A.** Three sentences, each from data, none from a model at run time:
  1. the fact: «حيث أن » + the fact's sentence from the phrase table (below), with its date and amount;
  2. the text: «وحيث أن المادة {article} من {name} تنص على أنه: «{quote}»» (the quote inserted by code, whole paragraphs);
  3. the application, one fixed sentence by the role the record gives the cited statement:
     - `basis`: «وحيث أن الواقعة المبينة أعلاه تدخل في حكم هذا النص، مما يجعل طلب المدعي مؤسسا من هذا الوجه.»
     - `proof`: «وحيث أن المدعي يقدم ضمن مرفقات هذه العريضة ما يثبت الواقعة المذكورة وفق ما يقتضيه هذا النص.»
     - `damages`: «وحيث أن شروط التعويض التي يقررها هذا النص متوفرة في قضية الحال على النحو المبين أعلاه.»
     - `prerequisite`, `procedure`, `limit`: «وحيث أن المدعي احترم ما يوجبه هذا النص قبل رفع دعواه على النحو المبين أعلاه.»
  The four sentences are this handbook's starting text. Before the freeze a fresh Sonnet 5.5 agent checks their Arabic once (correct, formal, natural in a petition) and may propose a corrected sentence; the executor applies it. After the freeze they do not change.

### Part B. The facts section (`facts`)

- **Models:** [../prompts/gen-facts.md](../prompts/gen-facts.md), one call per sheet: 120 calls per model, at most about 400 output tokens.
- **Template T-B.** One «حيث» paragraph per fact, in date order, from **the phrase table**: for each element of the six claim types (23 elements), two short Arabic sentences with places for the date and the amount: the fact asserted, and the fact negated (for a `denied` element). A `disputed` fact takes the asserted sentence followed by «، وهو ما ينازع فيه المدعى عليه».
- **Who writes the phrase table.** The executor (Opus 5.5), from each element's Arabic proposition in `claim-elements.json`: 46 sentences. It states facts only: no article, no legal qualification. A fresh Sonnet 5.5 agent checks the Arabic of all 46 in one run. (These are product templates, not training notes: [../notes/TERMS.md](../notes/TERMS.md) does not forbid a Claude model here.) The table is `research/core-logic/src/DzLegal.CoreLogic.Templates/phrases.json`, committed and frozen with the sheets.
- **The baseline that needs no measure:** the app's behaviour today, the lawyer's own text. It is faithful by definition and is reported as a line of the table, not judged.

## 4. The measures

**1. Faithfulness, by script, on every output** (the main measure; `scripts/step5_faithful.py`, with tests). An output is **unfaithful** as soon as one of these holds:

- it holds a number, a date, an amount or an article reference that is not on its sheet, after the normalisations of the labelling guide, section 8 (digits in either script, numbers in words, dates in every format, sums with their units);
- it holds a name that is on the script's list of all cast names of the 120 sheets and is not on its own sheet (a script cannot recognise every invented Arabic name: the judges check the rest, and the report says so);
- **part A:** it puts between «» or quotation marks words that are not a stretch of the sheet's quoted article, or cites an article number other than the sheet's;
- **part B:** a date or an amount of the sheet's facts is **missing** from it (a fact without either cannot be checked for presence by script: the judges do).

Reported per part and per writer: outputs faithful over outputs, with the Wilson interval. The template is run through the same script: a template that fails it is a bug to fix before the freeze.

**2. Usability, by two blind judges, on a sample** ([../prompts/gen-judge.md](../prompts/gen-judge.md)): Sonnet 5.5 and Opus 5.5, independently, no tool, 15 sheets per run. Candidates carry letters in random order per sheet; the template's output is one of them, unmarked; outputs the script found unfaithful are **still included**, so that the judges' own notice of invented items can be compared with the script's.

| Part | Judged | Runs per judge |
|---|---|---:|
| A | 120 paragraphs: the first element of each sheet | 8 |
| B | 60 sheets, drawn by seed | 4 |

**24 judge runs in all (12 Sonnet, 12 Opus); say so before launching.** The usable rate that counts for a writer is **the lower of the two judges'**. Both judges are Claude models: say so beside every usable rate (HANDBOOK section 5b). That is why faithfulness, which no model decides, is the main measure.

**3. Time and memory**, under the measurement rule (mains, Best performance, idle CPU): seconds per output (median and 95th percentile) and peak memory per writer; for the template, milliseconds.

**4. Size.** The bytes on disk of every model file a set-up needs, measured on the day (the reader's five files; each local model's blob files as Ollama stores them).

**Laptop time, to state before starting:** about 12 hours of writing in all (part A: 240 calls for each of four models; part B: 120 for each), in two nights or one night and a free day. One model at a time; nothing else runs.

## 5. The result: one table, and two answers

**The set-ups, from smallest to largest:**

| Set-up | Holds | Under 1 GB (1,000,000,000 bytes)? |
|---|---|---|
| S0 | the reader and templates | yes (about 0.13 GB, to be measured) |
| S1 | S0 and Jais-3-0.9B | yes if the measured sum is under 1 GB (about 0.8 GB expected) |
| S2 | S0 and Qwen3.5-2B | no |
| S3 | S0 and Jais-3-3.7B | no |
| S4 | S0 and Gemma 4 E4B | no |

**The bars, fixed in advance:** a writer **passes** a part when its outputs are faithful in at least 98 % (the script) and usable in at least 90 % (the lower judge). The rule compares point estimates; the intervals are reported beside them (with 240 outputs 98 % is 236 or more; with 120, 118; usable: 108 of 120, 54 of 60).

**The table of `reports/step-5.md`:**

| Section | Written today by | Smallest set-up that passes | Its faithful rate | Its usable rate (lower judge) | Seconds |
|---|---|---|---|---|---|
| the eleven template sections; `requests` | templates | S0 (not measured again) | – | – | – |
| `facts` | the lawyer's own text | the smallest of S0 (T-B), S1 … S4 that passes; and always the line "the lawyer's own text: faithful by definition" | | | |
| `grounds` | the model | the smallest of S0 (T-A), S1 … S4 that passes | | | |

**Answer 1, how much templates can write:** the sections whose smallest passing set-up is S0, listed; and for `grounds`, T-A's two rates beside the best model's.

**Answer 2, under 1 GB:** **yes** when, for every measured section, the smallest passing set-up is S0 or S1 (for `facts`, the lawyer's own text always counts as S0); otherwise **no**, naming the section that needs more and the smallest set-up that passes it. If no set-up passes a section: say so, with each writer's rates, the kinds of error, and whether training could plausibly fix them. **Nothing is fine-tuned without the owner's word.**

## 6. Decision table

| Situation | Do |
|---|---|
| A final record gives an element no article | The element is never drawn for part A. If a claim type has fewer than two elements with an article, its sheets carry one; say so. |
| The template fails the faithfulness script on a sheet | A bug: fix the template or the script before the freeze, with a test. After the freeze: report the rate as it is. |
| A model returns nothing, a refusal, or text that is not Arabic | Counted as unfaithful and not usable. No retry with another seed. |
| The two judges' usable rates for a writer differ by more than 15 points | Report both; the lower counts. Read ten of the outputs they split on and describe the pattern in the report (do not change a verdict). |
| The judges call usable an output the script found unfaithful | Report the count per judge: it measures the judges. The script's verdict stands. |
| T-A passes faithfulness and misses usability by less than 10 points | Report it as it is. A second round with reworded template sentences is allowed **only** as a new run on 120 new sheets, reported apart; never reword after reading the judges' answers and rejudge the same sheets. |
| A model's size could not be measured (Ollama's store layout changed) | Use the size `ollama list` prints, and say it is Ollama's figure. |
| The laptop is not on mains or is in use | No writer runs; time and memory are not reported from such a run. |
| Six claim types are not final yet but four are | Start with the four (80 sheets) only if phase C would otherwise wait more than three days; say so; add the other two when final. |

## 7. Done when

1. The sheets, the templates and the phrase table are committed before the first output; the Arabic check of the template sentences and of the phrase table is recorded.
2. Every writer's outputs for parts A and B exist (stripped), under `docs/research/core-logic/results/step-5/`, with the run log naming each model's digest.
3. The faithfulness report covers every output; the script's tests pass (an invented date, an invented amount in words, a wrong article number, a quote that is not in the article, a missing date in part B, a clean output).
4. Both judges answered every sampled sheet; no run used a tool.
5. `reports/step-5.md` holds the table of section 5, the two answers in one sentence each, the intervals, time, memory and sizes, and the sentence on one model family.

## Traps

- **Model judges prefer fluent prose, including fluent inventions.** The script decides faithfulness; a judge never overrules it.
- **Do not let a model write the requests** "to complete the picture": the app's own audit is the evidence there, and the definition says why.
- **A template is not safe because it is a template.** Its fixed sentences assert something in the lawyer's name; that is why they are judged like any other writer, and why the lawyer still reviews before export.
- **"Under 1 GB" is about model files on disk.** Report peak memory beside it, and do not present one as the other.
- **Balanced sheets are not real files.** Say so beside the rates.
