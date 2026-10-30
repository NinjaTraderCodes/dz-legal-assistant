# Guide release

## Phase 0: recent cases and workbench navigation

Only GUIDE-BRIEF.pdf sections 5.1-5.4 are implemented. AGENTS.md and all 23 pages of the brief were read before code changes. Work started on a clean `djihane/guide` at `1b7f1c40754d7253ec88e96bc3eb1a6b656e181f`; a remote query confirmed that guide and the latest default branch pointed to that commit. No other branch was changed.

### Implementation and design

- `AvaloniaUserFiles` implements the existing `IHomeRecentFiles` interface. It exposes local picker paths, clears stale metadata on cancellation/failure, and opens a known path without another picker.
- An optional `RecentCasesStore` constructor dependency is supplied only by desktop composition. Existing successful save/open paths call RecordSaved/RecordOpened after stream completion and draft acceptance, with the full .dzdraft.json path and claim subject, falling back to the plaintiff name.
- Cancelled pickers, failed writes, unreadable/wrong-type files, PDF/Word exports and recovery restoration record nothing. A failed Save first keeps the pending replacement and unsaved case.
- `OpenRecentCaseAsync` shares OpenCommand's replacement decision and file loader. The pending path survives Save first / Continue without saving and is cleared by Cancel. Picker and recent opens share the document-type check and localized error path. App startup and workbench Home both receive the callback.
- Home/Profile icons sit beside New/Open/Save and reuse the existing icon package and color tokens, with localized tooltips and accessible names. Existing `HomeWindow` / `HomeWorkspace` keep the workbench model alive; Home/Back preserve the case without a save prompt. The existing ProfileSettingsWindow edits defaults for new cases without changing the current case.
- Enter opens both controls. Escape returns from Home; Profile Escape uses the existing unsaved-edit confirmation. Inner confirmations retain their own Escape behavior.
- Sample, Tour, Help and Library remain disabled with HomeComingLater. The web demo retains IUserFiles and needs no store. No later phase, legal text, output renderer or model was changed.

### Approved sizing conflict

The old 1100 x 640 logical-pixel workbench minimum could not fit 1366 x 768 at 125% (about 1093 x 614 logical pixels). Work paused and the owner explicitly approved 800 x 560, matching onboarding. Rendering checks assert the actual requested window size at both scales; they do not bypass the minimum.

ProfileButton and ProfileButtonHint use Appendix A's exact Arabic in both resource files. No wording change, new color, app dependency, font or icon asset was introduced.

### Tests

New `RecentCaseIntegrationTests`: 12 cases across these methods:

- SuccessfulSaveAndPickerOpenRecordTheFullPathAndSubject
- CancelledPickersFailedWritesAndUnreadableFilesRecordNothing
- PdfAndWordExportsAndRecoveryRestoreDoNotRecordCases
- RecentOpenWaitsForTheExistingReplaceDecisionAndCancelChangesNothing
- RecentOpenUsesTheOriginalSaveFirstAndContinueWithoutSavingPaths
- CancelledOrFailedSaveFirstDoesNotOpenTheRecentCase
- PickerAndRecentOpenUseTheSameTypeCheckAndUnreadableFileMessage
- MovedCaseCanBeRemovedFromTheListWithoutDeletingItsFile
- DesktopAdapterExposesLocalPathsAndClearsCancelledOrFailedPickerMetadata
- SharedWebFileServiceStillSavesAndOpensWithoutARecentStore

New `PhaseZeroWorkbenchTests`: 8 cases across these methods:

- HomeAndBackToCaseKeepTheSameUnsavedCaseWithoutASavePrompt (Arabic/English; Back/Escape)
- ProfileOpensFromTheWorkbenchAndDoesNotChangeTheCurrentCase (Enter/Escape)
- ProfileEscapeKeepsUnsavedProfileEditsBehindTheExistingConfirmation
- PhaseZeroScreensRenderWithAccessibleControlsAndNoTitleBarOverflow
- ProfileWordingMatchesAppendixAInBothResourceFiles

The layout matrix covers Arabic/English, light/dark, both display sizes and 100%/125% scaling. Updated existing test `SetupTests.EverySetupBindingHasArabicAndEnglishWording` to scan MainWindow.axaml too. The harness accepts IUserFiles and an optional store. No existing test or assertion was deleted, skipped or weakened.

### Screenshots

Rendered outside the repository, individually opened and inspected, then copied to docs/images/guide/. All use fictitious fixtures at 1366 x 768.

| Screen | Arabic light | Arabic dark | English light | English dark |
| --- | --- | --- | --- | --- |
| Workbench | [View](images/guide/phase0-workbench-ar-light-1366x768.png) | [View](images/guide/phase0-workbench-ar-dark-1366x768.png) | [View](images/guide/phase0-workbench-en-light-1366x768.png) | [View](images/guide/phase0-workbench-en-dark-1366x768.png) |
| Recent cases | [View](images/guide/phase0-recents-ar-light-1366x768.png) | [View](images/guide/phase0-recents-ar-dark-1366x768.png) | [View](images/guide/phase0-recents-en-light-1366x768.png) | [View](images/guide/phase0-recents-en-dark-1366x768.png) |

### Verification

- Full Release build: 0 warnings, 0 errors.
- Full Release suite: 857 passed, 0 failed, 0 skipped, including all 24 DzLegal.Web.Tests.
- Native `dotnet run --project src/DzLegal.App -c Release --no-build --no-restore` walk-through passed with Without AI explicitly selected: Welcome/Home, native case save, full path in recent.json, Home shows the case, recent-case callback opens it, and Profile opens/closes from the title bar.
- Existing profile/preferences/recent/recovery files were temporarily protected during the native check, restored, and verified with SHA-256. Generated files were preserved separately; no user files were deleted. No AI generation was run in the native check; test fixtures use template mode. PDF/Word rendering code is unchanged.

### Scope and remaining work

Outside DzLegal.App, DzLegal.Presentation, DzLegal.Storage and their tests, only this report and the eight requested PNGs changed. No legal engine/data/prompts, model providers, web source, dependencies, build configuration, tools, deployment or CI changes.

Phases 1-3 and their sample/model assessment, screens and screenshots remain out of scope. No unresolved Phase 0 wording or design decision remains.
