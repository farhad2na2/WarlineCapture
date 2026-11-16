import copy
import importlib.util
from pathlib import Path
import unittest


MODULE_PATH = Path(__file__).with_name("project_s003_current_factory_metadata.py")
SPEC = importlib.util.spec_from_file_location("s003_current_factory_projection", MODULE_PATH)
PROJECTOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROJECTOR)


class S003CurrentFactoryMetadataProjectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.capture = PROJECTOR._json(PROJECTOR.ROOT / PROJECTOR.CAPTURE_REL)
        cls.pins = cls.capture["source_pins"]
        cls.factory_sources = PROJECTOR.read_factory_sources(PROJECTOR.ROOT)
        cls.capture_sha = PROJECTOR.EXPECTED["capture_sha256"]

    def project(self, capture=None, capture_sha=None):
        return PROJECTOR.project_payload(
            copy.deepcopy(capture if capture is not None else self.capture),
            capture_sha if capture_sha is not None else self.capture_sha,
            self.pins,
            self.factory_sources,
        )

    def test_actual_capture_projects_three_existing_consumer_shapes(self):
        artifacts = self.project()
        self.assertEqual(set(artifacts), {
            "building-definitions.json", "unit-production-metadata.json", "configured-metadata.json"
        })
        definitions = artifacts["building-definitions.json"]
        units = artifacts["unit-production-metadata.json"]
        configured = artifacts["configured-metadata.json"]
        self.assertEqual(definitions["kind"], "NativeQualifiedSkirmishBuildingDefinitions")
        self.assertEqual(units["kind"], "NativeQualifiedSkirmishUnitProductionMetadata")
        self.assertEqual(configured["kind"], "NativeQualifiedConfiguredBuildingMetadata")
        self.assertEqual(len(definitions["definitions"]), 10)
        self.assertEqual(len(units["units"]), 19)
        self.assertEqual(len(units["transports"]), 1)
        self.assertEqual(len(configured["rows"]), 10)
        self.assertEqual(definitions["native_origin"]["artifact_sha256"], self.capture_sha)
        self.assertEqual(definitions["native_origin"]["metadata_key_sha256"], PROJECTOR.EXPECTED["metadata_key_sha256"])
        self.assertEqual(units["source_pins"], self.pins)
        self.assertEqual(set(configured["source_pins"]), {
            item for rel in self.factory_sources for item in (rel, rel + ".meta")
        })
        for artifact in artifacts.values():
            self.assertEqual(artifact["unsupported_values"], [])
            self.assertIs(artifact["full_mission_initialized"], False)
            self.assertIs(artifact["native_visual_readiness"], False)

    def test_current_capture_qualifies_against_run6_and_source_pin_closures(self):
        root = PROJECTOR.ROOT
        compiler_root = root / "T Logs/SkirmishProduction/s003-current-final-gate-compiler-20261103-T13"
        build_receipt = root / "T Logs/SkirmishProduction/s002-current-s003-final-gate-build-20261103-T13/build.json"
        graph_closure = root / PROJECTOR.CURRENT_COMPILER_GRAPH_REL
        required = [compiler_root / "receipt.json", compiler_root / "post-run-pin-audit.json", build_receipt, graph_closure]
        if not all(path.is_file() for path in required):
            self.skipTest("Optional workspace-specific current S003 compiler proof inputs are absent")
        if PROJECTOR._sha_file(graph_closure) != PROJECTOR.EXPECTED["current_compiler_graph_sha256"]:
            self.skipTest("Optional workspace proof predates the strict grid/surface graph extension")
        compiler_receipt = PROJECTOR._json(compiler_root / "receipt.json")
        if compiler_receipt.get("source_graph", {}).get("sha256") != PROJECTOR.EXPECTED["current_compiler_graph_sha256"]:
            self.skipTest("Optional workspace compiler receipt predates the strict grid/surface graph extension")
        evidence = PROJECTOR.validate_current_evidence(root, *required)
        self.assertEqual(evidence["qualification_receipt"]["setup_hash"], 2394754492)
        self.assertEqual(evidence["qualification_receipt"]["run6_receipt_sha256"], PROJECTOR.EXPECTED["run6_receipt_sha256"])
        self.assertEqual(evidence["current_compiler"]["graph_closure_sha256"], PROJECTOR.EXPECTED["current_compiler_graph_sha256"])
        self.assertNotEqual(evidence["current_compiler"]["graph_closure_sha256"], PROJECTOR.EXPECTED["run6_graph_sha256"])
        PROJECTOR.verify_pins(PROJECTOR.ROOT, self.pins)

    def test_augmented_compiler_graph_is_distinct_from_capture_and_run6_graphs(self):
        path = PROJECTOR.ROOT / PROJECTOR.CURRENT_COMPILER_GRAPH_REL
        graph = PROJECTOR._json(path)
        PROJECTOR.validate_augmented_compiler_graph(graph, PROJECTOR._sha_file(path))
        self.assertEqual(PROJECTOR._sha_file(path), PROJECTOR.EXPECTED["current_compiler_graph_sha256"])
        self.assertNotEqual(PROJECTOR.EXPECTED["current_compiler_graph_sha256"], PROJECTOR.EXPECTED["run6_graph_sha256"])
        self.assertEqual(graph["catalog_closure_provenance"]["base_s003_graph_sha256"], PROJECTOR.EXPECTED["graph_sha256"])
        self.assertEqual(len(graph["source_pins"]), 147)
        selection_extension, grid_extension = PROJECTOR.validate_source_graph_extensions(PROJECTOR.ROOT, graph, path)
        self.assertEqual(selection_extension["path"], str((PROJECTOR.ROOT / PROJECTOR.SOURCE_SELECTION_EXTENSION_REL).resolve()))
        self.assertEqual(selection_extension["sha256"], PROJECTOR.EXPECTED["source_selection_extension_receipt_sha256"])
        self.assertEqual(grid_extension["path"], str((PROJECTOR.ROOT / PROJECTOR.SOURCE_GRID_SURFACE_EXTENSION_REL).resolve()))
        self.assertEqual(grid_extension["sha256"], PROJECTOR.EXPECTED["source_grid_surface_extension_receipt_sha256"])
        self.assertEqual(len(graph["nodes"]), 37)
        self.assertEqual(PROJECTOR._sha_file(PROJECTOR.ROOT / PROJECTOR.SELECTION_COMPILER_GRAPH_REL), PROJECTOR.EXPECTED["selection_compiler_graph_sha256"])

    def test_t26_receipt_producer_is_exact_and_mutated_provenance_fails(self):
        receipt_path = PROJECTOR.ROOT / PROJECTOR.SOURCE_GRID_SURFACE_EXTENSION_REL
        receipt = PROJECTOR._json(receipt_path)
        PROJECTOR.validate_extension_producer(PROJECTOR.ROOT, receipt)
        mutated = copy.deepcopy(receipt)
        mutated["producer"]["sha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "producer provenance"):
            PROJECTOR.validate_extension_producer(PROJECTOR.ROOT, mutated)
        self.assertEqual(receipt["negative_controls"], [
            {"case": "tampered-asset-bytes", "result": "RejectedAsExpected"},
            {"case": "script-guid-class-type-mismatch", "result": "RejectedAsExpected"},
            {"case": "source-backed-class-outside-game-configs", "result": "RejectedAsExpected"},
            {"case": "malformed-packed-surface-payload", "result": "RejectedAsExpected"},
            {"case": "duplicate-guid-target", "result": "AmbiguousAsExpected"},
            {"case": "lexical-path-escape", "result": "RejectedAsExpected"},
        ])

    def test_source_adapter_is_pinned_to_t26_and_checks_producer(self):
        adapter = (PROJECTOR.ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceConfigurationQualification.s003.cs").read_text(encoding="utf-8")
        self.assertIn("s003-source-graph-extensions-20261103-T26/grid-surface/source-config-graph-with-grid-surface-nodes.json", adapter)
        self.assertIn("3bb17b71c7be1defb607ec3fef98ae947a8a8618cb19333b5b2284effc95ceaa", adapter)
        self.assertIn("68c6e6a3c4538837a11c78003479306db5db12368f4f84804bdfa9baa3924d69", adapter)
        self.assertIn("AutonomousWorld.PinnedFile(producerPath,producerSha,4*1024*1024)", adapter)
        self.assertIn("source-backed-class-outside-game-configs", adapter)
        self.assertNotIn("s003-final-runtime-source-graph-20261103-T1", adapter)

    def test_selector_extension_mutations_are_rejected(self):
        path = PROJECTOR.ROOT / PROJECTOR.SELECTION_COMPILER_GRAPH_REL
        graph = PROJECTOR._json(path)
        mutated = copy.deepcopy(graph)
        mutated["source_pins"].pop(next(iter(set(mutated["source_pins"]) - set(
            PROJECTOR._json(PROJECTOR.ROOT / PROJECTOR.BASE_COMPILER_GRAPH_REL)["source_pins"]
        ))))
        with self.assertRaisesRegex(ValueError, "node and pin counts|base/extension pin counts|exact base plus selector"):
            PROJECTOR.validate_source_selection_extension(PROJECTOR.ROOT, mutated, path)
        mutated = copy.deepcopy(graph)
        mutated["nodes"] = mutated["nodes"][:-1]
        with self.assertRaisesRegex(ValueError, "node and pin counts|exact base plus selector"):
            PROJECTOR.validate_source_selection_extension(PROJECTOR.ROOT, mutated, path)

    def test_grid_surface_extension_mutations_are_rejected(self):
        path = PROJECTOR.ROOT / PROJECTOR.CURRENT_COMPILER_GRAPH_REL
        graph = PROJECTOR._json(path)
        base = PROJECTOR._json(PROJECTOR.ROOT / PROJECTOR.SELECTION_COMPILER_GRAPH_REL)
        mutated = copy.deepcopy(graph)
        mutated["source_pins"].pop(next(iter(set(mutated["source_pins"]) - set(base["source_pins"]))))
        with self.assertRaisesRegex(ValueError, "base or output counts|exactly the e498 graph plus four"):
            PROJECTOR.validate_source_grid_surface_extension(PROJECTOR.ROOT, mutated, path)
        mutated = copy.deepcopy(graph)
        mutated["nodes"] = mutated["nodes"][:-1]
        with self.assertRaisesRegex(ValueError, "base or output counts|exactly the two expected"):
            PROJECTOR.validate_source_grid_surface_extension(PROJECTOR.ROOT, mutated, path)

    def test_base_or_historical_graph_cannot_substitute_for_selector_extension(self):
        base_path = PROJECTOR.ROOT / PROJECTOR.BASE_COMPILER_GRAPH_REL
        base = PROJECTOR._json(base_path)
        with self.assertRaisesRegex(ValueError, "selector-extended canonical-catalog identity"):
            PROJECTOR.validate_augmented_compiler_graph(base, PROJECTOR._sha_file(base_path))
        current = PROJECTOR._json(PROJECTOR.ROOT / PROJECTOR.CURRENT_COMPILER_GRAPH_REL)
        with self.assertRaisesRegex(ValueError, "selector-extended canonical-catalog identity"):
            PROJECTOR.validate_augmented_compiler_graph(current, PROJECTOR.EXPECTED["run6_graph_sha256"])

    def test_historical_run6_graph_cannot_substitute_for_current_compiler_graph(self):
        graph = PROJECTOR._json(PROJECTOR.ROOT / PROJECTOR.CURRENT_COMPILER_GRAPH_REL)
        with self.assertRaisesRegex(ValueError, "selector-extended canonical-catalog identity"):
            PROJECTOR.validate_augmented_compiler_graph(graph, PROJECTOR.EXPECTED["run6_graph_sha256"])

    def test_current_catalog_membership_mutation_is_rejected(self):
        graph = copy.deepcopy(PROJECTOR._json(PROJECTOR.ROOT / PROJECTOR.CURRENT_COMPILER_GRAPH_REL))
        graph["canonical_catalog_augmentation"]["catalog_membership_audit"]["row"]["DefinitionId"] = "skirmish.s004"
        with self.assertRaisesRegex(ValueError, "actual BattleCatalog membership"):
            PROJECTOR.validate_augmented_compiler_graph(graph, PROJECTOR.EXPECTED["current_compiler_graph_sha256"])

    def test_wrong_capture_hash_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "capture hash"):
            self.project(capture_sha="0" * 64)

    def test_wrong_target_seed_is_rejected(self):
        capture = copy.deepcopy(self.capture)
        capture["context"]["seed"] += 1
        with self.assertRaisesRegex(ValueError, "compiler context"):
            self.project(capture)

    def test_metadata_digest_drift_is_rejected(self):
        capture = copy.deepcopy(self.capture)
        capture["context"]["metadata"]["buildings"][0]["fields"]["CanRequest"] = True
        with self.assertRaisesRegex(ValueError, "metadata digest"):
            self.project(capture)

    def test_duplicate_native_unit_identity_is_rejected(self):
        capture = copy.deepcopy(self.capture)
        capture["context"]["metadata"]["units"][1]["prefab"]["key"] = capture["context"]["metadata"]["units"][0]["prefab"]["key"]
        capture["context"]["metadata_key_sha256"] = PROJECTOR._sha_bytes(
            __import__("json").dumps(capture["context"]["metadata"], separators=(",", ":"), ensure_ascii=False).encode("utf-8")
        )
        with self.assertRaisesRegex(ValueError, "unit order or identity"):
            self.project(capture)

    def test_readiness_promotion_is_rejected(self):
        capture = copy.deepcopy(self.capture)
        capture["mission_ready"] = True
        with self.assertRaisesRegex(ValueError, "false scope flag"):
            self.project(capture)

    def test_configured_projection_requires_native_request_witness(self):
        capture = copy.deepcopy(self.capture)
        del capture["context"]["metadata"]["buildings"][0]["configured_entry"]["fields"]["CanRequest"]["value"]
        capture["context"]["metadata_key_sha256"] = PROJECTOR._sha_bytes(
            __import__("json").dumps(capture["context"]["metadata"], separators=(",", ":"), ensure_ascii=False).encode("utf-8")
        )
        with self.assertRaisesRegex(ValueError, "configured building request witness"):
            self.project(capture)


if __name__ == "__main__":
    unittest.main()
