import hashlib
import json
import struct
import tempfile
import unittest
from pathlib import Path

from compare_s073_blocker_occupancy import compare


def artifact(path, producer, *, counts=(0, 1, 0), blocked=(0, 1, 0), friendly=(0, 0, 0), occupied=(0, 0, 0)):
    cells = len(counts)
    bits = lambda values: bytes(sum((1 << (i & 7)) for i, value in enumerate(values) if value)
                                for _ in range((cells + 7) // 8))
    data = (b"S073BOC1" + struct.pack("<IIffffQQQI", cells, 1, 1.0, 0.0, 0.0, 0.0,
                                      7, 7, 11, cells) + bits(blocked) +
            struct.pack(f"<{cells}i", *counts) + bytes(friendly) + bits(occupied))
    path.write_bytes(data)
    receipt = {
        "kind": "S073BlockerOccupancyRawSnapshot", "schema_version": 1, "status": "Captured",
        "producer": producer, "observation": "pre_enqueue_input_observation",
        "raw_path": str(path), "raw_sha256": hashlib.sha256(data).hexdigest(), "byte_length": len(data),
        "width": cells, "height": 1, "cell_size": 1.0, "origin": [0.0, 0.0, 0.0],
        "storage_epoch": "7", "applied_storage_epoch": "7", "applied_revision": "11",
        "mission": "S073", "seed": 104729, "native_session_id": "native-session",
        "source_map_id": "opmap.skirmish.industrial_basin", "source_campaign_map_id": "opmap.skirmish.desert_base_01",
        "scheduler_admitted": False, "mission_ready": False,
        "scope": ("IndependentNativeRawBlockerOccupancyCapture_NoSourceStateImport_NoAdmission"
                  if producer == "native" else
                  "IndependentSourceHostRawBlockerOccupancyCapture_NoNativeStateImport_NoAdmission"),
        ("source_host_state_transferred" if producer == "native" else "source_campaign_state_transferred"): False,
    }
    if producer == "source":
        receipt["source_session_id"] = "source-session"
    else:
        receipt.update({"campaign_state_transferred": False, "readiness_claimed": False})
    receipt_path = Path(str(path) + ".json")
    receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
    return receipt_path, receipt


class BlockerOccupancyComparisonTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        root = Path(self.temp.name)
        self.native_raw = root / "native.bin"
        self.source_raw = root / "source.bin"
        self.native_receipt, self.n = artifact(self.native_raw, "native")
        self.source_receipt, self.s = artifact(self.source_raw, "source", counts=(0, 1, 2),
                                               blocked=(0, 1, 1), friendly=(0, 0, 3), occupied=(0, 1, 0))
        root = Path(self.temp.name)
        source_build = root / "build.json"; source_build.write_text("{}", encoding="utf-8")
        executable = root / "source.exe"; executable.write_bytes(b"executable")
        bundle = root / "bundle.json"; bundle.write_text("{}", encoding="utf-8")
        graph = root / "graph.json"; graph.write_text("{}", encoding="utf-8")
        def pin(path):
            return {"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
        self.source_build_pin, self.executable_pin = pin(source_build), pin(executable)
        self.bundle_pin, self.graph_pin = pin(bundle), pin(graph)
        request_path = root / "run-request.json"
        request = {"kind": "SkirmishSourceSimulationRun", "mission": "S073", "seed": 104729,
                   "source_build_receipt": self.source_build_pin, "source_executable": self.executable_pin}
        request_path.write_text(json.dumps(request), encoding="utf-8")
        self.s.update({"source_build_receipt": self.source_build_pin, "source_executable": self.executable_pin,
                       "run_request": pin(request_path), "bundle": self.bundle_pin, "source_graph": self.graph_pin})
        self.source_receipt.write_text(json.dumps(self.s), encoding="utf-8")
        worker_path = root / "result.json"
        worker = {"source_blocker_occupancy_raw_capture": {
            "path": str(self.source_raw.resolve()), "sha256": self.s["raw_sha256"],
            "receipt_path": str(self.source_receipt.resolve()),
            "receipt_sha256": hashlib.sha256(self.source_receipt.read_bytes()).hexdigest()}}
        worker_path.write_text(json.dumps(worker), encoding="utf-8")
        self.source_run_receipt = root / "source-run-receipt.json"
        self.source_run_receipt.write_text(json.dumps({"mission": "S073", "seed": 104729,
            "status": "PreparingPhysicalSourceRequired", "process_exit": 2,
            "validation_passed": False, "full_outcome_observed": False, "worker_result": pin(worker_path)}), encoding="utf-8")
        self.native_snapshot = root / "native-snapshot.json"
        self.editor_log = root / "editor.log"
        self.editor_log.write_text("native capture log", encoding="utf-8")
        self.native_snapshot.write_text(json.dumps({"kind": "S073NativePathfindingCommandDiagnostic",
            "schema_version": 1, "seed": 104729, "scenario_index": 3,
            "session_id": "native-session", "pre_enqueue_input_observation": {
                "raw_blocker_occupancy_snapshot": self.n}, "player_readiness": False}), encoding="utf-8")
        self.native_wrapper_receipt = root / "native-wrapper-receipt.json"
        self.native_wrapper_receipt.write_text(json.dumps({"status": "Recorded", "wrapper_exit": 0,
            "mission_ready": False, "scope": "IndependentInstrumentedNativeCapture_NotNormalInputOrReadiness",
            "session_id": "native-session", "snapshot_sha256": pin(self.native_snapshot)["sha256"],
            "editor_log_sha256": pin(self.editor_log)["sha256"]}), encoding="utf-8")

    def tearDown(self):
        self.temp.cleanup()

    def run_compare(self, max_coordinates=64):
        return compare(self.native_raw, self.native_receipt, self.source_raw,
                       self.source_receipt, self.native_wrapper_receipt, self.native_snapshot,
                       self.source_run_receipt, max_coordinates)

    def refresh_source_worker_binding(self):
        run_receipt = json.loads(self.source_run_receipt.read_text())
        worker_path = Path(run_receipt["worker_result"]["path"])
        worker = json.loads(worker_path.read_text())
        worker["source_blocker_occupancy_raw_capture"]["receipt_sha256"] = hashlib.sha256(
            self.source_receipt.read_bytes()).hexdigest()
        worker_path.write_text(json.dumps(worker), encoding="utf-8")
        run_receipt["worker_result"]["sha256"] = hashlib.sha256(worker_path.read_bytes()).hexdigest()
        self.source_run_receipt.write_text(json.dumps(run_receipt), encoding="utf-8")

    def test_reports_field_counts_and_bounded_coordinates_without_readiness(self):
        result = self.run_compare(max_coordinates=1)
        self.assertEqual(result["status"], "DifferencesObserved")
        self.assertEqual(result["mismatching_cells_by_field"],
                         {"blocked": 1, "counts": 1, "friendly_pass_faction": 1, "occupied": 1})
        self.assertEqual(len(result["first_mismatch_coordinates"]), 1)
        self.assertEqual(result["first_mismatch_coordinates"][0]["x"], 1)
        self.assertFalse(result["parity_claim"])
        self.assertFalse(result["scheduler_admitted"])
        self.assertFalse(result["mission_ready"])

    def test_accepts_passive_native_playing_raw_capture_receipt(self):
        self.s["source_identity_hash"] = "identity"
        self.s["source_content_hash"] = "content"
        self.source_receipt.write_text(json.dumps(self.s), encoding="utf-8")
        self.refresh_source_worker_binding()
        native = json.loads(self.native_receipt.read_text())
        native["observation"] = "playing_passive_input_observation"
        native["native_session_id"] = "native-session"
        native["source_identity_hash"] = "identity"
        native["source_content_hash"] = "content"
        self.native_receipt.write_text(json.dumps(native), encoding="utf-8")
        snapshot = {
            "kind": "S073NativeWaterMaskObservation", "schema_version": 1,
            "scope": "SameSessionOriginalSkirmishWaterPlacementCreateMask_NotReadinessOrMissionAcceptance",
            "seed": 104729, "scenario_index": 3, "phase": "Playing",
            "session_id": "native-session", "mission_ready": False, "player_readiness": False,
            "playing_passive_input_observation": {"raw_blocker_occupancy_snapshot": native},
        }
        self.native_snapshot.write_text(json.dumps(snapshot), encoding="utf-8")
        wrapper = {
            "kind": "S073NativeWaterMaskCaptureReceipt", "validation_status": "Passed",
            "wrapper_exit_code": 0, "session_id": "native-session", "mission_ready": False,
            "observation_sha256": hashlib.sha256(self.native_snapshot.read_bytes()).hexdigest(),
            "editor_log_sha256": hashlib.sha256(self.editor_log.read_bytes()).hexdigest(),
        }
        self.native_wrapper_receipt.write_text(json.dumps(wrapper), encoding="utf-8")
        result = self.run_compare()
        self.assertEqual(result["status"], "DifferencesObserved")
        self.assertEqual(result["native_capture"]["native_session_id"], "native-session")
        self.assertFalse(result["parity_claim"])
        self.assertFalse(result["mission_ready"])

    def test_raw_tamper_is_rejected_even_when_receipt_is_unchanged(self):
        raw = bytearray(self.native_raw.read_bytes())
        raw[-1] ^= 1
        self.native_raw.write_bytes(raw)
        with self.assertRaisesRegex(ValueError, "hash/length"):
            self.run_compare()

    def test_rejects_updated_hash_for_inconsistent_mask_and_counts(self):
        raw = bytearray(self.native_raw.read_bytes())
        raw[60] |= 1  # Cell 0 blocked bit, while its count remains zero.
        self.native_raw.write_bytes(raw)
        self.n["raw_sha256"] = hashlib.sha256(raw).hexdigest()
        self.native_receipt.write_text(json.dumps(self.n), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "mask/count inconsistency"):
            self.run_compare()

    def test_rejects_readiness_or_cross_world_transfer_claims(self):
        self.s["mission_ready"] = True
        self.source_receipt.write_text(json.dumps(self.s), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "disclaim admission"):
            self.run_compare()

    def test_keeps_independent_session_ids_and_rejects_grid_frame_mismatch(self):
        self.s["source_session_id"] = "separate-source-host-session"
        self.source_receipt.write_text(json.dumps(self.s), encoding="utf-8")
        self.refresh_source_worker_binding()
        result = self.run_compare()
        self.assertTrue(result["sessions_are_independent"])
        self.assertEqual(result["native_capture"]["native_session_id"], "native-session")
        self.assertEqual(result["source_capture"]["source_session_id"], "separate-source-host-session")
        self.s["origin"] = [4.0, 0.0, 0.0]
        self.source_receipt.write_text(json.dumps(self.s), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "receipt/header mismatch"):
            self.run_compare()

    def test_rejects_native_wrapper_snapshot_tampering(self):
        receipt = json.loads(self.native_wrapper_receipt.read_text())
        receipt["snapshot_sha256"] = "0" * 64
        self.native_wrapper_receipt.write_text(json.dumps(receipt), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "wrapper receipt, snapshot or editor log"):
            self.run_compare()

    def test_rejects_unbound_source_worker_result_or_build_inputs(self):
        worker = json.loads(Path(json.loads(self.source_run_receipt.read_text())["worker_result"]["path"]).read_text())
        worker["source_blocker_occupancy_raw_capture"]["sha256"] = "0" * 64
        worker_path = Path(json.loads(self.source_run_receipt.read_text())["worker_result"]["path"])
        worker_path.write_text(json.dumps(worker), encoding="utf-8")
        run_receipt = json.loads(self.source_run_receipt.read_text())
        run_receipt["worker_result"]["sha256"] = hashlib.sha256(worker_path.read_bytes()).hexdigest()
        self.source_run_receipt.write_text(json.dumps(run_receipt), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "worker result does not bind"):
            self.run_compare()


if __name__ == "__main__":
    unittest.main()
