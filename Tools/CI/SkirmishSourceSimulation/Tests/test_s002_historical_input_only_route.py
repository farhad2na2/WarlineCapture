import hashlib
import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
CAPTURE_DIR = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1"
PRESERVATION = ROOT / "Logs/SkirmishProduction/s073-captured-source-preservation-20261101-r1/receipt.json"
EXPECTED = {
    "campaign-physical-source.json": "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
    "campaign-water-capture.json": "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5",
    "campaign-startup-observation.json": "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03",
}
BINARIES = {
    "Game.Editor.dll": "d2f0a04e13a2acf5ea7b83ecddb9f6b916147a2691c0de14de1c8c195d86bb2c",
    "Game.Components.dll": "8c58453bb1d9a38b1d9a307a374213545124a1e537ed195856291baa208eddd1",
    "Game.Configs.dll": "2dcc1b5e955a530dc8ce85268d60f2c2e5ee3ef84e57ac1b12ab18d1947ec7e6",
    "Game.Rendering.dll": "305682b87cbdaef0f666b2864a5b65a92fcbb81d3104429b9fc6a9814b8758f6",
    "Unity.Entities.dll": "96ae130581a9ddc1b3212064f50f9d5767bea3ed989ca59c278cd0835502cf2f",
    "Game.Runtime.dll": "94aba4983c636a80ac57760f0d41cd0dbf3a5c0991f43f7528d5ddec0464390a",
    "Game.Composition.dll": "4a71b71b1c5eda926a7ac3f417b7b6021648e2e07f04f1eb5b8b2c7af9b4e1fe",
}


def require_pin_inventory(read_pins, preserved, external):
    binary_rows = {}
    ordinary = {}
    for path, digest in read_pins.items():
        if "/Library/ScriptAssemblies/" in path:
            name = Path(path).name
            if name in binary_rows or BINARIES.get(name) != digest:
                raise ValueError("unrecognized historical producer digest")
            binary_rows[name] = digest
        else:
            ordinary[path] = digest
    if binary_rows != BINARIES:
        raise ValueError("exact seven historical producer digest records required")
    capture_root = "/private/tmp/s005-primary-source-project-r1"
    missing = []
    for path, digest in ordinary.items():
        if path.startswith(capture_root + "/"):
            key = path[len(capture_root) + 1:]
            expected = preserved.get(key)
        elif path in external:
            key = path
            expected = external.get(key)
        elif path.startswith("/private/tmp/"):
            key = path
            expected = external.get(key)
        else:
            key = path
            expected = preserved.get(key)
        if expected != digest:
            missing.append(path)
    if missing:
        raise ValueError("historical pin missing from exact retained inventory: " + missing[0])
    return len(ordinary), len(binary_rows)


def require_existing_diagnostic_match(existing, historical):
    names = {"physical": "physical", "water": "water", "startup": "startup", "relocation": "preservation"}
    if type(existing) is not dict or set(existing) != {"physical", "water", "startup", "relocation"}:
        raise ValueError("exact immutable original-owner diagnostic required")
    for old, new in names.items():
        left, right = existing[old], historical[new]
        if type(left) is not dict or type(right) is not dict or set(left) != {"path", "sha256"} or set(right) != {"path", "sha256"} or left != right:
            raise ValueError("historical input descriptor differs from immutable initialization")


def materialization_facts(source_entity_count, completed_navigation_rows):
    if type(source_entity_count) is not int or source_entity_count < 0:
        raise ValueError("nonnegative observed source entity count required")
    if type(completed_navigation_rows) is not int or not 0 <= completed_navigation_rows <= 4954:
        raise ValueError("completed historical row count outside pinned input domain")
    return {
        "source_host_entities_materialized": source_entity_count > 0,
        "historical_navigation_rows_materialized": completed_navigation_rows,
        "historical_navigation_rows_materialization_complete": completed_navigation_rows == 4954,
    }


class S002HistoricalInputOnlyRouteTests(unittest.TestCase):
    def test_retained_artifact_hashes_and_inventory_are_exact(self):
        for name, digest in EXPECTED.items():
            self.assertEqual(hashlib.sha256((CAPTURE_DIR / name).read_bytes()).hexdigest(), digest)
        preservation = json.loads(PRESERVATION.read_text())
        self.assertEqual(preservation["kind"], "S073CapturedSourcePinPreservation")
        self.assertEqual(preservation["status"], "Passed")
        self.assertEqual(preservation["capture_sha256"], EXPECTED["campaign-physical-source.json"])
        self.assertEqual(preservation["project_files"], 6522)
        self.assertEqual(len(preservation["files"]), 6522)
        self.assertEqual(len(preservation["external_pins_verified_in_place"]), 4)
        physical = json.loads((CAPTURE_DIR / "campaign-physical-source.json").read_text())
        preserved = {row["relative_path"]: row["sha256"] for row in preservation["files"]}
        external = {row["path"]: row["sha256"] for row in preservation["external_pins_verified_in_place"]}
        ordinary, unverified = require_pin_inventory(physical["readPins"], preserved, external)
        self.assertEqual((ordinary, unverified), (6526, 7))

    def test_historical_binary_allowlist_fails_closed(self):
        pins = {"Assets/example.asset": "a" * 64}
        pins.update({"/private/tmp/s005-primary-source-project-r1/Library/ScriptAssemblies/" + name: digest
                     for name, digest in BINARIES.items()})
        require_pin_inventory(pins, {"Assets/example.asset": "a" * 64}, {})
        mutated = dict(pins)
        mutated["/private/tmp/s005-primary-source-project-r1/Library/ScriptAssemblies/Game.Runtime.dll"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "unrecognized historical producer digest"):
            require_pin_inventory(mutated, {"Assets/example.asset": "a" * 64}, {})
        mutated = dict(pins)
        mutated["/private/tmp/s005-primary-source-project-r1/Library/ScriptAssemblies/Unexpected.dll"] = "b" * 64
        with self.assertRaisesRegex(ValueError, "unrecognized historical producer digest"):
            require_pin_inventory(mutated, {"Assets/example.asset": "a" * 64}, {})

    def test_route_is_opt_in_and_default_loader_stays_current_strict(self):
        loader = (ROOT / "Tools/CI/SkirmishSourceSimulation/Placement/SourcePlacementDomain.OriginalOwnerCapture.cs").read_text()
        self.assertIn("LoadS002SourceDiagnosticCore(physicalPath,physicalSha,waterPath,waterSha,startupPath,startupSha,sourceRoot,relocationPath,relocationSha,false)", loader)
        self.assertIn("LoadS002SourceDiagnosticCore(physicalPath,physicalSha,waterPath,waterSha,startupPath,startupSha,sourceRoot,relocationPath,relocationSha,true)", loader)
        self.assertIn("if(!historicalInputOnly)RequireCurrentCapturedDependency", loader)
        for name, digest in BINARIES.items():
            self.assertIn('["' + name + '"]="' + digest + '"', loader)
        program = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceSimulationProgram.cs").read_text()
        self.assertIn("s002_historical_input_diagnostic", program)
        for digest in EXPECTED.values():
            self.assertIn(digest, program)
        for flag in ("current_source_equivalence", "current_blocker_equivalence", "current_surface_equivalence", "physical_source_fit", "water_equivalence", "scene_fit_certified", "mission_admitted", "mission_ready", "native_world_created", "native_scene_loaded", "native_input_observed", "player_order_observed", "validated_victory"):
            self.assertIn('["' + flag + '"] = false', program)
        cli = (ROOT / "Tools/CI/run_skirmish_source_simulation.py").read_text()
        build_runner = (ROOT / "Tools/CI/skirmish_source_build.py").read_text()
        batch = (ROOT / "Tools/CI/run_skirmish_source_batch.py").read_text()
        self.assertIn("--s002-historical-input-diagnostic", cli)
        self.assertIn("s002_historical_input_only", build_runner)
        self.assertIn("mission_victory':None", batch)

    def test_preserved_nonbinary_pin_mutation_is_rejected(self):
        pins = {"Assets/a": "a" * 64, "Assets/b": "b" * 64}
        pins.update({"/private/tmp/s005-primary-source-project-r1/Library/ScriptAssemblies/" + name: digest
                     for name, digest in BINARIES.items()})
        with self.assertRaisesRegex(ValueError, "historical pin missing"):
            require_pin_inventory(pins, {"Assets/a": "a" * 64, "Assets/b": "0" * 64}, {})

    def test_historical_mode_only_overrides_exact_init_capture_descriptors(self):
        bundle = json.loads((ROOT / "Logs/SkirmishProduction/s002-derived-seed-20261103-r2/data/data-bundle.json").read_text())
        initial = json.loads(Path(bundle["initialization"]["path"]).read_text())
        existing = initial["placement_domain"]["s002_original_owner_diagnostic"]
        historical = {"physical": existing["physical"], "water": existing["water"], "startup": existing["startup"], "preservation": existing["relocation"]}
        require_existing_diagnostic_match(existing, historical)
        mutated = json.loads(json.dumps(historical))
        mutated["physical"]["sha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "differs"):
            require_existing_diagnostic_match(existing, mutated)
        mutated = json.loads(json.dumps(existing))
        del mutated["relocation"]
        with self.assertRaisesRegex(ValueError, "exact immutable"):
            require_existing_diagnostic_match(mutated, historical)
        program = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceSimulationProgram.cs").read_text()
        startup = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/307_GeneratedS002StartingWorld.lifetime.cs").read_text()
        self.assertIn("RequireExistingPlacementDescriptorMatch(initial[\"placement_domain\"]", program)
        self.assertIn('(run["s002_historical_input_diagnostic"]!=null?" source_owners_initialized=True payment_not_observed_at_startup=True":" fresh=True source_aria=True same_world_paid=True")', program)
        self.assertIn("RequireExistingPlacementDescriptorMatch(diagnostic,s002HistoricalInputDiagnostic)", startup)

    def test_materialization_classification_requires_observed_owner_state(self):
        before_import = materialization_facts(0, 0)
        self.assertFalse(before_import["source_host_entities_materialized"])
        self.assertEqual(before_import["historical_navigation_rows_materialized"], 0)
        self.assertFalse(before_import["historical_navigation_rows_materialization_complete"])
        after_full_import = materialization_facts(4954, 4954)
        self.assertTrue(after_full_import["source_host_entities_materialized"])
        self.assertEqual(after_full_import["historical_navigation_rows_materialized"], 4954)
        self.assertTrue(after_full_import["historical_navigation_rows_materialization_complete"])
        partial_import = materialization_facts(4954, 4953)
        self.assertTrue(partial_import["source_host_entities_materialized"])
        self.assertFalse(partial_import["historical_navigation_rows_materialization_complete"])
        program = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceSimulationProgram.cs").read_text()
        importer = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/307_GeneratedS002StartingWorld.lifetime.cs").read_text()
        materializer = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/299_GeneratedAuthoredNavigationImporter.cs").read_text()
        self.assertIn("historicalNavigationRowsMaterialized == 4954", program)
        self.assertIn("Classification(world.Entities.HasMaterializedSourceEntities,world.HistoricalNavigationRowsMaterialized)", program)
        self.assertIn("Classification(world?.Entities.HasMaterializedSourceEntities??false,world?.HistoricalNavigationRowsMaterialized??0)", program)
        self.assertIn("count=>HistoricalNavigationRowsMaterialized=count", importer)
        self.assertLess(materializer.index('em.SetName(e,"authored-navigation:"+row.Item1)'), materializer.index("completedRowObserver?.Invoke(completed)"))
        self.assertIn("completedRowObserver?.Invoke(completed)", materializer)

    def test_preserved_t1_correction_sidecar_binds_unmaterialized_failure(self):
        result_path = ROOT / "T Logs/SkirmishProduction/s002-historical-initialization-20261103-T1/result.json"
        sidecar_path = result_path.with_name("classification-correction.json")
        result_raw = result_path.read_bytes()
        sidecar = json.loads(sidecar_path.read_text())
        self.assertEqual(hashlib.sha256(result_raw).hexdigest(), sidecar["original_result"]["sha256"])
        result = json.loads(result_raw)
        self.assertEqual(result["tick"], 0)
        self.assertIn("Metadata dependency changed", result["error"])
        self.assertTrue(result["historical_input_classification"]["source_host_entities_materialized"])
        self.assertFalse(sidecar["correction"]["corrected_value"])
        self.assertEqual(sidecar["correction"]["historical_navigation_rows_materialized"], 0)
        self.assertEqual(sidecar["correction"]["historical_navigation_row_count"], 4954)


if __name__ == "__main__":
    unittest.main()
