using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeCheckpointPrefabRegistryTests
    {
        [Test]
        public void NativeKeysResolveFromConfiguredAssetsPopulatedAfterCaching()
        {
            var assets = new List<GameObject>();
            var aliases = new Dictionary<string, GameObject>();
            var registry = new SkirmishNativeCheckpointPrefabRegistry(assets, aliases);
            var prefab = new GameObject("Configured_Rifle");
            var unknown = new GameObject("Configured_Rifle");
            try
            {
                Assert.IsFalse(registry.Validate(out _));
                assets.Add(prefab); aliases.Add("infantry.rifle", prefab);
                Assert.IsTrue(registry.Validate(out var count));
                Assert.AreEqual(1, count);
                Assert.AreEqual("native-unit/configured_rifle", registry.Key(prefab));
                Assert.AreSame(prefab, registry.Resolve(registry.Key(prefab)));
                Assert.IsNull(registry.Key(unknown), "A matching name cannot authorize an unconfigured object.");
                Assert.IsNull(registry.Resolve("asset/editor-guid"));
                Assert.IsNull(registry.Resolve("native-unit/Configured_Rifle"));
            }
            finally { Object.DestroyImmediate(prefab); Object.DestroyImmediate(unknown); }
        }

        [Test]
        public void AmbiguousAndRemovedConfiguredPrefabsFailClosed()
        {
            var first = new GameObject("duplicate");
            var second = new GameObject("DUPLICATE");
            var assets = new List<GameObject> { first, second };
            var registry = new SkirmishNativeCheckpointPrefabRegistry(assets, null);
            try
            {
                Assert.IsFalse(registry.Validate(out _));
                Assert.IsNull(registry.Key(first));
                Assert.IsNull(registry.Resolve("native-unit/duplicate"));
                assets.Remove(second);
                string key = registry.Key(first);
                Assert.IsNotNull(key);
                assets.Clear();
                Assert.IsNull(registry.Resolve(key));
            }
            finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
        }

        [Test]
        public void RuntimeCaptureRequiresARegistryFromItsActualSessionOwner()
        {
            using var world = new World(nameof(RuntimeCaptureRequiresARegistryFromItsActualSessionOwner));
            var em = world.EntityManager;
            SkirmishNativeCheckpointCodecTests.Capture(em, out var setup, out _);
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity session = sessions.GetSingletonEntity();
            Assert.IsFalse(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, out _, out _));
            var prefab = new GameObject("configured-unit");
            try
            {
                Entity entity = em.CreateEntity(typeof(SkirmishSharedActorTag));
                em.AddComponentData(entity, new SkirmishAttemptOwnedComponent
                { SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                  StableObjectId = new FixedString64Bytes("base"), IsStructure = 1, FactionId = 1 });
                em.AddComponentData(entity, new RuntimeBuildingCombatInfo
                { RuntimeBuildingId = 7, OwnerFactionId = 1, FootprintCells = new int2(2, 2) });
                em.AddComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Building_Barrack") });
                em.AddComponentObject(entity, new SkirmishNativeBuildingCheckpointRecord
                {
                    Building = new RuntimeBuildingEntity { Id = 7, CombatEntity = entity },
                    Prefabs = new SkirmishNativeCheckpointPrefabRegistry(new[] { prefab }, null)
                });
                Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, out var saved, out var failure), failure);
                Assert.IsTrue(SkirmishNativeCheckpointCodec.Validate(saved, setup));
            }
            finally { Object.DestroyImmediate(prefab); }
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeCheckpointPrefabRegistryTests();
            suite.NativeKeysResolveFromConfiguredAssetsPopulatedAfterCaching();
            suite.AmbiguousAndRemovedConfiguredPrefabsFailClosed();
            suite.RuntimeCaptureRequiresARegistryFromItsActualSessionOwner();
            Debug.Log("[SkirmishNativeCheckpointPrefabRegistryTests] result=Passed tests=3 editorPrefabResolver=0");
        }
    }
}
