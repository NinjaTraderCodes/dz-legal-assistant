# Design: the case file «الملف»

<!-- Written from the built site (branch claude/case-file-site, 5 Nov 2026), after the finish review. The direction was approved as a draft on 4 Nov 2026; the candidate directions were derived by hand with the Impeccable method (its concept-seed program was not run). -->

Scope: the public website in `site/` only. The desktop app, the demo at `/app` and every app screenshot keep their own look. `src/styles/tokens.css` is generated from the app's theme and still serves the pieces of the app the site draws (`AppPiece.astro`) and the petition; the site's own world is `src/styles/world.css`.

## The idea

The website is a lawyer's case file. Each section of the home is a folder in a cardboard colour, with a numbered tab rising above its top edge; the real screens, the real AI output and the law are white sheets filed on the folders, held by a clip or a staple; progress is shown the way a registry shows it, with violet ink: ticks in a register and one stamp. It refuses both category defaults: the dark SaaS page with a neon accent, and the stock legal look (scales, books, gold on brown).

Dark is the default theme (decided 4 Nov 2026): the same file in the archive room after hours. The folders deepen; the sheets stay paper.

## Colour

Strategy: **full palette**. Each folder colour says which file the reader is in; whole sections are coloured, never small accents.

| Token | Light | Dark | Job |
|---|---|---|---|
| `--w-kraft` | `#CDBB98` | `#1B1915` | the ground: the bar, the footer (back cover), the browser's theme colour |
| `--w-board` | `#D9CAAB` | `#26231E` | the kraft folder, one shade off the ground so its tab shows against the bar |
| `--w-salmon` | `#E8A88F` | `#562A22` | the home's own folders (01, 03, 05) |
| `--w-blue` | `#A6C6DA` | `#1F3341` | the five-minute test, the try page, the documents |
| `--w-green` | `#AECBA3` | `#22352A` | demo or download (07), privacy |
| `--w-yellow` | `#ECCD74` | `#3E3418` | the law library |
| `--w-sheet` | `#FBFAF6` | `#F4F1EA` | paper, in both themes |
| `--w-text` | `#1D1A17` | `#ECE7DE` | text on folders (on sheets always `#1D1A17`) |
| `--w-muted` | `#463D33` | `#BFB4A4` | secondary text on folders (on sheets `#5B5249`) |
| `--w-ink` | `#4A3B8F` | `#B4A8F2` | violet ink: ticks, stamps, tags (on sheets always `#4A3B8F`) |
| `--w-sheet-red` | `#B3261E` | same | what is missing, on sheets only |

In the dark theme the muted text takes each folder's hue (72% text, 28% folder; rose `#E3BCAE` on salmon) rather than one grey, 5.9 to 7.9:1. Small text has a 12 px floor, and running text wraps with `text-wrap: pretty` so no last word stands alone. Measured: text on folders 8.6 to 14:1, muted text 5.3 to 11:1, violet ink 4.5 to 8.2:1, all at AA or better in both themes. Red text would fail on the light folders (3.2 to 4.2:1), so red appears only on sheets (6.3:1). A sheet re-declares the app's tokens at their light values, so the app's own cards and pieces stay legible on paper in the dark theme. axe reports 0 violations on every page in both themes.

## Type

- **Titles: Markazi Text** (OFL, variable 400–700), Arabic and Latin from one family, so French will sit with them. Never italic. It also sets the petition fragment in "The problem" and, on phones until the page has loaded, the petition's text (below).
- **Text and controls: Readex Pro** (OFL, variable, `HEXP` pinned).
- **Stamps only: Aref Ruqaa** (OFL), the hand of everyday administrative writing, cut to the letters of the stamp texts (20 KB). English stamps are Readex Pro capitals.
- **The petition and the app's pieces: Noto Naskh Arabic and Inter**, as in the app.

Each face is cut into an Arabic and a Latin file by `scripts/fonts.mjs`; a page preloads its script's two faces (73.8 KB on Arabic pages, 61.1 KB on English ones). On a phone the petition's and the app's faces would be fetched with the first layout although they are used only far down the home; `theme-init.js` marks the page `data-faces="later"` and `ui.js` lifts it after load (Lighthouse mobile LCP 3.5 s with them, 2.4 s without). Desktops and visitors without scripts are not affected.

## Materials

- **Folder** (`Folder.astro`): full width, its cardboard colour with a fine board grain and the light of a desk lamp (from the top on the reading-start side, the shade towards the far corner; centred in the dark theme), a trapezoid tab on the reading-start side with a register number and a few words. Each folder casts a soft shadow onto the one above it, and each tab overlaps the bottom of the folder above, so the page reads as one stack. The bar, the footer and the page ground carry the same grain without the lamp. Added on 5 Nov 2026, after the flat fields proved tiring to look at.
- **Sheet** (`.sheet`, and `.panel` and `.card`, which are sheets): paper with 2 px corners and a short, firm shadow. A sheet carries at most one clip (`Clip.astro`) or one staple.
- **Stamp** (`.stamp`): violet ink in a double frame, turned −5° to −7°, its edge and ink made uneven by the inline filter `#ink` in `Base.astro`. Two on the whole site: «دون اتصال بالإنترنت» / "Works offline" on the hero, «جاهزة للإيداع» / "Ready to file" on the register at 9 / 9, with the line «بعد مراجعة المحامي وتوقيعه» / "once the lawyer has reviewed and signed". No emblem, no court, no imitation of a seal.
- **The petition** (`StepStory.astro`): the app's own paper for the fictitious sample, section by section, with the alignment, weight, spacing and headings the form definition gives it (`src/content/paper-ar.json`, read from the demo by `tests/paper-dump.mjs`; never retyped). The legal grounds are whole paragraphs as the app writes them, without quotation marks; the code's part is marked on the site only (a light underline and the "from the official text" chip).
- **Register** (`StepStory.astro`): the nine entries ruled like a register, numbered 01 to 09, each ticked with a violet pen stroke (`#tick-shape`).
- **Report** (`CheckReport.astro`): the formal check typed on a sheet: the app's label, the score, nine boxes that fill with violet ink (red while an element is missing), the verdict.
- **Log and guarantees**: the AI's four stages are a numbered log under a heavy rule, two of them with their capture beside them and the drafting stage with the live panel; the guarantees are one clipped sheet of four numbered entries with two pieces of the app attached beside it. The hidden-set figures (94%, 67%, 36%) are typed on a clipped sheet with their counts and their source.
- **Buttons**: carbon fill (`.btn-primary`) for the one primary action of a block, carbon outline otherwise; 2 px corners, 48 px tall.
- **Tags** (`.tag`): a square frame in violet ink; the app's own rounded chips stay inside its pieces.

## The home, folder by folder

| No. | Folder | Contents |
|---|---|---|
| 01 | salmon | the title, the tagline, Try the demo (filled), Download for Windows, the note; the laptop lying on the folder with the "works offline" stamp on its corner |
| 02 | board | the problem; a petition fragment with the defendant's domicile blank, the red note under it with art. 15-3, the check at 7 / 9 |
| 03 | salmon | the scroll story: the register and the petition; then, at the peak, one line and the two actions |
| 04 | board | the proof: the drafting log (pictures on stages 01 and 03, the live panel on 04), the hidden-set report, and the guarantees as one numbered sheet with the law-library card and the refused-export notice attached |
| 05 | blue | the five-minute test, five numbered sheets |
| 06 | board | the team as a signature block: identity photos clipped to the file, the names over a rule |
| 07 | green | the demo and the download as two slips: the file ends on the action |

The home had eight folders until 5 Nov 2026; after the second Impeccable critique the AI log and the exhibits became one proof folder, the repeated pictures went (the drafted paper is the story's alone), an action strip follows the story, and the team moved before the download.

Secondary pages are one folder each (`Base.astro`, `folder`): try and documents blue, privacy green, law yellow, download, about and 404 board. Their former labels above the title are now the folder's tab.

## Motion

- **The scroll story** (desktop, motion welcome): the register is pinned beside the petition. Each entry is ticked as its section is written on the paper; after the defendant, the domicile disappears, the check drops and names art. 15-3, then it comes back; at 8 / 9 the check turns red and names the missing grounds; the grounds are written last (the quoted rule, then the AI's lines, then the review box), the check reaches 9 / 9 and the stamp strikes once.
- **Stamps** elsewhere strike once as they come into view (`data-strike`, 170 ms).
- The try page's steps keep their reveal and their filling line, now in violet.
- No parallax and no fade-up on every block. With reduced motion, and on phones, the final state is shown: ticked, 9 / 9, stamped.

## Never

- State emblems, «الجمهورية الجزائرية الديمقراطية الشعبية», court names or seal designs as site decoration. Inside the petition the document's own header lines stay: they are part of the document.
- Scales, gavels, columns, books or gold as decoration (the brand mark stays).
- Stains, torn edges, leather, wood, or a photographed paper texture. The folders' surface is a fine generated board grain under a lamp light (`--w-grain`, `--w-speck`, `--w-lamp`, `--w-shade` in world.css), faint enough to be felt rather than seen; it never carries meaning and never lowers text contrast.
- Small spaced-out labels above headings, grids of same-size cards, cards in cards, gradient text, glow, a coloured stripe down the side of a box. (The gold line beside the AI-written section on the petition is the app's own authorship mark and stays.)
- Italic display type; a typewriter look.
- "Filed" or «مودعة» for what the software does.
- Anything loaded from another origin; any image without its licence or source.

## Finish review: where the build departs from the approved draft

- **The stamp is on the register, not on the petition**, and the folder does not close. The sample case forces the app's «مسودة» watermark on the paper, and a "ready to file" stamp over a draft watermark would contradict it; on the register it marks what is actually complete (9 / 9 and the review), and the petition stays a draft until the lawyer signs.
- **Entries are ticked, not stamped**: nine stamps would break the one-stamp-per-screen rule. The register's violet ticks carry the same ink.
- **Sheets do not slide in.** Only the stamps strike and the try page's steps reveal; the rest is placed and still.
- **One stamp text was not used**: «مستوفي الشروط الشكلية» would claim more than the form check proves.
- **Added**: `--w-board` (so the kraft tab shows against the kraft bar), the dark salmon moved to `#562A22`, the deferred faces on phones, and the problem's fragment set in Markazi.
- **Removed duplication**: the drafted-grounds paper appears once, in the story; the log and the guarantees attach only what nothing else on the home shows.

## Honest risks

- **Kitsch** if stamps multiply or textures creep in: keep one stamp per screen and flat colour.
- **The laptop in Arabic** faces the page's left edge, away from the text (noted in phase 6; unchanged).
- **The phone swap**: a visitor who scrolls to the petition before the page has finished loading sees it in Markazi for a moment, then in Noto Naskh.
