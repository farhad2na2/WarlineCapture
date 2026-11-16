import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


MODULE_PATH = Path(__file__).with_name("finalize_s073_synthetic_loading_capture.py")
SPEC = importlib.util.spec_from_file_location("s073_synthetic_loading_capture", MODULE_PATH)
module = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(module)


def digest(data):
    return hashlib.sha256(data).hexdigest()


class S073SyntheticLoadingCaptureTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name).resolve()
        self.directory = self.root / "capture"
        self.directory.mkdir()
        self.repo = self.root / "repo"
        self.repo.mkdir()
        self.observer_path = self.directory / "observer.json"
        self.log_path = self.directory / "editor.log"
        self.pins_path = self.directory / "source-pins.json"
        self.observer = {
            "kind": "S073NativeLoadingActivationObservation",
            "schema_version": 1,
            "scope": "SameNativeSessionMatchStartAndAutomaticLoadingGateTransition_NotNormalInputAttributionOrMissionReadiness",
            "result": "Observed",
            "session_id": "0123456789abcdef0123456789abcdef",
            "normal_input_invoked_by_observer": False,
            "launch_input_attribution": "NotObserved",
            "samples": [
                self.sample("Queued", None, entity=[337, 1]),
                self.sample("Queued", "WaitingForMatchLoaded", entity=[337, 1], loaded=True),
                self.sample("Preparing", "Starting", entity=[337, 1], loaded=True),
                self.sample("Playing", "Starting", entity=[337, 1], loaded=True,
                            complete=True, active=1, play=1),
                self.sample("Playing", "Started", entity=[337, 1], loaded=True,
                            complete=True, has_started=1, active=1, play=1),
            ],
        }
        self.make_pins()
        self.write_capture()

    def tearDown(self):
        self.temp.cleanup()

    @staticmethod
    def sample(phase, status, entity, loaded=False, complete=False,
               has_started=False, active=0, play=0):
        return {
            "session_id": "0123456789abcdef0123456789abcdef",
            "scenario_index": 3,
            "seed": 104729,
            "session_entity": entity,
            "phase": phase,
            "match_start_status": status,
            "match_start_has_started": has_started,
            "match_scene_loaded": loaded,
            "gameplay_start_complete": complete,
            "simulation_active": active,
            "play_requested": play,
        }

    def make_pins(self):
        pins = {}
        for relative in module.PIN_PATHS:
            path = self.repo / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            content = ("fixture:" + relative).encode()
            path.write_bytes(content)
            pins[relative] = digest(content)
        self.pins_path.write_text(json.dumps({
            "provenance": "PostCaptureCurrentTree",
            "source_pins_sha256": pins,
        }), encoding="utf-8")

    def write_capture(self, marker_path=None):
        marker_path = marker_path or self.observer_path
        self.observer_path.write_text(json.dumps(self.observer), encoding="utf-8")
        log = (
            f"[S073SyntheticLoadingActivationProbe] result=Started scope=SyntheticPlacementProbe normalInput=0 output={marker_path}\n"
            f"[S073SyntheticLoadingActivationProbe] result=Passed scope=SyntheticPlacementProbe normalInput=0 reason=Observed output={marker_path}\n"
        )
        self.log_path.write_text(log, encoding="utf-8")

    def test_passes_only_ordered_same_session_synthetic_native_transition(self):
        receipt = module.validate(self.repo, self.directory, 0)
        self.assertEqual(receipt["validation_status"], "Passed")
        self.assertEqual(receipt["session_id"], self.observer["session_id"])
        self.assertEqual(receipt["session_entity"], [337, 1])
        self.assertEqual(receipt["source_pin_provenance"], "PostCaptureCurrentTree")
        self.assertTrue(receipt["synthetic_probe"])
        self.assertFalse(receipt["normal_input_playthrough"])
        self.assertFalse(receipt["mission_ready"])
        self.assertEqual(set(receipt["source_pins_sha256"]), set(module.PIN_PATHS))
        self.assertEqual(receipt["observer_sha256"], digest(self.observer_path.read_bytes()))
        self.assertEqual(receipt["editor_log_sha256"], digest(self.log_path.read_bytes()))

    def test_requires_transition_order_and_stable_session_seed_entity(self):
        self.observer["samples"].reverse()
        self.write_capture()
        with self.assertRaisesRegex(ValueError, "Ordered native"):
            module.validate(self.repo, self.directory, 0)
        self.observer["samples"].reverse()
        self.observer["samples"][-1]["seed"] = 104730
        self.write_capture()
        with self.assertRaisesRegex(ValueError, "changed session"):
            module.validate(self.repo, self.directory, 0)

    def test_requires_synthetic_markers_and_current_source_pins(self):
        self.log_path.write_text("normal-input pass\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Started marker"):
            module.validate(self.repo, self.directory, 0)
        self.write_capture()
        changed = self.repo / module.PIN_PATHS[0]
        changed.write_text("changed", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Capture source changed"):
            module.validate(self.repo, self.directory, 0)

    def test_wrapper_timeout_stays_incomplete_without_transition_claim(self):
        receipt = module.validate(self.repo, self.directory, 124)
        self.assertEqual(receipt["validation_status"], "Incomplete")
        self.assertEqual(receipt["wrapper_exit_code"], 124)
        self.assertIsNone(receipt["session_id"])
        self.assertFalse(receipt["mission_ready"])

    def test_loading_must_precede_completion_and_pre_run_provenance_is_explicit(self):
        for sample in self.observer["samples"]:
            sample["match_scene_loaded"] = False
        self.write_capture()
        with self.assertRaisesRegex(ValueError, "Ordered native"):
            module.validate(self.repo, self.directory, 0)
        self.observer["samples"][1]["match_scene_loaded"] = True
        self.observer["samples"][2]["match_scene_loaded"] = True
        self.observer["samples"][3]["match_scene_loaded"] = True
        self.observer["samples"][4]["match_scene_loaded"] = True
        pin_manifest = json.loads(self.pins_path.read_text(encoding="utf-8"))
        pin_manifest["provenance"] = "PreRunCapture"
        self.pins_path.write_text(json.dumps(pin_manifest), encoding="utf-8")
        self.write_capture()
        receipt = module.validate(self.repo, self.directory, 0)
        self.assertEqual(receipt["source_pin_provenance"], "PreRunCapture")

    def test_rejects_normal_input_or_readiness_attribution(self):
        self.observer["launch_input_attribution"] = "NormalInput"
        self.write_capture()
        with self.assertRaisesRegex(ValueError, "attribution"):
            module.validate(self.repo, self.directory, 0)

    def test_durable_copy_must_match_original_output_named_by_markers(self):
        original = self.root / "private-tmp-original.json"
        self.observer_path.write_text(json.dumps(self.observer), encoding="utf-8")
        original.write_bytes(self.observer_path.read_bytes())
        self.write_capture(marker_path=original)
        receipt = module.validate(self.repo, self.directory, 0, original_output_path=original)
        self.assertEqual(receipt["observer_sha256"], receipt["original_observer_output_sha256"])
        self.assertEqual(receipt["original_observer_output_path"], str(original))

        original.write_text("different output", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "byte-identical"):
            module.validate(self.repo, self.directory, 0, original_output_path=original)


if __name__ == "__main__":
    unittest.main()
