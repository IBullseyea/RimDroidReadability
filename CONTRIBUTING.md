# Contributing

Bug reports and translation corrections are welcome in English or Turkish. Use the repository's Issues templates and include enough detail to reproduce the problem. Attach the relevant Mod Settings status or mod log; omit unrelated personal information and do not upload game DLLs, saves or APKs by default.

## Development

Build and validate with the commands in [README.md](README.md), providing your own audited game references via `--game-managed-dir`. Scripts use the installed .NET SDK and Python, without downloads or NuGet restore. The static audit additionally needs an installed `ilspycmd`.

The build runs standalone C# tests and Python tooling tests before packaging. Actual game/API identities and every packaged file are checked separately by `tools/validate.py`. Run `git diff --check` before submitting a pull request. Local tests do not replace phone tests; describe what device testing you performed.

Preserve saved setting keys, filter values, Native Android detection and exact DLL/API/Harmony checks. Keep game, Unity and Harmony binaries, decompiled third-party code, build caches and local reports out of commits. Release ZIPs contain only the explicit allowlist in `tools/build.py`.

## Translations

Edit `1.6/Languages/<language>/Keyed/Readability.xml`. English is the reference. Preserve XML keys and numbered placeholders such as `{0}` and `{1}`; placeholders may change order, but must retain the same counts. Do not translate colonist names or technical identifiers. Use terminology natural to the corresponding game language.

For a new language, add it to `LANGUAGES` in `tools/build.py` and provide all English keys. Check long button labels, non-Latin fonts and language changes on a device where possible; include screenshots with corrections when useful.

## License

Contributions are provided under this project's [MIT license](LICENSE). Submit original material or material you have permission to contribute. Third-party game content is outside this license.
