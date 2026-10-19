"""Synthetic metadata controls, not compiler/native receipts. Sparse fixtures use immutable Git blobs."""
import copy
import importlib.util
import hashlib
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


# Source-attributed Windows review ee2b60b0b94753302b46e6125512897ed0647765b643624f125ab1db07be7a0f.
# Exact reported bytes reconstructed here; no Windows execution is claimed by these tests.
WINDOWS_META_FIXTURES = (
    ("Assets/Game/Scripts/Editor/Game.Editor.asmdef.meta", "f904b4f32c8634430b43f82380f2405f",
     "067108409c1a06a304e510162bb4b7e387283c0c5f774cfc4193bcc01e804540",
     "9017754e152c81c549ace3e241f970c7f21f6c9fac7b4fb885bb8aff07b084ec"),
    ("Assets/Game/Scripts/Game.Runtime.asmdef.meta", "361d0433287ed4a42bf3fd161956446a",
     "6a7cc30139d637263c214e4f51636b6efc8088fafe8afec58267034a168e7573",
     "840593dd66b6dc031b9740936ac8f7fd4fae4f1f2e61e7a79ca196b6baea203d"),
    ("Assets/Tests/Editor/Game.Tests.Editor.asmdef.meta", "75d949434cfb844baba7f313bdfe96db",
     "e8603d2b95c2c28344a467f5442a64e58d36a6046f3a39d99b99ad217648354b",
     "002c45d5c773129d194c891d0036945d0a5ca3e6ecd2a5b65ad97e92474a3547"),
)


def reported_windows_bytes(guid):
    return ("fileFormatVersion: 2\r\nguid: " + guid + "\r\nAssemblyDefinitionImporter:\r\n"
            "  externalObjects: {}\r\n  userData: \r\n  assetBundleName: \r\n"
            "  assetBundleVariant: \r\n").encode("utf-8")


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
        cls.authority_raw = {relative: (cls.root / relative).read_bytes()
                             for path in profile.METADATA_PINS
                             for relative in (path, path + ".meta")}

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

    def test_three_exact_windows_crlf_fixtures_keep_both_raw_identities(self):
        for path, guid, git_hash, windows_hash in WINDOWS_META_FIXTURES:
            with self.subTest(path=path):
                raw = reported_windows_bytes(guid)
                self.assertEqual(hashlib.sha256(raw).hexdigest(), windows_hash)
                self.assertEqual(hashlib.sha256(self.authority_raw[path]).hexdigest(), git_hash)
                row = profile._checkout_row(path, self.authority_raw[path], raw)
                self.assertEqual(row["classification"], "DifferentRawBytes_CrlfLfEquivalent")
                self.assertEqual(row["immutable_git"]["raw_sha256"], git_hash)
                self.assertEqual(row["checkout_observation"]["raw_sha256"], windows_hash)
                self.assertEqual(row["checkout_observation"]["crlf_count"], 7)
                self.assertFalse(row["raw_identity_equal"])
                self.assertFalse(row["source_identity_proved"])
                self.assertTrue(row["diagnostic_crlf_lf_equivalent"])

    def test_crlf_companion_does_not_relax_strict_producer(self):
        originals = {}
        try:
            for path, guid, _, _ in WINDOWS_META_FIXTURES:
                target = self.root / path
                originals[path] = target.read_bytes()
                target.write_bytes(reported_windows_bytes(guid))
            with self.assertRaisesRegex(ValueError, "raw assembly metadata"):
                self.make()
            report = profile.checkout_provenance(self.root, self.authority_raw.__getitem__)
            actual = {x["path"]: x for x in report["observations"]}
            for path, _, _, _ in WINDOWS_META_FIXTURES:
                self.assertEqual(actual[path]["classification"], "DifferentRawBytes_CrlfLfEquivalent")
            self.assertEqual(report["flags"], profile.FLAGS)
            self.assertFalse(report["native_compile_authority"])
            self.assertEqual(report["status"], "MetadataOnly_PendingCheckoutProvenance")
            with self.assertRaisesRegex(ValueError, "raw assembly metadata"):
                self.make()
        finally:
            for path, raw in originals.items():
                (self.root / path).write_bytes(raw)

    def test_content_guid_whitespace_bare_cr_and_bom_drift_are_not_equivalent(self):
        path, guid, _, _ = WINDOWS_META_FIXTURES[0]
        raw = reported_windows_bytes(guid)
        mutations = [raw.replace(b"guid: f9", b"guid: e9"),
                     raw.replace(b"externalObjects: {}", b"externalObjects: {x: 1}"),
                     raw.replace(b"userData: ", b"userData:  "),
                     raw.replace(b"\r\n", b"\r"), b"\xef\xbb\xbf" + raw]
        for mutated in mutations:
            row = profile._checkout_row(path, self.authority_raw[path], mutated)
            self.assertEqual(row["classification"], "ContentDrift")
            self.assertFalse(row["diagnostic_crlf_lf_equivalent"])
            self.assertFalse(row["source_identity_proved"])

    def test_git_authority_cannot_be_normalized_or_substituted(self):
        path, guid, _, _ = WINDOWS_META_FIXTURES[0]
        windows = reported_windows_bytes(guid)
        for wrong_git in (windows, self.authority_raw[path] + b"\n"):
            with self.assertRaisesRegex(ValueError, "Immutable Git raw authority"):
                profile._checkout_row(path, wrong_git, windows)

    def test_exact_and_missing_checkout_rows_are_still_pending(self):
        path = WINDOWS_META_FIXTURES[0][0]
        exact = profile._checkout_row(path, self.authority_raw[path], self.authority_raw[path])
        missing = profile._checkout_row(path, self.authority_raw[path], None)
        self.assertEqual(exact["classification"], "ExactRawMatch")
        self.assertTrue(exact["raw_identity_equal"])
        self.assertFalse(exact["source_identity_proved"])
        self.assertEqual(missing["classification"], "MissingCheckoutFile")
        self.assertIsNone(missing["checkout_observation"])
        self.assertEqual(missing["status"], "MetadataOnly_PendingCheckoutProvenance")

    def test_companion_cli_is_separate_pending_diagnostic(self):
        result = subprocess.run([sys.executable, str(SOURCE), "--project", str(PROJECT),
                                 "--checkout-provenance"], capture_output=True, text=True, timeout=30)
        self.assertEqual(result.returncode, 0, result.stderr)
        report = json.loads(result.stdout)
        self.assertEqual(report["authority_commit"], profile.AUTHORITY_COMMIT)
        self.assertEqual(len(report["observations"]), len(profile.METADATA_PINS) * 2)
        self.assertEqual(report["flags"], profile.FLAGS)
        self.assertEqual(report["status"], "MetadataOnly_PendingCheckoutProvenance")

    def test_companion_cannot_substitute_for_descriptor_or_mixed_cli_mode(self):
        report = profile.checkout_provenance(self.root, self.authority_raw.__getitem__)
        with self.assertRaisesRegex(ValueError, "Descriptor pin differs"):
            profile.validate_descriptor(report, self.root, report["provenance_sha256"])
        mixed = subprocess.run([sys.executable, str(SOURCE), "--project", str(PROJECT),
                                "--checkout-provenance", "--output", "not-written.json"],
                               capture_output=True, text=True, timeout=20)
        self.assertNotEqual(mixed.returncode, 0)
        self.assertIn("Checkout diagnostic mode only", mixed.stderr)

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
