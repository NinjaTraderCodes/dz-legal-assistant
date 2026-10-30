# Welcome screen

## Scope and navigation

The desktop lifetime now opens a separate onboarding window. An absent
`%LOCALAPPDATA%\DzLegalAssistant\profile.json` selects Welcome. Any existing profile,
including an empty or malformed one, selects the start placeholder without parsing
or overwriting it. Existing workbench construction remains unchanged.

`WelcomeView` and `WelcomeViewModel` own the new screen. `OnboardingWindow` accepts
factories for setup step 1 and the start screen. Neither exists in this checkout,
so the default factories display a localized placeholder and a button that opens
the existing drafting workspace. No fake profile is created: until real setup
writes a profile, a later launch still follows the absent-profile rule.

Language switching uses the existing `Loc.SetLanguage` localization mechanism,
without constructing or modifying `MainWindowViewModel`. The selection is stored
separately in `welcome-preferences.json` so it cannot bypass setup. Missing or
malformed preferences fall back to Arabic; an unwritable preferences folder does
not crash the screen (the view model exposes `LanguageSaveFailed`).

## Design

- The 1280x720 JPEG is the project owner's own generated image (30 Oct; see its licence note,
  which states the source); it replaced an earlier photograph with no stated source. The existing
  `AvaloniaResource Include="Assets\**"` wildcard registers both. Git records the
  canonical `Assets/Images` paths. Windows still refuses the physical directory
  rename (`assets` to a temporary name returns access denied); no original file
  was deleted, copied over, or rewritten. The new view resolves the embedded
  asset name case-insensitively, so the current checkout also works. Future
  checkouts use the canonical paths recorded in Git.
- A static charcoal gradient covers the dark photograph. The light design uses
  warm ivory with dark text and a deeper gold mark; it is not an inversion.
- New, separately named theme tokens are added to `Workbench.axaml`; existing
  tokens and workbench styles are unchanged. Gold matches the existing
  `#C6A76D` palette. Fonts are the existing bundled fonts.
- The symmetric scales geometry is a shared resource. There is no gavel, external
  icon package, blur effect, download, or analytics.
- Arabic is RTL, English LTR. English title tracking is 0.6; Arabic tracking is zero.
  Language labels retain their own scripts. Tab visits Arabic, English, then Start
  in the mirrored reading order. Start is the default Enter action.
- Logo, language controls, and Start have automation names. Focus has an explicit
  border. A 120 ms opacity fade uses Windows' client-area animation preference;
  platforms without that preference render without motion.

## Tests and screenshots

New tests live only in `tests/DzLegal.App.Tests/WelcomeTests.cs`. They cover launch
routing, malformed profile/preference handling, language persistence, explicit
translations in both resource files, keyboard navigation, screen bounds, overlap,
and pixel-based contrast over the rendered photograph plus overlay.

The rendering matrix covers Arabic/English, light/dark, 1366x768 and 1920x1080 at
100% and 125%. Screenshot output is opt-in via `DZLEGAL_SCREENSHOTS` outside the
repository; the eight final 100% images are copied to `docs/images/onboarding`.
All eight were visually inspected: no overlap or clipping was found.
Contrast measurements sample the entire bounds beneath each text/mark after
hiding foreground content, rather than assuming a flat background.

The final Release build passed with zero warnings and errors. All 14 new Welcome
tests pass, including the full layout/contrast matrix at both scales. The full
regression suite passed: 690 tests, zero failures, zero skipped, including all
24 web tests. Checks used `--no-restore` (and `--no-build` for the full test run)
to avoid dependency downloads; build telemetry was disabled for the commands.

The final 100% screenshots measure at least 5.36:1 for title/tagline/footer text,
6.79:1 for button text, 13.67:1 for language text, and 3.17:1 for the logo,
divider and component outlines. Both 125% layouts also pass the 4.5:1 text and
3:1 non-text thresholds. The focus token is checked against the language-control
surface (at least 13.67:1); against the photograph it uses the same high-contrast
ink as primary text.

An actual desktop launch was attempted. This execution session exposed no usable
UI Automation controls, and the window did not close normally; only that verified
smoke-check process was stopped. No workbench was opened and no user preference
was saved by that attempt. Interactive desktop verification is not claimed.

## Protection and exceptions

No existing tests, workbench view models, legal engine, law data, prompts, web
demo, PDF renderer, or Word renderer were edited. Document-output identity has not
been independently measured; the source that produces those outputs is unchanged.

The requested release notes (this file) and eight final screenshots are the only
deliverables outside App, Presentation, Storage, and App tests. No push will be performed.

## Wording proposal only

The implemented Arabic wording is exactly the approved text. A possible future
tagline, not used in the UI:

«دقّة الصياغة، وسرّية المهنة» / “Precise drafting. Professional confidentiality.”

## Known limitations

Setup and start remain placeholders. The supplied background is deliberately
1280x720. The current Windows filesystem directory casing could not be normalized,
although Git paths are canonical and resource loading works with either casing.
An interactive check on the user's desktop remains necessary; headless keyboard,
layout, rendering and contrast checks pass.

## Files delivered

- Existing files: `src/DzLegal.App/App.axaml.cs`,
  `src/DzLegal.App/Styles/Workbench.axaml`, and the two Presentation string files.
- New App files: `Services/WelcomeMotion.cs`, `Views/OnboardingWindow.cs`,
  `Views/WelcomeView.axaml`, and `Views/WelcomeView.axaml.cs`.
- New Presentation file: `ViewModels/WelcomeViewModel.cs`.
- New Storage file: `WelcomePreferences.cs`.
- New App tests file: `WelcomeTests.cs`.
- The two supplied assets under the canonical `Assets/Images` Git paths.
- This release note and eight PNGs under `docs/images/onboarding`.

## Feature 1 edit exceptions

The owner explicitly authorized edits to `src/DzLegal.App/Views/OnboardingWindow.cs`
and `src/DzLegal.Storage/WelcomePreferences.cs`. Both were created by the Welcome
feature on `djihane/onboarding`. The exception is limited to Step C: setup
navigation, the setup-seen marker, and language/theme persistence through
`preferences.json`. Neither file may be renamed or deleted. Existing tests and
all other protection rules remain unchanged.

## Fixes before merging (30 Oct 2026, branch `claude/onboarding-fixes`)

- **Welcome background.** It is now the project owner's own generated image, with a licence note stating its source.
- **Strings.** The 102 strings held in `SetupLocalization` and `HomeLocalization` moved into `Strings.resx` and `Strings.en.resx`. The two classes now read the shared resources only.
  - `EverySetupBindingHasArabicAndEnglishWording` now checks that every `L[...]` key of the Welcome, Setup and Home screens resolves in both languages.
  - The Home language test no longer reads the removed table.
- **Contrast.**
  - The welcome footer sits on the reading panel, so its contrast no longer depends on the photograph.
  - The dark theme's secondary text is a little lighter (`#ECE6DC`), so the tagline stays at least 4.5:1 over the new image.
- **Removed from the repository:**
  - `screenshots/`: screenshots are saved outside the repository (`DZLEGAL_SCREENSHOTS`);
  - an empty `package-lock.json`;
  - a copy of the onboarding brief 1.1, which the default branch holds as `docs/briefs/ONBOARDING-BRIEF.pdf`.
- **Screenshots.** The eight welcome screenshots under `docs/images/onboarding` were regenerated with the new image.
