# SImulator presentation source

SImulator remains a Windows-first, offline application. Its audience-facing screen is rendered by a local WebView2 instance from files in `src/SImulator/SImulator/webtable`; the runtime does not load its UI from a remote website.

## Source layout

- `web/simulator-ui` contains the SIOnline source imported as a Git subtree.
- `web/simulator-ui.integration.json` records the upstream revision and integration mode.
- `tools/build-simulator-ui.mjs` builds the `library-table` entry and synchronizes its generated assets.
- `src/SImulator/SImulator/webtable` contains the local runtime shell and generated presentation bundle.

The SImulator-specific `index.html`, `script.js`, and `style.css` files are preserved during synchronization. Webpack owns `main.js`, `vendor.js`, and hashed assets. Do not edit generated JavaScript manually.

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
