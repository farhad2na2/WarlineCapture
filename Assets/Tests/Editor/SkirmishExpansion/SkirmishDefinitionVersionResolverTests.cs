using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Components;
using Game.Runtime;
using Game.Composition;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishDefinitionVersionResolverTests
    {
        readonly List<UnityEngine.Object> owned = new();
        T Own<T>(T value) where T : UnityEngine.Object { owned.Add(value); return value; }
        [TearDown] public void Cleanup()
        { foreach (var value in owned) UnityEngine.Object.DestroyImmediate(value); owned.Clear(); }
        SkirmishScenarioDefinitionConfig Definition(string id, int version)
        {
            var value = Own(ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>());
            value.Configure(id, null, null, null, null, null, null, null, null);
            Version(value, version); return value;
        }
        static void Version(SkirmishScenarioDefinitionConfig definition, int version)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("contentVersion").intValue = version;
            serialized.FindProperty("contentHash").stringValue = definition.DefinitionId + ".v" + version;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static void SetArray(SkirmishExpansionCatalogConfig catalog, string field,
            params SkirmishScenarioDefinitionConfig[] values)
        {
            var serialized = new SerializedObject(catalog); var array = serialized.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [Test] public void CurrentAndArchivedVersionsResolveExactlyWithoutNearestFallback()
        {
            var current = Definition("S002", 3); var old = Definition("S002", 2);
            var set = new SkirmishExpansionAuthoredSet { Definitions = new[] { current }, HistoricalDefinitions = new[] { old } };
            Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(set, "S002", 3, out var resolved)); Assert.AreSame(current, resolved);
            Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(set, "S002", 2, out resolved)); Assert.AreSame(old, resolved);
            set.HistoricalDefinitions = Array.Empty<SkirmishScenarioDefinitionConfig>();
            Assert.IsFalse(SkirmishExpansionCatalogFactory.TryGetDefinition(set, "S002", 2, out resolved)); Assert.IsNull(resolved);
            set.Definitions = null; set.DefinitionS002 = old;
            Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(set, "S002", 2, out resolved)); Assert.AreSame(old, resolved);
        }
        [Test] public void InvalidVersionsAndIdentitiesRefuseEvenWhenAnAssetExists()
        {
            foreach (int version in new[] { -1, 0, 1, 4 })
            {
                var asset = Definition("S002", version);
                Assert.IsFalse(SkirmishDefinitionVersionResolver.TryResolve(new[] { asset }, null, "S002", version, out var result)); Assert.IsNull(result);
            }
            foreach (string id in new[] { null, "s002", " S002", "S121", "S000" })
            { Assert.IsFalse(SkirmishDefinitionVersionResolver.TryResolve(null, null, id, 2, out var result)); Assert.IsNull(result); }
            var wrong = Definition("S002", 2); var serialized = new SerializedObject(wrong);
            serialized.FindProperty("definitionId").stringValue = "skirmish.s003"; serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(SkirmishDefinitionVersionResolver.TryResolve(new[] { wrong }, null, "S002", 2, out var rejected)); Assert.IsNull(rejected);
        }
        [Test] public void AmbiguousCurrentOrArchivedReferencesFailClosed()
        {
            var old = Definition("S002", 2); var duplicate = Definition("S002", 2);
            Assert.IsFalse(SkirmishDefinitionVersionResolver.TryResolve(new[] { old }, new[] { duplicate }, "S002", 2, out var result)); Assert.IsNull(result);
            Assert.IsFalse(SkirmishDefinitionVersionResolver.TryResolve(new[] { old, old }, null, "S002", 2, out result)); Assert.IsNull(result);
        }
        [Test] public void ConvertedCatalogOwnsItsHistoricalReferenceSnapshot()
        {
            var catalog = Own(ScriptableObject.CreateInstance<SkirmishExpansionCatalogConfig>());
            var old = Definition("S002", 2); var current = Definition("S002", 3);
            SetArray(catalog, "definitions", current); SetArray(catalog, "historicalDefinitions", old);
            var snapshot = catalog.ToAuthoredSet(); SetArray(catalog, "historicalDefinitions");
            Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(snapshot, "S002", 2, out var result)); Assert.AreSame(old, result);
            Assert.IsFalse(catalog.TryGetDefinition("S002", 2, out result)); Assert.IsNull(result);
            Assert.IsTrue(catalog.TryGetDefinition("S002", out result)); Assert.AreSame(current, result);
        }
        [Test] public void OnlyExplicitS002V3OptsIntoSourceBoundLogicalCompilation()
        {
            Assert.IsFalse(SkirmishMissionDispatch.RequiresSourceBoundDefinition("S002"));
            Assert.IsFalse(SkirmishMissionDispatch.RequiresSourceBoundDefinition("S002", 2));
            Assert.IsTrue(SkirmishMissionDispatch.RequiresSourceBoundDefinition("S002", 3));
            Assert.IsFalse(SkirmishMissionDispatch.RequiresSourceBoundDefinition("S002", 4));
            Assert.IsFalse(SkirmishMissionDispatch.RequiresSourceBoundDefinition("S003", 3));
            var setup = new SkirmishResolvedSetup { CatalogId = "S002", ContentVersion = 2, ObjectiveKind = SkirmishObjectiveKind.BaseAssault };
            Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup));
            setup.ContentVersion = 3; Assert.IsTrue(SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup));
        }
        void Fixture(out SkirmishExpansionCatalogConfig catalog, out SkirmishScenarioDefinitionConfig old,
            out SkirmishScenarioDefinitionConfig candidate, out List<SkirmishSetupMatrixRow> matrix)
        {
            catalog = SkirmishExpansionCatalogConfig.Load(); Assert.IsNotNull(catalog);
            Assert.IsTrue(catalog.TryGetDefinition("S002", 2, out old));
            candidate = Own(UnityEngine.Object.Instantiate(old)); Version(candidate, 3);
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), out matrix, out string error), error);
        }
        SkirmishExpansionCatalogConfig CandidateCatalog(SkirmishExpansionCatalogConfig packaged,
            SkirmishScenarioDefinitionConfig old, SkirmishScenarioDefinitionConfig candidate)
        {
            var catalog = Own(UnityEngine.Object.Instantiate(packaged));
            SetArray(catalog, "definitions", packaged.Definitions.Select(x => x == old ? candidate : x).ToArray());
            SetArray(catalog, "historicalDefinitions", old); return catalog;
        }
        [Test] public void PublishingANewCurrentReferencePreservesOldSetupHashesAndFreshReplayVersion()
        {
            Fixture(out var packaged, out var old, out var candidate, out var matrix);
            var before = new List<string>(); var hashes = new List<uint>();
            var sizes = new[] { SkirmishSizeId.Standard, SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar };
            var difficulties = new[] { SkirmishDifficultyId.Regular, SkirmishDifficultyId.Commander, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Regular };
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = old.RequiredFeatureIds };
            for (int i = 0; i < sizes.Length; i++)
            {
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(old, difficulties[i], sizes[i], 104731, manifest, matrix, out var setup, out var errors), string.Join(";", errors));
                before.Add(JsonUtility.ToJson(setup)); hashes.Add(setup.SetupHash);
            }
            var catalog = CandidateCatalog(packaged, old, candidate);
            using (SkirmishExpansionCatalogConfig.UseEditorValidationCatalog(catalog))
            {
                var authored = catalog.ToAuthoredSet();
                Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, "S002", 2, out var resolved)); Assert.AreSame(old, resolved);
                for (int i = 0; i < sizes.Length; i++)
                {
                    Assert.IsTrue(SkirmishSetupCompiler.TryCompile(resolved, difficulties[i], sizes[i], 104731, manifest, matrix, out var setup, out var errors), string.Join(";", errors));
                    Assert.AreEqual(hashes[i], setup.SetupHash); Assert.AreEqual(before[i], JsonUtility.ToJson(setup));
                }
                using var world = new World("S002 archived replay version control");
                var request = new SkirmishReplayRequest { CatalogId = "S002", ContentVersion = 2, DifficultyId = SkirmishDifficultyId.Regular,
                    SizeId = SkirmishSizeId.Standard, Seed = 104731, Mode = SkirmishReplayMode.FreshStart, SourceSessionId = "old-session" };
                Assert.IsTrue(SkirmishCheckpointCompositionSystemHelper.TryQueueFreshReplay(world.EntityManager, request, authored, matrix, manifest,
                    out var payload, out var replay, out var reason), reason.ToString());
                Assert.AreEqual(2, payload.ContentVersion); Assert.AreEqual(hashes[0], replay.SetupHash); Assert.AreNotEqual(request.SourceSessionId, payload.SessionId);
                // Synthetic map identity isolates the archived camera lookup. This is not a native source-fit witness.
                var profile = SkirmishMissionViewProfile.Load("S002"); Assert.IsNotNull(profile); Assert.IsTrue(profile.UsesActiveMapIdentity);
                using var builder = new BlobBuilder(Allocator.Temp);
                ref var map = ref builder.ConstructRoot<OperationMapBlob>();
                map.OperationMapId = new FixedString64Bytes(profile.ActiveOperationMapId);
                map.ContentHash = new FixedString128Bytes(profile.ActiveMapContentHash);
                map.SourceIdentityHash = new FixedString128Bytes(profile.ActiveMapSourceIdentityHash);
                using var blob = builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
                var metadata = world.EntityManager.CreateEntity(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
                world.EntityManager.SetComponentData(metadata, new ActiveOperationMapComponent { OperationMapId = map.OperationMapId, Generation = 1 });
                world.EntityManager.SetComponentData(metadata, new OperationMapMetadataComponent { Blob = blob, Generation = 1, PhysicalSourceValidated = 1 });
                Assert.IsTrue(SkirmishMissionViewProfileApplication.TryResolve(world.EntityManager, out var archivedProfile),
                    "An archived v2 session must keep its own camera profile when the current catalog reference is v3.");
                Assert.AreSame(profile, archivedProfile);
                Debug.Log("[SkirmishDefinitionVersions] preservedV2SetupHashes=" + string.Join(",", hashes.Select(x => x.ToString("X8"))) + " configurations=4 archivedFreshReplay=Passed");
            }
            Assert.AreSame(packaged, SkirmishExpansionCatalogConfig.Load());
        }
        [Test] public void UnprovedV3AndUnknownReplayCannotInheritV2LaunchOrReadiness()
        {
            Fixture(out var packaged, out var old, out var candidate, out var matrix);
            candidate.BindCampaignMap(null); candidate.BindMissionLogicalBinding(null);
            var catalog = CandidateCatalog(packaged, old, candidate);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = candidate.RequiredFeatureIds };
            using (SkirmishExpansionCatalogConfig.UseEditorValidationCatalog(catalog))
            {
                Assert.IsFalse(SkirmishSetupCompiler.TryCompile(candidate, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 104731,
                    manifest, matrix, out var setup, out var errors), "Changing the version cannot adopt old layout/source/readiness evidence.");
                Assert.IsNull(setup); Assert.IsNotEmpty(errors);
                foreach (int version in new[] { -1, 0, 1, 3, 4 })
                {
                    using var world = new World("S002 rejected replay " + version);
                    var request = new SkirmishReplayRequest { CatalogId = "S002", ContentVersion = version, DifficultyId = SkirmishDifficultyId.Regular,
                        SizeId = SkirmishSizeId.Standard, Seed = 104731, Mode = SkirmishReplayMode.FreshStart };
                    int before = world.EntityManager.UniversalQuery.CalculateEntityCount();
                    Assert.IsFalse(SkirmishCheckpointCompositionSystemHelper.TryQueueFreshReplay(world.EntityManager, request, catalog.ToAuthoredSet(), matrix,
                        manifest, out var payload, out _, out _), "Unexpected replay version " + version);
                    Assert.IsNull(payload); Assert.AreEqual(before, world.EntityManager.UniversalQuery.CalculateEntityCount());
                }
            }
        }
        public static void RunFocusedValidation()
        {
            var suite = new SkirmishDefinitionVersionResolverTests();
            try
            {
                suite.CurrentAndArchivedVersionsResolveExactlyWithoutNearestFallback();
                suite.InvalidVersionsAndIdentitiesRefuseEvenWhenAnAssetExists();
                suite.AmbiguousCurrentOrArchivedReferencesFailClosed();
                suite.ConvertedCatalogOwnsItsHistoricalReferenceSnapshot();
                suite.OnlyExplicitS002V3OptsIntoSourceBoundLogicalCompilation();
                suite.PublishingANewCurrentReferencePreservesOldSetupHashesAndFreshReplayVersion();
                suite.UnprovedV3AndUnknownReplayCannotInheritV2LaunchOrReadiness();
                SkirmishAuthoredDefinitionLookupTests.RunFocusedValidation();
                Debug.Log("[SkirmishDefinitionVersionResolverTests] result=Passed versionControls=7 existingLookupControls=5 nativeMissionAcceptance=Unmeasured routeSourceFit=Unmeasured");
            }
            catch (Exception error) { Debug.LogError("[SkirmishDefinitionVersionResolverTests] result=Failed\n" + error); throw; }
            finally { suite.Cleanup(); }
        }
    }
}
