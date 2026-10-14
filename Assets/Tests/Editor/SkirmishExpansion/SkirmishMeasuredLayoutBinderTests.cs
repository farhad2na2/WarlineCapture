using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishMeasuredLayoutBinderTests
    {
        private readonly List<UnityEngine.Object> disposable = new List<UnityEngine.Object>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var asset in disposable) UnityEngine.Object.DestroyImmediate(asset);
            disposable.Clear();
        }

        [Test]
        public void ProjectsAgainstTwoExistingCampaignFramesWithoutChangingSources()
        {
            string previousFingerprint = null;
            foreach (string path in new[] {
                "Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset",
                "Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Definition.asset" })
            {
                var definition = Fixture(path, out var setup);
                string sourceBefore = JsonUtility.ToJson(definition.CampaignMap);
                var reasons = new List<SkirmishCompileReason>();
                Assert.IsTrue(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string fingerprint), Format(reasons));
                Assert.IsTrue(setup.MeasuredLayoutBound);
                Assert.AreEqual(64, fingerprint.Length);
                Assert.AreNotEqual(previousFingerprint, fingerprint, "An actual source/frame change must change the dependency fingerprint.");
                previousFingerprint = fingerprint;
                Assert.AreEqual(definition.CampaignMap.GridMetadata.Origin.x, setup.GridOriginX);
                Assert.AreEqual(definition.CampaignMap.GridMetadata.CellSize, setup.CellSize);
                Assert.AreEqual(definition.CampaignMap.Bounds.WorldMax.x - definition.CampaignMap.Bounds.WorldMin.x, setup.WorldWidthMetres);
                Assert.Greater(setup.PlayerBaseWorldX, definition.CampaignMap.Bounds.WorldMin.x);
                Assert.AreEqual(sourceBefore, JsonUtility.ToJson(definition.CampaignMap));
                Assert.AreEqual("S049", setup.CatalogId);
                Assert.AreEqual("opmap.skirmish.mountain_pass", setup.OperationMapId);
            }
        }

        [Test]
        public void MissingExplicitSourceOrLayoutAndUnknownIdentityFailCleanly()
        {
            var definition = Fixture(ArmorBreakPath, out var setup);
            var reasons = new List<SkirmishCompileReason>();
            definition.BindCampaignMap(null);
            Assert.IsFalse(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string fingerprint));
            Assert.IsNull(fingerprint);
            Assert.IsFalse(SkirmishMeasuredLayoutBinder.TryBind(setup, null, reasons, out _));
            definition = Fixture(ArmorBreakPath, out setup);
            setup.CatalogId = "S999";
            Assert.IsFalse(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out _));
            var data = new SerializedObject(definition);
            data.FindProperty("mapLayout").objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out _));
        }

        [Test]
        public void MissingTypedPadAndOutOfSourceBoundsLeaveSetupUntouched()
        {
            var definition = Fixture(ArmorBreakPath, out var setup);
            string before = JsonUtility.ToJson(setup);
            // X=100 is inside the historical DB bounds but outside Armor Break (min X=740).
            var data = new SerializedObject(definition.MapLayout);
            data.FindProperty("worldBinding").FindPropertyRelative("OriginX").floatValue = 100;
            data.ApplyModifiedPropertiesWithoutUndo();
            var reasons = new List<SkirmishCompileReason>();
            Assert.IsFalse(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out _));
            Assert.AreEqual(before, JsonUtility.ToJson(setup), "Failed projection must not partly change arrays/scalars.");
            definition = Fixture(ArmorBreakPath, out setup);
            before = JsonUtility.ToJson(setup);
            data = new SerializedObject(definition.MapLayout);
            var pads = data.FindProperty("pads");
            for (int i = pads.arraySize - 1; i >= 0; i--)
                if (pads.GetArrayElementAtIndex(i).FindPropertyRelative("Kind").enumValueIndex == (int)SkirmishLegalPadKind.InfantrySpawn)
                    pads.DeleteArrayElementAtIndex(i);
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out _));
            Assert.AreEqual(before, JsonUtility.ToJson(setup));
        }

        [Test]
        public void FingerprintIsStableAndTracksChangedRouteAndWorldFrame()
        {
            var definition = Fixture(ArmorBreakPath, out var setup);
            var reasons = new List<SkirmishCompileReason>();
            Assert.IsTrue(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string initial), Format(reasons));
            Assert.IsTrue(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string repeat), Format(reasons));
            Assert.AreEqual(initial, repeat);
            var data = new SerializedObject(definition.MapLayout);
            var route = data.FindProperty("routes").GetArrayElementAtIndex(0);
            route.FindPropertyRelative("WidthMetres").floatValue += 1;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string changed), Format(reasons));
            Assert.AreNotEqual(initial, changed);
            data.Update();
            data.FindProperty("worldBinding").FindPropertyRelative("OriginX").floatValue += 5;
            data.ApplyModifiedPropertiesWithoutUndo();
            RebindMeasuredDeployments(definition.MapLayout);
            Assert.IsTrue(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string frameChanged), Format(reasons));
            Assert.AreNotEqual(changed, frameChanged);
            var structure = setup.Structures[4];
            structure.AcrossOffsetMetres += 1;
            setup.Structures[4] = structure;
            Assert.IsTrue(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string actorsChanged), Format(reasons));
            Assert.AreNotEqual(frameChanged, actorsChanged, "Compiled facility offset changes must change setup provenance.");
        }

        [Test]
        public void RecreatedSourceMetadataAndPlacementObjectsKeepSemanticFingerprint()
        {
            var definition = Fixture(ArmorBreakPath, out var setup);
            var original = definition.CampaignMap;
            // Recreate metadata objects only. No asset, scene, terrain or prefab geometry is generated.
            var first = UnityEngine.Object.Instantiate(original);
            var second = UnityEngine.Object.Instantiate(original);
            disposable.Add(first); disposable.Add(second);
            var firstPlacements = PlacementFixture(new Vector3(1000, 0, 500));
            var secondPlacements = PlacementFixture(new Vector3(1000, 0, 500));
            SetAdditionalPlacements(first, firstPlacements);
            SetAdditionalPlacements(second, secondPlacements);
            Assert.AreNotEqual(first.GetEntityId(), second.GetEntityId());
            Assert.AreNotEqual(firstPlacements.GetEntityId(), secondPlacements.GetEntityId());
            var reasons = new List<SkirmishCompileReason>();
            definition.BindCampaignMap(first);
            Assert.IsTrue(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string firstHash), Format(reasons));
            definition.BindCampaignMap(second);
            Assert.IsTrue(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string secondHash), Format(reasons));
            Assert.AreEqual(firstHash, secondHash, "Runtime instance IDs must never enter config provenance.");
            SetAdditionalPlacements(second, PlacementFixture(new Vector3(1001, 0, 500)));
            Assert.IsTrue(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, reasons, out string movedHash), Format(reasons));
            Assert.AreNotEqual(secondHash, movedHash, "Meaningful placement values must remain in provenance.");
        }

        private MapBuildingPlacementConfig PlacementFixture(Vector3 position)
        {
            var config = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
            disposable.Add(config);
            config.EditorSetPlacements(new List<MapBuildingPlacementConfigEntry>
            {
                new MapBuildingPlacementConfigEntry("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset",
                    "fixture", null, 0, position, position, Vector3.zero, Vector3.one, 0, false)
            });
            return config;
        }

        [Test]
        public void LegacyPreservationRequiresPackagedFourLayoutAndSourceContract()
        {
            var catalog = SkirmishExpansionCatalogConfig.Load(); Assert.IsNotNull(catalog);
            foreach (string id in new[] { "S002", "S003", "S004", "S005" })
            {
                Assert.IsTrue(catalog.TryGetDefinition(id, out var definition));
                string baseline = SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout);
                var setup = new SkirmishResolvedSetup { CatalogId = id, OperationMapId = SkirmishMapLayoutBuilder.DesertBaseMapId,
                    LayoutId = SkirmishMapLayoutBuilder.DesertBaseLayoutId, ObjectiveKind = SkirmishObjectiveKind.BaseAssault };
                Assert.IsTrue(SkirmishMeasuredLayoutBinder.PreservesPackagedLegacyBinding(setup, definition, baseline), id);
                Assert.IsFalse(SkirmishMeasuredLayoutBinder.PreservesPackagedLegacyBinding(setup, definition, null), "No reviewed baseline must fail closed.");
                var clone = UnityEngine.Object.Instantiate(definition); disposable.Add(clone);
                clone.BindCampaignMap(AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(ArmorBreakPath));
                Assert.IsFalse(SkirmishMeasuredLayoutBinder.PreservesPackagedLegacyBinding(setup, clone, baseline), "Different actual source cannot inherit DB compatibility.");
                clone = UnityEngine.Object.Instantiate(definition); disposable.Add(clone);
                var changedLayout = UnityEngine.Object.Instantiate(definition.MapLayout); disposable.Add(changedLayout);
                var logical = new SerializedObject(changedLayout); logical.FindProperty("worldWidthMetres").floatValue += 1; logical.ApplyModifiedPropertiesWithoutUndo();
                var clonedDefinition = new SerializedObject(clone); clonedDefinition.FindProperty("mapLayout").objectReferenceValue = changedLayout; clonedDefinition.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsFalse(SkirmishMeasuredLayoutBinder.PreservesPackagedLegacyBinding(setup, clone, baseline), "Immutable snapshot rejects changed layout, even if packaged reference later changes.");
                setup.CatalogId = "S026";
                Assert.IsFalse(SkirmishMeasuredLayoutBinder.PreservesPackagedLegacyBinding(setup, definition, baseline), "New DB logical mission must use generic source pinning.");
            }
        }

        [Test]
        public void ReverseEquivalentAndSamePositionRoutesFailBeforeSetupMutation()
        {
            var definition = Fixture(ArmorBreakPath, out var setup); string before = JsonUtility.ToJson(setup);
            var data = new SerializedObject(definition.MapLayout); var routes = data.FindProperty("routes"); routes.arraySize = 2;
            var first = routes.GetArrayElementAtIndex(0).FindPropertyRelative("WaypointAnchorIds");
            var second = routes.GetArrayElementAtIndex(1).FindPropertyRelative("WaypointAnchorIds"); second.arraySize = first.arraySize;
            for (int i=0;i<first.arraySize;i++) second.GetArrayElementAtIndex(i).stringValue = first.GetArrayElementAtIndex(first.arraySize-1-i).stringValue;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, new List<SkirmishCompileReason>(), out _));
            Assert.AreEqual(before, JsonUtility.ToJson(setup));
            definition = Fixture(ArmorBreakPath, out setup); before = JsonUtility.ToJson(setup);
            data = new SerializedObject(definition.MapLayout); routes = data.FindProperty("routes");
            first = routes.GetArrayElementAtIndex(0).FindPropertyRelative("WaypointAnchorIds");
            first.GetArrayElementAtIndex(1).stringValue = first.GetArrayElementAtIndex(0).stringValue;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, new List<SkirmishCompileReason>(), out _)); Assert.AreEqual(before, JsonUtility.ToJson(setup));
        }

        [Test]
        public void InvalidRouteKindLengthAndTimingFailBeforeSetupMutation()
        {
            foreach (string field in new[] { "Kind", "LengthMetres", "InfantryTransitSeconds", "GroundTransitSeconds", "AirTransitSeconds", "InfantryFirstContactSeconds", "GroundFirstContactSeconds" })
            {
                var definition = Fixture(ArmorBreakPath, out var setup); string before = JsonUtility.ToJson(setup);
                var data = new SerializedObject(definition.MapLayout);
                var value = data.FindProperty("routes").GetArrayElementAtIndex(0).FindPropertyRelative(field);
                if (field == "Kind") value.enumValueIndex = 0;
                else value.floatValue = field == "LengthMetres" ? 0 : float.NaN;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsFalse(SkirmishMeasuredLayoutBinder.TryBind(setup, definition, new List<SkirmishCompileReason>(), out _), field);
                Assert.AreEqual(before, JsonUtility.ToJson(setup), field);
            }
        }

        private static void SetAdditionalPlacements(OperationMapDefinition source, MapBuildingPlacementConfig placements)
        {
            var data = new SerializedObject(source);
            data.FindProperty("additionalBuildingPlacements").objectReferenceValue = placements;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private const string ArmorBreakPath = "Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset";
        private SkirmishScenarioDefinitionConfig Fixture(string sourcePath, out SkirmishResolvedSetup setup)
        {
            var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(sourcePath);
            Assert.IsNotNull(source);
            var profiles = SkirmishExpansionCatalogConfig.Load().ToAuthoredSet();
            var layout = UnityEngine.Object.Instantiate(profiles.DefinitionS005.MapLayout);
            disposable.Add(layout);
            var data = new SerializedObject(layout);
            data.FindProperty("operationMapId").stringValue = "opmap.skirmish.mountain_pass";
            data.FindProperty("layoutId").stringValue = "layout.skirmish.mp.ba";
            var frame = data.FindProperty("worldBinding");
            frame.FindPropertyRelative("SourceHash").stringValue = source.ContentHash;
            frame.FindPropertyRelative("HasMeasuredDeployments").boolValue = true;
            frame.FindPropertyRelative("BindAllConfigurations").boolValue = true;
            var min = source.Bounds.WorldMin; var max = source.Bounds.WorldMax;
            frame.FindPropertyRelative("OriginX").floatValue = min.x + (max.x - min.x) * .4f;
            frame.FindPropertyRelative("OriginZ").floatValue = min.z + (max.z - min.z) * .5f;
            frame.FindPropertyRelative("ForwardX").floatValue = 1;
            frame.FindPropertyRelative("ForwardZ").floatValue = 0;
            frame.FindPropertyRelative("AcrossX").floatValue = 0;
            frame.FindPropertyRelative("AcrossZ").floatValue = 1;
            frame.FindPropertyRelative("ForwardMetres").floatValue = (max.x - min.x) * .2f;
            frame.FindPropertyRelative("AcrossMetres").floatValue = (max.z - min.z) * .2f;
            data.ApplyModifiedPropertiesWithoutUndo();
            RebindMeasuredDeployments(layout);
            var definition = ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();
            disposable.Add(definition);
            definition.Configure("S049", profiles.ObjectiveBa, profiles.ArmyGround, profiles.StartField,
                profiles.Economy, profiles.Upgrades, profiles.Intel, profiles.Roles, layout);
            definition.BindCampaignMap(source);
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(root, out var matrix, out string error), error);
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard,
                104778, new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                out setup, out var reasons), Format(reasons));
            return definition;
        }

        private static void RebindMeasuredDeployments(SkirmishMapLayoutConfig layout)
        {
            layout.TryGetAnchor("base.player", out var player); layout.TryGetAnchor("base.enemy", out var enemy);
            layout.TryProjectToMap(player.NormalizedU, player.NormalizedV, out float px, out float pz);
            layout.TryProjectToMap(enemy.NormalizedU, enemy.NormalizedV, out float ex, out float ez);
            var data = new SerializedObject(layout); var frame = data.FindProperty("worldBinding");
            frame.FindPropertyRelative("PlayerDeployment").vector2Value = new Vector2(px, pz);
            frame.FindPropertyRelative("EnemyDeployment").vector2Value = new Vector2(ex, ez);
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static string Format(List<SkirmishCompileReason> reasons) => string.Join("\n", reasons);
        public static void CaptureLegacyLayoutBaselines()
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            foreach (string id in new[] { "S002", "S003", "S004", "S005" })
            {
                if (catalog == null || !catalog.TryGetDefinition(id, out var definition) || definition.MapLayout == null)
                    throw new InvalidOperationException("Existing packaged legacy layout missing: " + id);
                Debug.Log("[SkirmishLegacyLayoutBaseline] catalog=" + id + " sha256=" + SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout));
            }
            Debug.Log("[SkirmishLegacyLayoutBaseline] result=Captured count=4 nativeGameplayCertified=0");
        }
        public static void RunFocusedValidation()
        {
            var suite = new SkirmishMeasuredLayoutBinderTests();
            Action[] tests = { suite.ProjectsAgainstTwoExistingCampaignFramesWithoutChangingSources,
                suite.MissingExplicitSourceOrLayoutAndUnknownIdentityFailCleanly,
                suite.MissingTypedPadAndOutOfSourceBoundsLeaveSetupUntouched,
                suite.FingerprintIsStableAndTracksChangedRouteAndWorldFrame,
                suite.RecreatedSourceMetadataAndPlacementObjectsKeepSemanticFingerprint,
                suite.LegacyPreservationRequiresPackagedFourLayoutAndSourceContract,
                suite.ReverseEquivalentAndSamePositionRoutesFailBeforeSetupMutation,
                suite.InvalidRouteKindLengthAndTimingFailBeforeSetupMutation };
            try
            {
                foreach (var test in tests) { try { test(); } finally { suite.Cleanup(); } }
                Debug.Log("[SkirmishMeasuredLayoutBinderTests] result=Passed tests=8");
            }
            catch (Exception e) { Debug.LogError("[SkirmishMeasuredLayoutBinderTests] result=Failed\n" + e); throw; }
        }
    }
}
