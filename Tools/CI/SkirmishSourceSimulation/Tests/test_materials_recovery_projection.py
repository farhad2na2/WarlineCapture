"""Contract checks for extraction; these do not establish native parity."""
import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[4]
RECIPE = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/regenerate_materials_recovery_probe.py"
SOURCES = (
    "Assets/Game/Scripts/Systems/MaterialsScenarioRecoveryStartupSystemHelper.cs",
    "Assets/Game/Scripts/Systems/ResourceExchangeStartupProjectionSystemHelper.cs",
    "Assets/Game/Scripts/Components/AssistantComponents.cs",
)
spec = importlib.util.spec_from_file_location("recovery_projection", RECIPE)
recipe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(recipe)


class RecoveryProjectionTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        for path in SOURCES:
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / path, target)

    def tearDown(self):
        self.tmp.cleanup()

    def test_generates_original_with_only_bounded_absent_buffer_adaptations(self):
        out = self.root / "out"
        receipt = recipe.generate(self.root, out)
        original = (self.root / SOURCES[0]).read_text().replace("\r\n", "\n")
        expected = original.replace("!buildings.IsCreated", "buildings == null").replace("!units.IsCreated", "units == null")
        self.assertEqual(expected, (out / "MaterialsScenarioRecoveryStartupSystemHelper.cs").read_text())
        self.assertEqual(len(receipt["source_sha256"]), 5)
        self.assertEqual((out / "MaterialsRecoveryReadOnlyBuffers.cs").read_bytes(),
                         (RECIPE.parent / "MaterialsRecoveryReadOnlyBuffers.cs.txt").read_bytes())
        self.assertIn("TryResolveGate", (out / "ResourceExchangeGateProbe.cs").read_text())

    def test_changed_buffer_check_fails_closed(self):
        path = self.root / SOURCES[0]
        path.write_text(path.read_text().replace("!units.IsCreated", "false"))
        with self.assertRaisesRegex(ValueError, "buffer check changed"):
            recipe.generate(self.root, self.root / "out")


if __name__ == "__main__":
    unittest.main()
