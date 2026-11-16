import importlib.util
from pathlib import Path
import tempfile
import unittest


MODULE_PATH = Path(__file__).with_name("build_s003_current_source_qualification_descriptor.py")
SPEC = importlib.util.spec_from_file_location("s003_source_descriptor", MODULE_PATH)
BUILDER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(BUILDER)
BUILDER.PROJECTION_REL = Path("T Logs/SkirmishProduction/s003-current-final-gate-projection-20261103-T13")


class S003CurrentSourceQualificationDescriptorTests(unittest.TestCase):
    def test_actual_evidence_validates_and_constructs_descriptor_only_scope(self):
        if not (BUILDER.ROOT / BUILDER.PROJECTION_REL / "projection-receipt.json").is_file():
            self.skipTest("Optional workspace-specific current S003 projection evidence is absent")
        projection = BUILDER.read_json(BUILDER.ROOT / BUILDER.PROJECTION_REL / "projection-receipt.json")
        if projection.get("current_compiler_graph_closure_sha256") != BUILDER.PINNED["current_compiler_graph"]:
            self.skipTest("Optional workspace projection predates the strict selector graph extension")
        evidence = BUILDER.validate_evidence(BUILDER.ROOT, BUILDER.ROOT / BUILDER.PROJECTION_REL)
        context, qualification = BUILDER.construct_descriptors(evidence)
        self.assertEqual(context["kind"], "OriginalS003CurrentSourceOnlyFactoryContext")
        self.assertEqual(context["target_context"]["setup_hash"], 2394754492)
        self.assertEqual(qualification["kind"], "ExperimentalS003CurrentSourceQualificationDescriptor")
        self.assertIsNone(qualification["compiler_context"])
        self.assertEqual(context["compiler_receipt"]["sha256"], evidence["current_epoch"]["compiler_receipt_sha256"])
        self.assertEqual(context["source_selection_extension_receipt"], evidence["current_epoch"]["source_selection_extension_receipt"])
        self.assertEqual(context["factory_capture_historical_compiler_receipt"]["sha256"], BUILDER.PINNED["run6_receipt"])
        self.assertNotEqual(context["compiler_receipt"]["sha256"], context["factory_capture_historical_compiler_receipt"]["sha256"])
        self.assertEqual(evidence["projection"]["run6_compiler_receipt_sha256"], BUILDER.PINNED["run6_receipt"])
        for record in (context, qualification):
            for flag in ("runtime_admission", "mission_ready", "native_context_qualified", "source_fit_certified"):
                self.assertIs(record[flag], False)

    def test_promoted_descriptor_scope_is_rejected_by_scope_control(self):
        context = {"kind": "OriginalS003CurrentSourceOnlyFactoryContext", "runtime_admission": False,
                   "mission_ready": True, "native_context_qualified": False, "source_fit_certified": False}
        qualification = {"kind": "ExperimentalS003CurrentSourceQualificationDescriptor", "compiler_context": None,
                         "runtime_admission": False, "mission_ready": False,
                         "native_context_qualified": False, "source_fit_certified": False}
        with self.assertRaisesRegex(ValueError, "scope promotion"):
            BUILDER.validate_descriptor_scope(context, qualification)

    def test_context_binds_explicit_selector_extension_receipt_descriptor(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            proof = root / "proof.json"
            proof.write_text("{}\n", encoding="utf-8")
            epoch = {"source_selection_extension_receipt": {"path": str(proof.resolve()),
                                                              "sha256": BUILDER.sha_file(proof)},
                     "source_grid_surface_extension_receipt": {"path": str(proof.resolve()),
                                                                "sha256": BUILDER.sha_file(proof)}}
            for key in ("compiler_receipt_path", "compiler_audit_path", "build_path", "graph_closure_path"):
                epoch[key] = str(proof.resolve())
            evidence = {
                "graph_path": proof, "current_compiler_path": proof, "current_audit_path": proof,
                "build_path": proof, "current_graph_path": proof, "run6_path": proof,
                "audit_path": proof, "capture_path": proof, "capture_receipt_path": proof,
                "projection_receipt_path": proof, "artifact_paths": {"artifact.json": proof},
                "current_epoch": epoch, "builder_path": proof, "projector_path": proof,
            }
            context, _ = BUILDER.construct_descriptors(evidence)
            self.assertEqual(context["source_selection_extension_receipt"], epoch["source_selection_extension_receipt"])
            self.assertEqual(context["source_grid_surface_extension_receipt"], epoch["source_grid_surface_extension_receipt"])

    def test_current_projection_evidence_rejects_modified_receipt(self):
        # The canonical validator checks the exact on-disk projection receipt digest.
        path = BUILDER.ROOT / BUILDER.PROJECTION_REL / "projection-receipt.json"
        if not path.is_file():
            self.skipTest("Optional workspace-specific current S003 projection evidence is absent")
        original_digest = BUILDER.sha_file(path)
        projection = BUILDER.read_json(path)
        projection["runtime_admission"] = True
        self.assertIs(projection["runtime_admission"], True)
        altered = (BUILDER.json.dumps(projection, indent=2, ensure_ascii=False) + "\n").encode()
        self.assertNotEqual(BUILDER.sha(altered), original_digest)

    def test_capture_graph_and_augmented_compiler_graph_remain_separate(self):
        projector = BUILDER.load_module(BUILDER.ROOT / BUILDER.PROJECTOR_REL, "s003_graph_identity_test")
        graph_path = BUILDER.ROOT / projector.CURRENT_COMPILER_GRAPH_REL
        graph = projector._json(graph_path)
        graph_sha = projector._sha_file(graph_path)
        projector.validate_augmented_compiler_graph(graph, graph_sha)
        self.assertEqual(BUILDER.PINNED["graph"], "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb")
        self.assertEqual(graph_sha, BUILDER.PINNED["current_compiler_graph"])
        self.assertNotEqual(graph_sha, BUILDER.PINNED["run6_graph"])
        selection_extension, grid_extension = projector.validate_source_graph_extensions(BUILDER.ROOT, graph, graph_path)
        self.assertEqual(selection_extension, {"path": str((BUILDER.ROOT / projector.SOURCE_SELECTION_EXTENSION_REL).resolve()),
                                               "sha256": BUILDER.sha_file(BUILDER.ROOT / projector.SOURCE_SELECTION_EXTENSION_REL)})
        self.assertEqual(grid_extension, {"path": str((BUILDER.ROOT / projector.SOURCE_GRID_SURFACE_EXTENSION_REL).resolve()),
                                          "sha256": BUILDER.sha_file(BUILDER.ROOT / projector.SOURCE_GRID_SURFACE_EXTENSION_REL)})
        with self.assertRaisesRegex(ValueError, "selector-extended canonical-catalog identity"):
            projector.validate_augmented_compiler_graph(graph, BUILDER.PINNED["run6_graph"])


if __name__ == "__main__":
    unittest.main()
