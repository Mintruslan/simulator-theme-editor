# SImulator Theme Editor — Windows test build

This package is a self-contained Windows x86 build of the SImulator Theme Editor fork. It includes the .NET runtime and presentation bundle. Microsoft Edge WebView2 Runtime must be installed on the test computer.

## Start

1. Extract or copy the complete folder without moving individual files out of it.
2. Run `SImulator.exe`.
3. Select a local `.siq` package and start the game.
4. Open **Внешний вид** and select **Открыть Theme Editor**.

## Local fonts

1. Open **Типографика** in Theme Editor.
2. Select a presentation element.
3. Choose **Загрузить…** and select `Samples/Jost-Regular.ttf` (or another TTF, OTF, WOFF, or WOFF2 file up to 5 MB).
4. Confirm that the live preview and open game table change immediately.
5. Select **Сохранить копию** for the built-in theme, then export it to verify portability.

Imported fonts are embedded in the theme JSON. The editor lists only the bundled `Standard` font and fonts imported into the active theme; it does not enumerate Windows fonts. A theme can contain up to eight font files.

## Test checklist

- Launch with networking disconnected and open a local `.siq` package.
- Check all ten preview states in Theme Editor.
- Save, reload, duplicate, export, import, and delete a user theme.
- Verify text, board, players, timer, correct/incorrect states, and the final round.
- Restart SImulator and confirm that the active theme and imported font are restored.

When reporting a problem, include Windows version, WebView2 version, package name, selected preview/game state, and exact reproduction steps.
