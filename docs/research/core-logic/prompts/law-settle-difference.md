# Settling a difference between two transcriptions

- **Model:** Opus 5.5, a fresh agent that opens the images with its Read tool, on the image paths it is given and nothing else. One run settles all the differences of up to about ten articles.
- **Used for:** two page readings of a missing article that differ ([../law/README.md](../law/README.md), step 5); a fresh page reading that differs from the store's text ("Re-readings"); a wording of the consolidated PDF that differs from the page reading (step 7). In every case the two wordings are shown the same way, and the settler is not told where either comes from.
- **Placeholders:** `{{worktree}}`; `{{items}}`: for each difference, its kind (`words` or `paragraphBreak`), the absolute image paths (the page at 300 dpi, and close-ups at 600 dpi cut by script in tiles of at most 1400 pixels a side and 1.1 million pixels, over the region where the first reader said the article is, with the next column when the article reaches the foot of its column: the image viewer reduces a larger image before the model sees it, so a whole 600-dpi column reached the settler at about 185 dpi, no sharper than the page; measured on 13 November 2026), the article number, the paragraph, the ten words before and after, and the two candidates **labelled X and Y in random order**. For a `paragraphBreak` item, X and Y are "a new paragraph starts before the word «…»" and "the text runs on", in random order.
- **Output:** JSON array, one entry per difference: `{ "article", "paragraph", "kind", "printed": "X" | "Y" | "neither", "words": "<what the page prints>", "seen": "<what you see on the image that decides it: letter shapes, dots, the digit, the line break>", "sure": true | false }`.
- **Checking a run:** the executor reads the run's reported tool-use count. A run with more tool uses than twice the number of images in `{{items}}` is discarded and run again by a fresh agent.
- **Why blind to the source:** told that one wording is "the library's" or "the corpus's", a model sides with it. The page decides, not the pedigree.
- **If `sure` is false or `printed` is "neither":** a second Opus run looks at close-ups at 800 dpi (tiles of the same size, so a smaller part of the page each); if still unsure, the article is **not verified**, and every item citing it is grade D.

--- PROMPT ---

The repository is the worktree `{{worktree}}` (`C:\Users\TheShadow\dz-fable-core`, branch `fable/core-logic`). Your shell may start elsewhere: use absolute paths under it, and never write anywhere else. This task needs no file of the repository and writes no file.

**What you may open:** only the image files named in the items below, with your Read tool, and nothing else. You run no command and write no file. Everything else is off limits, in particular: every file of the repository (above all `data/law/`, `docs/research/core-logic/law/` and the rule records); Codex's corpus and every other git branch; any other page image; the article store and everything else under `C:\Users\TheShadow\dz-core-research` beyond the images named below; the plan file and the state file (`C:\Users\TheShadow\dz-core-research\STATE.md`); any memory file (do not read or write anything under `C:\Users\TheShadow\.claude`); anything under `C:\dz-hidden*`; the web. If an image cannot be opened, say so in your reply and stop.

Two transcriptions of the same article, from a page of the Official Journal of the Algerian Republic, differ on a few words or on where a paragraph ends. For each difference below you have the page image, close-ups of the page around the article at full resolution, and the two candidates, X and Y. Decide what the page prints.

{{items}}

For each difference:

1. Open the page and find the line (the page is shown to you reduced). Then open the close-up that holds that line (one or two; their position on the page is given) and decide from the close-up, never from the reduced page. Read the word or digit **letter by letter**: the shape of each letter, the dots above and below (count them), the hamza and its seat, the madda, whether a digit is 6 or 8 or 0, 1 or 7, 3 or 8. For a paragraph break, look at the line itself: a new printed paragraph starts on a new line, usually indented or after a short line; a line that simply wraps does not.
2. Decide: "X", "Y", or "neither" (then write what you read in "words").
3. In "seen", say what on the image decides it, concretely ("two dots under the third letter, so ي not ى"; "the digit has a closed lower loop and an open upper stroke: 6"; "the previous line ends at mid-column and this one is indented").
4. "sure": true only if the image is clear enough that you would not expect another reader to disagree. A plausible word is not a reason: legal texts often use the less expected word.

Do not use the meaning of the sentence to choose, except to notice that a candidate is not a word at all. Do not consult any other copy of the text.

Return only the JSON array.

Do not open any file other than the images named in the items, and do not run any command.
