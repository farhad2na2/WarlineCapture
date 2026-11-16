import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).with_name('project_s003_derived_consumers.py')
spec = importlib.util.spec_from_file_location('s003_derived_consumers', SCRIPT)
consumer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(consumer)


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class DerivedConsumerProjectionTests(unittest.TestCase):
    def setUp(self):
        self.temp = Path(tempfile.mkdtemp(prefix='s003-derived-consumers-')).resolve()
        self.logs = self.temp / 'T Logs'
        self.logs.mkdir()
        self.capture_sha = 'b614fe9c80d1c975115b9952454a68d230e0420504c2970ef5ba9fa7dfe39aa4'
        self.capture_path = self.temp / 'capture.json'
        rows = [
            {'index': 0, 'prefab': {'key': 'Building_A'}, 'fields': {'cost': 10}},
            {'index': 1, 'prefab': {'key': 'Building_B'}, 'fields': {'cost': 20}},
        ]
        self.native = {
            'providers': {'baseline': [], 'scene': [], 'units': []},
            'recipes': [], 'delivery_seconds': [], 'units': [], 'transports': [],
            'buildings': rows,
        }
        self.capture = {'kind': 'NativeS003OriginalCompilerFactoryMetadataCapture',
                        'context': {'metadata': self.native}, 'source_pins': {}}
        self.capture_path.write_text(json.dumps(self.capture))
        # The consumer intentionally binds the real capture's pinned digest.
        self.capture_path.write_bytes(b'capture fixture')

        self.metadata = json.loads(json.dumps(self.native))
        self.metadata['buildings'] = [dict(json.loads(json.dumps(self.native['buildings'][1])), index=0),
                                      dict(json.loads(json.dumps(self.native['buildings'][0])), index=1)]
        self.context_path = self.temp / 'context.json'
        self.prior_path = self.temp / 'prior.json'
        self.context = {
            'schema_version': 2,
            'kind': 'OriginalS003DerivedNativeFactoryContext',
            'authority': 'ActualCompilerContext_HistoricalS003FactoryFacts_SourceOnly',
            'native_factory_reference': {'path': str(self.capture_path), 'sha256': self.capture_sha},
            'target_context': {'catalog_id': 'S075', 'seed': 104804, 'setup_hash': 1280564676},
            'native_origin': {'catalog_id': 'S003', 'size': 'Standard', 'difficulty': 'Regular',
                              'seed': 104732, 'setup_hash': 2394754492,
                              'metadata_key_sha256': '0' * 64},
            'derived_metadata': self.metadata,
            'consumed_input_pins': {},
            'world_allocated': False, 'runtime_loader_admitted': False,
            'source_fit_certified': False, 'mission_ready': False,
            'identities_allocated': 0,
        }
        self.context_path.write_text(json.dumps(self.context))
        self.prior_path.write_text(json.dumps(self.metadata))
        self.input_path = self.temp / 'pinned-input'
        self.input_path.write_text('pin')
        self.context['consumed_input_pins'] = {str(self.input_path): digest(self.input_path)}
        self.context_path.write_text(json.dumps(self.context))
        self.prior_path.write_text(json.dumps(self.metadata))

        self.evidence = self.temp / 'evidence.txt'
        self.evidence.write_text('evidence')
        self.baseline_path = self.temp / 'baseline.py'
        self.baseline_path.write_text('')
        self.receipt_path = self.temp / 'source-receipt.json'
        descriptor = lambda path: {'path': str(path), 'sha256': digest(path)}
        self.receipt = {
            'kind': 'S003FactoryExporterSourceControl',
            'status': 'PassedSourceOnlyCompilerContext',
            'world_allocated': False, 'native_s075_capture': False,
            'runtime_loader_admitted': False, 'source_fit_certified': False,
            'mission_ready': False, 'origin_catalog_id': 'S003',
            'target': self.context['target_context'],
            'source_context': descriptor(self.context_path),
            'origin_capture': {'path': str(self.capture_path), 'sha256': self.capture_sha},
            'previous_projection': descriptor(self.prior_path),
        }
        for name in ('exporter_source', 'compile_response', 'compile_log', 'run_log',
                     'baseline_context', 'previous_baseline_context', 'baseline_run_log'):
            self.receipt[name] = descriptor(self.evidence)
        self.receipt_path.write_text(json.dumps(self.receipt))

        # Supply the baseline payload builder without importing or running the full pipeline.
        self.baseline_path.write_text('''
def read_factory_sources(root): return []
def _project_payload_checked(capture, sha, pins, sources):
    return {
      "building-definitions.json": {"definitions": [
        {"key": "Building_A"}, {"key": "Building_B"}]},
      "unit-production-metadata.json": {"units": []},
      "configured-metadata.json": {"rows": [
        {"key": "Building_A"}, {"key": "Building_B"}]}}
''')

    def tearDown(self):
        shutil.rmtree(self.temp)

    def run_project(self, name):
        out = self.logs / name
        original_read_pin = consumer.read_pin
        original_sha = consumer.sha
        def read_pin(descriptor):
            if descriptor['path'] == str(self.capture_path):
                return self.capture_path, self.capture
            return original_read_pin(descriptor)
        def sha(path):
            if Path(path) == self.capture_path:
                return self.capture_sha
            return original_sha(path)
        with patch.object(consumer, 'ROOT', self.temp), patch.object(consumer, 'BASELINE', self.baseline_path), patch.object(consumer, 'CAPTURE_SHA', self.capture_sha), patch.object(consumer, 'read_pin', read_pin), patch.object(consumer, 'sha', sha):
            return consumer.project(self.receipt_path, out), out

    def test_reorders_complete_building_rows_and_configured_indices(self):
        report, out = self.run_project('projection')
        definitions = json.loads((out / 'building-definitions.json').read_text())
        configured = json.loads((out / 'configured-metadata.json').read_text())
        self.assertEqual([row['key'] for row in definitions['definitions']],
                         ['Building_B', 'Building_A'])
        self.assertEqual([(row['key'], row['definition_index']) for row in configured['rows']],
                         [('Building_B', 0), ('Building_A', 1)])
        self.assertFalse(report['world_allocated'])
        self.assertFalse(report['runtime_admission'])
        self.assertFalse(report['mission_ready'])

    def test_rejects_mutated_projected_building_fact(self):
        self.metadata['buildings'][0]['fields']['cost'] = 999
        self.context['derived_metadata'] = self.metadata
        self.context_path.write_text(json.dumps(self.context))
        self.prior_path.write_text(json.dumps(self.metadata))
        self.receipt['source_context'] = {'path': str(self.context_path), 'sha256': digest(self.context_path)}
        self.receipt['previous_projection'] = {'path': str(self.prior_path), 'sha256': digest(self.prior_path)}
        self.receipt_path.write_text(json.dumps(self.receipt))
        with self.assertRaisesRegex(ValueError, 'Projected building differs'):
            self.run_project('mutated')


if __name__ == '__main__':
    unittest.main()
