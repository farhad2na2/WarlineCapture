"""Planning-generator portability regressions; no Unity or generated file writes."""
import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location('generate_handoff', Path(__file__).with_name('generate_handoff.py'))
generator = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(generator)


class PlanningPortabilityTests(unittest.TestCase):
    def test_lf_and_crlf_have_same_planning_inventory_values(self):
        lf = '---\n  displayName: Rifleman · Persian فارسی\n  health: 100\n'.encode('utf-8')
        expected = ('Rifleman · Persian فارسی', hashlib.sha256(lf).hexdigest())
        self.assertEqual(expected, generator.planning_source_inventory_values(lf))
        self.assertEqual(expected, generator.planning_source_inventory_values(lf.replace(b'\n', b'\r\n')))

    def test_meaningful_source_change_keeps_hash_drift(self):
        source = b'  displayName: Rifleman\n  health: 100\n'
        changed = source.replace(b'100', b'101')
        self.assertNotEqual(generator.planning_source_inventory_values(source)[1],
                            generator.planning_source_inventory_values(changed)[1])

    def test_utf8_output_comparison_and_real_text_drift(self):
        with tempfile.TemporaryDirectory(prefix='planning-portability-', dir=Path(__file__).resolve().parent) as folder:
            path = Path(folder) / 'packet.md'
            text = 'Mission · فارسی — source → objective\n'
            path.write_text(text, encoding='utf-8')
            original = Path.read_text
            encodings = []

            def read_with_recording(p, *args, **kwargs):
                encodings.append(kwargs.get('encoding'))
                return original(p, *args, **kwargs)

            with patch.object(Path, 'read_text', read_with_recording):
                self.assertTrue(generator.planning_output_matches(path, text))
                self.assertFalse(generator.planning_output_matches(path, text.replace('Mission', 'Changed')))
            self.assertEqual(['utf-8', 'utf-8'], encodings)
            self.assertFalse(generator.planning_output_matches(path.with_name('missing.md'), text))

    def test_inventory_paths_use_portable_separators(self):
        with tempfile.TemporaryDirectory(prefix='planning-portability-', dir=Path(__file__).resolve().parent) as folder:
            root = Path(folder)
            configs = root / 'Assets' / 'Game' / 'Configs' / 'Prefabs'
            configs.mkdir(parents=True)
            for prefix, count in [('Prefab_UnitGrid', 51), ('Prefab_BuildingDefinition', 24)]:
                for index in range(count):
                    (configs / f'{prefix}_Fixture{index}_Config.asset').write_bytes(b'  displayName: Fixture\r\n')
            with patch.object(generator, 'REPO', root):
                rows = generator.roster_audit()
            self.assertEqual(75, len(rows))
            self.assertTrue(all(row['source_config'].startswith('Assets/Game/Configs/Prefabs/') for row in rows))
            self.assertTrue(all('\\' not in row['source_config'] for row in rows))
            self.assertTrue(all(row['observed_display_name'] == 'Fixture' for row in rows))


if __name__ == '__main__':
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(PlanningPortabilityTests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    if result.wasSuccessful():
        print('[SkirmishPlanningPortabilityTests] result=Passed tests=4')
    raise SystemExit(0 if result.wasSuccessful() else 1)
