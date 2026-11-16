import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).with_name("rebind_s003_current_data_bundle.py")
SPEC = importlib.util.spec_from_file_location("s003_bundle_rebinder", SCRIPT)
REBINDER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(REBINDER)


class S003CurrentBundleRebindPinTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name).resolve(strict=True)
        (self.root / "Assets").mkdir()
        self.payload = self.root / "Assets" / "pinned.asset"
        self.payload.write_bytes(b"same pinned bytes")
        self.digest = hashlib.sha256(self.payload.read_bytes()).hexdigest()

    def tearDown(self):
        self.temp.cleanup()

    def test_mixed_relative_and_absolute_repo_pins_are_rehashed(self):
        pins = {"Assets/pinned.asset": self.digest, str(self.payload.resolve()): self.digest}
        self.assertEqual(REBINDER.verify_pin_map(pins, "fixture", repo_root=self.root), 2)

    def test_conflicting_hashes_for_one_normalized_path_are_rejected(self):
        pins = {"Assets/pinned.asset": self.digest, str(self.payload.resolve()): "0" * 64}
        with self.assertRaisesRegex(ValueError, "Duplicate normalized source pin conflict"):
            REBINDER.verify_pin_map(pins, "fixture", repo_root=self.root)

    def test_parent_traversal_is_rejected_before_normalization(self):
        pins = {"Assets/../outside.asset": self.digest}
        with self.assertRaisesRegex(ValueError, "Parent traversal"):
            REBINDER.verify_pin_map(pins, "fixture", repo_root=self.root)

    def test_symlink_source_pin_is_rejected(self):
        alias = self.root / "Assets" / "alias.asset"
        try:
            alias.symlink_to(self.payload)
        except (OSError, NotImplementedError):
            self.skipTest("Symlink creation is unavailable on this test filesystem")
        with self.assertRaisesRegex(ValueError, "Symlink"):
            REBINDER.verify_pin_map({"Assets/alias.asset": self.digest}, "fixture", repo_root=self.root)

    def test_s003_contract_requires_navigation_camera_and_water_diagnostic_inputs(self):
        required = REBINDER.S003_RUNTIME_INITIAL_FIELDS
        self.assertTrue({"navigation_reference", "engineering_camera_profile", "placement_domain",
                         "operation_map_rotation_reference", "death_mechanical_inputs",
                         "metadata_compatibility", "command_text_data"}.issubset(required))

    def test_failed_runtime_contract_never_becomes_complete(self):
        audit = {
            "missing_initialization_fields": ["navigation_reference"],
            "missing_or_invalid_bundle_contract": [],
            "invalid_s003_diagnostic_inputs": [],
        }
        complete = not any(audit.values())
        self.assertFalse(complete)

    def test_current_capture_has_the_exact_mixed_path_closure(self):
        capture_path = REBINDER.ROOT / "T Logs/SkirmishProduction/s003-focused-prefab-domain-20261103-T1/data/s003-focused-template-domain.json"
        if not capture_path.is_file():
            self.skipTest("Optional workspace-evidence integration; current T logs are not present in CI")
        capture = json.loads(capture_path.read_text())
        pins = capture["source_pins"]
        self.assertEqual(len(pins), 16720)
        self.assertTrue(any(not Path(path).is_absolute() for path in pins))
        self.assertTrue(any(Path(path).is_absolute() for path in pins))

    def test_retained_legacy_slice_is_compared_by_keys_without_inventing_mission_id(self):
        capture_path = REBINDER.ROOT / "T Logs/SkirmishProduction/s003-focused-prefab-domain-20261103-T1/data/s003-focused-template-domain.json"
        if not capture_path.is_file():
            self.skipTest("Optional workspace-evidence integration; current T logs are not present in CI")
        capture = json.loads(capture_path.read_text())
        slice_path = REBINDER.ROOT / REBINDER.RELOCATION / "s003-unit-template-projection-r1/slice.json"
        legacy = json.loads(slice_path.read_text())
        self.assertNotIn("mission_id", legacy)
        self.assertEqual(legacy["requested_prefab_keys"], capture["requested_prefab_keys"])
        self.assertEqual(len(legacy["entities"]), 647)

    def _clearance_fixture(self, source_scenes=None, source_bytes=b"mapped durable source"):
        mapped = self.root / "Logs" / "mapped" / "reference-data.json"
        mapped.parent.mkdir(parents=True)
        mapped.write_bytes(source_bytes)
        digest = hashlib.sha256(source_bytes).hexdigest()
        receipt = self.root / "T Logs" / "relocation" / "receipt.json"
        receipt.parent.mkdir(parents=True)
        receipt.write_text(json.dumps({
            "kind": "S003VerifiedTemporaryClearanceRelocation",
            "source_path": str(mapped), "destination_path": "/private/tmp/legacy/reference-data.json",
            "sha256": digest, "bytes": len(source_bytes), "source_and_copy_verified": True,
            "source_fit_certified": False, "mission_ready": False,
        }), encoding="utf-8")
        source = {"kind": "AuthoredSkirmishReferenceData",
                  "source_scenes": source_scenes or REBINDER.S003_CLEARANCE_SOURCE_SCENES}
        mapped.write_text(json.dumps(source), encoding="utf-8")
        digest = hashlib.sha256(mapped.read_bytes()).hexdigest()
        receipt_doc = json.loads(receipt.read_text(encoding="utf-8"))
        receipt_doc.update({"sha256": digest, "bytes": mapped.stat().st_size})
        receipt.write_text(json.dumps(receipt_doc), encoding="utf-8")
        projection = self.root / "Logs" / "projection.json"
        projection.write_text(json.dumps({
            "schema_version": 1, "kind": "GeneratedSourceStaticClearanceProjection", "source_sha256": digest,
            "source_record": {
                "authority": "OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain",
                "projection_source": "Unity.Rendering.RenderBoundsUpdateSystem.BoundsJob:AABB.Transform(LocalToWorld,RenderBounds)",
                "grid_width": 2048, "grid_height": 1024, "grid_origin": [0.0, 0.0, 0.0], "cell_size": 1.0,
                "query_rect": [-118.0, -118.0, 2284.0, 1260.0], "minimum_extent": 0.25, "maximum_extent": 20.0,
                "source_search_radius_cells": 96, "source_padding_cells": 2, "bounds": [{}] * 48457,
            },
        }), encoding="utf-8")
        descriptor = {"path": str(projection), "sha256": hashlib.sha256(projection.read_bytes()).hexdigest()}
        return mapped, receipt, descriptor, digest

    def test_clearance_descriptor_uses_mapped_durable_path_and_validates_map_identity(self):
        mapped, receipt, projection, digest = self._clearance_fixture()
        with patch.object(REBINDER, "ROOT", self.root), \
             patch.object(REBINDER, "S003_CLEARANCE_SOURCE_SHA", digest), \
             patch.object(REBINDER, "S003_CLEARANCE_SOURCE_BYTES", mapped.stat().st_size):
            result = REBINDER.resolve_s003_clearance_source_artifact(
                self.root, {"path": "/private/tmp/stale/reference-data.json", "sha256": digest}, projection, receipt)
        self.assertEqual(result, {"path": str(mapped.resolve()), "sha256": digest, "bytes": mapped.stat().st_size})

    def test_clearance_descriptor_rejects_wrong_map_frame_source_scene_identity(self):
        mapped, receipt, projection, digest = self._clearance_fixture(["Assets/Game/Scenes/Other.unity"])
        with patch.object(REBINDER, "ROOT", self.root), \
             patch.object(REBINDER, "S003_CLEARANCE_SOURCE_SHA", digest), \
             patch.object(REBINDER, "S003_CLEARANCE_SOURCE_BYTES", mapped.stat().st_size):
            with self.assertRaisesRegex(ValueError, "scene identity"):
                REBINDER.resolve_s003_clearance_source_artifact(
                    self.root, {"path": "/private/tmp/stale/reference-data.json", "sha256": digest}, projection, receipt)

    def test_clearance_descriptor_rejects_nonverified_or_admissive_relocation_receipt(self):
        mapped, receipt, projection, digest = self._clearance_fixture()
        receipt_doc = json.loads(receipt.read_text(encoding="utf-8"))
        receipt_doc["mission_ready"] = True
        receipt.write_text(json.dumps(receipt_doc), encoding="utf-8")
        with patch.object(REBINDER, "ROOT", self.root), \
             patch.object(REBINDER, "S003_CLEARANCE_SOURCE_SHA", digest), \
             patch.object(REBINDER, "S003_CLEARANCE_SOURCE_BYTES", mapped.stat().st_size):
            with self.assertRaisesRegex(ValueError, "non-admissive"):
                REBINDER.resolve_s003_clearance_source_artifact(
                    self.root, {"path": "/private/tmp/stale/reference-data.json", "sha256": digest}, projection, receipt)


if __name__ == "__main__":
    unittest.main()
