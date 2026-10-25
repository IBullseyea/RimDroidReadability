# RimDroid Readability 0.2.4

Pre-release · MIT License

## Features

- Large colonist/item indicators with independent text and marker sizes.
- Item categories, grouped stack totals and a touch-friendly finder that preserves zoom.
- Bottom-right item filter shortcut.
- Automatically follows the game language, with 29 included translations.
- English/Turkish installation instructions and a complete phone screenshot gallery.

## Install

Download **RimDroidReadability-0.2.4.zip** below. Extract/update the entire `RimDroidReadability` folder in the instance's `Mods` directory, enable the mod and fully restart the game. Enable indicators in **Options → Mod Settings → RimDroid Readability**; they are off by default.

The `.sha256` file contains the ZIP checksum. GitHub's automatic Source code archives do not contain the compiled mod DLL.

## Screenshots

Current English UI screenshots supplied by the player on Samsung S25 FE / RimDroid 0.3.0v2: the filter shortcut, large labels and stack totals.

![Resources filter and grouped stack totals](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/resource-filter.jpg)

<details>
<summary>Full gallery — 10 more screenshots</summary>

| | |
| --- | --- |
| **Colonists / Items off**<br>![Colonists / Items off](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/colonist-indicators.jpg) | **Pause tooltip and colonists**<br>![Pause tooltip and colonists](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/pause-tooltip.jpg) |
| **All items**<br>![All items](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/all-items.jpg) | **Medicine shortcut tooltip**<br>![Medicine shortcut tooltip](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/medicine-tooltip.jpg) |
| **Medicine filter**<br>![Medicine filter](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/medicine-filter.jpg) | **Wood filter**<br>![Wood filter](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/wood-filter.jpg) |
| **Weapons filter**<br>![Weapons filter](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/weapon-filter.jpg) | **Food filter**<br>![Food filter](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/food-filter.jpg) |
| **Apparel filter**<br>![Apparel filter](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/apparel-filter.jpg) | **Return to Items off**<br>![Return to Items off](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/items-off-return.jpg) |

</details>

## Compatibility and validation

Requires RimDroid Native Unity on Android, the audited RimWorld 1.6.4633 rev1254 DLL identity in README.md, and a single already loaded Harmony 2.x. Other builds and non-Native routes are outside this package's supported scope.

The player supplied English UI examples on Samsung S25 FE / RimDroid 0.3.0v2. Local compilation, standalone logic/translation/tooling tests, exact API signatures and ZIP integrity checks passed. Comprehensive device/performance checks and font coverage for all languages remain pending.

At most 120 markers and 40 labels are drawn; the finder still lists all eligible targets. Item scans refresh every 250 ms and their cost depends on item count. Search uses OrdinalIgnoreCase; Turkish dotted İ and i differ.

Original mod code, translations and text documentation are MIT licensed. Third-party game artwork in screenshots remains with its respective owners. Game, Unity and Harmony binaries are not included.
