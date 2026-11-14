# The local filter of training notes: a closed sheet about one note

- **Model:** a **local** model on loopback (Ollama, `127.0.0.1`), never a Claude model and never Codex (owner, 14 November 2026: [../notes/TERMS.md](../notes/TERMS.md)): **Gemma 4 E4B** (`gemma4:e4b`, think off) or **Jais-3-3.7B** (`jais3-3.7b-low`), whichever won the filter trial ([../specs/notes-pipeline.md](../specs/notes-pipeline.md), "The filter trial"). One note per call; temperature 0; a fixed seed; at most 120 output tokens; the answer constrained to the schema below through Ollama's `format`. A tested script strips the marker lines a Jais 3 copy may put before its answer.
- **Placeholders:** `{{subject}}`, `{{facts}}`, `{{requests}}`, `{{documents}}` (the note's four fields; an empty field is written «(فارغ)»); `{{questions}}` (the sheet, numbered from 1, each question with its lettered options, built by `notes filter-sheet`); `{{count}}` (the number of questions).
- **Output schema:** `schemas/filter-sheet.schema.json`: `{ "answers": ["a" | "b" | "c" | "d" | "e" | "f" | "g" | "h" | "i" | "j" | "k" | "l"] }`: one letter per question, in order. A letter that is not among its question's options makes the answer invalid.
- **The sheet's wordings, fixed here** (version `filter-sheet-1`; a change is a new version, recorded in every note's `provenance.filter.version`). The options of each question are shuffled per note, except "the note does not mention it", which is always last:

  | Question | Arabic wording | Options |
  |---|---|---|
  | a claim type (one question for each claim type of the scenario, and for three distractor claim types drawn by seed **from other areas**) | «هل تتعلق هذه المذكرة بدعوى: «<the claim type's Arabic label from `data/law/claims`>»؟» | «نعم»؛ «لا». A scenario's own claim type answered «لا» is a claim disagreement (the note is dropped); a distractor answered «نعم» is a decoy failure. **An `outside` scenario gets no claim question**: the code check of the telling fact's quote stands alone, and the report says so |
  | the plaintiff's role, for a claim type that offers a choice | «ما صفة المدعي في هذه الدعوى؟» | the Arabic labels of that claim type's roles from `claim-elements.json`; «لا تذكر المذكرة ذلك» |
  | what a date or an amount is for (one question for each value of the scenario that has a role) | «إلى ماذا يعود هذا التاريخ: «<the date as the note writes it>»؟» / «إلى ماذا يعود هذا المبلغ: «<the amount as written>»؟» | the Arabic propositions or labels of the roles that value could have in this scenario (its events that take that kind of value, its requests, its date slots), at most ten; «لا شيء مما سبق» |
  | an element | «ماذا تقول المذكرة عن: «<the element's Arabic proposition>»؟» | «تذكر أن ذلك حصل»؛ «تذكر أن ذلك لم يحصل»؛ «تذكر أن الطرفين مختلفان في حصوله»؛ «لا تذكره المذكرة» |
  | a yes/no slot | the slot's own Arabic question from `slots.json` | «نعم»؛ «لا»؛ «لا تذكر المذكرة ذلك» |
  | a choice slot | the slot's own Arabic question | the choices' Arabic labels; «لا تذكر المذكرة ذلك» |
  | a request | «هل تطلب المذكرة: «<the request's Arabic label>»؟» | «نعم»؛ «لا» |

  For an element whose proposition is itself a negative («لم يسدد …»), the options keep their meaning: "it happened" means the proposition as written is what the note says.
- **What the script does with the answer:** the five-step rule of the notes pipeline, section 4a (invalid, decoy, claim, blank, keep). The filter's letters are compared with the scenario; **nothing the model wrote is stored in the note** except the note's status. The model never sees the scenario's answers.
- **Decoys:** two elements whose event kind none of the scenario's claim types has (nor, for a near-miss, the claim type it resembles), and one request kind that has an Arabic label and is in none of those claim types' request lists. **Not asked, and so not filtered:** each event's actor and who disputes it; the noise measurement covers them.
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

"answers" قائمة فيها {{count}} حرفا بالضبط، بترتيب الأسئلة: الحرف الأول جواب السؤال 1، والثاني جواب السؤال 2، وهكذا. الحروف الممكنة هي "a" و"b" و"c" و"d" و"e" و"f" و"g" و"h" و"i" و"j" و"k" و"l"، ولا يصح لسؤال إلا حرف من خياراته.
