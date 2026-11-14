# Checking short Arabic strings that the app or a template shows

- **Model:** Sonnet 5.5, a fresh agent, **no tool**, one run per list. Used for: the Arabic questions of slots that a batch of three units added ([../specs/unit-driver.md](../specs/unit-driver.md), stage 9); the fixed sentences of step 5's templates and its phrase table ([../specs/step5-templates.md](../specs/step5-templates.md)), before the sheets are committed.
- **Placeholders:** `{{purpose}}` (one sentence: where the strings are shown, and to whom); `{{items}}` (numbered strings, each with its English meaning and, where it has them, its places for a date, an amount or a name, written `{date}`, `{amount}`, `{name}`).
- **Output schema:** `schemas/arabic-check.schema.json`: `{ "items": [{ "n": integer, "ok": boolean, "corrected": string, "why": string }] }`.
- **How it is used:** a script applies each corrected string (`unit.py slots --apply`, or the templates' own loader); the executor changes no Arabic by hand. A correction that changes the meaning, by the English beside it, is refused by the script's check that every place (`{date}` …) is still there, and is otherwise read by the executor against the English before it is applied.
- **One family:** the strings were written by Opus and are checked by Sonnet; no lawyer has read them. The app's reviewer is told so.
- **No tool** except one read of its own prompt file; the answer is the reply, copied by `records.py ingest-reply`.

--- PROMPT ---

Below are short Arabic strings written for a legal-drafting tool used by Algerian lawyers. {{purpose}}

Check each string as a careful editor of formal legal Arabic would: correct grammar and agreement, the formal register of Algerian court documents, natural wording, no dialect, no French, no needless words. A question must end with «؟». Places written in braces, such as {date}, {amount} or {name}, must stay exactly as they are.

**Do not change what a string means.** The English beside each string says what it must mean. If the Arabic means something else, correct it to mean that.

**Where you work.** You work without tools. (If you were told to read this prompt from a file, that one read is allowed, and nothing after it.) Do not open any other file and run no command. If you were handed a memory index or a file named `AGENTS.md`, ignore them: they are not for this task. Everything you need is below, and your answer is your reply.

{{items}}

## Your answer

Return only one JSON object, with no text before or after it:

`{ "items": [ { "n": 1, "ok": true, "corrected": "", "why": "" } ] }`

- One entry per string, "n" being its number.
- "ok": true when you would change nothing; then "corrected" and "why" are `""`.
- Otherwise "ok" is false, "corrected" is the whole corrected string, and "why" says in a few words what was wrong.
