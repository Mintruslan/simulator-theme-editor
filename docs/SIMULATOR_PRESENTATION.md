# SImulator presentation source

SImulator remains a Windows-first, offline application. Its audience-facing screen is rendered by a local WebView2 instance from files in `src/SImulator/SImulator/webtable`; the runtime does not load its UI from a remote website.

## Source layout

- `web/simulator-ui` contains the SIOnline source imported as a Git subtree.
- `web/simulator-ui.integration.json` records the upstream revision and integration mode.
- `tools/build-simulator-ui.mjs` builds the `library-table` entry and synchronizes its generated assets.
- `src/SImulator/SImulator/webtable` contains the local runtime shell and generated presentation bundle.

The SImulator-specific `index.html`, `script.js`, and `style.css` files are preserved during synchronization. Webpack owns `main.js`, `vendor.js`, and hashed assets. Do not edit generated JavaScript manually.

## Local theme storage

- `Default SImulator Theme` is built in and read-only; it preserves legacy colors and uses the bundled `Standard` font.
- User presets are individual, versioned `*.theme.json` files under `%LOCALAPPDATA%\Khil-soft\SImulator\Settings\Themes`.
- The active theme document is also stored in `user.config`, so the application can restore it without network access.
- `FileThemeRepository` validates IDs and schema versions, writes atomically, ignores broken presets while listing the library, and supports save, duplicate, delete, import, and export operations.

Schema version 1 embeds imported TTF, OTF, WOFF, and WOFF2 files as local data URLs in `assets.fonts`. A theme can contain up to eight fonts of 5 MB each. Typography tokens can reference only the bundled `Standard` family or one of these embedded assets, so exported font themes stay portable and never query the operating system or network. Image assets remain URL/path references.

## Theme Editor MVP

Open the **Design** tab in the desktop application and select **Открыть Theme Editor**. The editor itself is bundled in `webtable` and requires no server or internet connection.

- Left: visual sections for global settings, typography, board, question, and players.
- Center: a 16:9 live preview using the same presentation components as the game.
- Right: structured, non-CSS controls for the selected section.
- Typography: choose the bundled font or upload/remove fonts stored directly inside the theme JSON; keep the legacy adaptive sizing or switch a role to an exact pixel size; enable or disable its text shadow.
- Board: configure cell spacing and colors, or hide all cell/theme borders with one switch while preserving their saved width and color.
- Preview states: board, text question, image, video, audio, players, buzzer, correct answer, incorrect answer, and final round.
- Preset actions: load, save, duplicate, delete, import JSON, and export JSON.

Changes update both the editor preview and an active game presentation immediately. Saving writes the preset to the local theme library; closing SImulator persists the active theme in normal application settings.

The local `table` bridge includes the complete theme-name list together with the price matrix. This prevents the board from losing rows when it opens before the one-by-one theme announcement animation finishes; generic SIOnline callers can continue sending the original prices-only payload.

## Initial setup and build

```powershell
npm ci --prefix web/simulator-ui
pwsh tools/simulator-ui-build.ps1
dotnet build src/SImulator/SImulator/SImulator.csproj -p:Configuration=Release
```

The Node.js build requires downloaded development dependencies. The resulting WPF application and all presentation assets are local and continue to run without an internet connection.

For a cross-platform presentation-only build, run:

```sh
npm ci --prefix web/simulator-ui
node tools/build-simulator-ui.mjs
```

Use `node tools/build-simulator-ui.mjs --sync-only` to copy an already-built `web/simulator-ui/dist` directory without invoking webpack.

## Updating the SIOnline subtree

```sh
git fetch sionline-upstream master
git subtree pull --prefix=web/simulator-ui sionline-upstream master --squash
```

After an update, review the bridge in `src/LibraryCore.tsx`, build the presentation bundle, and run the offline smoke test before committing generated artifacts.

## Required verification

1. Build the `library-table` bundle without TypeScript errors.
2. Run SIOnline unit tests and lint.
3. Run `node tools/verify-simulator-ui.mjs` for an isolated WebView bridge and offline-resource smoke test.
4. Build SImulator on Windows.
5. Disconnect networking and open a local `.siq` package.
6. Verify table, text, image, video, audio, timers, players, answer states, and final round.
