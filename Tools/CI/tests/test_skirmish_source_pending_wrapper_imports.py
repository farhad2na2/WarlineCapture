"""Keep the generated timeline interface in type parity with the pending DTO wrapper."""
from pathlib import Path
import importlib.util
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[3]
GENERATOR = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/regenerate_faction_queue.py"


def load_generator():
    spec = importlib.util.spec_from_file_location("regenerate_faction_queue", GENERATOR)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class SourcePendingWrapperImportTests(unittest.TestCase):
    def test_timeline_interface_resolves_source_pending_receipt_types(self):
        with tempfile.TemporaryDirectory(prefix="pending-wrapper-imports-") as temporary:
            output = Path(temporary) / "generated"
            load_generator().generate(ROOT, output)
            timeline = (output / "BuildingProductionQueueCompositionSystemHelper.timeline.cs").read_text()
            pending = (ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json").read_text()
            self.assertIn("using Unity.Entities;", timeline)
            self.assertIn("using Unity.Collections;", timeline)
            self.assertIn("Entity ReceiptOwner { get; set; }", timeline)
            self.assertIn("FixedString64Bytes ReceiptAttempt { get; set; }", timeline)
            self.assertIn('"output": "251_SourcePendingProduction.cs"', pending)


if __name__ == "__main__":
    unittest.main()
