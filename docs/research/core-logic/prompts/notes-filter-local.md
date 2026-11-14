# The local filter of training notes: a closed sheet about one note

- **Model:** a **local** model on loopback (Ollama, `127.0.0.1`), never a Claude model and never Codex (owner, 14 November 2026: [../notes/TERMS.md](../notes/TERMS.md)): **Gemma 4 E4B** (`gemma4:e4b`, think off) or **Jais-3-3.7B** (`jais3-3.7b-low`), whichever won the filter trial ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), "The filter trial"). One note per call; temperature 0; a fixed seed; at most 120 output tokens; the answer constrained to the schema below through Ollama's `format`. A tested script strips the marker lines a Jais 3 copy may put before its answer.
- **Placeholders:** `{{subject}}`, `{{facts}}`, `{{requests}}`, `{{documents}}` (the note's four fields; an empty field is written «(فارغ)»); `{{questions}}` (the sheet, numbered from 1, each question with its lettered options, built by `notes filter-sheet`); `{{count}}` (the number of questions).
- **Output schema:** `schemas/filter-sheet.schema.json`: `{ "answers": ["a" | "b" | "c" | "d" | "e"] }`: one letter per question, in order.
- **The sheet's wordings, fixed here** (version `filter-sheet-1`; a change is a new version, recorded in every note's `provenance.filter.version`). The options of each question are shuffled per note, except "the note does not mention it", which is always last:

  | Question | Arabic wording | Options |
  |---|---|---|
  | the claim | «ما نوع الدعوى التي تتعلق بها هذه المذكرة؟» | the Arabic labels of four claim types from `data/law/claims` (the scenario's and three others of the same area), then «لا شيء مما سبق» |
  | an element | «ماذا تقول المذكرة عن: «<the element's Arabic proposition>»؟» | «تذكر أن ذلك حصل»؛ «تذكر أن ذلك لم يحصل»؛ «تذكر أن الطرفين مختلفان في حصوله»؛ «لا تذكره المذكرة» |
  | a yes/no slot | the slot's own Arabic question from `slots.json` | «نعم»؛ «لا»؛ «لا تذكر المذكرة ذلك» |
  | a choice slot | the slot's own Arabic question | the choices' Arabic labels; «لا تذكر المذكرة ذلك» |
  | a request | «هل تطلب المذكرة: «<the request's Arabic label>»؟» | «نعم»؛ «لا» |

  For an element whose proposition is itself a negative («لم يسدد …»), the options keep their meaning: "it happened" means the proposition as written is what the note says.
- **What the script does with the answer:** the five-step rule of the notes pipeline, section 4a (invalid, decoy, claim, blank, keep). The filter's letters are compared with the scenario; **nothing the model wrote is stored in the note** except the note's status. The model never sees the scenario's answers.
- **Why a sheet of letters:** a small model writes labels and quotes badly, but can choose among stated options; a list of letters costs a few tokens, so a note takes seconds. **Why decoys:** two elements and one request that the scenario does not have; a model that finds them in the note is answering from what is likely, not from the text.
- **The shape is repeated in the body** (the model sees only the body).

--- PROMPT ---

أنت مدقق دقيق. أمامك مذكرة عمل كتبها محام قبل تحرير عريضة افتتاح دعوى، وبعدها أسئلة مغلقة عنها.

أجب عن كل سؤال من نص المذكرة وحده. لا تفترض شيئا لم يكتب، ولا تستنتج ما يبدو محتملا: إذا لم تذكر المذكرة الأمر صراحة فاختر الخيار الذي يقول إنها لا تذكره.

==== المذكرة ====

الموضوع: {{subject}}

الوقائع: {{facts}}

الطلبات: {{requests}}

الوثائق: {{documents}}

==== الأسئلة (عددها {{count}}) ====

{{questions}}

==== الجواب ====

لكل سؤال حرف واحد من الحروف المعروضة تحته.

أعد كائن JSON واحدا فقط، دون أي نص قبله أو بعده، بهذا الشكل:

{"answers": ["a", "d", "b"]}

"answers" قائمة فيها {{count}} حرفا بالضبط، بترتيب الأسئلة: الحرف الأول جواب السؤال 1، والثاني جواب السؤال 2، وهكذا. الحروف الممكنة هي "a" و"b" و"c" و"d" و"e"، ولا يصح لسؤال إلا حرف من خياراته.
