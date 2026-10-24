# RimDroid Readability — 0.2.4

[Türkçe](README.tr.md) · MIT License · Experimental / pre-release

Large map indicators and a touch-friendly target finder for **RimWorld 1.6 on RimDroid Native for Android**. Find colonists and item stacks without changing camera zoom.

## Screenshots

Current English UI screenshots supplied by the player on Samsung S25 FE / RimDroid 0.3.0v2: readable labels and the bottom-right item filter shortcut.

![Resource filter with grouped stack totals and the Items shortcut](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/docs/images/resource-filter.jpg)

<details>
<summary>More examples: wood filter and colonist indicators</summary>

**Wood filter** — only wood stacks are labeled; colonist markers stay visible.

![Wood filter showing stack quantities](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/docs/images/wood-filter.jpg)

**Colonist indicators** — item markers are disabled using Items: Off.

![Colonist indicators with Items set to Off](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/docs/images/colonist-indicators.jpg)

</details>

## Features

- Independent text (18/22/26/32) and marker (8/16/24 UI units) sizes.
- Distinct colonist/item shapes and optional grouping of nearby item stacks.
- Finder with colonist lists, item categories, search, stack totals and a Go action that preserves zoom.
- Bottom-right **Items: …** shortcut below the time controls: Off → All → Medicine → Wood → Weapons → Food → Resources → Apparel.
- Automatically follows the game language, with 29 translations including English, Turkish, Simplified Chinese and Traditional Chinese. Other languages use the game's English fallback.

Only free colonists and loose items on the current map are included. Fogged targets, inventory, animals, enemies, buildings and world-map targets are excluded.

## Install

1. Open [Releases](https://github.com/IBullseyea/RimDroidReadability/releases) and download **`RimDroidReadability-0.2.4.zip`**. GitHub's automatic **Source code** archives do not contain the compiled mod DLL.
2. Extract the `RimDroidReadability` folder into your RimDroid instance's `Mods` directory, or use RimDroid's mod import facility. Update the **whole folder**, including `1.6/Languages`.
3. Enable the mod in the mod list and restart the game completely. It uses Harmony already loaded by RimDroid; no additional Harmony is bundled.
4. Open **Options → Mod Settings → RimDroid Readability** and enable indicators. The mod is **off by default**.

Use **Find** to select a colonist or browse item types and individual stacks. **Go** selects and centers a valid target while preserving zoom. The item shortcut and finder share the same saved category. To disable the overlay, turn indicators off in Mod Settings, or disable the mod in the mod list and restart.

## Compatibility and status

Requires **Native Unity on Android**, RimWorld **1.6.4633 rev1254**, and a single already loaded Harmony 2.x. Desktop Linux, Box64 and alternative Native Mono routes are outside the supported scope. The exact game DLL/API identity is checked; a different build disables the mod and displays a reason in Mod Settings.

| Audited reference | Identity |
| --- | --- |
| `Assembly-CSharp.dll` SHA-256 | `39c5532c8f9fd8b31cdc9450d3148bc2f372f03f030dc5cddd170c9a08774076` |
| MVID | `c337411a-5cb6-49b6-8f4c-0609acf16d96` |
| Unity reference | `2022.3.35f1` |

A user reported versions 0.2.2 and 0.2.3 working on **Samsung S25 FE / RimDroid 0.3.0v2**. Version 0.2.4 passes local build, logic, translation, API and package checks; its language/font behavior and comprehensive device/performance checks remain pending. This is a pre-release candidate.

The overlay draws up to 120 markers and 40 labels; the finder still lists all eligible targets. Item scans refresh every 250 ms and their cost depends on item count. Search uses `OrdinalIgnoreCase`; Turkish dotted İ and i differ.

## Build from source

Use Python 3, an installed .NET SDK with a matching .NET runtime/reference pack, and your own game `Managed` directory matching the audited identity. No game or Unity binaries are distributed here. The scripts perform no dependency installation or NuGet restore. Build tooling has been validated on Linux.

```sh
python3 tools/build.py --msbuild --game-managed-dir "/path/to/RimWorldLinux_Data/Managed"
python3 tools/validate.py --game-managed-dir "/path/to/RimWorldLinux_Data/Managed"
```

The static audit needs an existing `ilspycmd` on PATH or in `~/.dotnet/tools`; set `ILSPYCMD` to its full path if necessary. The build runs standalone C# and Python tooling tests, then produces `1.6/Assemblies/RimDroidReadability.dll` and `dist/RimDroidReadability-0.2.4.zip`. Game references are read-only. Binaries, generated reports and caches are ignored by Git.

## Contributing and license

Bug reports should include mod/game/RimDroid versions, engine, device, UI scale and the exact Mod Settings status or relevant log. Translation corrections are welcome. In the source repository, see [CONTRIBUTING.md](CONTRIBUTING.md), [detailed Turkish usage](docs/user-guide.tr.md) and [device validation records](docs/device-validation.tr.md).

Original mod code, translations and documentation are licensed under [MIT](LICENSE). RimWorld, RimDroid, Unity and Harmony remain governed by their respective owners and licenses. This is an unofficial mod; game files and third-party dependencies are not included.

Gameplay screenshots include RimWorld artwork belonging to its respective owners; that artwork is outside the mod’s MIT license.
