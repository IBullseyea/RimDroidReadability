# RimDroid Readability 0.2.4 — Game language support

Pre-release candidate · MIT License

## What's included

- All in-game mod text follows the game language, with 29 original translations and English fallback for other languages.
- Localized settings, item shortcut, finder, tooltips, quantities and mod status messages. Colonist names and item labels remain supplied by the game.
- Retains the bottom-right item filter shortcut from 0.2.3, collection-enumeration fix from 0.2.2 and Native Android detection fix from 0.2.1.
- English/Turkish installation documentation and the MIT license are included in the ZIP.

## Preview

Current English UI screenshot supplied by the player on Samsung S25 FE / RimDroid 0.3.0v2, showing grouped resource labels and the bottom-right item filter shortcut.

![Large map indicators and grouped stacks](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/docs/images/resource-filter.jpg)

## Install or update

Download **RimDroidReadability-0.2.4.zip** from the release assets. Extract/update the entire `RimDroidReadability` folder, including `1.6/Languages`, in the instance's `Mods` directory. Enable it in the mod list and fully restart the game. Enable indicators in **Options → Mod Settings → RimDroid Readability**; the mod is off by default. See README.md / README.tr.md for details.

GitHub's automatic Source code archives contain source only and cannot be installed directly as the compiled mod.

## Compatibility and validation

Requires RimDroid Native Unity on Android, the audited RimWorld 1.6.4633 rev1254 DLL identity documented in README.md, and a single already loaded Harmony 2.x. Other DLL builds and non-Native routes are not supported by this package.

Local compilation, standalone logic/translation/tooling tests, exact API signatures and ZIP contents passed. A user reported 0.2.2 and 0.2.3 working on Samsung S25 FE / RimDroid 0.3.0v2. Version 0.2.4 language/font behavior and comprehensive device/performance checks remain pending.

## Known limits

- At most 120 markers and 40 labels are drawn; the finder still lists all eligible targets.
- Item scan cost depends on map item count, with a 250 ms refresh interval.
- Search uses OrdinalIgnoreCase; Turkish dotted İ and i differ.
- Font coverage, long translations, touch behavior and performance need validation on the target device.

Original mod code, translations and documentation are MIT licensed. Game, Unity and Harmony binaries are not included. Detailed development and device acceptance records are in `docs/device-validation.tr.md` in the source repository.
