import hashlib
import contextlib
import io
import json
import tempfile
import unittest
from pathlib import Path
import sys
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import run_s002_semantic_production_move as runner


class S002SemanticProductionMoveRunnerTests(unittest.TestCase):
    def test_aria_classification_requires_trace_and_unqualified_real_inputs(self):
        repo = Path(__file__).resolve().parents[3]
        receipt = json.loads((repo / "T Logs/SkirmishProduction/s002-current-package-semantic-run-20261104-T10/semantic-production-move-receipt.json").read_bytes())
        result = receipt["source_result"]
        descriptor = result["s002_current_source_diagnostic_package"]
        result.pop("semantic_command_control_test")
        result["status"] = "BoundedPrefix"
        with tempfile.TemporaryDirectory() as directory:
            worker = Path(directory)
            classify = lambda value=result, **kw: runner._classify_source_aria(value, descriptor, worker,
                kw.get("code", 0), kw.get("timed_out", False), kw.get("pins_match", True), kw.get("preflight", True))
            self.assertFalse(classify())
            (worker / "aria-trace.jsonl").write_text('{}\n')
            self.assertTrue(classify())
            self.assertFalse(classify(timed_out=True))
            self.assertFalse(classify(pins_match=False))
            self.assertFalse(classify(preflight=False))
            self.assertFalse(classify(code=1))
            altered = dict(result, semantic_command_control_test={})
            self.assertFalse(classify(altered))
            altered = dict(result, s002_current_source_diagnostic_package={})
            self.assertFalse(classify(altered))

    def test_historical_clearance_requires_exact_captured_record(self):
        repo = Path(__file__).resolve().parents[3]
        projection = repo / "T Logs/SkirmishProduction/s002-clearance-projection-20261104-T1/static-clearance.json"
        descriptor = runner.validate_historical_clearance_projection(projection, repo)
        self.assertEqual(runner.sha(projection.read_bytes()), descriptor["sha256"])
        with tempfile.TemporaryDirectory() as directory:
            tampered = Path(directory) / "clearance.json"
            value = json.loads(projection.read_bytes())
            value["source_record"]["bounds"][0]["center"][0] += 1
            tampered.write_text(json.dumps(value))
            with self.assertRaises(ValueError):
                runner.validate_historical_clearance_projection(tampered, repo)

    def test_current_owner_package_and_copied_r10_bundle_are_validated(self):
        repo = Path(__file__).resolve().parents[3]
        bundle = repo / runner.CURRENT_BUNDLE
        package = repo / runner.CURRENT_PACKAGE
        package_sha = runner.sha(package.read_bytes())
        report, pins, loaded = runner.audit_current_package_inputs(
            bundle, package, package_sha, repo)
        self.assertEqual("Passed", report["status"])
        self.assertFalse(report["worker_admitted"])
        self.assertFalse(report["mission_ready"])
        self.assertEqual(8, len(loaded["building_rows"]))
        self.assertEqual(12, len(loaded["unit_rows"]))
        self.assertEqual(runner.COPIED_R10_INITIALIZATION_SHA,
                         pins[str((repo / runner.COPIED_R10_INITIALIZATION).resolve())])
        self.assertEqual("c82539fe56368ce70fb3ecc669647ac75c7d2678e5392354b2d0e2c98cf48eba",
                         pins[str((repo / "Logs/SkirmishProduction/s002-derived-seed-20261103-r2/data/initialization.json").resolve())])
        for relative, digest in runner.HISTORICAL_INPUTS.items():
            self.assertEqual(digest, pins[str((repo / relative).resolve())])

    def test_current_owner_package_lane_rejects_alias_or_digest_mismatch(self):
        repo = Path(__file__).resolve().parents[3]
        package_sha = runner.sha((repo / runner.CURRENT_PACKAGE).read_bytes())
        with self.assertRaises(ValueError):
            runner.audit_current_package_inputs(repo / runner.CURRENT_BUNDLE,
                                                repo / runner.CURRENT_PACKAGE,
                                                "0" * 64, repo)
        with tempfile.TemporaryDirectory() as directory:
            alias = Path(directory) / "package.json"
            alias.write_bytes((repo / runner.CURRENT_PACKAGE).read_bytes())
            with self.assertRaises(ValueError):
                runner.audit_current_package_inputs(repo / runner.CURRENT_BUNDLE,
                                                    alias, package_sha, repo)

    def test_diagnostic_build_requires_attested_manifest_and_generated_source(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            build_path = root / "build" / "build.json"
            source = root / "build" / "source" / "generated" / "S002CurrentSelectedMapGraphBuildAttestation.cs"
            source.parent.mkdir(parents=True)
            source.write_text("attested graph and package digests")
            repo = Path(__file__).resolve().parents[3]
            manifest = repo / runner.DIAGNOSTIC_SOURCE_MANIFEST
            manifest_raw = manifest.read_bytes()
            expected_source_sha = runner.sha(source.read_bytes())
            build = {
                "canonical_source_manifest": {"path": str((repo / runner.CANONICAL_SOURCE_MANIFEST).resolve()),
                                               "sha256": runner.sha((repo / runner.CANONICAL_SOURCE_MANIFEST).read_bytes()),
                                               "bytes": (repo / runner.CANONICAL_SOURCE_MANIFEST).stat().st_size},
                "source_manifest": {"path": str(manifest.resolve()), "sha256": runner.sha(manifest_raw),
                                    "bytes": len(manifest_raw)},
                "source_and_reference_pins": {str(source): expected_source_sha},
            }
            self.assertEqual([], runner.diagnostic_attestation_build_mismatches(
                build, build_path, expected_source_sha, runner.sha(manifest_raw), len(manifest_raw), repo))

            bad_build = json.loads(json.dumps(build))
            alias_manifest = root / "alias-source-manifest.json"
            alias_manifest.write_bytes(manifest_raw)
            bad_build["source_manifest"]["path"] = str(alias_manifest)
            self.assertIn("Diagnostic_manifest_attestation_source_mismatch",
                          [row["reason"] for row in runner.diagnostic_attestation_build_mismatches(
                              bad_build, build_path, expected_source_sha, runner.sha(manifest_raw), len(manifest_raw), repo)])

            bad_build = json.loads(json.dumps(build))
            bad_build["source_and_reference_pins"][str(source)] = "0" * 64
            self.assertIn("Build_attestation_source_digest_mismatch",
                          [row["reason"] for row in runner.diagnostic_attestation_build_mismatches(
                              bad_build, build_path, expected_source_sha, runner.sha(manifest_raw), len(manifest_raw), repo)])

    def test_main_passes_rederived_manifest_descriptor_to_build_gate(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            bundle = root / "bundle.json"
            package = root / "package.json"
            bundle.write_text("{}")
            package.write_text("{}")
            build_path = root / "build.json"
            build_path.write_text(json.dumps({"status": "Passed", "mission": "S002", "seed": 104731,
                                              "source_and_reference_pins": {}}))
            audit = {"status": "Passed", "attestation_source_sha256": "a" * 64,
                     "source_manifest_sha256": "b" * 64, "source_manifest_bytes": 321}
            loaded = {"path": str(package), "sha256": "c" * 64}
            seen = {}

            def build_gate(build, received_build_path, attestation_sha, manifest_sha, manifest_bytes, repo):
                seen.update({"build": build, "build_path": received_build_path,
                             "attestation_sha": attestation_sha, "manifest_sha": manifest_sha,
                             "manifest_bytes": manifest_bytes, "repo": repo})
                return [{"reason": "test_stop_before_prepare_or_worker"}]

            output = root / "output"
            with patch.object(runner, "audit_current_package_inputs", return_value=(audit, {}, loaded)), \
                    patch.object(runner, "diagnostic_attestation_build_mismatches", autospec=True,
                                 side_effect=build_gate) as gate, \
                    contextlib.redirect_stdout(io.StringIO()):
                code = runner.main([
                    "--repo", str(root), "--bundle", str(bundle),
                    "--current-source-package", str(package), "--current-source-package-sha256", "c" * 64,
                    "--build-receipt", str(build_path), "--output", str(output),
                    "--max-wait-ticks", "1", "--max-ticks", "5", "--timeout", "1",
                ])
            self.assertEqual(2, code)
            gate.assert_called_once()
            self.assertEqual("a" * 64, seen["attestation_sha"])
            self.assertEqual("b" * 64, seen["manifest_sha"])
            self.assertEqual(321, seen["manifest_bytes"])
            self.assertEqual(build_path.resolve(), seen["build_path"])
            receipt = json.loads((output / "semantic-production-move-receipt.json").read_text())
            self.assertFalse(receipt["worker_launched"])

    def test_historical_scope_is_independent_and_false_by_construction(self):
        result = {
            "historical_input_classification": {
                "kind": "HistoricalInputOnlyDiagnostic_Unqualified", "historical_input_epoch": "2026-11-01",
                "historical_producer_digest_bytes_verified": False, "historical_producer_digest_only_count": 7,
                "preserved_source_pin_count": 6526, "preservation_project_file_count": 6522,
                "preservation_external_pin_count": 4, "historical_navigation_row_count": 4954,
                "historical_navigation_rows_materialized": 4954,
                "historical_navigation_rows_materialization_complete": True,
                "physical_sha256": "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
                "water_sha256": "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5",
                "startup_sha256": "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03",
                "preservation_sha256": "68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297",
                **{key: False for key in ("current_source_equivalence", "current_blocker_equivalence",
                                          "current_surface_equivalence", "physical_source_fit", "water_equivalence",
                                          "scene_fit_certified", "mission_admitted", "mission_ready",
                                          "native_world_created", "native_scene_loaded", "native_input_observed",
                                          "player_order_observed", "validated_victory")},
            }
        }
        self.assertTrue(runner._valid_historical_classification(result))
        result["historical_input_classification"]["mission_ready"] = True
        self.assertFalse(runner._valid_historical_classification(result))

    def test_historical_semantic_success_requires_all_navigation_rows_materialized(self):
        repo = Path(__file__).resolve().parents[3]
        classification = {
            "kind": "HistoricalInputOnlyDiagnostic_Unqualified", "historical_input_epoch": "2026-11-01",
            "historical_producer_digest_bytes_verified": False, "historical_producer_digest_only_count": 7,
            "preserved_source_pin_count": 6526, "preservation_project_file_count": 6522,
            "preservation_external_pin_count": 4, "historical_navigation_row_count": 4954,
            "historical_navigation_rows_materialized": 4954,
            "historical_navigation_rows_materialization_complete": True,
            "physical_sha256": "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
            "water_sha256": "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5",
            "startup_sha256": "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03",
            "preservation_sha256": "68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297",
            **{key: False for key in ("current_source_equivalence", "current_blocker_equivalence",
                                      "current_surface_equivalence", "physical_source_fit", "water_equivalence",
                                      "scene_fit_certified", "mission_admitted", "mission_ready",
                                      "native_world_created", "native_scene_loaded", "native_input_observed",
                                      "player_order_observed", "validated_victory")},
        }
        self.assertTrue(runner._valid_historical_classification({"historical_input_classification": classification}))
        for field, value in (("historical_navigation_rows_materialized", None),
                             ("historical_navigation_rows_materialized", 4953),
                             ("historical_navigation_rows_materialization_complete", False)):
            mutated = dict(classification)
            mutated[field] = value
            with self.subTest(field=field, value=value):
                self.assertFalse(runner._valid_historical_classification({"historical_input_classification": mutated}))

    def test_historical_placement_graft_is_performed_by_current_package_derivation(self):
        repo = Path(__file__).resolve().parents[3]
        import s002_current_source_diagnostic_package as package_reader
        package_path = repo / runner.BASE_VALIDATED_PACKAGE
        loaded = package_reader.load_current_s002_package(package_path, runner.BASE_VALIDATED_PACKAGE_SHA, repo)
        derived_package = json.loads((repo / runner.CURRENT_PACKAGE).read_bytes())
        loaded["value"]["source_config_graph"] = derived_package["source_config_graph"]
        binding = package_reader.load_s002_historical_placement_binding(repo)
        base_path = repo / runner.COPIED_R10_INITIALIZATION
        base = json.loads(base_path.read_text())
        base_sha = runner.sha(base_path.read_bytes())
        derived, _ = package_reader.derive_current_s002_initialization(
            base, str(base_path), base_sha, loaded, binding)
        base_derived, _ = package_reader.derive_current_s002_initialization(
            base, str(base_path), base_sha, loaded)
        derivation = package_reader.make_s002_historical_placement_derivation(binding)
        self.assertTrue(package_reader.require_s002_historical_placement_derivation(derived, base, binding, derivation))
        self.assertEqual(base["placement_domain"]["s002_diagnostic_capture"],
                         derived["placement_domain"]["s002_diagnostic_capture"])
        self.assertEqual(binding["placement_descriptor"],
                         derived["placement_domain"]["s002_original_owner_diagnostic"])
        self.assertEqual(base_derived["source_epoch"], derived["source_epoch"])
        self.assertNotIn("s002_historical_placement_derivation", derived)
        with self.assertRaises(ValueError):
            package_reader.derive_current_s002_initialization(
                base, str(base_path), base_sha, loaded, {**binding, "placement_descriptor": {}})
        omitted = json.loads(json.dumps(derived))
        omitted["placement_domain"].pop("s002_original_owner_diagnostic")
        with self.assertRaises(ValueError):
            package_reader.require_s002_historical_placement_derivation(
                omitted, base, binding, package_reader.make_s002_historical_placement_derivation(binding))

    def test_outer_runner_source_pin_detects_mutation(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / "runner.py"
            source.write_text("version one")
            pin = runner.pin_runner_source(source)
            self.assertTrue(runner.runner_source_pin_matches(pin, source))
            source.write_text("version two")
            self.assertFalse(runner.runner_source_pin_matches(pin, source))

    def test_actual_qualified_s002_bundle_is_blocked_by_current_transitive_source_pins(self):
        repo = Path(__file__).resolve().parents[3]
        bundle = Path("/private/tmp/s002-source-owner-rebind-qualified-20261102-r1/data-bundle.json")
        qualification = Path("/private/tmp/s002-source-owner-rebind-qualified-20261102-r1/rebind-receipt.json")
        if not bundle.is_file() or not qualification.is_file():
            self.skipTest("Current session's retained qualified S002 bundle is unavailable")
        audit, _ = runner.audit_reachable_inputs(bundle, qualification, repo)
        self.assertEqual("Failed", audit["status"])
        self.assertFalse(audit["worker_admitted"])
        capture_map = next(row for row in audit["source_pin_map_audits"]
                           if "prefab-reference-data.json#source_pins" in row["map"])
        self.assertEqual(7967, capture_map["source_pin_count"])
        self.assertGreater(capture_map["mismatch_count"], 0)
        stale_paths = {row["path"] for row in audit["mismatches"]}
        self.assertIn(str(repo / "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs"), stale_paths)
        self.assertIn(str(repo / "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs"), stale_paths)
        self.assertGreater(audit["missing_source_pin_count"], 0)

    def test_transitive_capture_source_pin_drift_is_detected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "authored.cs"
            original = b"source version used by capture"
            source.write_bytes(original)
            capture = root / "prefab-reference-data.json"
            capture.write_text(json.dumps({"source_pins": {str(source): hashlib.sha256(original).hexdigest()}}))
            initialization = root / "initialization.json"
            init = {"reference_templates": {
                "path": str(root / "template-domain.json"), "sha256": "0" * 64,
                "source_reference_path": str(capture), "source_reference_sha256": hashlib.sha256(capture.read_bytes()).hexdigest()
            }}
            template = root / "template-domain.json"
            template.write_text(json.dumps({"kind": "FocusedTemplateDomain", "source_reference_sha256": init["reference_templates"]["source_reference_sha256"]}))
            init["reference_templates"]["path"] = str(template)
            init["reference_templates"]["sha256"] = hashlib.sha256(template.read_bytes()).hexdigest()
            initialization.write_text(json.dumps(init))
            bundle = root / "data-bundle.json"
            bundle_value = {"schema_version": 1, "mission": "S002", "seed": 104731,
                            "runtime_backend": "LegacyGroundS002V1",
                            "initialization": {"path": str(initialization), "sha256": hashlib.sha256(initialization.read_bytes()).hexdigest()}}
            bundle.write_text(json.dumps(bundle_value))
            qualification = root / "rebind-receipt.json"
            qualification.write_text(json.dumps({
                "kind": "S002QualifiedFocusedPrefabBundleRebind",
                "qualified_bundle_path": str(bundle), "qualified_bundle_sha256": hashlib.sha256(bundle.read_bytes()).hexdigest(),
                "qualified_initialization_path": str(initialization), "qualified_initialization_sha256": hashlib.sha256(initialization.read_bytes()).hexdigest(),
                "mission_ready": False, "native_visual_readiness": False
            }))

            source.write_bytes(b"changed after capture")
            audit, pins = runner.audit_reachable_inputs(bundle, qualification, root)

            self.assertEqual("Failed", audit["status"])
            self.assertFalse(audit["worker_admitted"])
            self.assertEqual(hashlib.sha256(original).hexdigest(), pins[str(source.resolve())])
            self.assertEqual([str(source.resolve())], [row["path"] for row in audit["mismatches"]])

    def test_success_classifier_requires_receipt_spawn_selection_move_and_observed_delta(self):
        result = {"semantic_command_control_test": {
            "stage": "ObservedPositionChange",
            "request_and_receipts": {
                "accepted": True, "receipt_id": 17,
                "actual_paid_reservation": {"reservation_id": 17},
                "spawn": {"receipt_id": 17, "spawn_succeeded": True, "entity_index": 44},
                "selection": {"source_return": True, "spawned_entity_selected": True,
                              "all_live_group_members_from_request_receipt": True},
                "movement": {"accepted": True, "spawned_entity_has_actual_move_intent": True, "intent_active": True}
            },
            "position_observations": [{"position_changed_from_pre_move": True}]
        }}
        self.assertTrue(runner._classify(result))
        result["semantic_command_control_test"]["position_observations"][0]["position_changed_from_pre_move"] = False
        self.assertFalse(runner._classify(result))

    def test_presentation_free_fixture_is_never_production_validated(self):
        run_requests = [
            {"presentation_free_delivery_fixture": None},
            {"presentation_free_delivery_fixture": 1},
            {"presentation_free_delivery_fixture": {"kind": "S002SingleRifleKnownNullTransportCamera"}},
        ]
        for run in run_requests:
            with self.subTest(run=run):
                self.assertFalse(runner._production_run_admits_fixture(run))
        self.assertTrue(runner._production_run_admits_fixture({"mission": "S002", "initialization": {"path": "/any"}}))

        valid_sequence = {"semantic_command_control_test": {
            "stage": "ObservedPositionChange",
            "request_and_receipts": {
                "accepted": True, "receipt_id": 17,
                "actual_paid_reservation": {"reservation_id": 17},
                "spawn": {"receipt_id": 17, "spawn_succeeded": True, "entity_index": 44},
                "selection": {"source_return": True, "spawned_entity_selected": True,
                              "all_live_group_members_from_request_receipt": True},
                "movement": {"accepted": True, "spawned_entity_has_actual_move_intent": True, "intent_active": True}
            },
            "position_observations": [{"position_changed_from_pre_move": True}]
        }}
        for marker in (
            {"fixture_only": True},
            {"transport_camera_fixture": {"kind": "ExplicitPresentationFreeS002NullWorldCameraFixture"}},
        ):
            with self.subTest(marker=marker):
                self.assertFalse(runner._classify({**valid_sequence, **marker}))


if __name__ == "__main__":
    unittest.main()
