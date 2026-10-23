# RimDroid Readability — 0.1.0 prototype

Wide-view medicine/wood markers and colonist names for **RimWorld 1.6 on RimDroid Native Unity**. This is an original, optional game mod. The camera, touch selection and Android application sources are not modified. **Phone compatibility, screenshots/readability and performance have not been tested.**

## Supported reference and startup gates

The prototype defaults **off**. It only installs its isolated Harmony postfix after checking Android Unity, `RIMDROID_IL2MONO_MANAGED`, RimWorld 1.6, the required API signatures, one active game assembly and an existing Harmony 2.x runtime.

The initial prototype deliberately requires the audited game DLL identity:

- Game-reported version: `1.6.4633 rev1254` (GOG package revision metadata is different).
- `Assembly-CSharp.dll` SHA-256: `39c5532c8f9fd8b31cdc9450d3148bc2f372f03f030dc5cddd170c9a08774076`.
- MVID: `c337411a-5cb6-49b6-8f4c-0609acf16d96`.
- Unity reference: `2022.3.35f1`.

A different, unreadable or replaced runtime game DLL disables this overlay with one diagnostic. It is not silently accepted as compatible. A new game build needs API verification and a deliberate identity update/rebuild. Prepatcher, BepInEx, runtime assembly replacement, Box64 and Waylith are outside the first supported test configuration.

RimDroid Native's source pins Harmony **2.4.1**, while the nearby downloaded Harmony mod is **2.4.3**. This build uses a small reflection adapter to the **already loaded** `HarmonyLib.Harmony` / `HarmonyMethod` / `Patch` APIs, following RimDroid's existing bridge pattern. Consequently there is no compile-time Harmony reference and no Harmony DLL in this distribution. Runtime must have exactly one loaded Harmony assembly, major version 2, and a matching patch signature. The actual phone version still needs checking; compatibility with any particular runtime Harmony has not been executed here.

## Build without modifying the game

Requires an installed .NET SDK and Python 3. No game is launched and no packages are downloaded. Framework/Unity/game references remain in the developer's own game directory; no references are copied.

From the workspace root:

```sh
python3 mods/RimDroidReadability/tools/build.py
```

For another **identical audited** game location:

```sh
python3 mods/RimDroidReadability/tools/build.py --game-managed-dir "/absolute/path/to/RimWorldLinux_Data/Managed"
```

The offline builder mirrors the repository's controller-mod Roslyn pattern: `csc.dll` runs with `-nostdlib+`, read-only game framework references, AnyCPU and a .NET Framework 4.7.2 target attribute. This is not a modern .NET runtime mod. SDK/reference versions are compared numerically. Standalone tests select an already installed compatible .NET runtime; missing compatible runtime/reference packs produce an error and are never installed automatically. The script places its temporary files, .NET CLI settings/caches and test artifacts inside this mod's `.build/` directory. It verifies all referenced DLL hashes and modification times before/after compilation. No NuGet restore or SDK installation is performed.

An MSBuild wrapper is also available. Invoke it through the Python launcher so .NET CLI caches **and named-mutex/runtime files** are confined before `dotnet build` starts:

```sh
python3 mods/RimDroidReadability/tools/build.py --msbuild
```

This runs `dotnet build Source/RimDroidReadability.csproj -c Release` with the external `GameManagedDir` property. The SDK may print a workload-verification notice on its first isolated CLI run; no workload is needed or installed. Then audit actual PE/CLI references and game API metadata using an existing `ilspycmd`:

```sh
python3 mods/RimDroidReadability/tools/validate.py
```

Set `ILSPYCMD` to its absolute executable path if needed. Static auditing reads metadata only; it does not execute the game or write proprietary decompiled source/IL.

Outputs: `1.6/Assemblies/RimDroidReadability.dll`, `dist/RimDroidReadability-0.1.0.zip`, `docs/validation.json`, and after the audit, `docs/static-validation.json`. Release packaging is allowlisted: only About XML, LoadFolders XML, this README and the original mod DLL. Source, tests, caches, proprietary DLLs/assets and `0Harmony.dll` are excluded.

## Device installation and controls — not yet performed

1. Confirm the **actual** Android instance is the reference 1.6 build on RimDroid **Native Unity**, and its original DLL SHA/MVID match. Selecting “Native” in the menu alone is insufficient; runtime support checks, extra environment settings and overrides affect loading.
2. Confirm the APK includes the Native module and the loaded game assembly is the expected file; check the existing Harmony identity and exclude assembly-replacing mods for this first test.
3. Import the release ZIP using RimDroid's mod workflow, or place its single `RimDroidReadability` folder in that instance's `Mods` directory. Enable it for that instance. **Do not copy a second Harmony or game DLL.** Restart the game normally.
4. Open Options → Mod Settings → **RimDroid Readability**. Read the startup status, then enable “İşaretleri aç”. “Kolonistleri göster” toggles colonists. The item button cycles **Kapalı → İlaç → Odun**. Font button cycles **18 → 22 → 26** GUI units (UI scale still applies).
5. To disable, clear the master checkbox or disable this mod in the instance's mod list. Saves, camera settings and item definitions are not changed by the mod.

## Drawing behavior and limits

- Registry queries use the current map's medicine group, `WoodLog` definition and `FreeColonistsSpawned`. Mental-state colonists are not intentionally excluded. This list has the game's own free-colonist/slave/subhuman semantics; prisoner markers are not added.
- The GUI postfix runs on repaint only in map view. Destroyed, unspawned, different-map, fogged and out-of-view objects are rejected **before** projecting their position. A loaded/cached candidate is never sufficient to bypass visibility.
- Candidate membership refreshes every 250 ms. The registry lists are scanned, but only currently visible, spawned, same-map, non-fogged candidates with a fully drawable GUI marker are retained; clipped edge objects cannot consume the candidate budget. Storage is bounded to 120, ordered by colonist priority and object ID. Visibility is checked again during drawing and positions/stack counts remain live. **Panning, revealing fog or adding a new object can delay its first marker by up to 250 ms.** Switching map or settings clears the cache.
- Labels have their own cached GUI style and dark background, independent of camera zoom. Markers are collected and drawn first; labels are placed afterward, avoiding every visible marker rectangle and previously placed label. Four nearby placements are attempted; an unfittable label is omitted while its small category square may remain. At most **40 labels / 120 markers** are drawn; text allocation/measurement stops once the 40-label cap is reached. Dense depots intentionally do not show every label; the current prototype does not group stacks or enlarge selection hitboxes.
- Labels are not interactive. Existing game UI can cover them. GUI color is restored after drawing, including errors. A rendering exception disables only this overlay and reports once. Assembly replacement notifications set a sticky volatile gate without touching rendering state on the loader thread; initialization cannot overwrite that gate, and the main-thread postfix handles it once before any drawing.

## Validation and remaining device acceptance

Automated local checks cover finite/edge/dense placement, pairwise label/marker exclusion, viewport containment, label/candidate/marker budgets, stable priority, all visibility input combinations, runtime/version gates and sticky assembly-gate interleavings. They test the layout reset and bounded-candidate helpers, **not actual game map/settings cache invalidation**. Tooling tests cover numeric version selection, missing runtime failure and negative ZIP/XML cases, including rejection under `python -O` / `PYTHONOPTIMIZE`. The standalone test assembly contains only original logic and does not reference or load game/Unity assemblies. Static checks inspect the output's AnyCPU flags, real game API metadata, exact XML routing, dependency references and all ZIP payload bytes against current sources. These checks **do not exercise Harmony patching or Unity GUI**.

Before claiming device compatibility, record:

- Actual APK build, Native route, instance full version, original DLL SHA/MVID, loaded assembly path and Harmony version. A matching PC DLL is not proof of matching Android files.
- Successful patch initialization and working toggles at all three font sizes; negative initialization on unsupported/mismatched inputs remains a device/runtime test too.
- Medicine/wood identification and amounts, moving/mental-state colonists, fog boundaries, offscreen edges, dense depots, despawns and map switches.
- Settings persist and a disabled master draws nothing. Existing selection/long-press controls continue normally.
- Repeat the same camera/save/speed with overlay off/on; record frame timing/FPS and allocation behavior. Phone readability, latency and performance are **pending**, not passed.

No device deployment or game launch is part of this repository build.
