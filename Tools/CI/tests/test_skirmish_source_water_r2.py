"""Exact-path, hash, and source-pin controls for the S073 loaded-source r2 water evidence."""
import hashlib
import json
from pathlib import Path
import sys
import unittest

CI = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(CI))
import skirmish_source_build as source_build
from skirmish_source_manifest import UnsupportedSourceShape


ROOT = Path(__file__).resolve().parents[3]
WATER = ROOT / "Logs/SkirmishProduction/s073-loaded-source-fit-20261102-r2"
START = Path("/private/tmp/s073-loaded-source-fit-20261102-r2-start.json")
START_MASK = Path("/private/tmp/s073-loaded-source-fit-20261102-r2-start.water.bin")
PLAY = Path("/private/tmp/s073-loaded-source-fit-20261102-r2-playing.json")
PLAY_MASK = Path("/private/tmp/s073-loaded-source-fit-20261102-r2-playing.water.bin")


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class S073LoadedSourceWaterR2Tests(unittest.TestCase):
    def test_exact_r2_receipt_snapshot_mask_hashes_and_pins(self):
        relative = "Logs/SkirmishProduction/s073-loaded-source-fit-20261102-r2"
        expected = (
            "ba28b7dc90e6db335686df62b64c1ef6246a056cfaa1648f94ca4cf5a23f561a",
            "165c1efdc74e6c5ab00e5a9cd5a90504ef0a32e26d1a538b2d585d48d95fad3f",
            "5647f05ec18958947d32874eeb788fa396a05d0bab7c1b71f112ceb7e9b31eee",
        )
        self.assertEqual(source_build.S073_WATER_CAPTURE_REVISIONS[relative], expected)
        self.assertEqual(tuple(sha(WATER / name) for name in
                               ("water-receipt.json", "snapshot.json", "snapshot.water.bin")), expected)
        snapshot = json.loads((WATER / "snapshot.json").read_text())
        self.assertEqual(set(snapshot["source_pins_sha256"]), source_build.S073_WATER_SOURCE_PIN_PATHS)
        self.assertEqual(len(snapshot["source_pins_sha256"]), 18)

    def test_exact_r2_joint_receipt_and_original_capture_paths(self):
        relative = "Logs/SkirmishProduction/s073-loaded-source-fit-20261102-r2"
        expected = "a1991821818bf53b31d66c7773ef2a68e10645ed9af744236266e0c04031d8c1"
        self.assertEqual(source_build.S073_WATER_PARITY_REVISIONS[relative], (expected, True))
        self.assertEqual(sha(WATER / "joint-receipt.json"), expected)
        joint = json.loads((WATER / "joint-receipt.json").read_text())
        self.assertEqual(Path(joint["startup_capture"]["observation_path"]), START)
        self.assertEqual(Path(joint["startup_capture"]["mask_path"]), START_MASK)
        self.assertEqual(Path(joint["playing_capture"]["observation_path"]), PLAY)
        self.assertEqual(Path(joint["playing_capture"]["mask_path"]), PLAY_MASK)
        self.assertIsNone(joint.get("wrapper_exit_path"))

    def test_neighbor_revision_path_is_not_admitted(self):
        selected = source_build._select_s073_exact_revision(
            ROOT, WATER / "water-receipt.json", source_build.S073_WATER_CAPTURE_REVISIONS)
        self.assertEqual(selected[0], WATER.resolve())
        wrong_path = WATER.parent / "s073-loaded-source-fit-20261102-r2-copy"
        wrong_path.mkdir()
        (wrong_path / "water-receipt.json").write_bytes((WATER / "water-receipt.json").read_bytes())
        (wrong_path / "joint-receipt.json").write_bytes((WATER / "joint-receipt.json").read_bytes())
        try:
            self.assertIsNone(source_build._select_s073_exact_revision(
                ROOT, wrong_path / "water-receipt.json", source_build.S073_WATER_CAPTURE_REVISIONS))
            self.assertIsNone(source_build._select_s073_exact_revision(
                ROOT, wrong_path / "joint-receipt.json", source_build.S073_WATER_PARITY_REVISIONS))
        finally:
            (wrong_path / "water-receipt.json").unlink()
            (wrong_path / "joint-receipt.json").unlink()
            wrong_path.rmdir()

    def test_mismatched_descriptor_hash_is_rejected(self):
        inputs = {
            "native_water_capture_receipt": {"path": str(WATER / "water-receipt.json"), "sha256": "0" * 64},
            "native_water_capture_snapshot": {"path": str(WATER / "snapshot.json"), "sha256": sha(WATER / "snapshot.json")},
            "native_water_capture_mask": {"path": str(WATER / "snapshot.water.bin"), "sha256": sha(WATER / "snapshot.water.bin")},
        }
        with self.assertRaisesRegex(UnsupportedSourceShape, "S073 native-water descriptor changed"):
            source_build._validate_s073_native_water_capture(ROOT, inputs, {})

    def test_csharp_parity_gate_binds_exact_r2_identity(self):
        source = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073NativeStartupWaterParity.cs").read_text()
        self.assertIn('"a1991821818bf53b31d66c7773ef2a68e10645ed9af744236266e0c04031d8c1"', source)
        self.assertIn('evidenceRelative == "Logs/SkirmishProduction/s073-loaded-source-fit-20261102-r2"', source)
        for exact_name in (START.name, START_MASK.name, PLAY.name, PLAY_MASK.name):
            self.assertIn('"' + exact_name + '"', source)
        self.assertIn("wrapperExitFileRequired = false;", source)

    def test_csharp_capture_adapter_accepts_only_exact_normalized_r2_receipt_chain(self):
        source = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073NativeWaterMaskAdapter.cs").read_text()
        for digest in (
            "ba28b7dc90e6db335686df62b64c1ef6246a056cfaa1648f94ca4cf5a23f561a",
            "9a5edbc286106da65bb4daec8c2b00bb694f46d7f4ab17ceb1f00a9e2729c4c8",
            "165c1efdc74e6c5ab00e5a9cd5a90504ef0a32e26d1a538b2d585d48d95fad3f",
            "5647f05ec18958947d32874eeb788fa396a05d0bab7c1b71f112ceb7e9b31eee",
        ):
            self.assertIn('"' + digest + '"', source)
        self.assertIn("normalized_observation_path", source)
        self.assertIn("HashFile(originalPath) == LoadedSourceR2ObservationSha256", source)
        self.assertIn('receipt["wrapper_exit_path"] == null', source)
        self.assertIn("Loaded-source r2 receipt must bind wrapper success", source)


if __name__ == "__main__":
    unittest.main()
