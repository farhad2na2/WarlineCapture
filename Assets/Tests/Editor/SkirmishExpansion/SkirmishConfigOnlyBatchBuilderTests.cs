using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Builder = Game.Editor.SkirmishConfigOnlyBatchBuilder;

namespace Game.Tests.Editor
{
    public sealed class SkirmishConfigOnlyBatchBuilderTests
    {
        private string fixtureRoot;
        private SkirmishExpansionCatalogConfig catalog;
        private SkirmishExpansionAuthoredSet profiles;
        private OperationMapDefinition source;

        [SetUp]
        public void Setup()
        {
            fixtureRoot = Builder.ScenarioRoot + "/__BatchBuilderTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder(Builder.ScenarioRoot, Path.GetFileName(fixtureRoot));
            var packaged = SkirmishExpansionCatalogConfig.Load();
            Assert.IsNotNull(packaged);
            profiles = packaged.ToAuthoredSet();
            catalog = UnityEngine.Object.Instantiate(packaged);
            catalog.name = "DisposableBatchCatalog";
            AssetDatabase.CreateAsset(catalog, fixtureRoot + "/Catalog.asset");
            source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                "Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Definition.asset");
            Assert.IsNotNull(source);
        }

        [TearDown]
        public void Cleanup()
        {
            // Delete only this suite's verified disposable asset directory.
            if (!string.IsNullOrEmpty(fixtureRoot) &&
                fixtureRoot.StartsWith(Builder.ScenarioRoot + "/__BatchBuilderTests_", StringComparison.Ordinal))
                AssetDatabase.DeleteAsset(fixtureRoot);
            fixtureRoot = null;
        }

        [Test]
        public void RealAssetApiBuildIsIdempotentAndLeavesSourcesPublicationAndLegacyUntouched()
        {
            var request = Request("S026");
            string sourceHash = Builder.HashFile(AssetDatabase.GetAssetPath(source));
            string publicationHash = Builder.HashFile(AssetDatabase.GetAssetPath(profiles.Publication));
            string legacyHash = Builder.HashFile(AssetDatabase.GetAssetPath(profiles.DefinitionS002));
            int existingCount = catalog.Definitions.Count;
            var result = Builder.BuildBatch(new[] { request }, catalog, fixtureRoot + "/Output");
            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(3, result.Rows.Count);
            Assert.IsTrue(result.Rows.TrueForAll(row => row.CompilerPassed));
            Assert.IsTrue(result.Rows.TrueForAll(row => !row.RuntimeLayoutBound), "Compiler does not bind new logical groups yet; retain this gate.");
            Assert.AreEqual(existingCount + 1, catalog.Definitions.Count);
            Assert.IsTrue(catalog.TryGetDefinition("S026", out var definition));
            Assert.AreSame(source, definition.CampaignMap);
            Assert.AreEqual("opmap.skirmish.city_crossroads", definition.OperationMapId);
            Assert.AreSame(profiles.DefinitionS002, catalog.ToAuthoredSet().DefinitionS002);
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string layoutPath = AssetDatabase.GetAssetPath(definition.MapLayout);
            string definitionGuid = AssetDatabase.AssetPathToGUID(definitionPath);
            string layoutGuid = AssetDatabase.AssetPathToGUID(layoutPath);
            string definitionHash = Builder.HashFile(definitionPath);
            string layoutHash = Builder.HashFile(layoutPath);
            string catalogHash = Builder.HashFile(AssetDatabase.GetAssetPath(catalog));

            var repeated = Builder.BuildBatch(new[] { request }, catalog, fixtureRoot + "/Output");
            Assert.IsTrue(repeated.Success, string.Join("\n", repeated.Errors));
            Assert.IsEmpty(repeated.ChangedAssets, "Identical second build must make no persistent changes.");
            Assert.AreEqual(definitionGuid, AssetDatabase.AssetPathToGUID(definitionPath));
            Assert.AreEqual(layoutGuid, AssetDatabase.AssetPathToGUID(layoutPath));
            Assert.AreEqual(definitionHash, Builder.HashFile(definitionPath));
            Assert.AreEqual(layoutHash, Builder.HashFile(layoutPath));
            Assert.AreEqual(catalogHash, Builder.HashFile(AssetDatabase.GetAssetPath(catalog)));
            Assert.IsFalse(EditorUtility.IsDirty(definition));
            Assert.IsFalse(EditorUtility.IsDirty(definition.MapLayout));
            Assert.IsFalse(EditorUtility.IsDirty(catalog));
            Assert.AreEqual(sourceHash, Builder.HashFile(AssetDatabase.GetAssetPath(source)));
            Assert.AreEqual(publicationHash, Builder.HashFile(AssetDatabase.GetAssetPath(profiles.Publication)));
            Assert.AreEqual(legacyHash, Builder.HashFile(AssetDatabase.GetAssetPath(profiles.DefinitionS002)));
        }

        [Test]
        public void WholeBatchPreflightRetainsAllRowsAndRejectsStaleSourceWithoutWrites()
        {
            var good = Request("S026");
            var stale = Request("S049");
            stale.ExpectedSourceContentHash = new string('0', 64);
            string before = Builder.HashFile(AssetDatabase.GetAssetPath(catalog));
            var result = Builder.BuildBatch(new[] { good, stale }, catalog, fixtureRoot + "/Output");
            Assert.IsFalse(result.Success);
            Assert.AreEqual(6, result.Rows.Count);
            Assert.AreEqual(3, result.Rows.FindAll(row => row.CompilerPassed).Count);
            Assert.IsEmpty(result.ChangedAssets);
            Assert.IsFalse(AssetDatabase.IsValidFolder(fixtureRoot + "/Output"));
            Assert.AreEqual(before, Builder.HashFile(AssetDatabase.GetAssetPath(catalog)));
            Assert.IsFalse(catalog.TryGetDefinition("S026", out _));
        }

        [Test]
        public void InvalidIdentityAndOutputEscapeCannotMutateAssets()
        {
            var request = Request("S026");
            request.CatalogId = "S005";
            string before = Builder.HashFile(AssetDatabase.GetAssetPath(catalog));
            var invalid = Builder.BuildBatch(new[] { request }, catalog, fixtureRoot + "/Output");
            Assert.IsFalse(invalid.Success);
            Assert.AreEqual(3, invalid.Rows.Count);
            request.CatalogId = "S026";
            var escape = Builder.BuildBatch(new[] { request }, catalog, Builder.ScenarioRoot + "/../Escaped");
            Assert.IsFalse(escape.Success);
            Assert.IsEmpty(escape.ChangedAssets);
            Assert.AreEqual(before, Builder.HashFile(AssetDatabase.GetAssetPath(catalog)));
        }

        [Test]
        public void InvalidSourceFitDeclarationAndDuplicateBatchFailBeforeWrites()
        {
            var request = Request("S026");
            var data = new SerializedObject(request.MeasuredLayout);
            data.FindProperty("worldBinding").FindPropertyRelative("OriginX").floatValue = source.Bounds.WorldMax.x + 1000;
            data.ApplyModifiedPropertiesWithoutUndo();
            var invalid = Builder.BuildBatch(new[] { request }, catalog, fixtureRoot + "/Output");
            Assert.IsFalse(invalid.Success);
            Assert.IsEmpty(invalid.ChangedAssets);
            Assert.IsFalse(AssetDatabase.IsValidFolder(fixtureRoot + "/Output"));
            var good = Request("S049");
            var duplicate = Builder.BuildBatch(new[] { good, good }, catalog, fixtureRoot + "/Output");
            Assert.IsFalse(duplicate.Success);
            Assert.AreEqual(6, duplicate.Rows.Count);
            Assert.IsFalse(AssetDatabase.IsValidFolder(fixtureRoot + "/Output"));
        }

        private Builder.Request Request(string id)
        {
            // Synthetic bounded fixture on an existing source exercises the API only.
            // It is deliberately not a production mission/source-fit certificate.
            var layout = UnityEngine.Object.Instantiate(profiles.DefinitionS005.MapLayout);
            layout.name = "FixtureLayout_" + id;
            var data = new SerializedObject(layout);
            data.FindProperty("operationMapId").stringValue = id == "S026" ? "opmap.skirmish.city_crossroads" : "opmap.skirmish.mountain_pass";
            data.FindProperty("layoutId").stringValue = id == "S026" ? "layout.skirmish.cc.ba" : "layout.skirmish.mp.ba";
            var frame = data.FindProperty("worldBinding");
            frame.FindPropertyRelative("SourceHash").stringValue = source.ContentHash;
            frame.FindPropertyRelative("HasMeasuredDeployments").boolValue = true;
            frame.FindPropertyRelative("BindAllConfigurations").boolValue = true;
            Vector3 min = source.Bounds.WorldMin;
            Vector3 max = source.Bounds.WorldMax;
            frame.FindPropertyRelative("OriginX").floatValue = min.x + (max.x - min.x) * 0.4f;
            frame.FindPropertyRelative("OriginZ").floatValue = min.z + (max.z - min.z) * 0.5f;
            frame.FindPropertyRelative("ForwardX").floatValue = 1;
            frame.FindPropertyRelative("ForwardZ").floatValue = 0;
            frame.FindPropertyRelative("AcrossX").floatValue = 0;
            frame.FindPropertyRelative("AcrossZ").floatValue = 1;
            frame.FindPropertyRelative("ForwardMetres").floatValue = (max.x - min.x) * 0.2f;
            frame.FindPropertyRelative("AcrossMetres").floatValue = (max.z - min.z) * 0.2f;
            data.ApplyModifiedPropertiesWithoutUndo();
            layout.TryGetAnchor("base.player", out var player);
            layout.TryGetAnchor("base.enemy", out var enemy);
            layout.TryProjectToMap(player.NormalizedU, player.NormalizedV, out float px, out float pz);
            layout.TryProjectToMap(enemy.NormalizedU, enemy.NormalizedV, out float ex, out float ez);
            data.Update();
            frame = data.FindProperty("worldBinding");
            frame.FindPropertyRelative("PlayerDeployment").vector2Value = new Vector2(px, pz);
            frame.FindPropertyRelative("EnemyDeployment").vector2Value = new Vector2(ex, ez);
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(layout, fixtureRoot + "/Input_" + id + ".asset");
            return new Builder.Request
            {
                CatalogId = id, CampaignSource = source, ExpectedSourceIdentityHash = source.SourceIdentityHash,
                ExpectedSourceContentHash = source.ContentHash, ExpectedSourceAssetSha256 = Builder.HashFile(AssetDatabase.GetAssetPath(source)),
                ExpectedSceneGuid = source.SourceSceneReference.AssetGUID, ExpectedGridGuid = source.GridMetadata.AssetGuid,
                ExpectedSurfaceGuid = source.MapSurfaceDataReference.AssetGUID, MeasuredLayout = layout, Profiles = profiles,
                Start = id == "S049" ? profiles.StartField : profiles.StartEstablished
            };
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishConfigOnlyBatchBuilderTests();
            Action[] tests = { suite.RealAssetApiBuildIsIdempotentAndLeavesSourcesPublicationAndLegacyUntouched,
                suite.WholeBatchPreflightRetainsAllRowsAndRejectsStaleSourceWithoutWrites,
                suite.InvalidIdentityAndOutputEscapeCannotMutateAssets,
                suite.InvalidSourceFitDeclarationAndDuplicateBatchFailBeforeWrites };
            try
            {
                foreach (Action test in tests) { try { suite.Setup(); test(); } finally { suite.Cleanup(); } }
                Debug.Log("[SkirmishConfigOnlyBatchBuilderTests] result=Passed tests=4");
            }
            catch (Exception exception)
            { Debug.LogError("[SkirmishConfigOnlyBatchBuilderTests] result=Failed\n" + exception); throw; }
        }
    }
}
