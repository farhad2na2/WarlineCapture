import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.BuildingData.cs"


class BuildingReferenceDataDispatchTests(unittest.TestCase):
    def test_legacy_building_owner_keeps_its_existing_compilation_and_key_route(self):
        source = SOURCE.read_text()
        legacy = source.split("public static void RunBuildingReferenceData()", 1)[1].split(
            "// Exact compiler-selected handoff", 1
        )[0]
        self.assertIn('"-skirmishBuildingReferenceData"', legacy)
        self.assertIn('BuildingReferenceArgument(args, "-skirmishBuildingReferenceKeys", null)', legacy)
        self.assertIn("SkirmishSetupCompiler.TryCompile", legacy)
        self.assertIn("SkirmishPresetConfig.DesertBaseScenarioIndex, null, null, null, null, null, null", legacy)
        self.assertNotIn("CompilerSelectedAuthoringEvidence.Validate", legacy)

    def test_selected_owner_uses_exact_compiler_and_unity_asset_handoff(self):
        source = SOURCE.read_text()
        selected = source.split("public static void RunCompilerSelectedBuildingReferenceData()", 1)[1].split(
            "private static void RunBuildingReferenceDataFor", 1
        )[0]
        for flag in (
            "-skirmishUnitScenario",
            "-skirmishUnitSeed",
            "-skirmishUnitPhysicalInputRequest",
            "-skirmishUnitPhysicalInputRequestSha256",
            "-skirmishUnitCompilerResult",
            "-skirmishUnitCompilerResultSha256",
            "-skirmishBuildingReferenceData",
        ):
            self.assertIn(f'"{flag}"', selected)
        self.assertIn("HashFile(requestPath) != requestSha", selected)
        self.assertIn("HashFile(resultPath) != resultSha", selected)
        self.assertIn("CompilerSelectedAuthoringEvidence.Validate(physicalRequest, selection", selected)
        self.assertIn("CompilerSelectedUnityAssetReadback.Validate(authoringRequest, selection", selected)
        self.assertIn("ResolveCompilerSelectedBuildingPreset(selection, projectRoot)", selected)
        self.assertIn("RunBuildingReferenceDataFor(selectedSetup, output, null, false", selected)
        self.assertNotIn("SkirmishSetupCompiler.TryCompile", selected)

    def test_selected_keys_come_from_composed_native_spawnables_and_keep_nonclaims(self):
        source = SOURCE.read_text()
        core = source.split("private static void RunBuildingReferenceDataFor", 1)[1].split(
            "static Type LoadedType", 1
        )[0]
        self.assertIn("SkirmishProductionCatalog.Create(setup, preset.buildingPlacement, sceneConfig)", core)
        self.assertIn("new List<GameObject>(production.Config.Spawnables)", core)
        self.assertIn("keys = buildings.Select(p => p.name).ToArray()", core)
        resolver = source.split("private static string ResolveCompilerSelectedBuildingPreset", 1)[1].split(
            "private static void RunBuildingReferenceDataFor", 1
        )[0]
        self.assertIn('"Assets/Game/Resources/"', resolver)
        self.assertIn('"One compiler-pinned SkirmishPresetConfig resource is required; unsupported or ambiguous source baseline"', resolver)
        self.assertIn("RequireCompilerSelectedInputPin(inputPins, Path.GetFullPath(Path.Combine(root, placementPath)))", resolver)
        self.assertIn("RequireCompilerSelectedInputPin(inputPins, sceneConfigAbsolutePath)", resolver)
        self.assertNotIn('catalogId == "S050"', source)
        self.assertIn("TryResolveConfiguredSpawnablePrefab", core)
        self.assertIn("CreateRuntimeBuildingDefinition", core)
        self.assertIn("compiler_selection_asset_readback", core)
        self.assertIn("if (compilerResultDescriptor == null)", core)
        legacy_pins = core.split("if (compilerResultDescriptor == null)", 1)[1].split("else", 1)[0]
        selected_pins = core.split("else", 1)[-1].split("var originalDefault", 1)[0]
        self.assertIn("Actual legacy scenario definition missing", legacy_pins)
        self.assertNotIn("Actual legacy scenario definition missing", selected_pins)
        self.assertIn("pins = SourcePins(buildingCatalog, sceneCatalog, unitCatalog, geometrySource)", selected_pins)
        self.assertIn("pins[pin.Key] = pin.Value", core)
        for flag in ("runtime_admission", "source_fit_certified", "mission_ready", "native_acceptance", "world_allocated"):
            self.assertIn(f'["{flag}"] = false', core)
        self.assertIn('"OriginalConfiguredPrefabMetadataFactory_NotMissionState"', core)


if __name__ == "__main__":
    unittest.main()
