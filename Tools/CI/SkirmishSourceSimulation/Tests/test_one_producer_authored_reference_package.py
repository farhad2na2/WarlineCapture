import hashlib
import json
import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
PACKAGE_JSON = ROOT / "Assets/Game/Resources/OneProducerAuthoredReferencePackage.json"
PACKAGE_ASSET = ROOT / "Assets/Game/Resources/OneProducerAuthoredReferencePackage.asset"


class OneProducerAuthoredReferencePackageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.package = json.loads(PACKAGE_JSON.read_text())
        cls.asset = PACKAGE_ASSET.read_text()

    def test_generic_scope_and_exact_source_pin_bytes(self):
        p = self.package
        self.assertEqual(p["kind"], "OneProducerAuthoredReferencePackage")
        self.assertEqual(p["scope"], "GenericAuthoredReferenceOnly")
        self.assertTrue(all(row["qualification"] == "GenericAuthoredReferenceWitness"
                            for row in (p["producer"], p["unit"], p["transport"])))
        self.assertEqual(p["sourcePinPaths"], sorted(p["sourcePinPaths"]))
        self.assertTrue(all(path.startswith("Assets/") and ".." not in path and "\\\\" not in path
                            for path in p["sourcePinPaths"]))
        self.assertEqual(len(p["sourcePinPaths"]), len(p["sourcePinSha256"]))
        for rel, digest in zip(p["sourcePinPaths"], p["sourcePinSha256"]):
            raw = (ROOT / rel).read_bytes()
            self.assertEqual(hashlib.sha256(raw).hexdigest(), digest, rel)

    def test_serialized_references_match_package_rows(self):
        p = self.package
        refs = {
            "producerPrefabReference": (p["producer"]["localId"], p["producer"]["guid"], 3),
            "unitPrefabReference": (p["unit"]["localId"], p["unit"]["guid"], 3),
            "transportPrefabReference": (p["transport"]["localId"], p["transport"]["guid"], 3),
            "producerConfigReference": (p["producer"]["configLocalId"], p["producer"]["configGuid"], 2),
            "unitConfigReference": (p["unit"]["configLocalId"], p["unit"]["configGuid"], 2),
            "transportConfigReference": (p["transport"]["configLocalId"], p["transport"]["configGuid"], 2),
        }
        for field, (local_id, guid, kind) in refs.items():
            pattern = rf"(?m)^\s*{re.escape(field)}: \{{fileID: {local_id}, guid: {guid}, type: {kind}\}}$"
            self.assertRegex(self.asset, pattern)

    def test_authored_slot_and_effective_config_refs_are_pinned(self):
        p = self.package
        producer = (ROOT / p["producer"]["path"]).read_text()
        self.assertIn(f"propertyPath: productions.Array.data[{p['recipeSlot']}].quantity\n      value: {p['quantity']}", producer)
        self.assertIn(f"propertyPath: productions.Array.data[{p['recipeSlot']}].spawnUnitPrefab", producer)
        self.assertIn(f"objectReference: {{fileID: {p['unit']['localId']}, guid: {p['unit']['guid']}, type: 3}}", producer)
        for row in (p["producer"], p["unit"], p["transport"]):
            meta = (ROOT / (row["path"] + ".meta")).read_text()
            config_meta = (ROOT / (row["configPath"] + ".meta")).read_text()
            self.assertRegex(meta, rf"(?m)^guid: {row['guid']}$")
            self.assertRegex(config_meta, rf"(?m)^guid: {row['configGuid']}$")

    def test_native_and_source_consumers_are_opt_in_and_share_contract(self):
        catalog_asset = (ROOT / "Assets/Game/Resources/SkirmishExpansionCatalog.asset").read_text()
        self.assertNotIn("productionInputPackage:", catalog_asset)
        native = (ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionCatalog.cs").read_text()
        self.assertIn("TryGetRolePrefabs", native)
        self.assertNotIn("Game.Authoring", native)
        self.assertNotIn("OneProducerAuthoredReferencePackageConfig", native)
        native_binding = (ROOT / "Assets/Game/Scripts/Composition/SkirmishProductionNativeReferenceBinding.cs").read_text()
        self.assertIn("packageConfig.IsBoundToNativeReferences", native_binding)
        self.assertIn("recipe.spawnUnitPrefab != unit", native_binding)
        self.assertIn("recipe.quantity != overlay.SquadMembers", native_binding)
        self.assertIn("matchingSetupRoles != 1", native_binding)
        self.assertIn("catalog.TryRole", native_binding)
        self.assertIn("overlay.RoleKind != SkirmishRoleKind.Rifle", native_binding)
        native_config = (ROOT / "Assets/Game/Scripts/Configs/Skirmish/OneProducerAuthoredReferencePackageConfig.cs").read_text()
        self.assertIn("ReferenceEquals(producerPrefabReference, producer)", native_config)
        source = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceSimulationProgram.cs").read_text()
        self.assertIn('run.Property("authored_reference_package")', source)
        self.assertIn("OneProducerAuthoredReferencePackageReader.Load", source)
        self.assertIn("PrepareActualProductionInputs(25,0x12345678u,transport.TryMetadata,authoredPackage)", source)
        match_view = (ROOT / "Assets/Game/Scripts/Composition/MatchSceneView.Skirmish.cs").read_text()
        self.assertIn("SkirmishProductionNativeReferenceBinding.Create", match_view)

    def test_native_serialized_adapter_is_excluded_from_source_host_manifests(self):
        for manifest_name in ("source_manifest.plan.json", "source_manifest.s003.plan.json",
                              "source_manifest.s026.plan.json", "source_manifest.s073-ai-experimental.plan.json"):
            manifest = json.loads((ROOT / "Tools/CI/SkirmishSourceSimulation" / manifest_name).read_text())
            paths = [row.get("path") for row in manifest.get("sources", [])]
            self.assertNotIn("Assets/Game/Scripts/Configs/Skirmish/OneProducerAuthoredReferencePackageConfig.cs", paths)
            self.assertNotIn("Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogConfig.ProductionInputPackage.cs", paths)
            declaration_rows = [row for row in manifest["sources"]
                                if row.get("path") == "Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogConfig.cs"]
            self.assertEqual(len(declaration_rows), 1)
            self.assertIn("public sealed partial class SkirmishExpansionCatalogConfig",
                          declaration_rows[0]["require_declarations"])
        expansion = (ROOT / "Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogConfig.cs").read_text()
        self.assertNotIn("OneProducerAuthoredReferencePackageConfig", expansion)
        self.assertIn("sealed partial class SkirmishExpansionCatalogConfig", expansion)
        native_partial = (ROOT / "Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogConfig.ProductionInputPackage.cs").read_text()
        self.assertIn("OneProducerAuthoredReferencePackageConfig ProductionInputPackage", native_partial)

    def test_source_reader_contains_executed_fail_closed_controls(self):
        reader = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/OneProducerAuthoredReferencePackageReader.cs").read_text()
        self.assertIn("DuplicatePropertyNameHandling.Error", reader)
        self.assertIn("RequireCurrentSourceBindings", reader)
        self.assertIn("unqualified identity promotion", reader)
        self.assertIn("native reference mutation", reader)
        self.assertIn("package bytes pin mutation", reader)

    def test_compiled_source_host_has_real_relative_package_path_control(self):
        program = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceSimulationProgram.cs").read_text()
        self.assertIn('args[0]=="--s002-current-package-descriptor-path-control"', program)
        self.assertIn('"Assets/Game/Resources/OneProducerAuthoredReferencePackage.asset"', program)
        self.assertIn("VerifyPackageDescriptorTree(package", program)
        self.assertIn("VerifyPackageSourcePins(package", program)
        self.assertIn("RequireCurrentPackageDescriptorFile", program)
        self.assertIn('"../Assets/Game/Resources/OneProducerAuthoredReferencePackage.asset"', program)
        self.assertIn('"Assets/../Assets/Game/Resources/OneProducerAuthoredReferencePackage.asset"', program)
        self.assertIn('"Assets/./Game/Resources/OneProducerAuthoredReferencePackage.asset"', program)
        self.assertIn("RequireNoSymlinkComponents(path)", program)
        self.assertIn("[CurrentPackageDescriptorPathControl] result=Passed positives=2 negatives=4 world=false", program)

    def test_native_catalog_control_rejects_missing_duplicate_and_filtered_roles(self):
        control = (ROOT / "Assets/Game/Scripts/Editor/Skirmish/OneProducerAuthoredReferenceNativeCatalogValidation.cs").read_text()
        self.assertIn("absent Rifle/Barracks overlay", control)
        self.assertIn("duplicate Rifle/Barracks overlays", control)
        self.assertIn("profile-filtered Rifle role", control)
        self.assertIn("mismatched serialized unit reference", control)
        self.assertIn("cases=5", control)
        self.assertIn('MatchBuildingCatalogGuid = "b2010000000000000000000000000004"', control)
        self.assertIn('Path.Combine(Application.dataPath,\n                MatchScenePath.Substring("Assets/".Length))', control)
        self.assertIn("AssetDatabase.GUIDToAssetPath(MatchBuildingCatalogGuid) != MatchBuildingCatalogPath", control)
        self.assertIn("ReferenceEquals(x, satelliteDish)", control)
        self.assertIn("baseline, sceneCatalog, null, package", control)
        self.assertIn("match_scene_edited=false", control)
        self.assertNotIn("baseline, baseline, null, package", control)
        match_scene = (ROOT / "Assets/Game/Scenes/Match.unity").read_text()
        self.assertRegex(match_scene, r"(?m)^\s*buildingPlacementConfig: \{fileID: 11400000, guid: b2010000000000000000000000000004, type: 2\}$")
        scene_catalog = (ROOT / "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset").read_text()
        self.assertRegex(scene_catalog, r"(?m)^\s*- \{fileID: 6603657636351658259, guid: 77550b2e24f244cdfac66cc7bacec90a, type: 3\}$")


if __name__ == "__main__":
    unittest.main()
