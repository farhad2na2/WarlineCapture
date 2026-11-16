import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch


MODULE = Path(__file__).with_name("finalize_s003_native_metadata_capture.py")
SPEC = importlib.util.spec_from_file_location("s003_metadata_receipt", MODULE)
finalizer = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(finalizer)


def digest(data):
    return hashlib.sha256(data).hexdigest()


class S003NativeMetadataReceiptTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.repo = Path(self.temp.name).resolve()
        self.capture_dir = self.repo / "Logs/SkirmishProduction/s003-current-metadata-capture-20261103-r1/data"
        self.capture_dir.mkdir(parents=True)
        self.graph_path = self.repo / "Logs/SkirmishProduction/s003-current-config-graph-20261103-r1/graph-r5/source-config-graph.json"
        self.graph_path.parent.mkdir(parents=True)
        self.capture_path = self.capture_dir / "s003-native-metadata.json"
        self.log_path = self.capture_dir.parent / "editor.log"

        self.graph_asset = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/SkirmishScenario_S003.asset"
        self.catalog_asset = "Assets/Game/Resources/SkirmishExpansionCatalog.asset"
        self._write_pin(self.graph_asset, "scenario-definition")
        self._write_pin(self.catalog_asset, "catalog")
        catalog_sha = digest(b"catalog")
        graph_pins = {
            str((self.repo / self.graph_asset).resolve()): self._sha_for(self.graph_asset),
            str((self.repo / self.catalog_asset).resolve()): catalog_sha,
        }
        (self.repo / self.catalog_asset).write_bytes(b"catalog")
        graph_pins[str((self.repo / self.catalog_asset).resolve())] = digest(b"catalog")
        self.graph = {"schema_version": 1, "authority": "AuthoredConfigAssets", "source_pins": graph_pins}
        graph_raw = json.dumps(self.graph, sort_keys=True).encode()
        self.graph_path.write_bytes(graph_raw)
        self._write_run6_evidence(graph_pins)

        capture_pins = dict(graph_pins)
        capture_pins[self.catalog_asset] = digest(b"catalog")
        capture_pins[str(self.graph_path.resolve())] = digest(graph_raw)
        for rel in finalizer.REQUIRED_OWNER_SOURCES:
            capture_pins[rel] = self._write_pin(rel, rel)
        self.capture = {
            "schema_version": 1,
            "kind": "NativeS003OriginalCompilerFactoryMetadataCapture",
            "scope": "CurrentPackagedS003CompilerAndOriginalMetadataFactories_DataOnly_NoMissionWorld",
            "context": {
                "catalog_id": "S003", "size": "Standard", "difficulty": "Regular", "seed": 104732,
                "setup_hash": finalizer.EXPECTED_SETUP_HASH,
                "wrong_context_hash_rejected": True, "wrong_target_seed_rejected": True,
                "metadata_key_sha256": "a" * 64,
                "metadata": {
                    "catalog_inputs": {"catalog_id": "S003"},
                    "providers": {
                        "baseline_asset": {"guid": "1" * 32, "local_id": 1},
                        "scene_asset": {"guid": "2" * 32, "local_id": 2},
                        "unit_registry_asset": {"guid": "3" * 32, "local_id": 3},
                        "baseline": [], "scene": [], "units": [],
                    },
                    "buildings": [{}], "units": [{}], "recipes": [{}],
                },
                "selected_native_domain": {"building_count": 1, "unit_count": 1, "recipe_count": 1,
                                            "all_recipe_producers_and_units_in_selected_domain": True},
            },
            "source_graph": {"path": str(self.graph_path.resolve()), "sha256": digest(graph_raw)},
            "source_pins": capture_pins,
            "full_mission_initialized": False, "utility_world_created": True,
            "utility_world_disposed": True, "mission_world_created": False,
            "runtime_state_transferred": False, "source_fit_certified": False,
            "native_context_qualified": False, "mission_ready": False,
        }
        self.capture["context"]["metadata_key_sha256"] = digest(json.dumps(
            self.capture["context"]["metadata"], separators=(",", ":"), ensure_ascii=False).encode())
        self._write_capture()
        self.log_path.write_text(
            "COMMAND LINE ARGUMENTS:\n" + finalizer.METHOD + "\n-skirmishS003Metadata\n-skirmishS003Graph\n" +
            finalizer.PASS_MARKER + "\n", encoding="utf-8")
        self.patches = (
            patch.object(finalizer, "GRAPH_RELATIVE_PATH", self.graph_path.relative_to(self.repo)),
            patch.object(finalizer, "CAPTURE_RELATIVE_PATH", self.capture_path.relative_to(self.repo)),
            patch.object(finalizer, "CAPTURE_LOG_RELATIVE_PATH", self.log_path.relative_to(self.repo)),
            patch.object(finalizer, "GRAPH_SHA256", digest(graph_raw)),
            patch.object(finalizer, "EXPECTED_CATALOG_SHA256", catalog_sha),
            patch.object(finalizer, "RUN6_RECEIPT_SHA256", digest((self.repo / finalizer.RUN6_RELATIVE_DIR / "receipt.json").read_bytes())),
            patch.object(finalizer, "RUN6_POST_RUN_AUDIT_SHA256", digest((self.repo / finalizer.RUN6_RELATIVE_DIR / "post-run-pin-audit.json").read_bytes())),
            patch.object(finalizer, "RUN6_GRAPH_SHA256", digest((self.repo / finalizer.RUN6_RELATIVE_DIR / "source-config-graph-with-current-catalog-closure.json").read_bytes())),
            patch.object(finalizer, "RUN6_BUILD_RECEIPT_SHA256", digest((self.repo / "Logs/SkirmishProduction/s004-import-rollback-source-build-20261103-r1/build.json").read_bytes())),
        )
        for item in self.patches:
            item.start()

    def tearDown(self):
        for item in self.patches:
            item.stop()
        self.temp.cleanup()

    def _sha_for(self, rel):
        return digest((self.repo / rel).read_bytes())

    def _write_pin(self, rel, content):
        path = self.repo / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        data = content.encode()
        path.write_bytes(data)
        return digest(data)

    def _write_capture(self):
        self.capture_path.write_text(json.dumps(self.capture), encoding="utf-8")

    def _write_run6_evidence(self, pins):
        run_dir = self.repo / finalizer.RUN6_RELATIVE_DIR
        run_dir.mkdir(parents=True)
        run_graph_path = run_dir / "source-config-graph-with-current-catalog-closure.json"
        run_graph = {"source_pins": pins}
        run_graph_raw = json.dumps(run_graph, sort_keys=True).encode()
        run_graph_path.write_bytes(run_graph_raw)
        build_path = self.repo / "Logs/SkirmishProduction/s004-import-rollback-source-build-20261103-r1/build.json"
        build_path.parent.mkdir(parents=True, exist_ok=True)
        build_path.write_bytes(b"build receipt")
        build_sha = digest(build_path.read_bytes())
        receipt = {
            "kind": "S003OriginalCompilerHarnessReceipt", "status": "Compiled", "seed": 104732,
            "source_graph": {"path": str(run_graph_path.resolve()), "sha256": digest(run_graph_raw)},
            "source_graph_source_pins": pins,
            "source_build": {"path": str(build_path.resolve()), "sha256": build_sha},
            "result": {
                "kind": "S003OriginalCompilerSourceOnlyResult", "compiled": True, "catalog_id": "S003",
                "definition_id": "skirmish.s003", "world_initialized": False,
                "physical_source_fit_verified": False, "mission_readiness": False,
                "setups": [{"size_id": "Standard", "seed": 104732, "difficulty_id": "Regular",
                            "setup_hash": finalizer.EXPECTED_SETUP_HASH}],
            },
        }
        (run_dir / "receipt.json").write_text(json.dumps(receipt), encoding="utf-8")
        audit = {
            "kind": "S003CurrentSourceCompilerPostRunPinAudit", "seed": 104732,
            "source_graph_sha256_expected": digest(run_graph_raw), "source_graph_sha256_after": digest(run_graph_raw),
            "source_graph_pin_mismatches": [], "source_build_receipt_sha256_expected": build_sha,
            "source_build_receipt_sha256_after": build_sha, "source_build_pin_mismatches": [],
            "world_initialized": False, "physical_source_fit_verified": False, "mission_readiness": False,
        }
        (run_dir / "post-run-pin-audit.json").write_text(json.dumps(audit), encoding="utf-8")

    def test_exact_data_only_capture_qualifies(self):
        receipt = finalizer.qualify(self.repo, self.capture_dir, 0)
        self.assertEqual(receipt["setup_hash"], finalizer.EXPECTED_SETUP_HASH)
        self.assertTrue(receipt["utility_world_disposed"])
        self.assertFalse(receipt["mission_ready"])

    def test_wrapper_marker_and_target_fail_closed(self):
        with self.assertRaisesRegex(ValueError, "Successful checked wrapper"):
            finalizer.qualify(self.repo, self.capture_dir, 1)
        self.log_path.write_text("no marker\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Successful checked wrapper"):
            finalizer.qualify(self.repo, self.capture_dir, 0)
        self.log_path.write_text("COMMAND LINE ARGUMENTS:\n" + finalizer.METHOD + "\n-skirmishS003Metadata\n-skirmishS003Graph\n" + finalizer.PASS_MARKER + "\n", encoding="utf-8")
        self.capture["context"]["setup_hash"] += 1
        self._write_capture()
        with self.assertRaisesRegex(ValueError, "run-6 current S003 compiler target"):
            finalizer.qualify(self.repo, self.capture_dir, 0)

    def test_scope_world_and_readiness_claims_fail_closed(self):
        for key, value in (("utility_world_disposed", False), ("mission_world_created", True), ("mission_ready", True)):
            self.capture[key] = value
            self._write_capture()
            with self.assertRaisesRegex(ValueError, "utility World disclosure"):
                finalizer.qualify(self.repo, self.capture_dir, 0)
            self.capture[key] = {"utility_world_disposed": True, "mission_world_created": False, "mission_ready": False}[key]

    def test_graph_and_source_drift_fail_closed(self):
        self._write_pin(self.graph_asset, "changed definition")
        with self.assertRaisesRegex(ValueError, "Capture source changed"):
            finalizer.qualify(self.repo, self.capture_dir, 0)

    def test_missing_native_rejection_control_fails_closed(self):
        self.capture["context"]["wrong_target_seed_rejected"] = False
        self._write_capture()
        with self.assertRaisesRegex(ValueError, "rejection controls"):
            finalizer.qualify(self.repo, self.capture_dir, 0)


if __name__ == "__main__":
    unittest.main()
