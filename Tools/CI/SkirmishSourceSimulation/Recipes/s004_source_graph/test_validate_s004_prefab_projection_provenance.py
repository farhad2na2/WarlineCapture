import copy
import json
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parent))
from validate_s004_prefab_projection_provenance import (
    gameplay_mutation_control,
    validate_entity_projection,
)


ROOT = Path(__file__).resolve().parents[5]
PROJECTION = ROOT / "Logs/SkirmishProduction/s004-focused-prefab-presentation-projection-20261103-T7/projection.json"
RAW = ROOT / "Logs/SkirmishProduction/s004-focused-prefab-capture-20261103-r1/data/prefab-reference-data.json"


class ProjectionProvenanceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.raw = json.loads(RAW.read_text())
        cls.projection = json.loads(PROJECTION.read_text())

    def test_exact_ledger_deletions_reconcile(self):
        self.assertEqual(validate_entity_projection(self.raw, self.projection), 16)

    def test_gameplay_mutation_rejected(self):
        result = gameplay_mutation_control(self.raw, self.projection)
        self.assertEqual(result["result"], "Passed")

    def test_unknown_exclusion_rejected(self):
        probe = copy.deepcopy(self.projection)
        probe["excluded_presentation_object_refs"][0]["field"] = "UnreviewedGameplayField"
        with self.assertRaises(ValueError):
            validate_entity_projection(self.raw, probe)

    def test_root_identity_mutation_rejected(self):
        probe = copy.deepcopy(self.projection)
        key = next(iter(probe["opening_prefabs"]))
        probe["opening_prefabs"][key] = "0:0"
        with self.assertRaises(ValueError):
            validate_entity_projection(self.raw, probe)


if __name__ == "__main__":
    unittest.main()
