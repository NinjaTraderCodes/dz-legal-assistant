# The facts section of the petition, from the confirmed facts (step 5, part B)

- **Writers compared:** the template T-B (no model: code composes one «حيث» paragraph per fact from the phrase table), **Jais-3-0.9B**, **Qwen3.5-2B** (think off; the value is the string "false"), **Jais-3-3.7B**, **Gemma 4 E4B** (think off). All local, on loopback, one model at a time, with the digests recorded in Part 7. One sheet per call, temperature 0.2, a fixed seed, at most about 400 output tokens. Rules, sheets and measures: [../specs/step5-templates.md](../specs/step5-templates.md).
- **Placeholders,** all rendered in Arabic by code from the sheet: `{{parties_ar}}` (the plaintiff and the defendant by name and capacity); `{{facts_ar}}` (the sheet's facts as a numbered list in date order: each fact's sentence from the phrase table with its date and its amount, and for a disputed fact the words «(ينازع فيها المدعى عليه)»).
- **Output:** plain Arabic text: the paragraphs of the section, nothing else.
- **Before anything reads an output:** the tested script strips the marker lines a Jais 3 copy may put at the start of its answer.
- **The main measure is the faithfulness script** (the specification, section 4): nothing that is not on the sheet, and no date or amount of the sheet missing. Then the blind judges ([gen-judge.md](gen-judge.md)).
- **No tool, no file:** the model receives this text and nothing else.

--- PROMPT ---

أنت محام جزائري تحرر قسم «الوقائع» من عريضة افتتاح دعوى.

أطراف الدعوى:

{{parties_ar}}

الوقائع الثابتة في الملف، مرتبة زمنيا (لا تضف إليها شيئا ولا تحذف منها شيئا):

{{facts_ar}}

اكتب قسم الوقائع بالعربية الفصحى القانونية، في فقرات قصيرة تبدأ كل واحدة منها بـ «حيث»، بالترتيب الزمني.

قواعد صارمة:
- اذكر كل واقعة من القائمة أعلاه مرة واحدة، بتاريخها ومبلغها كما وردا.
- لا تذكر أي تاريخ أو مبلغ أو اسم أو واقعة غير واردة أعلاه.
- الواقعة التي ينازع فيها المدعى عليه اذكرها ثم اذكر أنه ينازع فيها، دون أن تحسم النزاع.
- لا تذكر أي نص قانوني ولا أي طلب: هذا القسم للوقائع وحدها.
- لا تكتب عنوانا ولا مقدمة ولا خاتمة ولا شرحا: الفقرات وحدها.
