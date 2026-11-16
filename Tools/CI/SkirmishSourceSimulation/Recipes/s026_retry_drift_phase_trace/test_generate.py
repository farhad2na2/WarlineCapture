import importlib.util
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
spec = importlib.util.spec_from_file_location("s026_retry_drift_phase_generator", HERE / "generate.py")
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)

class GeneratorTests(unittest.TestCase):
    def test_overlay_is_bounded_opt_in_and_does_not_edit_host(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / generator.SOURCE
            source.parent.mkdir(parents=True)
            original = (ROOT / generator.SOURCE).read_bytes()
            source.write_bytes(original)
            proof = generator.generate(root, root / "out")
            result = (root / "out" / generator.OUTPUT).read_text()
            self.assertEqual(source.read_bytes(), original)
            self.assertEqual(result.count(generator.INSERT), 1)
            self.assertIn("WARLINE_S026_RETRY_DRIFT_PHASE_TRACE", result)
            self.assertIn("role.rifle.1.0.5", result)
            self.assertIn('name=="path"||name=="movement"', result)
            self.assertIn('FirstTick = 220, LastTick = 310, Capacity = 400', result)
            for field in ("UnitTarget", "UnitPathRequest", "UnitPathRange", "UnitPathFollow", "UnitPathRetryCooldown"):
                self.assertIn(field, result)
            self.assertTrue(proof["experimental_only"])

    def test_changed_boundary_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / generator.SOURCE
            source.parent.mkdir(parents=True)
            source.write_text("changed")
            with self.assertRaisesRegex(ValueError, "phase boundary"):
                generator.generate(root, root / "out")

    def test_existing_output_is_not_overwritten(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / generator.SOURCE
            source.parent.mkdir(parents=True)
            source.write_bytes((ROOT / generator.SOURCE).read_bytes())
            (root / "out").mkdir()
            with self.assertRaises(FileExistsError):
                generator.generate(root, root / "out")

if __name__ == "__main__":
    unittest.main()
