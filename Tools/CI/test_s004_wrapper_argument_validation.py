import importlib.util
from pathlib import Path
import unittest

SPEC = importlib.util.spec_from_file_location("s004_readmodel_finalizer", Path(__file__).with_name("finalize_s004_configured_building_readmodel_capture.py"))
finalizer = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(finalizer)


class S004WrapperArgumentValidation(unittest.TestCase):
    def setUp(self):
        self.root = Path("/tmp/s004-wrapper-fixture")
        self.raw = self.root / "capture/data/configured-spawnable-readmodel.json"
        self.evidence = self.root / "setup/compiled-setup.json"
        self.command = "\n".join(("Unity", "-projectPath", str(self.root), "-executeMethod", finalizer.METHOD,
                                   "-skirmishS004ConfiguredSpawnableReadModel", "capture/data",
                                   "-skirmishS004ConfiguredSpawnableSetupEvidence", "setup/compiled-setup.json"))

    def test_relative_actual_paths_match(self):
        finalizer.validate_wrapper_command(self.command, self.root, self.raw, self.evidence)

    def test_changed_path_is_rejected(self):
        with self.assertRaises(finalizer.FinalizeError):
            finalizer.validate_wrapper_command(self.command.replace("capture/data", "different/data"), self.root, self.raw, self.evidence)

    def test_duplicate_option_is_rejected(self):
        with self.assertRaises(finalizer.FinalizeError):
            finalizer.validate_wrapper_command(self.command + "\n-projectPath\n" + str(self.root), self.root, self.raw, self.evidence)

    def test_batchmode_is_rejected(self):
        with self.assertRaises(finalizer.FinalizeError):
            finalizer.validate_wrapper_command(self.command + "\n-batchmode", self.root, self.raw, self.evidence)


if __name__ == "__main__": unittest.main()
