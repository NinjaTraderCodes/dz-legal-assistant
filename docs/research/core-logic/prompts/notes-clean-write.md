# Clean-note writer (a local model)

- **Models:** in the trial, **Jais-3-3.7B** and **Gemma 4 E4B** (think off), each on the same 20 scenarios; then the better one for 1,000 notes, and for the phrases of the composed notes ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), sections 8 and 9). Called on loopback (`127.0.0.1`, Ollama's `/api/chat`), one note per call, temperature 0.7, a fixed seed per scenario, `num_predict` capped by the style card's length. Record the model's digest with every note.
- **Jais 3 runs from the no-trace copy prepared for Part 7** (described under `docs/research/fable-part7/` on the branch `fable/architecture-review`; read it with `git show`, which also gives the copy's Ollama tag and digest). Its answer may begin with marker lines (`<ifm|think_faster>`, `</ifm|think_faster>`). A script strips them **before** the answer is split and stored: it removes only leading lines that are exactly a marker, and its test feeds it a stored answer with the two marker lines and one without. Gemma 4 E4B runs with `think` set to false.
- **Why local:** these notes exist so that the shipped classifier can be trained on text whose licence is beyond doubt. Jais 3 and Gemma 4 are open-weight models; re-read each model's licence on the day and record link and date in `notes/TERMS.md`.
- **Which scenarios.** The base recipe, without the parts the sampler gives only in English (`how`, `contest`, `extraValues`, `distractors`, `outsideFact`): code cannot turn them into Arabic without writing the note's wording itself. So clean scenarios have no `outside` notes; the report says so.
- **Placeholders:**
  - `{{scenario_ar}}`: the scenario rendered **by code**, one line per fact. An event line is a tag, then the element's Arabic proposition from `claim-elements.json`, then its date and amount: `[ثابت]` for stated, `[منفي]` for denied, `[محل نزاع] … — ينازع فيه: المدعى عليه` (or `المدعي`) for disputed. Events that are `notMentioned` are **left out** of the list: a small model told not to mention a fact tends to mention it. A slot that is not `unknown` gets one line from a fixed table (one Arabic line per slot value, written once, committed beside the code). A request line is the request's Arabic label from `claim-elements.json` with its amount. Dates are written `yyyy-mm-dd` and amounts in digits, so that the note's own format comes from the style lines.
  - `{{style_ar}}`: one Arabic line per style parameter, from a fixed committed table, for example: «اكتب ملاحظات قصيرة جدا: سطران إلى أربعة أسطر.» (length); «ابدأ بما تطلبه من المحكمة ثم الوقائع.» (order); «اكتب نقاطا مرقمة.» (tidiness); «اكتب التواريخ بالأرقام هكذا: 12/03/2025.» (dates); «اكتب المبالغ بالسنتيم.» (amounts); «عبر عن النفي بصيغة الحصر.» (negation manner); «اذكر المنازعة بفعل "يدعي".» (dispute manner, drawn from the style card, so that the dispute verb varies across notes).
- **Output:** plain text, four parts separated by fixed markers; a script splits them.
- **Labels and quotes, kept clean:** the labels of a clean note are **the scenario's own** (made by code), not a provider model's. A small local model cannot be trusted to return quotes, so the evidence positions are found without any provider model: by the rule cues of the reader's plain-code rung ([../specs/reader.md](../specs/reader.md), rung 2) and by exact search of the scenario's dates and amounts. **A field whose evidence is not found is blanked**, in the schema's terms (pipeline, section 5): never kept as a label without words. The blind judge (Sonnet 5.5, no tool, [notes-judge.md](notes-judge.md)) is then used **only as a filter**: it labels the note from its text, and a note whose labels differ from the scenario's is dropped. Nothing the judge wrote is stored in a clean note. Whether a provider's model may serve as such a filter is use (3) of the terms report; if the answer is no, the filter is the local models themselves (each judges the other's notes) and the rule cues, and the drop rate is reported.
- **Checks that reject a note:** any 5-word window of a scenario line or of a style line copied verbatim; a tag word of the scenario lines (`ثابت`, `منفي`, `محل نزاع`) used as a label; Latin letters; the Darija list of the pipeline. Expect a high rejection rate; it is measured and reported, and it is the price of the licence. Probe 2a ([../specs/probes.md](../specs/probes.md)) runs on the trial's 40 notes, so that a planted cue word shows before the 1,000 are written.
- Keep the prompt short: small models follow three instructions, not thirty.

--- PROMPT ---

أنت محام جزائري تدون ملاحظاتك الخاصة قبل تحرير عريضة افتتاح دعوى. اكتب الملاحظات بالعربية الفصحى القانونية فقط، بأسلوبك أنت، كما يكتبها محام لنفسه: وقائع محددة، ثم ما يطلبه من المحكمة، ثم الوثائق المتوفرة.

معطيات القضية (لا تنقل هذه الجمل كما هي؛ عبر عنها بكلماتك):

{{scenario_ar}}

طريقة الكتابة:

{{style_ar}}

قواعد:
- اذكر كل معطى بالتاريخ والمبلغ كما وردا، ولا تضف وقائع أو تواريخ أو مبالغ أو مواد قانونية من عندك.
- ما وسم بـ[منفي] فاكتب أنه لم يحدث. وما وسم بـ[محل نزاع] فاذكر من ينازع فيه وبماذا يتمسك. وما وسم بـ[ثابت] فاذكره واقعة حاصلة.
- لا تكتب الوسوم نفسها في الملاحظات.
- لا تشرح القانون ولا تستنتج.

اكتب الجواب بهذا الشكل بالضبط:

الموضوع: …
الوقائع: …
الطلبات: …
الوثائق: …
