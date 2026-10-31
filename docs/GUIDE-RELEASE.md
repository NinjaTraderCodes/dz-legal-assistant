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

At the end of Phase 0, Phases 1-3 remained out of scope. No unresolved Phase 0 wording or design decision remained.

## Phase 1: interactive guide

Implemented Feature 3 only, on `djihane/guide`, starting at Phase 0 commit `4398b68599ff29a6406e374f0dee000508d2ea52`. Re-read AGENTS.md and brief sections 3, 6, 11, 12 and Appendices A/B before implementation. Library and About remain for their later phases.

### Tour and navigation

- Six stops resolve the named workbench controls; Export highlights both buttons. The tour selects Legal grounds for stop 4 and restores the original step on Finish, Skip or Escape. Hidden anchors use a centred card.
- Next/Back/Skip/Finish, the localized counter, reading-direction arrows and Escape work without typing, generating AI text, changing the engine or changing the draft mark. Tour navigation suppresses the existing automatic analysis-on-leaving-Facts behavior only while the tour runs. Starting without a case goes through the existing New replacement flow.
- Focus moves to the card and returns to the original control. The card exposes its title, text and counter as an assertive live-region announcement. Focus cycles within the card. The highlight has no animation, including with reduced motion.
- Window-relative anchor coordinates avoid an Avalonia RTL sibling-translation offset discovered during screenshot review. Tests compare the actual rendered outline with its targets, rather than only checking that a control was found.
- The existing `tourHandler` hook now offers Yes / Not now after successful first setup. A boolean in per-user `guide-preferences.json` remembers the offer; missing/broken files fall back safely. Later still uses the original path and creates no fake profile. Home and Help always offer the tour.

### Guided sample and actual no-model behavior

- Desktop links the existing web `Samples/debt.json`; there is still one tracked copy. The unchanged `SampleCase` loader moved to Presentation, and all four web samples retain their original data and behavior.
- Sample opening uses the existing unsaved-work decision. A permanent banner and six expandable tasks accompany the case. Show me only navigates. Completion reads Facts visitation, current analysis, claim confirmation, generated-draft state, the shared export review predicate, and an actual successful PDF/Word stream completion during this sample. An export started in an earlier sample cannot finish a replacement sample.
- Task 5 uses the existing `SectionViewModel.NeedsReview` predicate through one shared helper also used by export blockers. It can already be complete when no AI section needs review; no invented review action is required in template mode.
- The draft mark is forced on, its disabled checkbox explains why, and suggested names use `SampleFilePrefix`. Export options are captured with the draft snapshot. Leaving through New/Open/recent restores normal mode and the profile's usual draft-mark default; cancelled replacements keep the sample intact. The sample enters recent cases only after an actual save.
- Local availability is checked without generation, warm-up, case contents or an engine switch. With no local model, tasks 2 and 3 remain unticked and show `SampleNeedsModel`. The remaining four tasks can finish with the lawyer choosing the existing template engine.

**Approved mismatch:** the unchanged debt sample produces an export-blocking `(...)` paragraph in template mode. Work paused for the owner's decision; the owner approved **manual completion, preserving every blocker**. The guide reuses Appendix B4's existing explanation and the real Before export list. The lawyer must edit the generated paragraph before export. Tests and the native walkthrough performed that edit, then completed real exports. No sample content, prompt, drafting engine, export blocker, AI mark or review rule was changed to make a task tick.

### Offline Help and design

Seven resource-backed Arabic/English pages implement B1-B7, with an index, Back to help and a tour action. Home and the workbench title bar open Help. There is no web view or runtime download. Existing keyboard shortcuts remain unchanged; Escape closes Help.

Sample, Tour and Help are enabled on Home and their Coming later notes are removed. Library remains disabled with its note. New views use compiled bindings and existing theme tokens; `Styles/Guide.axaml` shares the guide's button/focus treatment. The 77 new resource keys exist directly in both resource files. Appendix Arabic is retained; no wording change is proposed.

### Tests added and adapted

Added **17 cases** in `PhaseOneGuideTests` and `PhaseOneSampleTests`:

- `TourFindsAllSixAnchorsAndRestoresCaseAndFocusWithoutStartingAi` (AR/EN)
- `HiddenAnchorCentresTheCardAndEscapeRestoresTheStep`
- `TourWithoutACaseUsesNewAndCancelPreservesUnsavedWork`
- `FirstSetupOffersTourOnceAndYesUsesAnEmptyCase`
- `SampleTasksFollowRealAnalysisConfirmationReviewAndSuccessfulExport`
- `SampleReplacementAsksFirstAndWatermarkDefaultReturnsOnlyWhenLeaving`
- `HelpHasSevenOfflinePagesAndAllDynamicStringsInBothLanguages`
- `SampleRecordsOnlyAnActualSaveAndFailedOrCancelledExportsNeverFinishIt`
- `DesktopDebtCopyIsByteForByteTheWebSampleAndSharedLoaderRetainsAllFour`
- `EveryGuideResourceExistsDirectlyInBothResourceFiles`
- `AnExportFromAnEarlierSampleCannotCompleteTheNewSample`
- `MissingAndBrokenTourPreferencesFallBackSafely` (three cases)
- `PhaseOneScreensRenderInBothLanguagesAndThemes`
- `SuppliedDebtCaseCompletesAndExportsInTemplateModeWithoutAnalysis` (requires manual placeholder completion)

Every adapted existing test:

- `HomeTests.FinishAndLaterOpenHomeAfterSetupAndRelaunchAlwaysShowsWelcome`: explicitly declines the new tour offer before retaining all Home/relaunch/Later/broken-profile assertions.
- `OnboardingPreferencesTests.WelcomeChoicesShareLocalizationAndSurviveSetupFinishAndRelaunch`: verifies the offer shares localization, declines it, then retains the existing theme/language/relaunch checks.
- `PhaseZeroWorkbenchTests.PhaseZeroScreensRenderWithAccessibleControlsAndNoTitleBarOverflow`: now requires Sample/Tour/Help to be enabled and only Library to retain Coming later; existing title-bar geometry and accessibility checks remain.
- `SetupTests.EverySetupBindingHasArabicAndEnglishWording`: extends the binding-key scan to Tour, Sample and Help views.

The harness gained optional no-model and profile-default fixtures. No existing test was deleted, skipped or weakened. `LaterNeverCreatesAFakeProfile` still passes. The new rendering matrix covers both languages/themes, 1366 x 768 and 1920 x 1080, at 100% and 125% scaling, including the watermark explanation and restored checkbox.

### Phase 1 screenshots

All 24 images were rendered outside the repository, opened individually, corrected where needed, and copied to `docs/images/guide/`. Fixtures contain fictitious data only. Task-3 images use the scripted test model's real analysis; finished images include actual template generation, manual completion and a successful test PDF export.

| Screen | Arabic light | Arabic dark | English light | English dark |
| --- | --- | --- | --- | --- |
| Tour stop 1 | [View](images/guide/phase1-tour1-ar-light-1366x768.png) | [View](images/guide/phase1-tour1-ar-dark-1366x768.png) | [View](images/guide/phase1-tour1-en-light-1366x768.png) | [View](images/guide/phase1-tour1-en-dark-1366x768.png) |
| Tour stop 4 | [View](images/guide/phase1-tour4-ar-light-1366x768.png) | [View](images/guide/phase1-tour4-ar-dark-1366x768.png) | [View](images/guide/phase1-tour4-en-light-1366x768.png) | [View](images/guide/phase1-tour4-en-dark-1366x768.png) |
| Sample task 3 | [View](images/guide/phase1-sample-task3-ar-light-1366x768.png) | [View](images/guide/phase1-sample-task3-ar-dark-1366x768.png) | [View](images/guide/phase1-sample-task3-en-light-1366x768.png) | [View](images/guide/phase1-sample-task3-en-dark-1366x768.png) |
| Sample finished | [View](images/guide/phase1-sample-finished-ar-light-1366x768.png) | [View](images/guide/phase1-sample-finished-ar-dark-1366x768.png) | [View](images/guide/phase1-sample-finished-en-light-1366x768.png) | [View](images/guide/phase1-sample-finished-en-dark-1366x768.png) |
| Help index | [View](images/guide/phase1-help-index-ar-light-1366x768.png) | [View](images/guide/phase1-help-index-ar-dark-1366x768.png) | [View](images/guide/phase1-help-index-en-light-1366x768.png) | [View](images/guide/phase1-help-index-en-dark-1366x768.png) |
| Help page B4 | [View](images/guide/phase1-help-page-ar-light-1366x768.png) | [View](images/guide/phase1-help-page-ar-dark-1366x768.png) | [View](images/guide/phase1-help-page-en-light-1366x768.png) | [View](images/guide/phase1-help-page-en-dark-1366x768.png) |

### Phase 1 verification and scope

- Full Release build: **0 warnings, 0 errors**.
- Complete Release suite (`dotnet test -c Release --no-restore -m:1`): **874 passed, 0 failed, 0 skipped**; App 250, Drafting 396, Law 113, FormEngine 67, Storage 18, Documents 6, Web 24. Project execution is serialized; every test remains enabled.
- Native desktop launched with `dotnet run --project src/DzLegal.App -c Release --no-build --no-restore`. Windows UI Automation exercised all six tour stops from Home, Help index/page/back/tour, the no-model sample, explicit template selection, the real placeholder blocker, manual editor completion, a real PDF save, SampleDone, and Leave/Cancel with unsaved work.
- Existing user profile, preferences, recent list, guide preference and recovery files were protected temporarily and restored with SHA-256 verification. Generated test files were preserved outside the repository. Nothing was deleted.
- Token contrast checks passed in both themes: text minimum 5.06:1, primary button text minimum 6.79:1, measured border minimum 3.47:1, focus ring minimum 8.02:1, and completion icon minimum 5.27:1.
- Protected legal engine/data/prompts, document renderers, model providers, Harness, tools, deployment and CI are unchanged. All four sample JSON files are unchanged. No dependencies, new projects or global shortcuts were added.
- Outside the desktop/presentation guide implementation, changes are `src/DzLegal.Storage/GuidePreferences.cs` (offer persistence), `src/DzLegal.Web/Services/WebEngine.cs` and `Components/_Imports.razor` (required shared-loader move), the listed App tests/harness, this report and the 24 Phase 1 PNGs. No other areas changed.

Only `djihane/guide` is used for the Phase 1 commit/push. No force-push, rebase or other-branch change. The manual-completion decision above is resolved; no other Phase 1 uncertainty remains. Phases 2 and 3 are not implemented.

## Help and tour: onboarding visual alignment

Visual-only follow-up to Phase 1, based on `9472b57`. Before editing, read WelcomeView, all setup step views, SetupView, HomeView, their window hosts and Styles/Workbench.axaml; opened the current Help/offer/tour through the existing rendering tests and inspected the reference and current captures. The differences were reported to the owner before source edits.

### Audit and alignment

The reference family uses the bundled photograph and WelcomeOverlayBrush, warm Welcome text/edge/focus brushes, SetupSurfaceBrush panels, WelcomeControlBrush confirmation cards, 16 px card corners, 10 px button corners, and gold primary buttons in both themes. Home's shell is 1366 x 768 with an 800 x 560 minimum. It uses normal OS window chrome; it has no separate custom chrome template. Its photograph is blurred by 8 px, while Welcome/setup use the photograph without that blur. The family does not add card shadows. Localization and the shared theme controller already worked in Help and the tour.

Previously Help was a smaller 900 x 600 window with a workbench canvas, an opaque 12 px-corner card, 18/32 body typography, text-only index rows and a separate tour footer. The offer lacked the photograph/overlay and used a 24 px regular title. The tour used a 22 px title, workbench surfaces/counter colours, 8 px button corners and green primary/highlight styling in light mode. Guide button padding and focus colours also differed from onboarding.

- Help now uses Home's window dimensions, page margins, blurred photograph, overlay, header and content-panel treatment. Seven consistent icon rows and the existing tour action share the index card. Page text wraps in a reading column and scrolls inside the card; Back to help stays inside its footer.
- The offer uses the Welcome/setup photograph, blurred by the same 8 px as Home, plus `WelcomeOverlayBrush` and one Home glass card. The tour card uses the same Home glass panel (`SetupSurfaceBrush`, 16 px radius, `20,16` padding, no border or shadow), Home title/body/counter typography and the same 44 px Home gold primary and outlined secondary button classes. The extra offer Info icon was removed so the dialog is a single Home-style card.
- Existing Welcome palette, UiFont, regular Fluent icons and the single bundled photograph are reused. No font, icon package or image file was added or duplicated. The photograph keeps its original orientation in both reading directions.
- A small opaque WelcomeControlBrush well protects the Help heading icon from the bright part of the photograph. Pixel checks found 2.84:1 without it in dark mode; the corrected icon meets the required contrast.
- Primary captions explicitly use WelcomeButtonTextBrush inside the workbench, whose inherited text styling otherwise interfered with the tour caption in dark mode. Next/Finish still reads the same NextText binding and invokes the same command.
- The tour dims the live workbench with a dark gradient built from the existing Home dark-overlay stops (`#B81D201E` / `#B31D201E`) in both themes, replacing the light `#B8F2F0EA` veil. The highlighted anchor keeps a static 3 px gold ring. The light theme now uses `OnboardingHighlightBrush` `#D2B981` (the existing light-family logo/edge gold) because the previous `WelcomeMarkBrush` `#786039` measured only 1.03:1 against the new dark backdrop; dark uses `#C6A76D`.
- The card stays next to the anchor and flips side. For a large anchor that leaves less than the preferred 420 px side (for example the Legal grounds panel at 125% scaling), the card narrows to the wider free side instead of covering the anchor. The card never intersects the highlight at any of the sixteen display modes; the anchor names, commands, keyboard handling, focus return and tour view model are unchanged.
- The highlight/card/backdrop remain static. No animation was added, so reduced motion requires no alternate animation path. Repeated layouts retain the same highlight visuals, avoiding flicker.

### Owner decisions

The initial brief asked for a glass card with a thin border and shadow, the exact 58 px "New petition" button style, and an outlined disabled Back. Home has no bordered/shadowed glass panel, so the owner chose the actual Home glass panel: `SetupSurfaceBrush`, 16 px radius, `20,16` padding, no border and no shadow. Next/Finish and the offer's Yes use the 44 px Home `primary` gold style (the same one as Resume/Confirm), not the 58 px main-action. Disabled Back keeps its 1 px outline and uses secondary text instead of Fluent's borderless disabled look.

### Shared resources added

Added `OnboardingPhotograph` in App.axaml, referencing the existing `Assets/Images/welcome-bg.jpg` once. Workbench.axaml now exposes 37 visual resources so the Help/tour views reference family values rather than carrying separate pixel values:

| Group | Resource keys |
| --- | --- |
| Window | OnboardingWindowWidth, OnboardingWindowHeight, OnboardingWindowMinWidth, OnboardingWindowMinHeight |
| Insets | OnboardingPageMargin, OnboardingCardPadding, OnboardingHeaderPadding, OnboardingIconPadding, OnboardingDialogPadding, OnboardingButtonPadding, OnboardingRowMargin, OnboardingActionMargin, OnboardingParagraphMargin |
| Shape / spacing | OnboardingEdgeThickness, OnboardingFocusThickness, OnboardingCardRadius, OnboardingButtonRadius, OnboardingPageGap, OnboardingDialogGap |
| Typography | OnboardingTitleSize, OnboardingBodySize, OnboardingBodyLineHeight, OnboardingSecondarySize, OnboardingButtonSize |
| Controls / content | OnboardingButtonHeight, OnboardingPrimaryMinWidth, OnboardingContentWidth, OnboardingReadingWidth, OnboardingDialogWidth, OnboardingIconSize, OnboardingBackgroundBlur |
| Tour geometry | OnboardingTourWidth, OnboardingTourMinWidth, OnboardingTourEdgeSpace, OnboardingTourAnchorGap |
| Dimming / highlight | OnboardingTourBackdropBrush (dark gradient in both themes), OnboardingHighlightBrush (light `#D2B981`, dark `#C6A76D`) |

Shared `onboarding-*` styles in Workbench.axaml cover cards, dialogs, text, buttons, captions and focus. Home's `home-panel`, `home-title`, `home-text`, `home-note`, `home` and `home primary` treatments moved from HomeView into the same shared stylesheet so the tour and offer reference them directly rather than copying values. Guide.axaml references those styles and adds the icon treatment. Normal Welcome/setup/Home states are unchanged; Home's disabled outline is now explicit and shared. Existing ControlRadius and all Welcome* colour/font resources remain authoritative.

### Tests and screenshot review

Added four tests in `GuideVisualTests`; **no test was deleted or skipped**. The three existing visual tests were extended for the new Home surface: their border/focus surface key is `SetupSurfaceBrush` instead of `WelcomeControlBrush`, and `home-note` (13 px) is accepted as secondary text. Every previous check remains:

- `HelpIndexAndAllSevenPagesFitAndMeetRenderedContrastInEveryDisplayMode`
- `TourOfferUsesTheFamilyCardAndButtonsWithoutClippingOrLowContrast`
- `EveryTourStopFitsAndMeetsContrastWithGoldHighlightAndStableBackdrop`
- `HomeAndTourUseTheSameFamilySurfaceAndButtons`

Every layout, clipping, typography, border, Tab-order, arrow, Escape and focus-ring check covers all sixteen display modes: Arabic/English, light/dark, 1366 x 768 and 1920 x 1080, at 100% and 125% scaling. All seven Help pages are checked, including full paragraph retention and scrolling. All six tour stops are checked, including Next/Finish text, enabled/disabled Back, control bounds, button separation, card/anchor non-overlap, dark-gradient stops and gold highlight stability. The new Home test checks that Home, the offer and the tour resolve the same `SetupSurfaceBrush` panel, `WelcomeGoldBrush` primary, `WelcomeControlBrush` secondary and `home-title` styles, and captures the Home comparison images. Existing tour state/focus restoration tests remain intact.

Pixel contrast is measured on the four language/theme combinations at 1366 x 768 and 100% scaling. Size and scale change geometry, not palette, so these four cover every colour the screens can show. Measuring the other twelve full frames was redundant, and hundreds of full-frame headless Skia captures eventually blocked inside `SurfaceRenderTarget.Blit`; the four-combination pass completes reliably while still checking every rendered colour combination.

Contrast follows WelcomeTests: hide foreground text/icons, render and read the actual background pixels, then compute WCAG luminance ratios, including opacity. This covers text over the real photograph and composited card surfaces. Button borders are checked against their rendered surroundings; focus checks sample the visible Fluent focus adorner; highlight checks use its rendered surroundings. Thresholds remain 4.5:1 for text and 3:1 for icons/borders/focus/highlight. Body text is 16 px, secondary text 13 px and buttons 15 px.

Twenty-eight final 1366 x 768 captures were opened and inspected individually. The sixteen existing Help/tour-stop images and the four offer images are replaced; four final-stop images and four Home comparison images are added. Sample-case and Phase 0 captures are unchanged.

| Screen | Arabic light | Arabic dark | English light | English dark |
| --- | --- | --- | --- | --- |
| Home | [View](images/guide/phase1-home-ar-light-1366x768.png) | [View](images/guide/phase1-home-ar-dark-1366x768.png) | [View](images/guide/phase1-home-en-light-1366x768.png) | [View](images/guide/phase1-home-en-dark-1366x768.png) |
| Help index | [View](images/guide/phase1-help-index-ar-light-1366x768.png) | [View](images/guide/phase1-help-index-ar-dark-1366x768.png) | [View](images/guide/phase1-help-index-en-light-1366x768.png) | [View](images/guide/phase1-help-index-en-dark-1366x768.png) |
| Help page B4 | [View](images/guide/phase1-help-page-ar-light-1366x768.png) | [View](images/guide/phase1-help-page-ar-dark-1366x768.png) | [View](images/guide/phase1-help-page-en-light-1366x768.png) | [View](images/guide/phase1-help-page-en-dark-1366x768.png) |
| Tour offer | [View](images/guide/phase1-tour-offer-ar-light-1366x768.png) | [View](images/guide/phase1-tour-offer-ar-dark-1366x768.png) | [View](images/guide/phase1-tour-offer-en-light-1366x768.png) | [View](images/guide/phase1-tour-offer-en-dark-1366x768.png) |
| Tour stop 1 | [View](images/guide/phase1-tour1-ar-light-1366x768.png) | [View](images/guide/phase1-tour1-ar-dark-1366x768.png) | [View](images/guide/phase1-tour1-en-light-1366x768.png) | [View](images/guide/phase1-tour1-en-dark-1366x768.png) |
| Tour stop 4 | [View](images/guide/phase1-tour4-ar-light-1366x768.png) | [View](images/guide/phase1-tour4-ar-dark-1366x768.png) | [View](images/guide/phase1-tour4-en-light-1366x768.png) | [View](images/guide/phase1-tour4-en-dark-1366x768.png) |
| Tour stop 6 | [View](images/guide/phase1-tour6-ar-light-1366x768.png) | [View](images/guide/phase1-tour6-ar-dark-1366x768.png) | [View](images/guide/phase1-tour6-en-light-1366x768.png) | [View](images/guide/phase1-tour6-en-dark-1366x768.png) |

Native verification launched the built Release executable and used Windows UI Automation to open Home, start the tour from Home, advance through stops 1, 4 and 6 (counter and title read back, last button read `Finish`) and close it with Escape. The offer dialog, the remaining tour stops and every Help page are exercised by the rendered tests. The per-user profile, preferences, setup marker and draft files were backed up before the launch and restored byte-for-byte afterwards, with matching SHA-256 hashes; no user file was deleted or left changed.

### Verification

- Full Release build: **0 warnings, 0 errors**.
- Complete Release suite: **878 passed, 0 failed, 0 skipped**; App 254, Drafting 396, Law 113, FormEngine 67, Storage 18, Documents 6, Web 24. Every test remains enabled.
- The twenty-eight captures were regenerated from the final source; the Help images are byte-identical to the committed captures, and every image was opened and inspected in both reading directions and both themes.

### Scope

This follow-up's production changes are limited to Styles/Workbench.axaml, Views/HomeView.axaml, TourOfferView.axaml, TourOverlay.axaml and the visual positioning/resource assignments in TourOverlay.axaml.cs. The earlier App.axaml, Styles/Guide.axaml and HelpWindow.axaml work remains from the previous commit. Outside those front-end files, only `GuideVisualTests`, this report and the requested PNGs changed. No resx, view model, wording, engine, safety rule, anchor name or command, sample or web-demo source changed. The card-positioning fallback is visual layout only. No dependency, new project, runtime internet source or asset file was introduced.
