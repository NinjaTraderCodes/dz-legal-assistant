# Charcoal & Gold — review study 01

[Open the comparison gallery](index.html).

These historical captures show a design study of the restored original UI. At capture time, no production view, style, view model, legal rule, test, data file or project configuration was changed. The subsequently approved [production implementation](../../UI-WORKBENCH-RELEASE.md) is documented separately.

## Review set

The gallery contains **16 native Avalonia screenshots**:

- Original and proposed treatment, using the same fictitious case.
- Arabic and English, with their original pane ordering.
- 1366×768 laptop and 1920×1080 large-window client sizes, at 100% render scale.
- Normal editing, with the AI-written section reviewed; and an export attempt refused because that section is unreviewed.

Use **Side by side**, **Proposal**, or **Wipe comparison**. Full-size PNG links avoid judging typography from a reduced thumbnail. The comparison also shows defects present in the unchanged original; those have not been retouched out of the baseline images.

## Proposed visual changes

- Warm charcoal/brown surfaces with a subtle opaque tonal gradient, thin warm borders, muted gold selected states and primary actions.
- The original gavel mark inside a small gold tile. The existing product name and fonts are retained.
- Ivory paper with dark ink and readable brown annotations, independent of the dark workspace.
- Larger section headings, secondary text raised to at least 12.5px, and a clearer selected Facts step.
- The original three-pane composition remains. The case-step chips remain labeled; no global sidebar or lower tools panel is added.
- The draft heading and drafting controls occupy two rows. At laptop width, export controls also get their own row so they fit their pane. The redundant document-type caption is hidden in the small title bar, and long save-state text uses an ellipsis with its full text as a tooltip.
- The formal-check caption wraps within its card. Existing warnings and export refusal remain permanently visible.

These changes are applied only by `RenderStudy/Treatment.cs` to an isolated instance of the original MainWindow. They are not imported into the application. The gallery is a visual comparison; it is not a working replacement for the app.

## Case and safety state

Every screenshot loads the values from `tests/drafting-cases/01-debt-arabic.json` into an in-memory draft, with the same four article references and legal-ground text. The AI authorship/review flags are controlled fixture state; **no real model is called**. The blocked state invokes the existing PDF export command and asserts that its guard refuses before any export file is created. The normal state asserts there are no export blockers.

The formal-check and substantive-warning wording comes from the original view model. A complete formal count does not remove its separate legal warnings. Captured save times are actual render-run metadata and may differ between frames; the case inputs and narrative are identical.

## Capture verification and limits

The isolated rendering test checks the original pane order in both interface languages, RTL for visible legal editors, minimum visible pane/paper widths, unchanged Facts text, the original export guard, and—in the proposal—the bounds of the language, drafting and PDF buttons. Capture metadata is in [renders/observations.json](renders/observations.json).

Verification completed for this review set: the native rendering test passed and produced all 16 frames; the gallery passed all 24 scene/mode checks with no page errors or external requests ([browser results](qa/results.json)); and all 14 selected text/control color pairs passed their stated contrast thresholds ([contrast results](qa/contrast.json)). The color checks cover the listed palette pairs, not every rendered interaction state.

The screenshots use Avalonia Headless with actual Skia drawing and the application's original bundled fonts. They are not generated artwork or browser approximations of the desktop. No reference screenshot, client information from those screenshots, external font, photograph, or remote asset is included in this study.

This review covers two static states at 100% scale. It does not certify physical Windows DPI, screen readers, every hover/focus/disabled state, generation under CPU load, or the complete application's accessibility. The optional-help interaction remains a later interaction-design item; these static scenes do not demonstrate it.

## Reproduce

From the repository root of a checkout at commit `98223ab`, in PowerShell. The historical renderer relies on the original view structure and does not target the subsequent production implementation:

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:AVALONIA_TELEMETRY_OPTOUT = '1'
$env:DZLEGAL_STUDY_OUTPUT = Join-Path (Get-Location) 'docs/design-studies/charcoal-gold/renders'
dotnet test docs/design-studies/charcoal-gold/RenderStudy -c Release
```

The renderer reuses the existing test fixtures/helpers through linked source files and is intentionally outside the product solution. It uses in-memory file pickers and clipboard, with no user-profile autosave path.

The browser review helper in `qa/review.cjs` uses an existing Playwright/Chromium installation via `PLAYWRIGHT_MODULE` and `CHROMIUM_PATH`. It checks every selector combination and comparison mode, plus the comparison slider. It does not install packages.

## Next decision

Review the visual direction and toolbar density, especially at laptop size. Only after feedback should this treatment be translated into production styles and checked across the full state/DPI matrix.
