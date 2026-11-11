# Instructions given to the four note writers

Four subagents (Claude Sonnet, one per plan file) each received the text below on 11 November 2026; only the part number differs (`plan-1.jsonl` / `notes-1.jsonl` to `plan-4.jsonl` / `notes-4.jsonl`). The plan files come from `make_plan.py`. The writers were forbidden to open any `tests` or `baselines` folder or anything named `hidden`, and had no web access.

Result: 1,542 notes (62 per claim type for the 21 types, 240 `other`); by style: formal Arabic 807, Darija 394, French 217, formal Arabic with French terms 124. Checked afterwards: every plan id present once, no duplicate text.

```text
You write FICTITIOUS training data for a text classifier. Work only inside C:/Users/TheShadow/dz-arch-research/F/data/ . Do NOT open any other folder of this computer: in particular never open any "tests" folder, any "baselines" folder, or anything with "hidden" in its name. No web access is needed. No real persons, no real cases: invent every name, place detail, date and amount.

Read the plan file C:/Users/TheShadow/dz-arch-research/F/data/plan-1.jsonl . Each line is one note to write: "id"; "label" (a claim type of Algerian civil, commercial, family or labour litigation, or "other"); "label_ar" and "what" (what that claim type or out-of-catalogue matter is); "lang"; "length"; "voice"; "hint"; "values".

For EACH plan line write one note: the facts of a case as an Algerian lawyer would jot them down before drafting a statement of claim (عريضة افتتاح دعوى). Follow the attributes:
- lang "msa": formal Arabic. "darija": Algerian Darija in Arabic script, as Algerians really write it (ما ...ش، راه، تاع، بزاف، خلّص، الكرا، دراهم، كاين، ما حبّش، French loan words allowed, sometimes a French word in Latin letters). "fr": French, as an Algerian lawyer writes notes in French. "msa_fr_terms": formal Arabic with two or three French legal or commercial terms in Latin letters (mise en demeure, facture, bail, reconnaissance de dette...).
- "length" and "voice" as given. "values": include exactly that many dates and amounts (dates like 12/03/2025 or 12 مارس 2025, Algerian month names جانفي فيفري مارس أفريل ماي جوان جويلية أوت سبتمبر أكتوبر نوفمبر ديسمبر; amounts in دج / DA).
- "hint": if "names the legal notion explicitly", one legal term of the claim may appear (for example تطليق, فسخ, حضانة); if "describes the facts only", NO legal qualification at all: only what happened, so that the type must be understood from the facts.
- The note must fit the label and no other claim type of the plan. For label "other" write the matter described in "what" (it is deliberately outside the 21 types).
- Vary everything: who the plaintiff is (man, woman, company), cities and wilayas, professions, objects (flat, shop, car, goods, land, wages...), sentence openings, vocabulary. Do not reuse a sentence frame more than twice. Do not copy the wording of "what". Parties are «المدعي» / «المدعى عليه» or «موكلي» / «الخصم» or names, mixed. Never write the label id.

OUTPUT: the file C:/Users/TheShadow/dz-arch-research/F/data/notes-1.jsonl , one JSON object per line: {"id": "<id from the plan>", "text": "<the note; use \n between lines>"} , UTF-8, in the plan's order, one line per plan line, nothing else in the file. Write it in several steps of about 60 notes (first Write, then append with a short Python or shell command, or write part files notes-1-a.jsonl, notes-1-b.jsonl ... and concatenate them at the end) so that no single step is too long. At the end run a check (python) that the output has exactly as many lines as the plan, that every id of the plan appears once, and that each line parses as JSON; fix what is missing. RETURN one line: the number of notes written and the number per language.
```
