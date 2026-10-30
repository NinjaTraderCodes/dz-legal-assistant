# Button alignment verification

The three welcome buttons are native Avalonia controls in
`src/DzLegal.App/Views/WelcomeView.axaml`; the Blazor demo has no corresponding
welcome page. Their shared style now centers content on both axes. The start
button's inherited `8,5,8,6` padding is replaced with symmetric `8,5.5` padding,
preserving its total padding and dimensions. Existing typography, RTL layout,
colors, commands, and the uncommitted theme work are preserved.

All 14 PNGs below were created after the correction and individually inspected.
Existing images under `docs/images/onboarding/` were left untouched.

## Live desktop captures

Started with the documented `dotnet run --project src/DzLegal.App` procedure,
using the already-built Release configuration (`-c Release --no-build --no-restore`).
Native Windows UI Automation successfully invoked Arabic, English, and Get started.
Arabic mirrored the interface; English restored LTR; Get started opened the existing
setup placeholder. Captures use the running window's client area, excluding Windows
chrome. The live window resized during verification; the exact captured dimensions
are recorded here. Original language and theme preference files were restored.

| Capture | Pixels |
| --- | --- |
| [landing-page-native-en-light.png](landing-page-native-en-light.png) | 1366 x 748 |
| [landing-page-native-ar-light.png](landing-page-native-ar-light.png) | 1362 x 693 |
| [setup-placeholder-native-en-light.png](setup-placeholder-native-en-light.png) | 1362 x 693 |

## Reproducible desktop rendering matrix

These are actual production views rendered by the repository's existing Avalonia
headless tests with the real Skia renderer and bundled fonts, not mockups or browser
screenshots. `DZLEGAL_SCREENSHOTS` pointed to a temporary folder outside the repo;
selected PNGs were copied here unchanged. All eight welcome combinations below are
at 100% scaling. The same combinations also pass layout, contrast, and label-center
checks at 125% scaling. Label centers allow one physical pixel for layout rounding.

| Arabic | English |
| --- | --- |
| [landing-page-desktop-ar-light-1366x768.png](landing-page-desktop-ar-light-1366x768.png) | [landing-page-desktop-en-light-1366x768.png](landing-page-desktop-en-light-1366x768.png) |
| [landing-page-desktop-ar-dark-1366x768.png](landing-page-desktop-ar-dark-1366x768.png) | [landing-page-desktop-en-dark-1366x768.png](landing-page-desktop-en-dark-1366x768.png) |
| [landing-page-desktop-ar-light-1920x1080.png](landing-page-desktop-ar-light-1920x1080.png) | [landing-page-desktop-en-light-1920x1080.png](landing-page-desktop-en-light-1920x1080.png) |
| [landing-page-desktop-ar-dark-1920x1080.png](landing-page-desktop-ar-dark-1920x1080.png) | [landing-page-desktop-en-dark-1920x1080.png](landing-page-desktop-en-dark-1920x1080.png) |

Additional production screens, rendered at 1440 x 900 with the existing fictitious
test case and template engine (no model calls):

- [drafting-workspace-ar.png](drafting-workspace-ar.png)
- [drafting-workspace-en.png](drafting-workspace-en.png)
- [document-preview-ar.png](document-preview-ar.png)

## Verification and limits

- Release build: zero warnings, zero errors.
- All 14 welcome tests pass, including keyboard navigation and the strengthened
  existing rendering test. No test was skipped, removed, or weakened.
- Full suite: **697 passed, zero failed, zero skipped**, using
  `dotnet test -c Release --no-build --no-restore -m:1` to execute test projects
  sequentially. Parallel runs exposed a separate timing
  failure at `tests/DzLegal.App.Tests/WaitingTests.cs:299`, in
  `ClosingTheAppFreesTheLocalModelsMemoryAndAServerThatHangsDoesNotHoldItUp`
  (expected release callback count 1, actual 0). That test passes in isolation
  and the complete App test project passes when projects run sequentially.
  The model-release code and its test were not changed.
- The local web demo starts with `dotnet run --project src/DzLegal.Web`
  (`-c Release --no-build --no-restore`) and returns HTTP 200 at
  `http://localhost:5080/`. Browser selection reports `No browser is available`,
  and browser discovery returns an empty list. Web visual, mobile, and browser
  interaction checks could not be performed; no mobile screenshot is claimed.
  Connect a browser through Settings > Computer use to complete those checks.
- The native onboarding window already has an 800 px minimum width and is not
  a mobile page. Setup and start remain the existing placeholders; this change
  does not implement onboarding steps.
- No commit or push was made. Other pre-existing changed and untracked files
  were checked against their initial SHA-256 hashes and preserved.
