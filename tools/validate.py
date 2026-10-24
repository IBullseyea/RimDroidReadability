#!/usr/bin/env python3
"""Execution-free release/metadata audit. Only the original logic-test DLL is executed by build.py."""
import argparse
from collections import Counter
import importlib.util
import io
import json
import os
import re
import shutil
from pathlib import Path
import struct
import string
import sys
import xml.etree.ElementTree as ET
import zipfile

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('readability_build', ROOT / 'tools/build.py')
build = importlib.util.module_from_spec(spec)
spec.loader.exec_module(build)
PAYLOAD = build.PAYLOAD

# Public declarations, read in memory only; no game source/IL is written or packaged.
API_SIGNATURES = {
    'Verse.Translator': ['public static string TranslateSimple(this string key)'],
    'Verse.LanguageDatabase': ['public static LoadedLanguage activeLanguage;'],
    'UnityEngine.SystemInfo': ['public static string operatingSystem'],
    'Verse.ThingOverlays': ['public void ThingOverlaysOnGUI()'],
    'RimWorld.MapInterface': ['public void MapInterfaceOnGUI_AfterMainTabs()'],
    'Verse.CameraJumper': ['public static void TryJumpAndSelect(GlobalTargetInfo target, MovementMode mode = MovementMode.Pan)'],
    'Verse.CameraDriver': ['public float RootSize', 'public IntVec3 MapPosition',
                          'public void SetRootSize(float size)', 'public void JumpToCurrentMapLoc(IntVec3 cell)',
                          'public void JumpToCurrentMapLoc(Vector3 loc)', 'private Vector3 CurrentRealPosition'],
    'Verse.Window': ['public virtual Vector2 InitialSize', 'public abstract void DoWindowContents(Rect inRect);',
                     'public bool absorbInputAroundWindow;', 'public bool preventCameraMotion = true;',
                     'public Rect windowRect;'],
    'Verse.WindowStack': ['public void Add(Window window)', 'public bool TryRemove(Window window, bool doCloseSound = true)'],
    'Verse.Widgets': ['public static bool ButtonText(Rect rect, string label, bool drawBackground = true, bool doMouseoverSound = true, bool active = true, TextAnchor? overrideTextAnchor = null)',
                      'public static string TextField(Rect rect, string text)'],
    'Verse.ListerThings': ['public List<Thing> ThingsInGroup(ThingRequestGroup group)'],
    'Verse.GenMapUI': ['public static Vector2 LabelDrawPosFor(Thing thing, float worldOffsetZ)'],
    'Verse.MapPawns': ['public List<Pawn> FreeColonistsSpawned'],
    'Verse.ThingDef': ['public ThingCategory category;', 'public IngestibleProperties ingestible;',
                       'public bool IsMedicine', 'public bool IsWeapon', 'public bool IsApparel', 'public bool CountAsResource'],
    'RimWorld.IngestibleProperties': ['public float CachedNutrition'],
}


def audit_api_declarations(declarations):
    verified = []
    for owner, signatures in API_SIGNATURES.items():
        # Strip expression bodies only. Signature and public access must still match exactly.
        lines = {re.split(r'\s*=>', line.strip())[0] for line in declarations.get(owner, '').splitlines()}
        for signature in signatures:
            require(signature in lines, 'exact public API signature: ' + owner + ' / ' + signature)
            verified.append(owner + ' / ' + signature)
    return verified


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def audit_payload_metadata(payload):
    metadata = ET.fromstring(payload['About/About.xml'])
    require(metadata.tag == 'ModMetaData', 'ModMetaData root')
    require(metadata.findtext('packageId') == 'erenalp.rimdroidreadability', 'mod package identity')
    require(metadata.findtext('name') == 'RimDroid Readability ' + build.VERSION and bool(metadata.findtext('author')), 'mod version/name and author')
    require(payload['README.md'].decode('utf-8').splitlines()[0] == '# RimDroid Readability — ' + build.VERSION, 'README release version')
    require([x.text for x in metadata.findall('./supportedVersions/li')] == ['1.6'], '1.6-only supported versions')
    folders = ET.fromstring(payload['LoadFolders.xml'])
    require(folders.tag == 'loadFolders' and [x.tag for x in folders] == ['v1.6'], '1.6-only load folder version')
    require([x.tag for x in folders[0]] == ['li'] and [x.text for x in folders[0]] == ['1.6'],
            'exact v1.6/li=1.6 routing')
    audit_translations(payload)


def read_keyed(contents, language):
    root = ET.fromstring(contents)
    require(root.tag == 'LanguageData', 'translation LanguageData root: ' + language)
    keys = {}
    for element in root:
        require(element.tag.startswith('RDR_') and element.tag not in keys, 'translation duplicate/invalid key: ' + language)
        require(len(element) == 0 and bool((element.text or '').strip()), 'translation empty or nested value: ' + language)
        keys[element.tag] = element.text
    return keys


def placeholders(value):
    try:
        parsed = list(string.Formatter().parse(value))
    except ValueError as error:
        raise RuntimeError('translation invalid format: ' + str(error)) from error
    fields = []
    for _, field, specifier, conversion in parsed:
        if field is None:
            continue
        require(field.isdecimal() and not specifier and conversion is None, 'translation numeric placeholders only')
        fields.append(field)
    return Counter(fields)


def audit_translations(payload):
    english = read_keyed(payload['1.6/Languages/English/Keyed/Readability.xml'], 'English')
    required = {'RDR_Filter' + value for value in ('Off', 'All', 'Medicine', 'Wood', 'Weapon', 'Food', 'Resource', 'Apparel')}
    for source in (ROOT / 'Source').glob('*.cs'):
        for call in re.findall(r'ModText\.(?:Get|Message|Error)\(([^;\n]*)', source.read_text()):
            required.update('RDR_' + key for key in re.findall(r'"([A-Z][A-Za-z]+)"', call) if key != 'Filter')
    require(required <= set(english), 'translation missing referenced source keys: ' + str(sorted(required - set(english))))
    formats = {key: placeholders(value) for key, value in english.items()}
    for language, path in zip(build.LANGUAGES, build.LANGUAGE_PAYLOAD):
        require(path in payload, 'translation missing language file: ' + language)
        translated = read_keyed(payload[path], language)
        require(set(translated) == set(english), 'translation missing/extra keys: ' + language)
        for key, value in translated.items():
            require(placeholders(value) == formats[key], 'translation placeholder mismatch: ' + language + '/' + key)
    return len(english)


def audit_release(archive_bytes, expected_payload):
    require(set(expected_payload) == set(PAYLOAD), 'source payload allowlist')
    allow = {'RimDroidReadability/' + x for x in PAYLOAD}
    with zipfile.ZipFile(io.BytesIO(archive_bytes)) as packaged:
        entries = packaged.namelist()
        require(len(entries) == len(allow) and set(entries) == allow, 'exact release allowlist without duplicate entries')
        require(packaged.testzip() is None, 'ZIP integrity')
        actual_payload = {}
        for rel in PAYLOAD:
            actual_payload[rel] = packaged.read('RimDroidReadability/' + rel)
            require(actual_payload[rel] == expected_payload[rel], 'stale or changed ZIP payload: ' + rel)
    audit_payload_metadata(actual_payload)


def managed_flags(path):
    data = path.read_bytes()
    pe = struct.unpack_from('<I', data, 0x3c)[0]
    require(data[pe:pe + 4] == b'PE\0\0', 'PE signature')
    machine, sections = struct.unpack_from('<HH', data, pe + 4)
    optional_size = struct.unpack_from('<H', data, pe + 20)[0]
    optional = pe + 24
    magic = struct.unpack_from('<H', data, optional)[0]
    require(magic in (0x10b, 0x20b), 'supported PE optional header')
    directories = optional + (96 if magic == 0x10b else 112)
    cli_rva = struct.unpack_from('<I', data, directories + 14 * 8)[0]
    for i in range(sections):
        start = optional + optional_size + i * 40
        virtual_size, virtual_address, raw_size, raw_pointer = struct.unpack_from('<IIII', data, start + 8)
        if virtual_address <= cli_rva < virtual_address + max(virtual_size, raw_size):
            flags = struct.unpack_from('<I', data, raw_pointer + cli_rva - virtual_address + 16)[0]
            require(flags & 1 and not flags & 2, 'IL-only / not 32-bit-required')
            require(machine == 0x14c, 'AnyCPU PE machine')
            return flags
    raise RuntimeError('CLI header not found')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--game-managed-dir', type=Path,
                        default=ROOT.parents[1] / 'RimWorld/game/RimWorldLinux_Data/Managed')
    options = parser.parse_args()
    env = build.environment()
    ilspy = os.environ.get('ILSPYCMD') or shutil.which('ilspycmd') or str(Path.home() / '.dotnet/tools/ilspycmd')
    if not Path(ilspy).is_file():
        raise RuntimeError('Static audit needs an existing ilspycmd; set ILSPYCMD. This script installs nothing.')

    def table(path, name):
        return json.loads(build.run([ilspy, '--disable-updatecheck', '--dump-table', name, '--json', path], env))['rows']

    own = ROOT / '1.6/Assemblies/RimDroidReadability.dll'
    game = options.game_managed_dir.resolve() / 'Assembly-CSharp.dll'
    before_mtime = game.stat().st_mtime_ns
    before = build.sha(game)
    require(before == build.EXPECTED_HASH, 'reference game SHA')
    flags = managed_flags(own)
    identity = table(own, 'Assembly')[0]
    require(identity['Version'] == build.VERSION + '.0', 'compiled assembly release version')
    refs = table(own, 'AssemblyRef')
    names = {r['Name'] for r in refs}
    require('Assembly-CSharp' in names and 'UnityEngine.CoreModule' in names, 'real game/Unity compile refs')
    require('0Harmony' not in names, 'no Harmony version-bound compile reference')
    require('System.Private.CoreLib' not in names, 'not a modern .NET runtime mod')
    types = table(game, 'TypeDef')
    methods = table(game, 'MethodDef')
    verified = []
    for type_name, method_name in [('Verse.ThingOverlays', 'ThingOverlaysOnGUI'),
                                   ('Verse.ListerThings', 'ThingsOfDef'),
                                   ('Verse.ListerThings', 'ThingsInGroup'),
                                   ('Verse.GenMapUI', 'LabelDrawPosFor'),
                                   ('Verse.MapPawns', 'get_FreeColonistsSpawned')]:
        found = False
        for i, row in enumerate(types):
            if row['Namespace'] + '.' + row['Name'] != type_name:
                continue
            start = int(row['MethodList'])
            end = int(types[i + 1]['MethodList']) if i + 1 < len(types) else len(methods) + 1
            found = any(m['Name'] == method_name and start <= m['RID'] < end for m in methods)
        require(found, type_name + '.' + method_name)
        verified.append(type_name + '.' + method_name)
    declarations = {owner: build.run([ilspy, '--disable-updatecheck', '-t', owner,
                    game.parent / 'UnityEngine.CoreModule.dll' if owner == 'UnityEngine.SystemInfo' else game], env)
                    for owner in API_SIGNATURES}
    verified.extend(audit_api_declarations(declarations))
    archive = ROOT / ('dist/RimDroidReadability-' + build.VERSION + '.zip')
    audit_release(archive.read_bytes(), {rel: (ROOT / rel).read_bytes() for rel in PAYLOAD})
    require(build.sha(game) == before and game.stat().st_mtime_ns == before_mtime, 'game hash/mtime unchanged by metadata audit')
    report = {'status': 'passed', 'checks': ['AnyCPU IL-only framework DLL', 'actual game API metadata present',
              'no 0Harmony assembly reference', 'exact 1.6 metadata/routing', 'all ZIP payload bytes match current sources',
              'exact ZIP allowlist, duplicate rejection and integrity',
              'source game SHA unchanged'], 'api_members': verified,
              'assembly_references': [{'name': x['Name'], 'version': x['Version']} for x in refs],
              'cli_flags': flags, 'game_sha256': before,
              'device_runtime_and_visual_tests': 'NOT RUN'}
    (ROOT / 'docs/static-validation.json').write_text(json.dumps(report, indent=2) + '\n')
    print('PASS: static API/AnyCPU/reference/XML/release integrity audit; game code not executed')


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print('STATIC AUDIT FAILED:', error, file=sys.stderr)
        sys.exit(1)
