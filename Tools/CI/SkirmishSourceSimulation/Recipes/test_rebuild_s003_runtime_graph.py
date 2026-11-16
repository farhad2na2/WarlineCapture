import copy
import json
import unittest
from pathlib import Path

from rebuild_s003_runtime_graph import rebuild


class RebuildS003GraphTests(unittest.TestCase):
    def test_current_closure_and_changed_nodes_fail_closed(self):
        root = Path(__file__).resolve().parents[4]
        fresh = json.loads((root / "T Logs/SkirmishProduction/s003-catalog-closure-refresh-20261104-T1/source-config-graph.json").read_bytes())
        old = json.loads((root / "T Logs/SkirmishProduction/s003-final-runtime-source-graph-20261103-T1/source-config-graph-with-grid-surface-nodes.json").read_bytes())
        result = rebuild(root, fresh, old)
        self.assertEqual(37, len(result["nodes"]))
        self.assertEqual(147, len(result["source_pins"]))
        changed = copy.deepcopy(old)
        changed["nodes"].pop()
        with self.assertRaises(ValueError):
            rebuild(root, fresh, changed)
        changed = copy.deepcopy(old)
        path = next(path for path in changed["source_pins"] if path not in fresh["source_pins"])
        changed["source_pins"][path] = "0" * 64
        with self.assertRaises(ValueError):
            rebuild(root, fresh, changed)


if __name__ == "__main__":
    unittest.main()
