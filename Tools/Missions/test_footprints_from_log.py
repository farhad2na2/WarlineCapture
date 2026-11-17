import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from footprints_from_log import extract

ROW = "[SkirmishFootprintTrace] structure={} width={} depth=4.000 rotatable=0 source=BuildingConfiguredSpawnableReadModel\n"
DONE = "[SkirmishFootprintTrace] result=Completed count={}\n"


class FootprintsTest(unittest.TestCase):
    def test_sample_and_cli(self):
        sample = "Unity log\n" + ROW.format("building.z", "3.000") + ROW.format("building.a", "2.000") + DONE.format(2)
        with tempfile.TemporaryDirectory() as directory:
            log = Path(directory) / "unity.log"
            log.write_text(sample, encoding="utf-8")
            result = subprocess.run([sys.executable, str(Path(__file__).with_name("footprints_from_log.py")), str(log)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertLess(result.stdout.index('"building.a"'), result.stdout.index('"building.z"'))
        self.assertEqual(extract(sample.splitlines()), {"building.a": [2.0, 4.0], "building.z": [3.0, 4.0]})

    def test_missing_or_incomplete_completion(self):
        for sample in (ROW.format("a", "2.000"), ROW.format("a", "2.000") + DONE.format(2), DONE.format(0) + ROW.format("a", "2.000")):
            with self.subTest(sample=sample), self.assertRaises(ValueError):
                extract(sample.splitlines())

    def test_conflicts_and_repeated_batches(self):
        row = ROW.format("a", "2.000")
        self.assertEqual(extract((row + DONE.format(1) + row + DONE.format(1)).splitlines()), {"a": [2.0, 4.0]})
        with self.assertRaises(ValueError):
            extract((row + ROW.format("a", "3.000") + DONE.format(2)).splitlines())

    def test_cli_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            log = Path(directory) / "unity.log"
            log.write_text(ROW.format("a", "2.000"), encoding="utf-8")
            result = subprocess.run([sys.executable, str(Path(__file__).with_name("footprints_from_log.py")), str(log)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 1)
        self.assertEqual(result.stdout, "")


if __name__ == "__main__":
    unittest.main()
