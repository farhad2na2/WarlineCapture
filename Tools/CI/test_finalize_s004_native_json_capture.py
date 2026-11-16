import importlib.util
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).with_name("finalize_s004_native_json_capture.py")
spec = importlib.util.spec_from_file_location("s004_finalizer", SCRIPT)
finalizer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(finalizer)


class S004NativeJsonFinalizerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = SCRIPT.resolve().parents[2]
        cls.log = cls.root / finalizer.CAPTURE
        cls.original = cls.log.read_text()

    def reject(self, replacement, wrapper_exit=0):
        with tempfile.TemporaryDirectory() as tmp:
            candidate = Path(tmp) / "editor.log"
            candidate.write_text(replacement)
            with self.assertRaises(finalizer.FinalizeError):
                finalizer.verify(candidate, self.root, wrapper_exit)

    def test_observed_capture_passes(self):
        receipt = finalizer.verify(self.log, self.root, 0)
        self.assertEqual(receipt["gameplay"], "NotRun")
        self.assertEqual(receipt["readiness"], "NotClaimed")
        self.assertEqual(len(receipt["compilerRows"]), 3)

    def test_nonzero_wrapper_exit_rejected(self):
        with self.assertRaises(finalizer.FinalizeError):
            finalizer.verify(self.log, self.root, 1)

    def test_duplicate_final_marker_rejected(self):
        self.reject(self.original + "\n" + finalizer.MARKER + "\n")

    def test_changed_source_pin_rejected(self):
        self.reject(self.original.replace("sha256=c2ffbdb5", "sha256=02ffbdb5", 1))

    def test_malformed_synthetic_json_rejected(self):
        line = next(row for row in self.original.splitlines() if "synthetic=null-empty json=" in row)
        self.reject(self.original.replace(line, line.rsplit("{", 1)[0] + "{broken", 1))

    def test_extra_compiler_row_rejected(self):
        row = next(row for row in self.original.splitlines() if "compiler size=Standard " in row)
        self.reject(self.original + "\n" + row + "\n")


if __name__ == "__main__":
    unittest.main()
