#!/usr/bin/env python3
"""Pure tooling/negative artifact checks, including an optimized-Python audit child.
No game code is loaded and fixtures are assembled only in memory.
"""
import importlib.util
import io
import os
from pathlib import Path
import subprocess
import sys
import unittest
import zipfile

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[1]


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


build = load('readability_build_tests', ROOT / 'tools/build.py')
audit = load('readability_audit_tests', ROOT / 'tools/validate.py')


def archive(payload, extra=None):
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, 'w') as result:
        for rel, contents in payload.items():
            result.writestr('RimDroidReadability/' + rel, contents)
        if extra:
            result.writestr(extra, b'unexpected')
    return buffer.getvalue()


class ToolingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.payload = {rel: (ROOT / rel).read_bytes() for rel in audit.PAYLOAD}

    def test_numeric_versions(self):
        self.assertGreater(build.version_key('10.0.113'), build.version_key('9.0.999'))
        self.assertGreater(build.version_key('10.0.10'), build.version_key('10.0.9'))
        self.assertGreater(build.version_key('10.0.1'), build.version_key('10.0.1-preview.12'))
        self.assertGreater(build.version_key('10.0.1-preview.12'), build.version_key('10.0.1-preview.9'))

    def test_compatible_installed_runtime_selection(self):
        packs = [Path('/fake/9.0.10/ref/net9.0'), Path('/fake/10.0.1/ref/net10.0')]
        refs, runtime = build.select_test_framework(packs, ['9.0.11', '10.0.2'])
        self.assertEqual(refs, packs[1])
        self.assertEqual(runtime, '10.0.2')
        refs, runtime = build.select_test_framework(packs, ['9.0.11'])
        self.assertEqual(refs, packs[0])
        self.assertEqual(runtime, '9.0.11')

    def test_missing_compatible_runtime_fails_without_installing(self):
        with self.assertRaisesRegex(RuntimeError, 'No installed'):
            build.select_test_framework([Path('/fake/10.0.5/ref/net10.0')], ['9.0.11', '10.0.4'])

    def test_current_payload_passes(self):
        audit.audit_release(archive(self.payload), self.payload)

    def test_every_stale_payload_file_is_rejected(self):
        for rel in audit.PAYLOAD:
            with self.subTest(rel=rel):
                changed = dict(self.payload)
                changed[rel] += b'changed'
                with self.assertRaisesRegex(RuntimeError, 'stale or changed'):
                    audit.audit_release(archive(changed), self.payload)

    def test_wrong_routing_rejected_even_when_source_and_archive_match(self):
        changed = dict(self.payload)
        changed['LoadFolders.xml'] = b'<loadFolders><v1.6><li>1.7</li></v1.6></loadFolders>'
        with self.assertRaisesRegex(RuntimeError, 'routing'):
            audit.audit_release(archive(changed), changed)

    def test_wrong_identity_rejected(self):
        changed = dict(self.payload)
        changed['About/About.xml'] = changed['About/About.xml'].replace(b'erenalp.rimdroidreadability', b'other.identity')
        with self.assertRaisesRegex(RuntimeError, 'identity'):
            audit.audit_release(archive(changed), changed)

    def test_extra_dependency_rejected(self):
        with self.assertRaisesRegex(RuntimeError, 'allowlist'):
            audit.audit_release(archive(self.payload, 'RimDroidReadability/1.6/Assemblies/0Harmony.dll'), self.payload)

    def test_optimized_python_still_rejects_bad_release(self):
        child = '''from pathlib import Path
import io, zipfile
from tools.validate import PAYLOAD, audit_release
payload = {rel: Path(rel).read_bytes() for rel in PAYLOAD}
payload['LoadFolders.xml'] = b'<loadFolders><v1.6><li>1.7</li></v1.6></loadFolders>'
buffer = io.BytesIO()
with zipfile.ZipFile(buffer, 'w') as archive:
    for rel, data in payload.items(): archive.writestr('RimDroidReadability/' + rel, data)
audit_release(buffer.getvalue(), payload)
'''
        env = build.environment()
        env['PYTHONOPTIMIZE'] = '1'
        result = subprocess.run([sys.executable, '-O', '-c', child], cwd=ROOT, env=env,
                                text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('exact v1.6/li=1.6 routing', result.stdout)


if __name__ == '__main__':
    result = unittest.TextTestRunner(stream=sys.stdout, verbosity=1).run(
        unittest.defaultTestLoader.loadTestsFromTestCase(ToolingTests))
    if not result.wasSuccessful():
        sys.exit(1)
    print('PASS: numeric toolchain selection and negative release audits, including python -O / PYTHONOPTIMIZE')
