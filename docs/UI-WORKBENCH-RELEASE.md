# Approved desktop workbench implementation

The approved charcoal/gold direction and document-workspace refinement are now implemented in the desktop app. The three original panes remain, with Arabic/English pane ordering and a retained light theme. Desktop startup selects the charcoal theme; the theme button still switches between themes.

## What changed

- Static tinted panel gradients, subtle shadows and warm gold emphasis. Paper, inputs and review surfaces remain opaque. No live backdrop blur, desktop capture, extra package or background animation was added.
- A numbered case outline with separate completion indicators replaces wrapped pills. Narrow windows reduce the side-pane widths; section navigation remains keyboard accessible.
- The document has a consistent paper width, Arabic Naskh typography, printed headings, paragraph alignment/spacing and a draft watermark. Editing remains continuous and fully editable; exact pagination stays in the existing PDF Preview. This deliberately avoids splitting or truncating editable text to imitate the mockup's static pages.
- Source hints and override/restore/redraft/undo/assistant actions live in a contextual strip outside the paper. Pointer selection, keyboard focus and case-section changes select its section. The actions call the existing commands.
- AI review checkboxes, review notes, live-writing status and export blockers remain visible outside the paper regardless of the selected section. They are not optional hover hints.
- Edit/Preview, watermark, PDF, Word and Copy fit in one toolbar row. Full export labels remain available as tooltips.

The change is limited to desktop views, styles, view-only selection/layout code, UI converters, two translated interface strings and tests. The original autosave, recovery and model-status lifecycle remains in place.

## Core logic protection

Compared with the original default-branch baseline `dfc2e96a08cd3e465741f766acdc92bd8b859c1d`, there are **no changes** in the domain, drafting, document/export rendering, Form Engine, law library, storage, providers, presentation view models, web application, legal data, templates, prompts, fonts or shared build/package configuration.

Validation on Windows:

- Release solution build: **zero warnings and errors**.
- Full solution suite: **669 passed, 0 failed, 0 skipped** (desktop 114, web 24, drafting 369, Form Engine 67, documents 6, law 86, storage 3).
- Form Engine corpus: **360/360 checks correct across 24 cases**.
- Grounds pipeline smoke run: **46 stub cases, zero execution failures**. Stub mode is not a measurement of model legal quality.
- New native UI coverage checks 24 combinations of Arabic/English, light/dark and 1100×640 / 1366×768 / 1920×1080, including toolbar bounds, overlap, pane ordering and RTL document text. Interaction tests use pointer/keyboard input for manual override, restore, section navigation, Preview, long text, AI review and PDF/Word export guards. An 8,001-character edit survives unchanged; changing reviewed AI text still invalidates review.

These results support the absence of detected regressions; they are not a complete certification of every Windows DPI, assistive technology or hardware configuration. No real model, client file or online provider was used for the new UI checks.

Hosted CI could not start: [the implementation run](https://github.com/NinjaTraderCodes/dz-legal-assistant/actions/runs/40126346398) reports `startup_failure` with no jobs or check runs. The same failure is present on [the previous mockup commit](https://github.com/NinjaTraderCodes/dz-legal-assistant/actions/runs/40111114173) and [the unchanged default-branch baseline](https://github.com/NinjaTraderCodes/dz-legal-assistant/actions/runs/40119959784). The underlying hosted-startup cause was not exposed by those run records. Workflow and Dockerfile contents are unchanged. The validation results above are local; they do not claim a successful hosted CI or Docker-image build.

## Implementation captures

These are native captures of the working application, with fictitious test inputs. The remaining gender/source warnings are intentionally retained.

- [English laptop, charcoal](images/workbench-release-en-laptop.png)
- [Arabic large window, charcoal](images/workbench-release-ar-large.png)
- [Unreviewed AI text blocks export](images/workbench-release-blocked.png)

The historical [study 01](design-studies/charcoal-gold/index.html) and [study 02](design-studies/document-workspace/index.html) remain available for comparison. Their static rendering helpers target the original UI at commit `98223ab`; use a checkout of that commit to reproduce the studies. Production verification uses `tests/DzLegal.App.Tests/WorkbenchTests.cs` instead.
