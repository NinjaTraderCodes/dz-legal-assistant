# Document workspace — review study 02

[Open the review gallery](index.html). The preceding [charcoal/gold study](../charcoal-gold/index.html) remains available unchanged.

This follow-up addresses the three requested refinements. It is an isolated, native Avalonia visual study. No production source, style, view model, export renderer, legal rule, data file or project configuration is changed.

## Case details: replace the pills

Nine numbered outline rows keep every section visible in its original order. The current row has a narrow gold marker, a restrained background and stronger text. Completion sits at the opposite edge, separate from the label. Small gaps after rows three and six help the eye scan without adding more headings or hiding sections inside an accordion.

Arabic labels and contextual controls have a larger type size than their English equivalents to account for the bundled Arabic face's appearance at these sizes.

The tradeoff is vertical space. The outline uses more height than the previous wrapped pills; the original form keeps its scroll area below it. The laptop captures are included so that this cost can be assessed. The rows are compact desktop navigation, not touch-sized controls.

## Statement of claim: separate writing from controls

- A4-shaped pages, consistent margins, bundled Noto Naskh Arabic, deliberate line spacing and paragraph alignment, stronger document titles and printed section headings.
- Field titles, source hints and manual-edit actions move off the sheet into a blue-grey contextual strip. Their sans-serif interface typography and cool color distinguish them from the warm black legal text. The normal example selects the Court document field; the form independently remains on Facts.
- An additional example shows **Edited by hand** and **Restore from form**. The original override command sets that state, while retaining the same court wording for a fair visual comparison.
- The intended interaction is for the strip to follow the selected document field, including keyboard focus. These static captures do not implement field selection, tooltips, hover behavior or a complete contextual editing system.
- The AI review checkbox remains visible outside the paper, including in the blocked state. The original export-blocker panel and substantive legal warnings remain visible. A checkbox marked reviewed is controlled fixture state, not an actual lawyer review.
- Edit/Preview, Draft mark, PDF, Word and Copy share one toolbar row at both sizes. The short export labels keep their full existing action names as tooltips in the native mockup. Arabic keeps mirrored toolbar ordering.

This is a proposed **editing appearance**, not a new rich-text editor. The page composition uses the original sections, original printed Arabic headings and layout roles, with study-specific type size and spacing. Whole sections are kept together for this fixture. Arbitrary documents, splitting long sections, caret movement between pages, undo, selection, accessible editing and print parity remain implementation work. The application’s actual PDF/Word formatting and Preview tab are unchanged. The study page is not an exact export proof.

The checked draft-watermark setting is reflected in a faint watermark on the study pages. Fit-to-width scaling is recorded in capture observations; the laptop view consequently has smaller document text. [The first page at native size](renders/page-proof.png) helps assess the typography separately from screen fitting.

## Glass: a restrained alternative

The gallery includes both **Warm solid** and **Subtle glass**, with a direct comparison mode. Glass here means translucent tinted panel gradients over a static warm ambient background, fine edge highlights and a restrained panel shadow. Paper, form fields, review cards and warnings retain opaque reading surfaces.

This is a static layered treatment. It uses no live backdrop blur, desktop capture, animated background, external image or acrylic compositor. It does not establish performance or battery cost for a future production implementation. My preference is the subtle finish on the surrounding frame with the solid reading surfaces shown here; broad blur or translucent document text would undermine the document’s clarity.

## Review and verification

The same fictitious debt-recovery fixture is used throughout, with no model invocation. Sixteen main captures cover solid/glass, Arabic/English, laptop/large window and editing/blocked export. Two additional large captures demonstrate manually overridden Court controls. The isolated renderer uses the restored production window and the previous study’s treatment, then applies this study’s changes only to that window instance.

The native checks verify identical section paragraphs, pane ordering, all nine case sections, RTL legal text, the original export guard, AI review state, metadata controls outside the paper and a single toolbar row without overlapping controls. [Capture observations](renders/observations.json) record the geometry and page count. Browser checks and selected color-pair measurements are in `qa/`.

These checks are bounded to this fixture and the shown states at 100% render scale. They do not certify the full app, Windows DPI, screen readers, every focus/disabled state, accessibility, production editor behavior or glass performance.

Completed results: all 18 native scenes passed the rendering checks, the [64 gallery combinations](qa/results.json) passed with no page errors or external requests, and all [11 selected contrast pairs](qa/contrast.json) passed their stated thresholds. The glass measurements use a conservative composite of its brightest panel and ambient color stops; they are not a complete rendered-state audit.

## Reproduce

From the repository root in PowerShell:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:AVALONIA_TELEMETRY_OPTOUT = '1'
$env:DZLEGAL_STUDY_SAMPLE = $null
$env:DZLEGAL_STUDY_OUTPUT = Join-Path (Get-Location) 'docs/design-studies/document-workspace/renders'
dotnet test docs/design-studies/document-workspace/RenderStudy -c Release
```

`DZLEGAL_STUDY_SAMPLE=1` produces only four English editing captures for a quick layout pass; omit it for the full set. The project is outside the product solution and reuses the original in-memory test helpers. Browser verification uses an existing Playwright/Chromium installation through `PLAYWRIGHT_MODULE` and `CHROMIUM_PATH`, without installing anything.
