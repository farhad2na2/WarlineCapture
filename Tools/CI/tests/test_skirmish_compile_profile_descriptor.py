"""Synthetic metadata controls, not compiler/native receipts. Sparse fixtures use immutable Git blobs."""
import copy
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SOURCE = Path(__file__).resolve().parents[1] / "skirmish_compile_profile_descriptor.py"
SPEC = importlib.util.spec_from_file_location("compile_profile_descriptor", SOURCE)
profile = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(profile)
PROJECT = SOURCE.parents[2]


def context():
    return dict(platform="Windows", editor_version="6000.5.8f1", target="Editor",
                defines=None, tests_included=None, **{key: None for key in profile.CONTEXT_REFS})


def synthetic_ref(name):
    # Intentionally nonexistent artifact, explicitly unverified by the descriptor.
    return dict(path="Logs/synthetic/" + name + ".json", sha256="a" * 64)


def observation(layer="native-selected-rsp", state="ReportedFreshCsc"):
    return dict(layer=layer, owner="Game.Editor", state=state, epoch=None,
                refs={key: None for key in profile.EVIDENCE_REFS}, source_paths=[], input_drift=False)


class DescriptorControls(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory(prefix="warline-profile-controls-")
        cls.root = Path(cls.temp.name).resolve()
        # Only exact assembly metadata, never Library, CSharp source, or a native receipt.
        for path in profile.METADATA_PINS:
            for relative in (path, path + ".meta"):
                raw = subprocess.check_output(
                    ["git", "show", profile.AUTHORITY_COMMIT + ":" + relative], cwd=PROJECT)
                target = cls.root / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(raw)
        cls.definitions = profile.metadata(cls.root)

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    def make(self, ctx=None, rows=None):
        return profile.make_descriptor(self.root, context() if ctx is None else ctx, rows)

    def reject_changed(self, value, mutate):
        changed = copy.deepcopy(value)
        mutate(changed)
        with self.assertRaises(ValueError):
            profile.validate_descriptor(changed, self.root, value["descriptor_sha256"])

    def test_exact_layers_stay_metadata_only(self):
        value = self.make()
        native = value["layers"]["native-selected-rsp"]
        clean = value["layers"]["isolated-owning-graph"]
        self.assertEqual({x["owner"] for x in native["owners"]}, set(profile.NATIVE))
        self.assertEqual({x["owner"] for x in clean["owners"]}, set(profile.CLEAN))
        self.assertEqual((native["owner_count"], clean["owner_count"]), (17, 26))
        self.assertEqual(set(profile.CLEAN) - set(profile.NATIVE), set(profile.EXTRA))
        self.assertIsNone(value["layers"]["legacy-offline24"]["owners"])
        self.assertIsNone(value["layers"]["legacy-offline24"]["owner_count"])
        self.assertEqual(value["flags"], profile.FLAGS)
        self.assertTrue(value["flags"]["metadata_only"])
        self.assertFalse(any(v for k, v in value["flags"].items() if k != "metadata_only"))
        self.assertEqual(profile.validate_descriptor(value, self.root, value["descriptor_sha256"])["status"],
                         "MetadataOnly_PendingEvidence")

    def test_same_count_substitution_and_duplicate_owner_refuse(self):
        value = self.make()
        for substitute in ("Game.Operations.Tactical", "Game.Configs"):
            self.reject_changed(value, lambda d: d["layers"]["native-selected-rsp"]["owners"][0].update(owner=substitute))

    def test_counts_and_missing_dependency_cannot_be_caller_authority(self):
        value = self.make()
        self.reject_changed(value, lambda d: d["layers"]["isolated-owning-graph"].update(owner_count=24))
        self.reject_changed(value, lambda d: d["layers"]["isolated-owning-graph"]["owners"].pop())
        self.reject_changed(value, lambda d: d["source_metadata"]["Game.Configs"]["definition"]["references"].remove("Game.Catalog.Contracts"))

    def test_topology_and_external_binary_roles(self):
        value = self.make()
        for name in ("native-selected-rsp", "isolated-owning-graph"):
            layer = value["layers"][name]
            order = {owner: i for i, owner in enumerate(layer["topological_order"])}
            for row in layer["owners"]:
                self.assertTrue(all(order[dep] < order[row["owner"]] for dep in row["owning_dependencies"]))
        editor = next(x for x in value["layers"]["native-selected-rsp"]["owners"] if x["owner"] == "Game.Editor")
        self.assertIn("Game.Catalog.Contracts", editor["referenced_game_binary_dependencies"])
        self.assertTrue(any(x["assembly"] == "Unity.Collections" for x in editor["external_binary_leaves"]))
        self.assertTrue(all(x["status"].startswith("Pending") for x in editor["external_binary_leaves"]))

    def test_nested_operations_test_is_not_parent_test_membership(self):
        path = "Assets/Tests/Editor/Operations/SyntheticControl.cs"
        self.assertEqual(profile.source_owner(path, self.definitions), "Game.Operations.Tests.Editor")
        row = observation()
        row.update(owner="Game.Tests.Editor", source_paths=[path])
        with self.assertRaisesRegex(ValueError, "Nested asmdef"):
            self.make(rows=[row])

    def test_raw_definition_or_guid_bytes_mutation_refuses(self):
        for relative in ("Assets/Game/Scripts/Configs/Game.Configs.asmdef", "Assets/Game/Scripts/Configs/Game.Configs.asmdef.meta"):
            path = self.root / relative
            raw = path.read_bytes()
            try:
                path.write_bytes(raw + b"\n")
                with self.assertRaisesRegex(ValueError, "raw assembly metadata"):
                    self.make()
            finally:
                path.write_bytes(raw)

    def test_new_nested_boundary_cannot_be_ignored(self):
        for suffix in (".asmdef", ".asmref"):
            path = self.root / ("Assets/Game/Scripts/Configs/NewBoundary" + suffix)
            try:
                path.write_text("{}")
                with self.assertRaisesRegex(ValueError, "path membership"):
                    self.make()
            finally:
                path.unlink()

    def test_context_mutation_and_reseal_do_not_match_original_pin(self):
        value = self.make()
        changed = context()
        changed["defines"] = ["SYNTHETIC_DIAGNOSTIC"]
        resealed = self.make(changed)
        self.assertNotEqual(value["descriptor_sha256"], resealed["descriptor_sha256"])
        with self.assertRaisesRegex(ValueError, "pin differs"):
            profile.validate_descriptor(resealed, self.root, value["descriptor_sha256"])
        self.reject_changed(value, lambda d: d["context"].update(target="Android"))

    def test_known_context_cannot_exclude_named_tests(self):
        for key, item in (("defines", ["WARLINE_MAP_NATIVE_TEST_PLAYER"]), ("tests_included", False)):
            ctx = context()
            ctx[key] = item
            with self.assertRaisesRegex(ValueError, "exclude"):
                self.make(ctx)

    def test_rsp_epoch_and_duplicate_observation_refuse(self):
        ctx = context()
        ctx["native_epoch"] = synthetic_ref("current")
        row = observation()
        row["epoch"] = synthetic_ref("old")
        with self.assertRaisesRegex(ValueError, "epoch differs"):
            self.make(ctx, [row])
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            self.make(rows=[observation(), observation()])

    def test_retained_origin_pointer_never_becomes_validated_origin(self):
        row = observation("isolated-owning-graph", "ReportedRetainedCompile")
        value = self.make(rows=[row])
        self.assertIn("Missing:retained_origin", value["observations"][0]["pending"])
        row["refs"]["retained_origin"] = synthetic_ref("origin")
        value = self.make(rows=[row])
        self.assertEqual(value["observations"][0]["status"], "Pending")
        self.assertIn("RetainedOriginAndCompleteInputDependencyJoinsNotValidated", value["observations"][0]["pending"])
        self.assertFalse(value["flags"]["compile_admitted"])

    def test_generated_ref_drift_cannot_be_waived_by_complete_pointers(self):
        row = observation()
        row["input_drift"] = True
        row["refs"] = {key: synthetic_ref(key) for key in profile.EVIDENCE_REFS}
        value = self.make(rows=[row])
        self.assertIn("ReportedInputDrift_NoValidatedPerConsumerWindow", value["observations"][0]["pending"])
        self.assertEqual(value["observations"][0]["status"], "Pending")
        self.assertFalse(value["flags"]["compile_admitted"])

    def test_backend_legacy_profile_and_positive_claim_spoof_refuse(self):
        value = self.make()
        self.reject_changed(value, lambda d: d["protected_legacy_contract"].update(backend_sha256="b" * 64))
        self.reject_changed(value, lambda d: d["flags"].update(compile_admitted=True))
        self.reject_changed(value, lambda d: d["layers"]["legacy-offline24"].update(owners=list(profile.CLEAN), owner_count=26))
        self.reject_changed(value, lambda d: d.update(qualified=True))
        row = observation()
        row["layer"] = "legacy-offline24"
        with self.assertRaisesRegex(ValueError, "Legacy named authority"):
            self.make(rows=[row])

    def test_layer_state_and_unknown_evidence_fields_refuse(self):
        row = observation()
        row["state"] = "ReportedRetainedCompile"
        with self.assertRaisesRegex(ValueError, "Owner/state"):
            self.make(rows=[row])
        row = observation()
        row["passed"] = True
        with self.assertRaisesRegex(ValueError, "observation fields"):
            self.make(rows=[row])
        row = observation()
        row["refs"]["new_truth"] = synthetic_ref("not-allowed")
        with self.assertRaisesRegex(ValueError, "reference keys"):
            self.make(rows=[row])

    def test_local_immutable_output_preserves_existing_artifact(self):
        value = self.make()
        path = self.root / "Logs/SkirmishProduction/descriptor.json"
        receipt = profile.write_immutable(self.root, path, value)
        raw = path.read_bytes()
        self.assertEqual(json.loads(raw), value)
        self.assertEqual(receipt["status"], "MetadataOnly_PendingEvidence")
        with self.assertRaises(FileExistsError):
            profile.write_immutable(self.root, path, value)
        self.assertEqual(path.read_bytes(), raw)
        with self.assertRaisesRegex(ValueError, "Local diagnostics"):
            profile.write_immutable(self.root, self.root / "Assets/generated.json", value)
        path.unlink()

    def test_cli_round_trip_stays_pending_and_rejects_wrong_external_pin(self):
        ctx = self.root / "synthetic-context.json"
        ctx.write_text(json.dumps(context()))
        target = self.root / "Logs/SkirmishProduction/cli-descriptor.json"
        base = [sys.executable, str(SOURCE), "--project", str(self.root)]
        create = subprocess.run(base + ["--context", str(ctx), "--output", str(target)],
                                capture_output=True, text=True, timeout=20)
        self.assertEqual(create.returncode, 0, create.stderr)
        receipt = json.loads(create.stdout)
        self.assertEqual(receipt["status"], "MetadataOnly_PendingEvidence")
        verify = subprocess.run(base + ["--verify", str(target), "--expected-sha256",
                                       receipt["descriptor_sha256"]], capture_output=True, text=True, timeout=20)
        self.assertEqual(verify.returncode, 0, verify.stderr)
        self.assertFalse(json.loads(verify.stdout)["flags"]["compile_admitted"])
        bad = subprocess.run(base + ["--verify", str(target), "--expected-sha256", "0" * 64],
                             capture_output=True, text=True, timeout=20)
        self.assertNotEqual(bad.returncode, 0)
        self.assertIn("Descriptor pin differs", bad.stderr)
        target.unlink()
        ctx.unlink()

    def test_omitted_scope_and_source_traversal_remain_explicit(self):
        value = self.make()
        self.assertEqual(set(value["out_of_scope_game_owners"]), {
            "Game.Operations.Capture", "Game.Operations.Content", "Game.Operations.Loop",
            "Game.Operations.Tactical", "Game.Operations.Tests.Editor"})
        for path in ("../Assets/a.cs", "/Assets/a.cs", "C:/Assets/a.cs", "Assets//a.cs", "Assets/a\\b.cs"):
            with self.assertRaises(ValueError):
                profile.source_owner(path, self.definitions)


if __name__ == "__main__":
    unittest.main()
