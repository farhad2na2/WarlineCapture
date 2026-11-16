import copy
import json
import unittest
from pathlib import Path
from unittest.mock import patch

import build_s003_refreshed_qualification as builder


class RefreshedQualificationTests(unittest.TestCase):
    def test_descriptor_requires_reconstructed_unqualified_evidence(self):
        root = Path(__file__).resolve().parents[4]
        receipt = json.loads((root / "T Logs/SkirmishProduction/s003-refreshed-source-evidence-audit-20261104-T4.json").read_bytes())
        with patch.object(builder, "audit", return_value=receipt):
            result = builder.construct(root, receipt)
            self.assertEqual("S003", result["target"]["catalog_id"])
            self.assertFalse(result["runtime_admission"])
            changed = copy.deepcopy(receipt)
            changed["runtime_admission"] = True
            with self.assertRaises(ValueError):
                builder.construct(root, changed)
        changed = copy.deepcopy(receipt)
        changed["mission_ready"] = True
        with patch.object(builder, "audit", return_value=changed):
            with self.assertRaises(ValueError):
                builder.construct(root, changed)


if __name__ == "__main__":
    unittest.main()
