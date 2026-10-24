# Original UI restoration and reference review

Date: 2026-10-24. Branch: `codex/r3-workbench`.

## Decision and restoration

The user rejected the R3 implementation and selected the original UI on `claude/stoic-fermat-mi1gko` as the foundation. The three R3 commits (`5f86248`, `f019f52`, `205c0b6`) are undone together, preserving their history. The source branch was fetched before restoration and resolves to `dfc2e96a08cd3e465741f766acdc92bd8b859c1d`.

Before adding this review document, the restored tracked tree was verified to match that source commit exactly. Desktop, presentation, web, assets and tests are restored. R3-only views, styling, fonts, layout preferences code, command palette, separate window, browser changes, feature tests and R3 delivery galleries are removed with that implementation. The original test suite is restored, without modifying its assertions. Existing user files and the supplied `ui-design-example` directory are left untouched.

No new visual direction is implemented in this change. Everything below is a proposal for discussion. Layout settings previously written by R3 are not deleted from the user's profile; the original application does not consume them.

Restoration verification: Release build **0 warnings / 0 errors**; **665 tests passed, 0 failed, 0 skipped**. The Arabic, English and dark-theme native rendering captures were inspected. They also show existing polish opportunities: the English draft-header controls can crowd at the test window width, and dark-theme warning annotations on white paper need separate contrast treatment. These remain unchanged in the exact restoration and should be addressed explicitly in a later approved design pass.

## What the original interface already does well

The application is a petition drafting workspace. Its original main grid contains the case form, the draft and the review/assistant area together. Arabic places the form on the physical right and review on the left; English follows the original opposite ordering. The draft remains in the middle and is continuously visible. The assistant uses the existing review-side space. These relationships should remain the starting point for a visual study.

The original warm paper, document typography, familiar case-step labels and direct access to drafting/export are useful foundations. R3 changed the visual language and information hierarchy together: blue workbench chrome, icon navigation and a lower tools area. The next comparison should retain the original hierarchy and first change its visual treatment.

## Review of all 14 supplied images

The files were inspected individually at their supplied resolution. Observations concern the visible design, rather than the client/case data shown in the screenshots.

The supplied screenshots remain local and untracked; this review refers to their filenames.

| Image | Main observation | Useful application to our restored UI |
| --- | --- | --- |
| UI (1) | Profile identity card, a clear page title, selected gold tab, and aligned form blocks establish hierarchy. | Give the current case and selected form section a clear identity; group field labels and inputs consistently. A profile module is not required for this benefit. |
| UI (2) | The closest reference to our task: a dark working area beside a white document surface, with document actions grouped around it. | Strengthen the existing central paper preview and its toolbar. Keep the original three-pane arrangement rather than adopting the reference's request-library layout. |
| UI (3) | Summary cards, a consistent row of search/filter/action controls, status badges and table rows make a dense page scannable. | Use a compact factual review summary and clear local action grouping. Large dashboard statistics would consume valuable editing height. |
| UI (4) | Legal research is split into named cards, with search as the main action and supporting information below. | Separate article search, selected articles and source/provenance into clear groups within the existing legal-grounds/review interface. |
| UI (5) | The gold primary action and consistent data alignment stand out; the large single-number card occupies much of the page. | Improve action priority and alignment while keeping case editing more prominent than summary counts. |
| UI (6) | Warm gold establishes identity, while green and amber communicate different invoice states. | Keep brand emphasis separate from legal/error/review semantics. A gold button should not imply that a draft is legally approved. |
| UI (7) | Navigation selection, table headings, case identifiers and a primary action repeat the same visual vocabulary. | Apply one selected-state treatment to our existing case-step labels and repeat it consistently across local tabs and actions. |
| UI (8) | A gold outline and date marker identify the current day; restrained colored entries communicate status. | Use a clear outline/marker for the current case step, supported by text and icons for completion or attention. A calendar feature is outside this UI study. |
| UI (9) | Search, filters, table headings and row actions have consistent alignment and spacing. | Organize the existing supporting-document and source information into predictable rows, with readable labels and visible action names. |
| UI (10) | The warm form surface, generous label spacing and grouped short fields feel coherent. The long modal requires internal scrolling. | Borrow field grouping and alignment. Keep the main legal form in its existing pane so the draft remains available during entry. |
| UI (11) | Legal-search results use a strong article heading, a small category marker and a quieter excerpt/action area. | Improve the hierarchy of existing article cards: citation, official text, provenance and selection. Keep essential source information visible. |
| UI (12) | A wide upload area communicates a clear task, but the lower portion of the modal is outside the captured viewport. | Give existing document-related actions a clear visual container. File-upload functionality should not be invented as part of a restyle. |
| UI (13) | The hearing form repeats the same control treatment. Both page and dialog scrollbars are visible, including horizontal overflow at the bottom. | Use consistent controls and contain overflow within the relevant pane. Avoid reproducing nested whole-page scrolling. |
| UI (14) | The compact form has aligned inputs and an unmistakable primary Save action beside a quieter Cancel. | Use a clear primary/secondary action hierarchy in existing confirmations and dialogs, with actions visible at the bottom. |

## Why the references have a stronger visual identity

1. **A warm, coherent palette.** Near-black brown, warm charcoal, muted gold and ivory repeat across navigation, panels, labels and buttons. Gold is used as an identity cue in several places, rather than as an isolated decoration.
2. **Clear layers.** The background is darkest, panel surfaces sit above it, input surfaces are slightly lighter, and the paper is distinctly white. Fine borders help explain those boundaries.
3. **Decisive hierarchy.** Large bold page titles, smaller gold group labels, body copy and quiet metadata have different jobs. The selected navigation item is immediately recognizable.
4. **Consistent grouping.** Rounded panels, aligned controls, repeatable spacing and predictable action placement make related information read as a unit.
5. **A calm surrounding frame for the document.** Image 2 shows why a bright document inside a dark workspace can remain the visual focal point.

The imagery and glass-like appearance add atmosphere, but screenshots cannot establish the underlying rendering technique. The font family cannot be identified reliably from these JPEGs either.

## Recommended adaptation

### Keep the original composition

Preserve the original form/draft/review arrangement, Arabic/English pane ordering, existing case-step navigation, and current assistant behavior for the first comparison. Keep the draft large and visible. Apply the new treatment to the same screen and the same populated case so the visual difference is easy to judge.

The reference sidebar navigates a practice-management product: clients, billing, hearings and calendar. Our current application has a different task. Its existing case steps should receive the stronger selected-state treatment; adding a second global sidebar would spend width without improving petition drafting.

### Preferred direction: warm charcoal and muted gold

Use an opaque warm charcoal workspace, restrained gold emphasis and an ivory document. Retain the original light theme as a second view to be considered alongside the dark treatment. A possible starting palette for mockups is:

| Role | Proposed starting color |
| --- | --- |
| Workspace | `#171411` |
| Panel | `#24201B` |
| Input / raised surface | `#302A23` |
| Primary text | `#F2EDE5` |
| Gold accent | `#C6A76D` |
| Document paper | `#FFFEFA` |
| Document ink | `#28231D` |

These are proposed design values, not exact measurements of the references or a verified accessible palette. Text, control boundaries, focus, selected and disabled states must be measured together before implementation. Gold can mark the current section, primary action, selected tab and a small brand detail. Errors, blockers, AI authorship and review requirements keep distinct wording and semantic treatment.

The user selected **warm charcoal and muted gold, closest to the supplied references**, during this review. This selection establishes the direction for the next design discussion; no new application styling has been authorized or implemented in this turn.

### Typography, spacing and depth

Keep the existing bundled fonts for the first visual comparison. Use a more deliberate weight/size hierarchy before changing typefaces again. Preserve the Arabic document face and explicit RTL legal text. Interface labels must remain readable in both languages; French-looking headings in the supplied screenshots do not prove the quality of a full Arabic interface.

The reference's large headings and generously rounded cards suit wide management pages. Our editing workspace needs a tighter adaptation: approximately 20–24px main headings, 14–16px interface text, 12.5–14px secondary text, and a small, consistent spacing/radius scale. These are mockup targets to test, rather than changes to apply now. Keep short labels visible and give narrative text adequate line height.

Reproduce depth with opaque surface layers, thin warm borders and, if useful, a restrained static tonal change. The large photograph behind the reference panels would compete with Arabic legal text and introduce variable backgrounds. Continuous blur and heavy transparency would also need performance testing on the target CPU laptop. The first proposal should establish its depth through solid surfaces.

### Actions and help

Give the current step one clear primary action. Keep New/Open/Save at application level and export with the draft. Use quiet secondary actions with consistent icons, spacing and text. Preserve all current save-first, recovery, overlong-input, source, AI review and export-blocking behavior.

Optional explanatory hints can be tested as adjacent disclosures opened by hover, keyboard focus and click. Permanent labels, errors, blocking notes and legal/source warnings remain visible. This is a proposed comparison, not permission to remove the original help text before the user reviews it.

## Limits visible in the references

Several muted labels and small action icons are difficult to read at the supplied image size. Some dark icons have weak visual separation from their buttons. Photo detail changes the apparent contrast behind translucent cards. Long dialogs conceal their lower content, and image 13 shows horizontal page overflow. These are visual observations, not measured accessibility failures.

The images do not show a complete light theme, a full Arabic application interface, keyboard focus/order, screen-reader behavior, error/disabled states, or live AI streaming. They also do not establish performance. Those states need their own design review; a visually attractive screenshot alone cannot certify them.

## Suggested next review

For the next review, prepare a faithful charcoal/gold treatment of the **same restored screen**, presented beside the original. Use the original pane arrangement and one consistent fictitious fixture. Show both a large view and the smallest target laptop view; include a normal editing state and a blocked-export state. Review the concrete mockups with the user before any new application styling is implemented.

The restoration and reference-analysis review ended here. Following approval of the proposed direction, a separate [charcoal/gold comparison study](design-studies/charcoal-gold/index.html) now shows the restored original beside the proposal in Arabic and English, at laptop and large-window sizes, in editing and blocked-export states. [Study notes and verification](design-studies/charcoal-gold/README.md) describe its scope. Production views and styles remain unchanged.

Following feedback on that proposal, [review study 02](design-studies/document-workspace/index.html) explores numbered case navigation, a cleaner page-based petition with contextual field controls outside the paper, a single-row draft toolbar and a subtle glass alternative. [The follow-up notes](design-studies/document-workspace/README.md) describe the layout concept and remaining interaction work. This is also an isolated mockup study; production styling and export formatting remain unchanged.

The user subsequently approved implementation and merging into the existing default branch. The [production release notes](UI-WORKBENCH-RELEASE.md) record the implemented desktop UI, its preservation of the core engines and the completed regression checks. The studies above remain historical snapshots.
