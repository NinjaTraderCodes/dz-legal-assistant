# Language switcher backgrounds

## Change and scope

The two-button switcher is native Avalonia UI in
`src/DzLegal.App/Views/WelcomeView.axaml`, not a web/CSS component. Its header
Border uses the existing `WelcomeLanguageSurfaceBrush` dynamic resource, and
each `Button.welcome-language` uses `WelcomeControlBrush`. The header Grid and
StackPanel introduce no additional background. The underlying photograph and
page overlay remain unchanged.

Only three production values changed in `src/DzLegal.App/Styles/Workbench.axaml`:

- Light `WelcomeLanguageSurfaceBrush`: ivory to `Transparent`.
- Light `WelcomeControlBrush`: ivory to opaque white (`#FFFFFF`).
- Dark `WelcomeLanguageSurfaceBrush`: transparent to `#B31D1812`, using the
  existing dark app-bar palette at approximately 70% background alpha.

The existing `DynamicResource` and application theme mechanism apply the colors
immediately. No group opacity, theme state, selector, component, layout, border,
typography, command, or other page background was changed. Repository searches
found this two-button group only on Welcome. The workbench and web demo use
different, single language buttons and were not modified.

## Fresh live captures

All five images are client-area captures of the running Release desktop app,
started with the documented `dotnet run --project src/DzLegal.App` procedure
using `-c Release --no-build --no-restore`. Each image was visually inspected.

| Capture | Pixels |
| --- | --- |
| [landing-page-native-en-light.png](landing-page-native-en-light.png) | 1366 x 748 |
| [landing-page-native-en-dark.png](landing-page-native-en-dark.png) | 1366 x 748 |
| [landing-page-native-ar-light.png](landing-page-native-ar-light.png) | 1366 x 748 |
| [landing-page-native-ar-dark.png](landing-page-native-ar-dark.png) | 1366 x 748 |
| [landing-page-native-minimum-width-ar-dark.png](landing-page-native-minimum-width-ar-dark.png) | 800 x 748 |

The light captures show two independent white buttons with the photograph
visible between and around them. The dark captures show the subtle dark
container without dimming the labels or buttons. The surrounding screen retains
its existing design.

## Verification

- `dotnet build -c Release --no-restore`: zero warnings, zero errors.
- `dotnet test -c Release --no-build --no-restore -m:1`: **697 passed, zero failed,
  zero skipped**. Test projects ran sequentially, as documented by the previous
  button-alignment verification.
- Existing welcome rendering coverage checks Arabic/English, light/dark,
  1366 x 768 and 1920 x 1080, at 100% and 125% scaling. Existing centering,
  keyboard navigation, language persistence, RTL/LTR, theme and safety tests pass.
- The existing contrast sampler in `tests/DzLegal.App.Tests/WelcomeTests.cs`
  was updated because the transparent inter-button gap now contains photograph.
  It renders the individual button fills with their labels hidden and samples
  inside the rounded borders. Component contrast remains at least 3:1 and text
  contrast at least 4.5:1; no threshold or test was removed or weakened.
- Native automation invoked both language buttons and the existing theme button.
  Six consecutive theme toggles updated the same window without reload. Captures
  confirm both backgrounds in both language directions.
- A 390 x 844 outer-window resize request was attempted. The existing native
  minimum width enforced an 800 px client area (816 px including window chrome).
  The switcher fits at that minimum. This screen has no supported mobile-width
  layout; no 390 px mobile verification or mobile screenshot is claimed.

## Preservation

The existing Debug app instance was left running; verification used and closed
only its own Release app. Restart the Debug app to load the new compiled styles.
Unrelated changed and untracked repository files, including all prior screenshots,
were checked against their initial SHA-256 hashes and preserved. No commit or
push was made. No legal engine, legal data, or navigation code was changed.
