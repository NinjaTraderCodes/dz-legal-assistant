# Design: the case file «الملف»

<!-- Draft direction for approval. Built with the Impeccable method, derived by hand (its concept-seed program was not run). After the build, this file is rewritten from what was actually built, as Impeccable's finish step requires. -->

Scope: the public website in `site/` only. The desktop app, the demo at `/app` and every app screenshot keep their own look. `src/styles/tokens.css` (generated from the app theme) stays, and is used only where the site shows pieces of the app; the site's own world lives in a new layer.

## Contract

- **Thesis.** The website is a lawyer's case file: each section is a folder or a sheet filed in it, and progress is shown the way a registry shows it, by stamps. It refuses both category defaults: the dark SaaS page with a neon accent (CyberLex) and the stock legal look (scales, books, gold on brown).
- **Own world.** Flat colour fields in real cardboard-folder colours, sharp-cornered white sheets clipped onto them, violet registry stamps, carbon-black text. With the words removed, it should still read as "a file on a lawyer's desk".
- **Story.** A lawyer understands in seconds that the assistant prepares the petition and checks it against the law. They believe it because they see the real screens and real output, filed like evidence. Then they try the demo or download the app.
- **First viewport.** One salmon folder fills the screen, with its tab at the top on the reading-start side, carrying the product name. On the start half: the title in large Arabic type, one sentence below it, then two actions (Try the demo, Download for Windows). On the end half: the hero laptop (a transparent cut-out) lying on the folder with a firm shadow. One stamp, «دون اتصال بالإنترنت» / "Works offline", overlaps the laptop's corner and the folder.
- **Signature moment.** In the nine-step story, each step is struck with a stamp when it completes, and the formal check counts up to 9 / 9. At the end the folder closes and receives «جاهزة للإيداع» / "Ready to file". It says *ready to file*, not *filed*, because the lawyer files it at court, not the software.
- **Finish.** Unreviewed and undocumented is unfinished. The build ends with the finish review, the verdict, a rewritten DESIGN.md and every shipping image carrying its source.

## Scene: dark or light

A lawyer reads this at the office under ordinary daylight or strip lighting, or on a phone in a court corridor between hearings. The scene argues for **light** as the native state of the world. You chose dark as the site's default earlier, so both themes are designed as equals:
- **light** is the folder on the desk;
- **dark** is the same file in the archive room after hours: the folders deepen, but the sheets stay paper.

**Decided (4 Nov 2026):** dark stays the default.

## Colour

Strategy: **full palette**. Every folder colour has a job: it tells the visitor which file they are in. Colour works at the scale of the page: whole sections are coloured, never small accents scattered on a neutral background.

| Role | Light | Dark | Job |
|---|---|---|---|
| Kraft (ground, folder backs) | `#CDBB98` | `#1B1915` | the desk / the archive shelf |
| Salmon folder | `#E8A88F` | `#4A2D25` | home: the case |
| Blue folder | `#A6C6DA` | `#1F3341` | documents and the guide |
| Green folder | `#AECBA3` | `#22352A` | privacy and offline |
| Yellow folder | `#ECCD74` | `#3E3418` | the law library |
| Sheet | `#FBFAF6` | `#F4F1EA` | filed documents (always paper) |
| Carbon (text) | `#1D1A17` | `#ECE7DE` (on folders) | reading text |
| Violet stamp ink | `#4A3B8F` | `#B4A8F2` | status, completion, the stamp |
| Red (missing element) | `#B3261E` | `#FF8A80` | only for what is missing, as in the app, and only on sheets |

Every text and background pair must pass WCAG AA in both themes before it ships. Measured on this palette: carbon text on the light folders is 8.6 to 11.2 to 1; violet stamp ink is 4.5 to 5.9 to 1 on the light folders and 5.7 to 8.2 to 1 in dark; light text on the dark folders is 10 to 14 to 1. Red text fails on the light folders (3.2 to 4.2 to 1), so red appears only on white sheets (6.3 to 1).

## Type

- **Display, Arabic and Latin: Markazi Text** (OFL, variable 400–700). It is a Naskh-rooted text face drawn with its own Latin, so English, and later French, sit with the Arabic as one family instead of a borrowed pairing. It is used for titles and big statements, and never in italics.
- **Running text and controls, Arabic and Latin: Readex Pro** (OFL, variable). Clear at small sizes on a mid-range phone, with full French accents.
- **Stamps only: Aref Ruqaa** (OFL). Ruqʿa is the hand of everyday Algerian administrative writing, which is exactly what a registry stamp carries. It is never used for headings or paragraphs. Stamps on English pages are set in Readex Pro semi-bold capitals.
- **Pieces of the app and the petition: Noto Naskh Arabic**, as today, so replicas match the app.

All fonts are bundled locally, subset to the glyphs used, each with its licence file. Fonts stay within `fontsPerPageGzip` (220 KB); Aref Ruqaa is subset to the stamp texts only.

## Materials and components

- **Folder field:** a full-width colour section with a top tab (product word or section number) on the start side. The next folder's tab peeks above the previous section's bottom, so the page reads as a stack. It is flat colour: no noise texture, no stains, no torn edges.
- **Sheet:** a white rectangle with 2 px corners and a short, firm shadow, as if it were lying on cardboard. It holds the real screenshots, real AI output and the law quotes. It is attached with a drawn paper clip or a staple (inline SVG), at most one per sheet.
- **Stamp:** a rectangular or double-ring frame in violet ink, rotated between −6° and +4°, with slightly uneven ink (an SVG mask, not a photograph). It carries status only: «مستوفي الشروط الشكلية», «دون اتصال», «راجعه المحامي», «جاهزة للإيداع». It carries no state emblem, no court name and no imitation of an official seal.
- **Registry numbering:** sections are numbered like entries in a register (01 to 09 in the story), set in Readex Pro tabular figures. This replaces small labels above headings.
- **Buttons:** carbon fill with sheet-coloured text, or a carbon outline; 2 px corners, 48 px tall. The primary action is always carbon-filled.
- **Figures and proof:** the measured results (94%, 360/360, 1,361 articles) are typed onto a sheet like a report, each with its source line under it, not set in a grid of cards.

## Page reach

| Page | Folder | Contents filed in it |
|---|---|---|
| Home | salmon | hero, the problem, the nine-step story, the AI section, proof, team |
| Try / guide, Documents | blue | guide steps as numbered sheets; documents as sheets in the file |
| Privacy | green | the offline statement stamped «دون اتصال» |
| Law library | yellow | articles as sheets with their source text and code |
| About, Download, 404 | kraft | plain sheets |

The header becomes the folder's label strip; the footer is the back cover in kraft.

## Motion

Things are **placed**, not floated:
- sheets slide in 12 px and settle (180 ms, ease-out);
- stamps strike: they scale from 1.15 to 1 in 120 ms, with a 1 px settle and an ink fade-in;
- the closing folder is the only large movement, and it happens once;
- there is no parallax, no fade-up on every block, and no scroll-jacking beyond the existing story.

With reduced motion, everything shows in its final state: stamps already struck and the folder already closed.

## Never

- State emblems, «الجمهورية الجزائرية الديمقراطية الشعبية», court names, real seal designs, or anything that suggests official endorsement, as site decoration. Inside the petition replica the document's own header lines stay, because they are part of the document.
- Scales, gavels, columns, books or gold gradients as decoration. The brand mark stays as it is.
- Fake paper texture, coffee stains, torn edges, leather or wood.
- Small spaced-out labels above headings, grids of same-size cards, cards inside cards, gradient text, glow, or a coloured stripe down the side of a box.
- Italic display type, or a typewriter look for the whole page.
- The word "filed" or «مودعة» for what the software does: it prepares; the lawyer files.
- Any font, image or script loaded from another origin; any new image without its licence or source.

## Honest risks

- **Kitsch.** If the folders get textures or the stamps multiply, it turns old-fashioned. The rule is at most one stamp per screen, and flat colour only.
- **Dark theme.** Cardboard colours lose their character when darkened; this needs real testing, not just inverted values.
- **Font weight.** Three new families fit the budget only with careful subsetting.
