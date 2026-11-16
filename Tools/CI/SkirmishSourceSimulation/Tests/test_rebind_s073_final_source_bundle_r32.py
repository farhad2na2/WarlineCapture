"""Fail-closed controls for the S073 r31-final2 to r32 water-only rebind."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).with_name("rebind_s073_final_source_bundle_r32.py")
spec = importlib.util.spec_from_file_location("rebind_s073_final_source_bundle_r32", SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def write(path, value):
    Path(path).write_text(json.dumps(value, sort_keys=True) + "\n", encoding="utf-8")
    return Path(path)


class S073FinalSourceBundleR32Tests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(dir="/private/tmp", prefix="s073-r32-test-")
        self.root = Path(self.temp.name)
        self.parent_files = {}
        keys = sorted(module.WATER_KEYS) + [f"retained_{i:02}" for i in range(26)]
        for index, key in enumerate(keys):
            path = self.root / f"parent-evidence-{index:02}.json"
            path.write_text(json.dumps({"key": key, "index": index}) + "\n", encoding="utf-8")
            self.parent_files[key] = {"path": str(path.resolve()), "sha256": sha(path)}
        self.bundle = {
            "schema_version": 1, "kind": "S073LegacyStartupBundle", "mission": "S073", "seed": 104729,
            "source_repository_root": str(module.ROOT.resolve()),
            "asset_repository_root": str(module.ROOT.resolve()), **self.parent_files,
        }
        self.bundle_path = write(self.root / "data-bundle-r31.json", self.bundle)
        # Use the retained, real r30 lineage artifacts to preserve the exact r31 ancestry.
        r30 = module.ROOT / "Logs/SkirmishProduction/s073-native-playing-occupancy-20261102-r3-finalized-r2/data-bundle-r30.json"
        r30_receipt = module.ROOT / "Logs/SkirmishProduction/s073-native-playing-occupancy-20261102-r3-finalized-r2/bundle-rebind-r30-receipt.json"
        self.receipt = {
            "kind": "S073FinalSourceBundleRebindReceipt", "status": "Passed",
            "output_bundle": {"path": str(self.bundle_path.resolve()), "sha256": sha(self.bundle_path)},
            "source_bundle": {"path": str(r30.resolve()), "sha256": "94ef7bedc5b304ea2dd1c3e29717fded148cb1b7d39b7161c4e023e2d08b9a45"},
            "source_bundle_receipt": {"path": str(r30_receipt.resolve()), "sha256": "bf2083c5ba5157e9e170a7084ba0c0b725f8dbbcbeddde4720e210174ce38c31"},
            "replaced_descriptors": {key: self.parent_files[key] for key in keys[:15]},
            "preserved_descriptors": {key: self.parent_files[key] for key in keys[15:]},
            "historical_binary_limitation": copy.deepcopy(module.EXPECTED_HISTORY),
            "source_execution_qualified": False, "mission_ready": False,
        }
        self.receipt_path = write(self.root / "bundle-rebind-r31-receipt.json", self.receipt)
        self.parent_patch = patch.multiple(
            module, PARENT_BUNDLE=self.bundle_path, PARENT_RECEIPT=self.receipt_path,
            PARENT_BUNDLE_SHA256=sha(self.bundle_path), PARENT_RECEIPT_SHA256=sha(self.receipt_path),
        )
        self.parent_patch.start()

    def tearDown(self):
        self.parent_patch.stop()
        self.temp.cleanup()

    def test_exact_parent_lineage_and_all_descriptors_validate(self):
        base, receipt, descriptors = module.validate_parent(self.bundle_path, self.receipt_path)
        self.assertEqual(base, self.bundle)
        self.assertEqual(receipt["status"], "Passed")
        self.assertEqual(len(descriptors), 30)

    def test_parent_bundle_hash_or_lineage_drift_is_rejected(self):
        with patch.object(module, "PARENT_BUNDLE_SHA256", "0" * 64):
            with self.assertRaisesRegex(ValueError, "exact retained r31-final2 parent bundle"):
                module.validate_parent(self.bundle_path, self.receipt_path)
        changed = json.loads(self.receipt_path.read_text())
        changed["historical_binary_limitation"]["unresolved_count"] = 6
        write(self.receipt_path, changed)
        with patch.object(module, "PARENT_RECEIPT_SHA256", sha(self.receipt_path)):
            with self.assertRaisesRegex(ValueError, "historical binary caveat"):
                module.validate_parent(self.bundle_path, self.receipt_path)

    def test_tampered_parent_artifact_is_rejected(self):
        retained_path = Path(self.parent_files["retained_00"]["path"])
        retained_path.write_text("tampered\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "descriptor artifact hash mismatch"):
            module.validate_parent(self.bundle_path, self.receipt_path)

    def test_water_replacements_preserve_every_non_water_descriptor(self):
        replacements = {key: {"path": f"/private/tmp/new-{key}", "sha256": "a" * 64}
                        for key in module.WATER_KEYS}
        rebound = module.apply_water_replacements(self.bundle, replacements)
        for key in self.bundle:
            if key in module.WATER_KEYS:
                self.assertEqual(rebound[key], replacements[key])
            else:
                self.assertEqual(rebound[key], self.bundle[key])
        bad = dict(replacements)
        bad.pop(sorted(bad)[0])
        with self.assertRaisesRegex(ValueError, "replacement descriptor set mismatch"):
            module.apply_water_replacements(self.bundle, bad)

    def _capture_fixture(self, parity="Match"):
        water_dir = self.root / "water"
        water_dir.mkdir()
        startup_obs = write(self.root / "startup.json", {
            "kind": "startup", "source_pins_sha256": {"Assets/Game/Startup.cs": "f" * 64},
        })
        startup_mask = self.root / "startup.water.bin"
        startup_mask.write_bytes(b"startup-water")
        snapshot_path = write(water_dir / "snapshot.json", {
            "kind": "S073NativeWaterMaskObservation", "phase": "Playing", "session_id": "a" * 32,
            "mission_ready": False, "source_fit_certified": False,
            "source_pins_sha256": {"Assets/Game/Source.cs": "b" * 64},
        })
        original_playing = write(self.root / "playing-original.json", {"kind": "original"})
        original_playing_mask = self.root / "playing-original.water.bin"
        original_playing_mask.write_bytes(b"playing-water")
        mask_path = water_dir / "snapshot.water.bin"
        mask_path.write_bytes(b"playing-water")
        log_path = water_dir / "editor.log"
        log_path.write_text("capture log\n", encoding="utf-8")
        water_receipt = {
            "kind": "S073NativeWaterMaskCaptureReceipt", "validation_status": "Passed",
            "wrapper_exit_code": 0, "required_marker_present": True, "session_id": "a" * 32,
            "mask_sha256": sha(mask_path), "editor_log_sha256": sha(log_path),
            "source_pins_sha256": {"Assets/Game/Source.cs": "b" * 64}, "mission_ready": False,
            "provider_assembly_sha256": "c" * 64,
            "original_observation_path": str(original_playing.resolve()),
        }
        water_receipt_path = write(water_dir / "water-receipt.json", water_receipt)
        joint = {
            "kind": "S073JointNativeWaterMaskComparison", "evidence_validation": "Passed",
            "parity_status": parity, "masks_match": parity == "Match", "differing_cells": 0 if parity == "Match" else 1,
            "wrapper_exit_code": 0, "mission_ready": False, "source_fit_certified": False,
            "session_id": "a" * 32, "editor_log_sha256": sha(log_path),
            "playing_capture": {"observation_path": str(original_playing.resolve()),
                                "receipt_sha256": sha(water_receipt_path), "mask_sha256": sha(mask_path),
                                "mask_path": str(original_playing_mask.resolve())},
            "startup_capture": {"observation_sha256": sha(startup_obs), "mask_sha256": sha(startup_mask)},
            "shared_source_pins_sha256": {"Assets/Game/Source.cs": "b" * 64},
        }
        write(water_dir / "joint-receipt.json", joint)
        return water_dir, startup_obs, startup_mask, snapshot_path, mask_path, water_receipt, joint

    def _finalizer_stubs(self, stale=False):
        water_result = {
            "validation_status": "Passed", "provider_assembly_sha256": "c" * 64,
            "provider_assembly_path": "/private/tmp/Game.Rendering.dll",
            "observation_sha256": "d" * 64, "mask_sha256": "e" * 64,
            "source_pins_sha256": {"Assets/Game/Source.cs": "b" * 64},
        }
        joint_result = {
            "evidence_validation": "Passed", "parity_status": "Match", "differing_cells": 0,
            "session_id": "a" * 32, "shared_source_pins_sha256": {"Assets/Game/Source.cs": "b" * 64},
        }

        class FakeWater:
            @staticmethod
            def validate(*args):
                if stale:
                    raise ValueError("Capture source changed: Assets/Game/Source.cs")
                return water_result

        class FakeJoint:
            @staticmethod
            def validate(*args):
                return joint_result

        def select(_path, name):
            return FakeJoint if "joint_water_finalizer" in name else FakeWater
        return select

    def test_failed_wrapper_is_rejected_before_rebinding(self):
        water_dir, startup_obs, startup_mask, *_ = self._capture_fixture()
        with self.assertRaisesRegex(ValueError, "wrapper must exit successfully"):
            module.validate_new_water(module.ROOT, water_dir, startup_obs, startup_mask, 1)
        output = self.root / "must-not-exist"
        with self.assertRaisesRegex(ValueError, "wrapper must exit successfully"):
            module.rebind(water_dir, startup_obs, startup_mask, output, 1)
        self.assertFalse(output.exists())

    def test_tampered_capture_descriptor_is_rejected(self):
        water_dir, startup_obs, startup_mask, *_ = self._capture_fixture()
        with patch.object(module, "load_module", side_effect=self._finalizer_stubs()):
            module.validate_new_water(module.ROOT, water_dir, startup_obs, startup_mask, 0)
            (water_dir / "snapshot.water.bin").write_bytes(b"tampered")
            with self.assertRaisesRegex(ValueError, "does not bind exact current snapshot, mask"):
                module.validate_new_water(module.ROOT, water_dir, startup_obs, startup_mask, 0)

    def test_current_source_pin_drift_from_finalizer_is_rejected(self):
        water_dir, startup_obs, startup_mask, *_ = self._capture_fixture()
        with patch.object(module, "load_module", side_effect=self._finalizer_stubs(stale=True)):
            with self.assertRaisesRegex(ValueError, "Capture source changed"):
                module.validate_new_water(module.ROOT, water_dir, startup_obs, startup_mask, 0)

    def test_nonmatching_joint_capture_is_rejected(self):
        water_dir, startup_obs, startup_mask, *_ = self._capture_fixture(parity="MismatchRecorded")
        with self.assertRaisesRegex(ValueError, "same-session startup/Playing water parity"):
            module.validate_new_water(module.ROOT, water_dir, startup_obs, startup_mask, 0)


if __name__ == "__main__":
    unittest.main()
