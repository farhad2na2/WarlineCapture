import copy
import importlib.util
import json
import unittest
from pathlib import Path
from unittest import mock


ROOT = Path(__file__).resolve().parents[4]
MANIFEST_SCRIPT = ROOT / "Tools/CI/make_s073_ui_shell_state_owner_manifest.py"
GENERATOR_SCRIPT = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_state_owner/generate.py"


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


MANIFEST = load("make_s073_ui_shell_state_owner_manifest", MANIFEST_SCRIPT)


class UiShellStateOwnerManifestTests(unittest.TestCase):
    def fixture(self):
        decision = {
            "profile": {"mission": "S002", "seed": 104731, "numeric_runtime": "InstalledMono"},
            "source_profile_variants": [{"mission": "S073", "seed": 104729,
                "runtime_backend": "LegacyS073OwnerV1", "scope": "LegacyStartupAndQueueOnly"}],
            "compiler_defines": ["WARLINE_SOURCE_S073"],
            "sources": [
                {"id": MANIFEST.RUNNER_ID, "kind": "native_direct", "path": MANIFEST.RUNNER_PATH},
                {"id": MANIFEST.HOST_TYPES_ID, "kind": "finite_adapter", "path": MANIFEST.HOST_TYPES_PATH,
                 "role": "engine_boundary", "contains_original_gameplay": False,
                 "manual_scope": "Finite source metadata marker types",
                 "scope_declaration_is_not_semantic_proof": True},
            ],
        }
        build = {"kind": "SkirmishSourceSimulationBuild", "status": "Passed", "compile_exit": 0,
            "mission": "S073", "seed": 104729, "runtime_backend": "LegacyS073OwnerV1",
            "native_or_player_launched": False}
        provenance = {"build_path": "/pinned/r3/build.json", "build_sha256": "a" * 64,
            "decision_path": "/pinned/r3/decision.json", "decision_sha256": "b" * 64,
            "canonical_manifest_path": "/canonical/manifest.json", "canonical_manifest_sha256": "c" * 64,
            "baseline_pin_count": 3896}
        return decision, build, provenance

    def test_derivation_is_opt_in_and_preserves_base_profile_identity(self):
        decision, build, provenance = self.fixture()
        before = copy.deepcopy(decision)
        with mock.patch.object(MANIFEST, "validate_base", return_value=provenance):
            derived, _ = MANIFEST.derive(decision, build, repo=Path("."))
        self.assertEqual(decision, before)
        self.assertEqual(derived["profile"], before["profile"])
        self.assertEqual(derived["source_profile_variants"], before["source_profile_variants"])
        self.assertEqual(derived["compiler_defines"][:-1], before["compiler_defines"])
        self.assertEqual(derived["compiler_defines"][-1], MANIFEST.DEFINE)
        self.assertEqual(derived["experimental_source_profile"]["id"], MANIFEST.PROFILE_ID)
        self.assertIn("matching explicit run/environment admission", derived["experimental_source_profile"]["scope"])
        provenance = {row["id"]: row for row in derived["derived_source_row_provenance"]}
        original_adapter = provenance[MANIFEST.HOST_TYPES_ID]["original_row"]
        derived_adapter = next(row for row in derived["sources"] if row["id"] == MANIFEST.HOST_TYPES_ID)
        self.assertEqual(original_adapter["role"], "engine_boundary")
        self.assertEqual(original_adapter["manual_scope"], "Finite source metadata marker types")
        self.assertEqual(derived_adapter["role"], original_adapter["role"])
        self.assertEqual(derived_adapter["manual_scope"], original_adapter["manual_scope"])
        self.assertNotIn(MANIFEST.DEFINE, before["compiler_defines"])

    def test_deriver_is_pinned_to_r3_and_refuses_existing_profile_rows(self):
        self.assertIn("s073-original-aria-probe-inputs-20261103-r3", MANIFEST.BASE_DECISION_REL.as_posix())
        self.assertIn("s073-original-aria-probe-build-20261103-r3", MANIFEST.BASE_BUILD_REL.as_posix())
        decision, build, provenance = self.fixture()
        decision["compiler_defines"].append(MANIFEST.DEFINE)
        with mock.patch.object(MANIFEST, "validate_base", return_value=provenance):
            with self.assertRaises(ValueError):
                MANIFEST.derive(decision, build, repo=Path("."))

    def test_generated_call_requires_both_explicit_admissions_before_legacy_startup(self):
        generator = load("s073_ui_shell_state_owner_generator", GENERATOR_SCRIPT)
        source = (ROOT / generator.RUNNER).read_text()
        self.assertEqual(source.count(generator.HOST_CONSTRUCTION), 1)
        self.assertIn('run["s073_ui_shell_state_owner_diagnostic"]', generator.OWNER_ADMISSION)
        self.assertIn('GetEnvironmentVariable("WARLINE_S073_UI_SHELL_OWNER_DIAGNOSTIC")', generator.OWNER_ADMISSION)
        self.assertIn("if (uiShellStateOwnerDiagnosticRequested)", generator.OWNER_ADMISSION)
        self.assertIn("SourceS073UiShellStateOwnerBootstrap.Initialize(em, host)", generator.OWNER_ADMISSION)
        self.assertLess(source.index(generator.HOST_CONSTRUCTION), source.index("host.PrepareFreshInitialRoads"))
        self.assertNotIn("SourceS073UiShellStateOwnerBootstrap.Initialize", source)

    def test_owner_transform_excludes_only_gateway_registration(self):
        generator = load("s073_ui_shell_state_owner_generator_boundary", GENERATOR_SCRIPT)
        owner = (ROOT / generator.OWNER).read_text()
        self.assertEqual(owner.count(generator.REGISTRATION), 1)
        self.assertIn("struct UiShellStateSystem", owner)
        self.assertIn("OnCreate", owner)
        self.assertEqual(generator.REGISTRATION_BOUNDARY.count("RegisterAsRuntimeGateway"), 0)
        generator_text = GENERATOR_SCRIPT.read_text()
        self.assertIn("owner_host = _replace_once(owner, REGISTRATION, REGISTRATION_BOUNDARY", generator_text)
        self.assertIn("owner_host.replace(REGISTRATION_BOUNDARY, REGISTRATION, 1) != owner", generator_text)

    def test_default_manifest_is_not_a_derivation_target(self):
        source = MANIFEST_SCRIPT.read_text()
        self.assertIn("--output", source)
        self.assertIn("Output must be fresh", source)
        self.assertNotIn("write_text", source.split("def derive(", 1)[1].split("def main(", 1)[0])
        canonical = ROOT / MANIFEST.CANONICAL_MANIFEST_REL
        self.assertTrue(canonical.is_file())


if __name__ == "__main__":
    unittest.main()
