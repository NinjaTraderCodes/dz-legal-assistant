# Reading articles from an Official Journal page image

- **Models:** first reading **Sonnet 5.5**, a fresh agent that opens the images with its Read tool, on the image paths it is given and nothing else; second reading **Codex** (`codex exec "<prompt>" … -i <image> [<image>]`: **prompt first, `-i` last**; rules of [../specs/codex-calls.md](../specs/codex-calls.md)). The two never see each other's text, the library's text or the corpus's. The same prompt serves the French check ([../law/README.md](../law/README.md), step 6) with `{{language}}` = French, read once by Sonnet 5.5.
- **Placeholders:** `{{worktree}}` (Sonnet only), `{{article_numbers}}` (one or more articles printed on the same page or pair of pages, as printed, for example «المادة 504، المادة 505»), `{{language}}` (Arabic | French), `{{image_note}}` (for example "one image: the page" or "two images: pages 46 and 47 of the issue"), and for the Sonnet agent `{{image_paths}}` (absolute paths).
- **Message for the Sonnet agent:** the PREAMBLE, then the PROMPT, then the LAST LINE for Sonnet. **For Codex:** the PROMPT, then the LAST LINE for Codex.
- **Output:** for Codex, schema `schemas/page-reading.schema.json`: `{ "articles": [ { "article": string, "found": boolean, "paragraphs": [string], "unreadable": [{ "paragraph": integer, "about": string }], "continuesOnNextPage": boolean, "numbersSeen": [string], "where": { "image": integer, "column": "right" | "left" | "full" | "none", "top": number, "bottom": number } } ] }`. The Sonnet agent replies with the same JSON and nothing else. `where` lets the script cut a band of the page at 600 dpi for the settler (no tool here can find words on an image).
- **Checking a Sonnet run:** the executor reads the run's reported tool-use count. A run with more tool uses than twice the number of images given is discarded and run again by a fresh agent, like a Codex call that ran a command.
- **Why "as printed":** the reading is compared letter by letter with another reading or with the store's text. A reader that modernises spelling or completes a word it expects produces differences that hide the real ones, or worse, agreements that are both wrong.
- **Known behaviour (12 November probe):** Codex read 65 of 66 words of art. 19 of Law 90-04 correctly and wrote «يقدم» for «يقيم». Expect about one word in sixty to be wrong in either reader, often a plausible word. That is why there are two readers and a third who looks again.

--- PREAMBLE (Sonnet agent only) ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it, and never write anywhere else. This task needs no file of the repository and writes no file.

**What you may open:** only these image files, with your Read tool, and nothing else:

{{image_paths}}

You run no command and write no file. Everything else is off limits, in particular: every file of the repository (above all `data/law/`, `docs/research/core-logic/law/` and the rule records under `docs/research/core-logic/rules/`); Codex's corpus and every other git branch; any other page image; the article store and everything else under `C:\Users\TheShadow\dz-core-research` beyond the images above; the plan file and the state file (`C:\Users\TheShadow\dz-core-research\STATE.md`); any memory file (do not read or write anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`; the web. If an image cannot be opened, say so in your reply and stop.

--- PROMPT ---

The images are pages of the Official Journal of the Algerian Republic ({{language}} edition). {{image_note}}

Transcribe each of these articles from the images, exactly as printed: **{{article_numbers}}**

- Copy every word and every digit as it appears. Do not correct spelling, do not modernise it (keep «ى» or «ي», «ا» or «أ», «ئ» or «ي», as printed), do not complete or replace a word by what you expect the law to say, and do not translate.
- One printed paragraph per item of "paragraphs". A numbered or lettered item of a list is its own paragraph, with its number or letter as printed. A new paragraph starts where the print starts a new line for it; do not join or split paragraphs by meaning. Do not include the heading «المادة …» itself, and do not include any article you were not asked for.
- Copy numbers exactly, including digits in brackets, for example «ستة (6) أشهر». List in "numbersSeen" every number, period and article reference in the article, as printed.
- If a word or digit is not clearly legible, write `[؟]` in its place and describe it in "unreadable" (which paragraph, and what you could see). **Never guess a word.** A marked gap is useful; a plausible guess is harmful.
- In "where", say where the article is printed: which image (counted from 1), which column, and the top and bottom of the article as fractions of the page's height (0 at the top, 1 at the bottom). Rough is enough.
- If an article is not on these pages, set "found" to false and "where" to `{"image": 0, "column": "none", "top": 0, "bottom": 0}`. If it continues beyond the images, set "continuesOnNextPage" to true and transcribe what is visible.

Return only the JSON object.

--- LAST LINE (Sonnet agent) ---

Do not open any file other than the images listed above, and do not run any command.

--- LAST LINE (Codex) ---

Do not run any command and do not open any file.
