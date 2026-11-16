import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
UNIT_SOURCE = ROOT / "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.UnitData.cs"
DISPATCH_SOURCE = ROOT / "Assets/Game/Scripts/Configs/Skirmish/SkirmishMissionDispatch.cs"


class UnitReferenceDataDispatchTests(unittest.TestCase):
    def test_legacy_preset_mapping_is_preserved_and_expanded_ids_use_shared_base(self):
        unit = UNIT_SOURCE.read_text()
        dispatch = DISPATCH_SOURCE.read_text()
        self.assertIn("SkirmishMissionDispatch.TryGetLegacyIndex(catalogId, out int legacyIndex)", unit)
        self.assertIn("? legacyIndex : SkirmishPresetConfig.DesertBaseScenarioIndex", unit)
        self.assertIn("var preset = SkirmishPresetConfig.Load(sourcePresetIndex)", unit)
        self.assertIn('catalogId=="S001"?0:catalogId=="S025"?1:catalogId=="S073"?3:catalogId=="S002"?4', dispatch)
        self.assertIn("return number>=1&&number<=120", dispatch)
        self.assertIn('["source_preset_index"] = sourcePresetIndex', unit)

    def test_compiler_selected_handoff_is_opt_in_pinned_and_keeps_native_factories(self):
        unit = UNIT_SOURCE.read_text()
        evidence = (ROOT / "Assets/Game/Scripts/Editor/Skirmish/CompilerSelectedAuthoringEvidence.cs").read_text()
        binder = (ROOT / "Assets/Game/Scripts/Configs/Skirmish/SkirmishMeasuredLayoutBinder.cs").read_text()
        self.assertIn("RunCompilerSelectedUnitReferenceDataWithIdentityAudit", unit)
        self.assertIn('"-skirmishUnitPhysicalInputRequestSha256"', unit)
        self.assertIn('"-skirmishUnitCompilerResultSha256"', unit)
        self.assertIn("!IsSha256(requestSha) || !IsSha256(resultSha)", unit)
        self.assertIn("HashFile(requestPath) != requestSha", unit)
        self.assertIn("HashFile(resultPath) != resultSha", unit)
        self.assertIn("selectionDescriptor[\"path\"]", unit)
        self.assertIn("selectionDescriptor.Count != 2", unit)
        self.assertIn("(string)selectionDescriptor[\"sha256\"] != resultSha", unit)
        self.assertIn('(string)selection["catalog_id"] != catalogId', unit)
        self.assertIn('(int?)selection["seed"] != seed', unit)
        self.assertIn("CompilerSelectedAuthoringEvidence.Validate(physicalRequest, selection", unit)
        self.assertIn("out SkirmishResolvedSetup selectedSetup", unit)
        self.assertIn("CompilerSelectedUnityAssetReadback.Validate", unit)
        self.assertIn("RunUnitReferenceDataFor(catalogId, seed, false, selectedSetup", unit)
        self.assertIn("SkirmishProductionCatalog.Create(setup, preset.buildingPlacement, sceneConfig)", unit)
        self.assertIn('LoadedType("Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper")', unit)
        self.assertIn('LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper")', unit)
        for flag in ("runtime_admission", "source_fit_certified", "mission_ready", "native_acceptance", "world_allocated"):
            self.assertIn(f'["{flag}"] = false', unit)
        self.assertNotIn("SourceS026TypedConfiguration", evidence)
        self.assertIn("MatchesExactCompilerSelectedMeasuredLayout", evidence)
        for requirement in ("CanonicalBinaryV2", "HasMeasuredDeployments", "BindAllConfigurations", "ComputeLayoutSemanticFingerprint"):
            self.assertIn(requirement, binder)


if __name__ == "__main__":
    unittest.main()
