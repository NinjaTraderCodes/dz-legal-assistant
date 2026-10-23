# R3 workbench implementation

Branch: `codex/r3-workbench`, based on `origin/claude/stoic-fermat-mi1gko` at `dfc2e96a08cd3e465741f766acdc92bd8b859c1d` (fetched before branching).

## Progress

1. Workbench shell: implemented. The draft stays on the physical right, with a compact outline below 1500 logical pixels, three preview splitters, persistent layout, four tools, and the existing case commands.
2. Help, typography, contrast and the complete rendering matrix: implemented; proof and remaining physical-device checks are described below.
3. Command palette, separate draft window and web parity: not started at the shell checkpoint.

The step checkpoints are separate commits. No drafting, legal, storage, document renderer, provider, rule, prompt or corpus files have been changed. Claim confirmation and article selection still call the original commands and setters.

## Capability inventory

| Existing capability | Location in the workbench |
| --- | --- |
| New / Open / Save and save-state text | Title bar; existing Ctrl+N/O/S shortcuts |
| Save first / continue / cancel | Persistent replace bar below the title bar |
| Autosave and recovery | Existing timer and close handling; persistent recovery bar |
| Every field, conditional field, choice and boolean | Outline → selected editor section |
| Whole over-long text and its blocking note | Original field binding and note directly below the field; no control MaxLength |
| Previous / next and completion state | Outline icons/status and editor footer |
| Local engine selection and readiness check | Editor footer; model status also in the status bar |
| Analysis and cancellation | Legal grounds editor; existing command and activity view |
| Automatic analysis after Facts | Existing selected-step behavior; background activity notice |
| Claim confirmation / rejection | Original legal grounds cards and commands |
| Article selection, manual search and official text | Grounds editor and Source tool, using the existing article template |
| Journal Officiel reference, provenance and source link | Article card; unavailable text stays visible as text |
| Suggested request/document insertion | Requests / Supporting documents editor sections |
| Request wording proposal and accept/reject | Requests editor section |
| Drafting and cancellation | Editor footer and existing Ctrl+G shortcut |
| Real activity steps, stream, elapsed time, history estimate and battery note | Activity tool, in-place activity views, status bar estimate/battery |
| Section being written | In the draft next to the section heading |
| Draft editing, manual override, restore from form, undo and redraft confirmation | Draft paper, by section |
| AI authorship and review checkbox | Every AI-written section, always visible |
| Section assistant, shortcuts, streaming, proposal diff and accept/reject/retry | Section Assistant button → Assistant tool |
| Formal completeness and legal checks | Before export tool; original findings/navigation commands |
| Proactive export blockers and blocked-export details | Draft notice/count and Before export tool; refusal opens the tool without focusing it |
| Actual PDF preview pages and watermark | Draft toolbar |
| PDF / Word / copy | Draft toolbar, through the original export guards; Ctrl+P retained |
| Draft evaluation form and save | Before export tool |
| Arabic/English and light/dark | Title bar; existing view model and localization |

All original XAML command names are still bound in the desktop shell or its shared templates. The only added view-model state concerns interface navigation and pane visibility. Layout changes are absent from the case fingerprint.

## Shell design decisions

![Shell in Arabic, light theme](images/r3/shell-ar-light.png)

![Shell in Arabic, dark theme](images/r3/shell-ar-dark.png)

- An explicitly LTR outer grid fixes the physical pane order. Inner panes follow the interface language. Legal inputs and paper editors remain explicitly RTL.
- Draft maximization changes column widths and visibility and keeps the same controls and section objects.
- All three native GridSplitters use `ShowsPreview=true`, minimum sizes, keyboard increments and double-click reset. Paper reflows on release, rather than at every pointer position.
- The tools strip stays visible when its content is closed. Tool content scrolls in short panels, keeping evaluation and assistant actions reachable.
- Layout JSON lives at `%LOCALAPPDATA%/DzLegalAssistant/settings/workbench-layout.json`, separate from drafts and recovery. It contains only pane widths/height, outline state, tool visibility and last tool. Invalid, oversized or unreadable preferences silently use defaults. Failed preference writes do not change case save status.
- Optional field explanations now use a permanent label and an information control. Hover, keyboard focus and click open the adjacent disclosure; Escape closes it. Required/missing/over-long notes remain separate, permanently visible text.

## Verification at the shell checkpoint

The original baseline passed a Release build with zero warnings and all 665 tests. The shell adds nine cases (674 total), including actual preference failures, keyboard splitter movement, pane geometry in both interface directions, stable editor identity through maximization/theme/language, and export refusal without focus theft.

RenderingTests retain the original assertions for save-first, missing fields, real preview images, analysis, live generation, requests, assistant acceptance and bidi order. Two legitimate structural updates were needed:

- Named controls in the shared DraftWorkspaceView are found through its visual descendants when they are outside the window namescope; window-owned names continue to use FindControl.
- The old assertion that an RTL form sits on the right is replaced by the R3 requirement that the draft sits physically to the right of the form.

No tests were skipped, removed or weakened. Full build/test results are recorded again at each subsequent checkpoint.

## Step 2: polish and proof

IBM Plex Sans Arabic Regular/Medium/SemiBold is bundled with its OFL licence. The document retains the bundled Noto Naskh Arabic. Plex includes the Latin glyphs used by the English interface; the original mixed Arabic/French/digits bidi test still passes.

The former window styles have been replaced with one application token/style set. The paper has fixed ink/annotation colours in both themes; in particular a dark-theme warning can no longer become pale yellow on white paper. All interface body text is at least 14 logical pixels and secondary text at least 12.5. The matrix tests assert the latter on every visible TextBlock. Buttons have an explicit native control template and hover/pressed/focus/disabled states. Transitions are disabled throughout, which also satisfies reduced-motion preference without polling it during generation. No blur or paper shadow is used.

The engine chooser moved into a keyboard-accessible flyout in the editor footer. The editor heading and previous/next actions share one row; step position moved to the status bar. Narrative boxes fit the available editor height in complete text lines. A geometry assertion specifically checks that the Facts box fits its viewport with tools open. Model readiness stays permanently in the status bar. Each splitter resets only its own dimension. Assistant proposals scroll into view after arrival without moving focus.

[Open the offline screenshot gallery](images/r3/index.html). It contains 256 losslessly compressed PNGs: 16 states × 2 client sizes × 2 render scales × 2 languages × 2 themes. States include empty, filled, analysis running/completed, generation, blockers, review, refused/completed export, unsaved work, recovery, errors, unavailable model, template mode, source and assistant. They use the fictitious first drafting-case fixture and controlled providers for deterministic streaming states. Export-done completes the template's legal-ground placeholder with that fixture's original notes, as a lawyer editing the section, before invoking the real PDF and Word exporters.

Every frame was inspected in contact sheets; the smallest Facts, blocked-export and drafting frames were also inspected at full size. The captures use Avalonia Headless with actual Skia drawing and SetRenderScaling(1/1.25), not scaled browser mockups. They certify client-layout geometry at those scales, not a physical multi-monitor Windows DPI transition.

The added matrix cases assert pane geometry, legal RTL, minimum type sizes, real streaming/writing state, permanent safety bars, actual export bytes and the unchanged export refusal. HelpTests exercises focus, hover, click and Escape. The engine-flyout test exercises focus, keyboard selection and dismissal. The existing screenshot helper now uses the supported PNG encoder overload, removing its old obsolete-API warning suppression.

Step 2 checkpoint: full Release build **0 warnings / 0 errors**; full suite **692 passed, 0 failed, 0 skipped** (App 137; Web 24; Drafting 369; FormEngine 67; Documents 6; Law 86; Storage 3).

### Measured contrast

The [complete 84-pair contrast table](R3-CONTRAST.md) is computed from the committed sRGB tokens using WCAG relative luminance. Text pairs have a minimum of **4.89:1**; interactive edges/focus colours have a minimum of **3.32:1**. HairlineBrush is decorative; interactive edges use ControlBorderBrush. Paper/diff annotations are measured separately from the surrounding dark chrome. Native platform popups and operating-system adorners have not all been pixel-sampled.

### Native Windows review and limits

The actual native executable was launched with an isolated fictitious profile, through DZLEGAL_REVIEW_HOME, and a separate review copy of the binary. The ordinary profile remains the default when that variable is absent. The real engine picker, language switch, recovery offer, layout JSON, outline navigation and Arabic text entry were exercised with Windows accessibility and keyboard input. Ollama reported gemma3:4b installed and ready; local warm-up and drafting commands were exercised on the review profile.

**The complete physical-app keyboard-only and mouse-only empty-case → PDF/Word runs are not certified.** The external Windows automation initially mishandled Unicode/control modifiers, and its foreground-target guard subsequently stopped runs when another application held focus. Review scripts were corrected rather than changing application/legal behavior to accommodate them. The full fictitious-case exports and refusal paths are verified in the native-layout integration tests, with in-memory file pickers. That is useful evidence, but it does not replace exercising both physical Windows save dialogs. A full gemma3:4b case run, every physical splitter drag limit, and the full physical DPI matrix remain manual acceptance checks.

Autosave/recovery behavior is exercised by the original safety tests and by all 16 matrix recovery configurations. Layout failure/round-trip and keyboard sizing are covered separately. No claim of testing a second 8 GB machine, a screen reader, or graphics drivers is made.

## Outstanding verification

The physical checks listed above remain. Smooth operation on a separate 8 GB laptop and a hardware graphics/scaling matrix cannot be inferred from headless rendering. The existing section-assistant send command does not itself apply the over-long-field refusal used by the other AI commands; legal/command behavior has been preserved within the requested scope, so this pre-existing discrepancy is recorded rather than changed here.
