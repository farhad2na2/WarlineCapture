import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
NATIVE = Path('/private/tmp/s002-two-seed-current-20261102-r2/two-seed-metadata.json')
PRIOR = ROOT / 'Logs/private-candidates/s005-planner-slice/evidence/s002-current-native-capture/two-seed-metadata.json'
BRIDGE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceMetadataCompatibility.cs'
SPEC = importlib.util.spec_from_file_location('native_projection', Path(__file__).with_name('project_native_qualified_metadata.py'))
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class S002NativeProjectionTests(unittest.TestCase):
    def test_exact_two_seed_projection_preserves_capture_values_and_scope(self):
        if not NATIVE.is_file() or not PRIOR.is_file():
            self.skipTest('retained native evidence is unavailable')
        with tempfile.TemporaryDirectory(prefix='s002-native-projection-') as temp:
            output = Path(temp) / 'projection'
            result = MODULE.project(NATIVE, BRIDGE, output, PRIOR)
            self.assertEqual(result['native_artifact_sha256'], hashlib.sha256(NATIVE.read_bytes()).hexdigest())
            self.assertEqual(result['prior_native_artifact_sha256'], hashlib.sha256(PRIOR.read_bytes()).hexdigest())
            self.assertTrue(result['metadata_semantics_equal_to_prior'])
            self.assertEqual([x['selection']['seed'] for x in result['contexts']], [104731, 1])
            native = json.loads(NATIVE.read_text())
            for item, context in zip(result['contexts'], native['contexts']):
                for field, filename in [('building_definitions', 'building-definitions.json'), ('unit_production_metadata', 'unit-production-metadata.json'), ('building_request_metadata', 'configured-metadata.json')]:
                    projected = json.loads(Path(item[field]['path']).read_text())
                    self.assertEqual(projected['native_origin']['artifact_sha256'], result['native_artifact_sha256'])
                    self.assertFalse(projected['full_mission_initialized'])
                    self.assertFalse(projected['native_visual_readiness'])
                    if field == 'building_definitions':
                        expected = [{'key': row['prefab']['key'], 'prefab': row['prefab'], 'fields': row['fields']} for row in context['metadata']['buildings']]
                        self.assertEqual(projected['definitions'], expected)
                    elif field == 'unit_production_metadata':
                        self.assertEqual(projected['transports'], context['metadata']['transports'])
                        self.assertEqual([row['key'] for row in projected['units']], [row['prefab']['key'] for row in context['metadata']['units']])
                    else:
                        self.assertEqual(len(projected['rows']), len(context['metadata']['buildings']))
            self.assertEqual(json.loads((output / 'projection-index.json').read_text())['runtime_admission'], False)

    def test_projection_requires_prior_capture_comparison(self):
        with tempfile.TemporaryDirectory(prefix='s002-native-projection-') as temp:
            with self.assertRaisesRegex(ValueError, 'Prior S002 native capture required'):
                MODULE.project(NATIVE, BRIDGE, Path(temp) / 'projection')


if __name__ == '__main__':
    unittest.main()
