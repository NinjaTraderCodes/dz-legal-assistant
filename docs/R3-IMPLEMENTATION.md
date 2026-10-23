# R3 workbench implementation

Branch: `codex/r3-workbench`, based on `origin/claude/stoic-fermat-mi1gko` at `dfc2e96a08cd3e465741f766acdc92bd8b859c1d` (fetched before branching).

## Progress

1. Workbench shell: implemented. The draft stays on the physical right, with a compact outline below 1500 logical pixels, three preview splitters, persistent layout, four tools, and the existing case commands.
2. Help, typography, contrast and the complete rendering matrix: next checkpoint; not yet certified.
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
- Optional help is still inline at the shell checkpoint. Its replacement and the remaining theme cleanup belong to step 2.

## Verification at the shell checkpoint

The original baseline passed a Release build with zero warnings and all 665 tests. The shell adds nine cases (674 total), including actual preference failures, keyboard splitter movement, pane geometry in both interface directions, stable editor identity through maximization/theme/language, and export refusal without focus theft.

RenderingTests retain the original assertions for save-first, missing fields, real preview images, analysis, live generation, requests, assistant acceptance and bidi order. Two legitimate structural updates were needed:

- Named controls in the shared DraftWorkspaceView are found through its visual descendants when they are outside the window namescope; window-owned names continue to use FindControl.
- The old assertion that an RTL form sits on the right is replaced by the R3 requirement that the draft sits physically to the right of the form.

No tests were skipped, removed or weakened. Full build/test results are recorded again at each subsequent checkpoint.

## Outstanding verification

The full state/size/language/theme/scaling screenshot matrix, measured contrast table, real Windows keyboard/mouse export runs, and layout/recovery reopening checks will be recorded in step 2. Smooth operation on a separate 8 GB laptop and a hardware graphics/scaling matrix cannot be inferred from headless rendering.
