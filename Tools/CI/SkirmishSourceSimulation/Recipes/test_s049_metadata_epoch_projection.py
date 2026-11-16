import hashlib
import importlib.util
import sys
import json
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
RECEIPT = ROOT / 'Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Native-11/s049-metadata.json'
PRIOR = ROOT / 'Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/direct-metadata-projection-r3/s049-direct-metadata.json'
DRIVER_SPEC = importlib.util.spec_from_file_location('native_qualified_metadata', Path(__file__).with_name('project_native_qualified_metadata.py'))
DRIVER = importlib.util.module_from_spec(DRIVER_SPEC)
DRIVER_SPEC.loader.exec_module(DRIVER)


def load_source_profiles():
    if 'skirmish_source_profiles' in sys.modules:
        return sys.modules['skirmish_source_profiles']
    owned_path = ROOT / 'Tools/CI/skirmish_owned_source.py'
    owned_spec = importlib.util.spec_from_file_location('skirmish_owned_source', owned_path)
    owned = importlib.util.module_from_spec(owned_spec)
    owned.__executed_source_pin__ = {'path': str(owned_path.resolve()), 'sha256': hashlib.sha256(owned_path.read_bytes()).hexdigest()}
    sys.modules['skirmish_owned_source'] = owned
    owned_spec.loader.exec_module(owned)
    owned.load_cli(ROOT / 'Tools/CI')
    return sys.modules['skirmish_source_profiles']


class S049MetadataEpochProjectionTests(unittest.TestCase):
    def test_adds_native11_epoch_and_preserves_prior_metadata_epoch(self):
        receipt_raw = RECEIPT.read_bytes()
        prior_raw = PRIOR.read_bytes()
        receipt = json.loads(receipt_raw)
        prior = json.loads(prior_raw)
        with tempfile.TemporaryDirectory(prefix='s049-metadata-epoch-') as temp:
            output = Path(temp) / 'Native-11'
            index = DRIVER.project(RECEIPT, None, output)
            epoch = json.loads((output / 'metadata-epoch.json').read_text())
            epoch_context = json.loads((output / 'metadata-epoch-context.json').read_text())
            qualification = json.loads((output / 'configuration-qualification.json').read_text())
            initialization = json.loads((output / 'initialization.json').read_text())
            bundle = json.loads((output / 'data-bundle.json').read_text())
            fixed_map = json.loads((output / 'fixed-map-context.json').read_text())
            self.assertEqual(index['kind'], 'NativeS049MetadataOnlyProjectionIndex')
            self.assertEqual(index['native_artifact_sha256'], hashlib.sha256(receipt_raw).hexdigest())
            self.assertEqual(epoch['prior_epoch']['sha256'], hashlib.sha256(prior_raw).hexdigest())
            self.assertEqual(epoch['projection_id'], 'Projection-16')
            self.assertEqual(epoch['source_lineage']['initialization_sha256'], '662d00b896e5a1b9cca36f420ff1e60b9b1de6525a76f55f633b624c8bcdd5d9')
            self.assertEqual(epoch['source_lineage']['data_bundle_sha256'], 'ded092c82bfc8db209dbc33dfc50e8a23606eaabfaa633a759dcd29c6fd39224')
            self.assertEqual(epoch_context['kind'], 'OriginalS049NativeMetadataEpochContext')
            self.assertEqual(epoch_context['prior_compiler_context']['sha256'], '4e0599e7f0ac194e5d175fbbde567dd4cc4c70be006f90ed7a21988d5af9a045')
            self.assertFalse(epoch_context['world_allocated'])
            self.assertFalse(epoch_context['source_fit_certified'])
            self.assertFalse(epoch_context['mission_ready'])
            self.assertFalse(epoch_context['runtime_loader_admitted'])
            self.assertEqual(qualification['compiler_context']['sha256'], hashlib.sha256((output / 'metadata-epoch-context.json').read_bytes()).hexdigest())
            self.assertFalse(qualification['native_context_qualified'])
            self.assertFalse(qualification['mission_ready'])
            self.assertEqual(initialization['metadata_compatibility']['sha256'], hashlib.sha256(receipt_raw).hexdigest())
            self.assertEqual(initialization['metadata_compiler_context'], qualification['compiler_context'])
            self.assertEqual(initialization['source_epoch'], 'OriginalCompilerDerivedNativeFactoryContext/v1')
            self.assertEqual(initialization['world_profile'], {'identity_backend':'FreshLogicalEntitiesV1','system_initialization':'OriginalOnCreate','future_native_inputs':False})
            self.assertEqual(initialization['configuration_qualification']['sha256'], hashlib.sha256((output / 'configuration-qualification.json').read_bytes()).hexdigest())
            self.assertEqual(fixed_map['metadata_compiler_context'], qualification['compiler_context'])
            self.assertEqual(fixed_map['configuration_qualification'], initialization['configuration_qualification'])
            self.assertFalse(fixed_map['native_mission_context_qualified'])
            self.assertFalse(fixed_map['runtime_admitted'])
            self.assertEqual(initialization['navigation_reference']['sha256'], hashlib.sha256((output / 'fixed-map-context.json').read_bytes()).hexdigest())
            self.assertEqual(bundle['initialization']['sha256'], hashlib.sha256((output / 'initialization.json').read_bytes()).hexdigest())
            for path, digest in epoch['projector_inputs'].items():
                self.assertEqual(hashlib.sha256(Path(path).read_bytes()).hexdigest(), digest)
            self.assertEqual(epoch['source_pin_count'], 8681)
            self.assertTrue(index['metadata_semantics_equal_to_prior'])
            self.assertEqual(receipt['context']['metadata'], prior['context']['metadata'])
            self.assertEqual(receipt['context']['metadata_key_sha256'], prior['context']['metadata_key_sha256'])
            self.assertEqual(receipt['context']['selected_native_domain'], prior['context']['selected_native_domain'])
            for name in ('building-definitions.json', 'unit-production-metadata.json', 'configured-metadata.json'):
                projected = json.loads((output / name).read_text())
                self.assertEqual(projected['native_origin']['artifact_sha256'], hashlib.sha256(receipt_raw).hexdigest())
                self.assertEqual(projected['native_origin']['previous_projection_sha256'], hashlib.sha256(prior_raw).hexdigest())
                self.assertFalse(projected['full_mission_initialized'])
                self.assertFalse(projected['native_visual_readiness'])
            self.assertEqual(index['metadata_epoch']['sha256'], hashlib.sha256((output / 'metadata-epoch.json').read_bytes()).hexdigest())
            self.assertFalse(index['runtime_admission'])
            self.assertFalse(index['mission_ready'])
            self.assertFalse(index['physical_map_created_or_changed'])
        self.assertEqual(RECEIPT.read_bytes(), receipt_raw)
        self.assertEqual(PRIOR.read_bytes(), prior_raw)

    def test_rejects_a_different_receipt_epoch(self):
        raw = RECEIPT.read_bytes() + b' '
        with tempfile.TemporaryDirectory(prefix='s049-metadata-epoch-invalid-') as temp:
            bad = Path(temp) / 'receipt.json'
            bad.write_bytes(raw)
            with self.assertRaisesRegex(ValueError, 'Exact passed native artifact required'):
                DRIVER.project(bad, None, Path(temp) / 'output')

    def test_runtime_context_rejects_target_and_readiness_tampering(self):
        context_path = ROOT / 'Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-16/metadata-epoch-context.json'
        context = json.loads(context_path.read_text())
        profiles = load_source_profiles()
        profiles._s049_metadata_epoch_target(context)
        changed_target = json.loads(json.dumps(context))
        changed_target['target_context']['seed'] = 104735
        with self.assertRaisesRegex(ValueError, 'compiler target differs'):
            profiles._s049_metadata_epoch_target(changed_target)
        changed_flag = json.loads(json.dumps(context))
        changed_flag['mission_ready'] = True
        with self.assertRaisesRegex(ValueError, 'context required'):
            profiles._s049_metadata_epoch_target(changed_flag)


if __name__ == '__main__':
    unittest.main()
