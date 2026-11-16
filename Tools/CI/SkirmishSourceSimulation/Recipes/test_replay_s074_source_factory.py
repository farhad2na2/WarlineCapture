import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


MODULE_PATH = Path(__file__).with_name('project_s074_source_factory.py')
SPEC = importlib.util.spec_from_file_location('s074_source_factory_projection_test', MODULE_PATH)
PROJECTOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROJECTOR)
ROOT = PROJECTOR.ROOT
REPLAY = ROOT / 'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r3'
LEGACY = ROOT / 'T Logs/SkirmishProduction/s074-source-factory-projection-20261107-r4'


class S074SourceFactoryReplayProjectionTests(unittest.TestCase):
    def test_replayed_rows_preserve_legacy_factory_template_equality(self):
        if not (REPLAY / 'projection/receipt.json').is_file() or not (LEGACY / 'receipt.json').is_file():
            self.skipTest('Workspace replay and legacy projection evidence are unavailable')
        for name, key in (('building-definitions.json', 'definitions'),
                          ('unit-production-metadata.json', 'units'),
                          ('configured-metadata.json', 'rows')):
            with self.subTest(artifact=name, content=key):
                replay = json.loads((REPLAY / 'projection' / name).read_bytes())
                legacy = json.loads((LEGACY / name).read_bytes())
                self.assertEqual(replay[key], legacy[key])
        replay_units = json.loads((REPLAY / 'projection/unit-production-metadata.json').read_bytes())
        legacy_units = json.loads((LEGACY / 'unit-production-metadata.json').read_bytes())
        self.assertEqual(replay_units['transports'], legacy_units['transports'])
        for artifact in ('building-definitions.json', 'unit-production-metadata.json', 'configured-metadata.json'):
            replay = json.loads((REPLAY / 'projection' / artifact).read_bytes())
            legacy = json.loads((LEGACY / artifact).read_bytes())
            self.assertEqual(replay['native_origin'], legacy['native_origin'])
            self.assertIs(replay['full_mission_initialized'], False)
            self.assertIs(replay['native_S050_capture'], False)
            self.assertIs(replay['native_visual_readiness'], False)

    def test_legacy_entry_rejects_replayed_context_without_allowlist_membership(self):
        context = REPLAY / 'exporter/s074/source-factory-context.json'
        receipt = REPLAY / 'exporter/factory-context-replay-receipt.json'
        initialization = ROOT / 'T Logs/SkirmishProduction/s026-direct-layout-runtime-inputs-20261107-r1/initialization.json'
        if not context.is_file() or not receipt.is_file():
            self.skipTest('Workspace replay evidence is unavailable')
        with tempfile.TemporaryDirectory(prefix='s074-legacy-guard-', dir=ROOT / 'T Logs') as parent:
            output = Path(parent) / 'projection'
            with self.assertRaisesRegex(ValueError, 'Exact retained S074 factory export and receipt required'):
                PROJECTOR.project(context, receipt, initialization, output)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
