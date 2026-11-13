# Support check: does this text support this statement? (Codex, blind pairs)

- **Model:** Codex. Batches of 40 pairs **across units**, modules first, then the claim types by area: about 26 calls in all, covering every statement, deadline, prior step, section rule and exit of every reconciled record. The first module, taken end to end alone (HANDBOOK §11.8), gets a smaller batch of its own.
- **Placeholders:** `{{pairs}}`: numbered pairs, each `N. STATEMENT: <English sentence built by script from the item> / TEXT: <the quoted Arabic paragraphs>`.
- **How the pairs are built (script `build-support-pairs`):** for each item of the quoted reconciled records, a sentence from its structured values ("A claim before the social section must be filed within 6 months of the delivery of the non-conciliation record; otherwise the right to sue is forfeited."), paired with its quoted text. **One pair in five is a seeded false pair,** built by the script and recorded in a key the checker never sees, kept outside the repository (`C:\Users\TheShadow\dz-core-research\support-keys\<batch>.json`): the same statement with another article's text; the right text with a changed period, a swapped starting event, "may" for "must", or a dropped exception. Pairs are shuffled within the batch, across its units.
- **Output schema:** `schemas/support.schema.json`: `{ "answers": [{ "n": integer, "supports": "yes" | "partly" | "no", "missingOrWrong": string }] }`. The script splits the answers of a batch back into `rules/reports/<unit>.support.json`.
- **How it is used:** first the seeded pairs: **if Codex accepts more than one seeded false pair in five, its "yes" answers in that call are not trusted** and the call is rerun with fresh pairs (and the prompt reviewed). One in five is a working bar: a checker that lets more false pairs through than that would let real errors through at a rate nobody could accept for a deadline. Then the real pairs: every "no" and "partly" becomes an attack in the unit's register, for the verifier.
- **Why blind and seeded:** a checker that is shown a finished record says "supported" almost always. Seeding measures that tendency instead of assuming it away. The prompt's sentence that "some of the statements are wrong on purpose" is true of every call (one pair in five), and it applies equally to the real pairs and the seeded ones, so the seeded rate still measures the checker as it treats the real pairs. It does not tell the checker about the planted-error test, which uses the same prompt and is never announced.

--- PROMPT ---

You are checking, pair by pair, whether an official Algerian legal text supports a statement. The pairs come from many different sources and some of the statements are wrong on purpose. Judge each pair on its own.

For each pair, answer:

- **"yes"** only if every part of the STATEMENT is said by the TEXT: who, what, each number and period, the event a period runs from, how the period is counted, each condition, the consequence, and whether something is an obligation («يجب», «يتعين», «لا يجوز») or a possibility («يجوز», «يمكن»).
- **"partly"** if the TEXT says some of it and is silent on the rest. In "missingOrWrong", say exactly which part the TEXT does not say.
- **"no"** if the TEXT contradicts the STATEMENT or is about something else. In "missingOrWrong", copy the Arabic words that contradict it, or say what the TEXT is about.

Read the numbers in the Arabic carefully, in words and in digits. Do not use what you know of Algerian law to fill a gap: if the TEXT does not say it, the answer is not "yes", even if the STATEMENT is true.

{{pairs}}

Answer every pair, by its number.

Do not run any command and do not open any file.
