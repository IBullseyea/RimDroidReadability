# RimDroid Readability — 0.2.4

**[Download mod ZIP](https://github.com/IBullseyea/RimDroidReadability/releases/download/v0.2.4/RimDroidReadability-0.2.4.zip)** · [All releases](https://github.com/IBullseyea/RimDroidReadability/releases) · [Türkçe](README.tr.md)

Large map indicators, item filters and a touch-friendly target finder for **RimWorld 1.6 on RimDroid Native for Android**. Find colonists and loose item stacks while preserving camera zoom.

MIT licensed · Experimental / pre-release

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

## Features

- Independent text (18/22/26/32) and marker (8/16/24 UI units) sizes.
- Distinct colonist/item shapes, high-contrast labels and optional grouping of nearby item stacks.
- Finder with colonist lists, item categories, search, stack totals and a Go action that preserves zoom.
- Bottom-right **Items: …** shortcut below the time controls: Off → All → Medicine → Wood → Weapons → Food → Resources → Apparel.
- Automatically follows the game language, with 29 translations including English, Turkish, Simplified Chinese and Traditional Chinese. Other languages use the game's English fallback.

Only free colonists and loose items on the current map are included. Fogged targets, inventory, animals, enemies, buildings and world-map targets are excluded.

## Install

1. Download **RimDroidReadability-0.2.4.zip** from [Releases](https://github.com/IBullseyea/RimDroidReadability/releases). GitHub's automatic **Source code** archives do not contain the compiled mod DLL.
2. Extract the `RimDroidReadability` folder into your RimDroid instance's `Mods` directory, or use RimDroid's mod import facility. Update the **whole folder**, including `1.6/Languages`.
3. Enable the mod in the mod list and restart the game completely. It uses Harmony already loaded by RimDroid; no additional Harmony is bundled.
4. Open **Options → Mod Settings → RimDroid Readability** and enable indicators. The mod is **off by default**.

Use **Find** to select a colonist or browse item types and individual stacks. **Go** selects and centers a valid target while preserving zoom. The item shortcut and finder share the same saved category. To disable the overlay, turn indicators off in Mod Settings, or disable the mod in the mod list and restart.

## Compatibility and limits

Requires **Native Unity on Android**, RimWorld **1.6.4633 rev1254**, and a single already loaded Harmony 2.x. Desktop Linux, Box64 and alternative Native Mono routes are outside the supported scope. The exact game DLL/API identity is checked; a different build disables the mod and displays a reason in Mod Settings.

| Audited reference | Identity |
| --- | --- |
| `Assembly-CSharp.dll` SHA-256 | `39c5532c8f9fd8b31cdc9450d3148bc2f372f03f030dc5cddd170c9a08774076` |
| MVID | `c337411a-5cb6-49b6-8f4c-0609acf16d96` |
| Unity reference | `2022.3.35f1` |

The player supplied working English UI examples on **Samsung S25 FE / RimDroid 0.3.0v2**. Local checks cover compilation, standalone logic, translations, API signatures and package integrity. Comprehensive phone/performance testing and font coverage across all languages remain pending.

At most 120 markers and 40 labels are drawn; the finder still lists all eligible targets. Item scans refresh every 250 ms and their cost depends on item count. Search uses `OrdinalIgnoreCase`; Turkish dotted İ and i differ.

## Build from source

Use Python 3, an installed .NET SDK with a matching .NET runtime/reference pack, and your own game `Managed` directory matching the audited identity. Build tooling has been validated on Linux. Scripts perform no dependency installation or NuGet restore.

```sh
python3 tools/build.py --msbuild --game-managed-dir "/path/to/RimWorldLinux_Data/Managed"
python3 tools/validate.py --game-managed-dir "/path/to/RimWorldLinux_Data/Managed"
```

The static audit needs an existing `ilspycmd` on PATH or in `~/.dotnet/tools`; set `ILSPYCMD` to its full path if necessary. The build runs standalone C# and Python tooling tests, then produces the mod DLL, `dist/RimDroidReadability-0.2.4.zip` and its `.sha256` file. Game references are read-only. Generated reports and caches stay in `.build/`; generated files are ignored by Git.

## Contributing and license

Bug reports should include mod/game/RimDroid versions, engine, device, UI scale and the exact Mod Settings status or relevant log. Translation corrections are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

Original mod code, translations and text documentation are licensed under [MIT](LICENSE). This is an unofficial mod. RimWorld, RimDroid, Unity and Harmony remain governed by their respective owners and licenses. Gameplay screenshots contain third-party game artwork outside the mod's MIT license. Game files and third-party dependencies are not included.
