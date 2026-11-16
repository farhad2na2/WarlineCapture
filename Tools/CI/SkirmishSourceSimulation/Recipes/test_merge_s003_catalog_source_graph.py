import copy
import hashlib
import tempfile
import unittest
from pathlib import Path

from merge_s003_catalog_source_graph import merge


class GraphMergeTests(unittest.TestCase):
    def test_conflicting_and_stale_inputs_fail_closed(self):
        with tempfile.TemporaryDirectory() as directory:
            repo = Path(directory)
            source = repo / "asset"
            source.write_bytes(b"source")
            graph = {"authority": "AuthoredConfigAssets", "compiled_setup": False,
                     "world_initialized": False, "native_world_state_used": False,
                     "catalog_path": "catalog", "source_pins": {"asset": hashlib.sha256(b"source").hexdigest()},
                     "nodes": [{"source_path": "asset", "fields": {"x": 1}}]}
            self.assertEqual(1, len(merge(graph, copy.deepcopy(graph), repo)["nodes"]))
            conflict = copy.deepcopy(graph)
            conflict["nodes"][0]["fields"]["x"] = 2
            with self.assertRaises(ValueError):
                merge(graph, conflict, repo)
            conflict = copy.deepcopy(graph)
            conflict["source_pins"]["asset"] = "0" * 64
            with self.assertRaises(ValueError):
                merge(graph, conflict, repo)
            source.write_bytes(b"changed")
            with self.assertRaises(ValueError):
                merge(graph, graph, repo)


if __name__ == "__main__":
    unittest.main()
