#!/usr/bin/env python3
"""Offline Roslyn build, standalone logic tests and allowlisted release packaging.

Reads the developer's game assemblies; never copies them, loads game code or writes
outside this mod. Run this script directly to ensure CLI cache/temp isolation.
"""
import argparse
import hashlib
import json
import os
import re
from pathlib import Path
import subprocess
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / '.build'
VERSION = '0.2.4'
LANGUAGES = ('Catalan', 'ChineseSimplified', 'ChineseTraditional', 'Czech', 'Danish', 'Dutch', 'English',
             'Estonian', 'Finnish', 'French', 'German', 'Greek', 'Hungarian', 'Italian', 'Japanese', 'Korean',
             'Norwegian', 'Polish', 'Portuguese', 'PortugueseBrazilian', 'Romanian', 'Russian', 'Slovak',
             'Spanish', 'SpanishLatin', 'Swedish', 'Turkish', 'Ukrainian', 'Vietnamese')
LANGUAGE_PAYLOAD = tuple('1.6/Languages/' + language + '/Keyed/Readability.xml' for language in LANGUAGES)
PAYLOAD = ('About/About.xml', 'LoadFolders.xml', '1.6/Assemblies/RimDroidReadability.dll', 'README.md', 'RELEASE.md') + LANGUAGE_PAYLOAD
EXPECTED_HASH = '39c5532c8f9fd8b31cdc9450d3148bc2f372f03f030dc5cddd170c9a08774076'


def environment():
    env = os.environ.copy()
    for key, leaf in [('DOTNET_CLI_HOME', 'dotnet-home'), ('NUGET_PACKAGES', 'nuget'),
                      ('NUGET_HTTP_CACHE_PATH', 'nuget-http'), ('TMPDIR', 'tmp'), ('TMP', 'tmp'),
                      ('TEMP', 'tmp'), ('XDG_CACHE_HOME', 'cache'), ('XDG_CONFIG_HOME', 'config'),
                      ('XDG_DATA_HOME', 'data'), ('XDG_RUNTIME_DIR', 'runtime')]:
        target = WORK / leaf
        target.mkdir(parents=True, exist_ok=True)
        if key == 'XDG_RUNTIME_DIR':
            target.chmod(0o700)
        env[key] = str(target)
    env.update(DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_NOLOGO='1',
               DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='1',
               DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='false',
               PYTHONDONTWRITEBYTECODE='1')
    return env


def run(args, env):
    result = subprocess.run([str(x) for x in args], cwd=ROOT, env=env, text=True,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if result.returncode:
        print(result.stdout, file=sys.stderr)
        raise RuntimeError('Command failed: ' + ' '.join(str(x) for x in args[:3]))
    return result.stdout


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def compiler(env):
    sdk_lines = run(['dotnet', '--list-sdks'], env).strip().splitlines()
    if not sdk_lines:
        raise RuntimeError('Install a .NET SDK before building; this script installs nothing')
    version, location = max((line.split(' ', 1) for line in sdk_lines), key=lambda item: version_key(item[0]))
    sdk = Path(location.strip().strip('[]')) / version
    return sdk / 'Roslyn/bincore/csc.dll', sdk.parent.parent


def version_key(value):
    match = re.fullmatch(r'(\d+)\.(\d+)\.(\d+)(?:-([0-9A-Za-z.-]+))?', value)
    if match is None:
        raise RuntimeError('Unsupported version string: ' + value)
    numbers = tuple(int(match[i]) for i in (1, 2, 3))
    suffix = match[4]
    parts = tuple((0, int(p)) if p.isdigit() else (1, p) for p in (suffix or '').split('.'))
    return numbers + (1 if suffix is None else 0, parts)


def select_test_framework(packs, runtime_versions):
    usable = []
    for refs in packs:
        pack_version = refs.parent.parent.name
        pack_key = version_key(pack_version)
        tfm = re.fullmatch(r'net(\d+)\.(\d+)', refs.name)
        if tfm is None or (int(tfm[1]), int(tfm[2])) != pack_key[:2]:
            continue
        compatible = [v for v in runtime_versions if version_key(v)[:2] == pack_key[:2]
                      and version_key(v) >= pack_key]
        if compatible:
            usable.append((refs, max(compatible, key=version_key)))
    if not usable:
        raise RuntimeError('No installed Microsoft.NETCore.App runtime is compatible with the available reference packs; nothing was installed')
    return max(usable, key=lambda pair: version_key(pair[0].parent.parent.name))


def response_file(path, arguments):
    # csc response file quoting, not shell interpolation.
    path.write_text('\n'.join('"' + str(x).replace('"', '\\"') + '"' for x in arguments) + '\n')


def compile_mod(managed, csc, env):
    required = ['Assembly-CSharp.dll', 'mscorlib.dll', 'System.dll', 'System.Core.dll',
                'UnityEngine.CoreModule.dll', 'UnityEngine.IMGUIModule.dll']
    for name in required:
        if not (managed / name).is_file():
            raise RuntimeError('Missing read-only reference: ' + str(managed / name))
    game = managed / 'Assembly-CSharp.dll'
    if sha(game) != EXPECTED_HASH:
        raise RuntimeError('Game DLL differs from audited 1.6 reference; revalidate this build before compiling')
    references = sorted(managed.glob('*.dll'))
    original = {str(p): (sha(p), p.stat().st_mtime_ns) for p in references}
    output = ROOT / '1.6/Assemblies/RimDroidReadability.dll'
    output.parent.mkdir(parents=True, exist_ok=True)
    args = ['-nologo', '-nostdlib+', '-target:library', '-platform:anycpu',
            '-langversion:7.3', '-optimize+', '-deterministic+', '-debug-',
            '-pathmap:' + str(ROOT) + '=/RimDroidReadability', '-out:' + str(output)]
    args += ['-reference:' + str(p) for p in references]
    args += [str(p) for p in sorted((ROOT / 'Source').glob('*.cs'))]
    rsp = WORK / 'mod-build.rsp'
    response_file(rsp, args)
    result = run(['dotnet', csc, '-noconfig', '@' + str(rsp)], env)
    if result.strip():
        print(result.strip())
    for p in references:
        if original[str(p)] != (sha(p), p.stat().st_mtime_ns):
            raise RuntimeError('Read-only reference changed during build: ' + str(p))
    return output, original


def logic_tests(csc, dotnet_root, env):
    packs = list((dotnet_root / 'packs/Microsoft.NETCore.App.Ref').glob('*/ref/net*'))
    if not packs:
        raise RuntimeError('Installed SDK framework reference pack missing')
    runtime_versions = [line.split()[1] for line in run(['dotnet', '--list-runtimes'], env).splitlines()
                        if line.startswith('Microsoft.NETCore.App ')]
    refs, runtime_version = select_test_framework(packs, runtime_versions)
    tfm = refs.name
    out = WORK / 'LogicTests.dll'
    args = ['-nologo', '-nostdlib+', '-target:exe', '-langversion:7.3',
            '-out:' + str(out)]
    args += ['-reference:' + str(p) for p in sorted(refs.glob('*.dll'))]
    args += [str(ROOT / rel) for rel in ('Source/LabelLayout.cs', 'Source/ReadabilityLogic.cs',
             'Source/TargetCatalog.cs', 'Source/ModText.cs', 'Tests/CatalogTests.cs', 'Tests/LocalizationTests.cs', 'Tests/LogicTests.cs')]
    rsp = WORK / 'logic-tests.rsp'
    response_file(rsp, args)
    run(['dotnet', csc, '-noconfig', '@' + str(rsp)], env)
    (WORK / 'LogicTests.runtimeconfig.json').write_text(json.dumps({
        'runtimeOptions': {'tfm': tfm, 'framework': {'name': 'Microsoft.NETCore.App', 'version': runtime_version},
                           'rollForward': 'Disable'}}))
    return run(['dotnet', out], env).strip()


def package(output):
    release = ROOT / 'dist'
    release.mkdir(exist_ok=True)
    target = release / ('RimDroidReadability-' + VERSION + '.zip')
    paths = PAYLOAD
    with zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED) as archive:
        for rel in paths:
            info = zipfile.ZipInfo('RimDroidReadability/' + rel, (2026, 10, 23, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            archive.writestr(info, (ROOT / rel).read_bytes())
    with zipfile.ZipFile(target) as archive:
        actual = set(archive.namelist())
        expected = {'RimDroidReadability/' + p for p in paths}
        if actual != expected:
            raise RuntimeError('Release allowlist mismatch')
        dlls = [p for p in actual if p.lower().endswith('.dll')]
        if dlls != ['RimDroidReadability/1.6/Assemblies/RimDroidReadability.dll']:
            raise RuntimeError('Unexpected dependency DLL in package')
    return target


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--game-managed-dir', type=Path,
                        default=ROOT.parents[1] / 'RimWorld/game/RimWorldLinux_Data/Managed')
    parser.add_argument('--skip-tests', action='store_true', help='Only build; release validation normally runs tests')
    parser.add_argument('--msbuild', action='store_true', help='Run the csproj wrapper with all writable CLI/runtime paths scoped')
    options = parser.parse_args()
    env = environment()
    if options.msbuild:
        if options.skip_tests:
            parser.error('--skip-tests cannot be combined with --msbuild')
        print(run(['dotnet', 'build', ROOT / 'Source/RimDroidReadability.csproj', '-c', 'Release',
                   '--disable-build-servers', '-p:GameManagedDir=' + str(options.game_managed_dir.resolve())], env), end='')
        return
    csc, dotnet_root = compiler(env)
    output, references = compile_mod(options.game_managed_dir.resolve(), csc, env)
    tests = 'SKIPPED' if options.skip_tests else logic_tests(csc, dotnet_root, env)
    tooling_tests = 'SKIPPED' if options.skip_tests else run([sys.executable, ROOT / 'Tests/ToolingTests.py'], env).strip()
    print(tests)
    print(tooling_tests)
    archive = package(output)
    report = {'status': 'build-passed-tests-skipped' if options.skip_tests else 'build-and-logic-checks-passed', 'game_sha256': EXPECTED_HASH,
              'mod_sha256': sha(output), 'archive_sha256': sha(archive), 'logic_tests': tests,
              'tooling_tests': tooling_tests,
              'reference_count': len(references), 'reference_hashes_and_mtimes_unchanged': True,
              'build_mode': 'offline Roslyn; game/framework DLLs read-only; no Harmony compile reference',
              'device_tests': 'NOT RUN: APK/Native route, loaded DLL and Harmony identities, GUI/input/fog visuals and FPS pending',
              'release': str(archive.relative_to(ROOT))}
    (ROOT / 'docs/validation.json').write_text(json.dumps(report, indent=2) + '\n')
    print('Built:', output.relative_to(ROOT))
    print('Release:', archive.relative_to(ROOT))
    print('Read-only game/framework reference hashes and mtimes unchanged:', len(references))
    print('Phone compatibility and visual/performance checks: NOT RUN')


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print('BUILD FAILED:', error, file=sys.stderr)
        sys.exit(1)
