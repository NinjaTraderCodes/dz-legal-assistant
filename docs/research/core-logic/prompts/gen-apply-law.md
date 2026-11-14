# The paragraph that applies the law to the confirmed facts (step 5, part A: the legal discussion)

- **Writers compared:** a **template** (no model: code fills three fixed sentences, the third chosen by the role of the cited statement: [../specs/step5-templates.md](../specs/step5-templates.md), part A), **Gemma 4 E4B** (think off), **Jais-3-3.7B**, **Jais-3-0.9B**, **Qwen3.5-2B** (think off; the value is the string "false"). All local, on loopback, one model at a time, with the digests recorded in Part 7 (`git show fable/architecture-review:docs/research/fable-part7/run/models.json`). Jais 3 runs from the no-trace copies prepared for Part 7 (`jais3-3.7b-low`, `jais3-0.9b-low`). One sheet per call, temperature 0.2, a fixed seed, at most about 220 output tokens. Time per sentence is measured only under the measurement rule (mains, Best performance, idle CPU).
- **The fact sheets:** 120, built by code from six final rule records and the sampler, 20 per claim type, with **two elements per sheet** (240 paragraphs): [../specs/step5-templates.md](../specs/step5-templates.md), section 2. For each: the confirmed facts, the fictitious names of the parties, and the articles the record gives as `basis` for the element concerned, with their quoted text. The sheets are rendered in Arabic by code.
- **Placeholders:** `{{articles_ar}}` (each article: its citation name, number, and the quoted paragraphs), `{{facts_ar}}` (the confirmed facts as a numbered list, with the parties' names), `{{element_ar}}` (the element to argue: the proposition and what is asked).
- **Output:** plain Arabic text: one paragraph.
- **Before anything reads an output:** a tested script strips the marker lines a Jais 3 copy may put at the start of its answer (everything up to and including the closing marker). Otherwise the two Jais writers would be recognisable to the judges by their first lines, and the lines would count against them.
- **The main measure is not style.** A script checks every output against its sheet: **every number, date, sum and article reference in the output must be on the sheet**, after the normalisations of the labelling guide, section 8 (digits in either script, numbers written in words, dates in every format, sums with their units). Names are checked against the sheet's own list of names only: a script cannot recognise every invented Arabic name, so the judges check the rest, and the report says so. One item that is not on the sheet makes the output **unfaithful**. Reported per writer: the share of outputs with no invented item, with the interval. An output that puts between «» words that are not a stretch of the sheet's quoted article is unfaithful too. Only then the blind judging ([gen-judge.md](gen-judge.md)); the bars and the result table are in the specification, section 5.
- **Files:** the sheets, the outputs (stripped), the faithfulness report, the judges' answers and the table go under `docs/research/core-logic/results/step-5/` (all fictitious); the run log records each model's digest.
- **Why:** with steps 1 to 4 in place, the generative model no longer chooses the claim, the articles, the deadlines or the facts. What is left to it is one sentence. The question of step 5 is whether a model is needed for that at all, and whether any fine-tuning is. **Nothing is fine-tuned without the owner's word.**

--- PROMPT ---

أنت محام جزائري تحرر فقرة واحدة من عريضة افتتاح دعوى: الفقرة التي تطبق النص القانوني على وقائع القضية.

النصوص القانونية (لا تستشهد بغيرها):

{{articles_ar}}

الوقائع الثابتة في الملف (لا تضف إليها شيئا):

{{facts_ar}}

المطلوب بيانه:

{{element_ar}}

اكتب فقرة واحدة بالعربية الفصحى القانونية، من ثلاث إلى خمس جمل، تبدأ بـ «حيث»، تذكر الواقعة ثم النص الذي يحكمها ثم ما يترتب عليه في هذه القضية.

قواعد صارمة:
- لا تذكر أي تاريخ أو مبلغ أو اسم أو رقم مادة غير وارد أعلاه.
- لا تنسب إلى النص ما لا يقوله، ولا تنقل منه إلا ما ينطبق على الواقعة.
- لا تذكر اجتهادا قضائيا ولا رأيا فقهيا.
- لا تكتب مقدمة ولا خاتمة ولا شرحا: الفقرة وحدها.
