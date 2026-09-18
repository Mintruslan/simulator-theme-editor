# SIGame Theme Editor

SIGame now uses the same versioned, portable Theme Schema and local visual editor as SImulator. The editor and preset repository live in the shared `SITheme` project; SIGame keeps its existing game logic and native WPF presentation and applies theme tokens through a small WPF resource adapter.

## Opening the editor

- Open **Settings → Design** and select **Open Theme Editor**.
- During a game, use the **🎨** button in the studio toolbar.
- Only one editor window is opened at a time. Closing it disposes its WebView2 control.

The editor is fully local. Its bundle is generated from `web/simulator-ui` and copied to both desktop applications by `node tools/build-simulator-ui.mjs`.

## Supported theme controls

- Global background color/image, image fit and position, text color, and accent color.
- All eight typography roles, including embedded fonts, adaptive or exact size, weight, color, alignment, and optional shadow.
- Board backgrounds, spacing, padding, border visibility, plain-text mode, hover/selection colors, radius, and answered opacity.
- Question background, padding, alignment, and image fit.
- Player layout, spacing, card styling, and buzzer/correct/incorrect states.
- Load, save, duplicate, delete, JSON import, and JSON export.

SIGame maps these tokens onto its existing native `SIUI.Table`, studio, and player controls. Theme changes received from the editor are applied immediately through dynamic WPF resources; no game rules or game-state code are duplicated.

## Portable fonts

The font selector never enumerates Windows fonts. It contains only the bundled `Standard` font and font files embedded in the active theme. TTF and OTF are loaded into the native SIGame font cache. WOFF and WOFF2 remain portable and render in the web preview, while native WPF uses the bundled fallback because WPF cannot load webfont containers directly.

## Local storage

SIGame presets are stored as individual `*.theme.json` files under `%LOCALAPPDATA%\Khil-soft\SIGame\Settings\Themes`. The selected preset ID is stored in the normal SIGame user settings. Presets remain compatible with SImulator through the shared schema and JSON import/export.

## Verification

```powershell
node tools/build-simulator-ui.mjs
npm test --prefix web/simulator-ui -- --runInBand
npm run lint --prefix web/simulator-ui
node tools/verify-simulator-ui.mjs
dotnet test test/SIGame/SIGame.ViewModel.Tests/SIGame.ViewModel.Tests.csproj -c Release -p:Platform=AnyCPU
dotnet run --project tools/SIGame.ThemeEditorSmoke/SIGame.ThemeEditorSmoke.csproj -c Release -p:Platform=AnyCPU
dotnet build src/SIGame/SIGame/SIGame.csproj -c Release -p:Platform=AnyCPU
```

The native smoke executable starts a real WPF `ThemeEditorWindow`, waits for the local WebView2 page, verifies the SIGame-branded editor and all five sections, sends a live theme change through the WebView bridge, checks the resulting native WPF resources, saves the preset, closes the window, and exits.
