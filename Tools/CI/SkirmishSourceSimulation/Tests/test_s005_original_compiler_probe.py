"""Controls for the retained S005 source-graph admission boundary."""
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


RECIPE = Path(__file__).resolve().parents[1] / 'Recipes/prepare_s005_original_compiler_probe.py'
spec = importlib.util.spec_from_file_location('s005_original_compiler_probe', RECIPE)
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class S005OriginalCompilerProbeTests(unittest.TestCase):
    def test_retained_graph_prepares_without_claiming_a_world(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'probe'
            receipt = probe.prepare(output)
            graph = json.loads((output / 'source-config-graph.json').read_text())
            self.assertEqual(receipt['status'], 'Prepared')
            self.assertEqual(graph['definition_path'], probe.SCENARIO)
            self.assertEqual(graph['derived_source_graph']['parent_sha256'], probe.GRAPH_SHA256)
            self.assertFalse(receipt['compiled_setup'])
            self.assertFalse(receipt['world_initialized'])
            self.assertFalse(receipt['mission_ready'])

    def test_modified_graph_is_rejected_before_creating_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            graph = json.loads(probe.GRAPH.read_text())
            graph['nodes'][0]['source_type'] = 'tampered'
            altered = root / 'altered.json'
            altered.write_text(json.dumps(graph))
            output = root / 'probe'
            with patch.object(probe, 'GRAPH', altered):
                with self.assertRaisesRegex(ValueError, 'Retained parent graph identity changed'):
                    probe.prepare(output)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
