#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace Game.Tests.Editor
{
    public static class SkirmishBakedActorSnapshotTests
    {
        public static void RunFocusedValidation()
        {
            using (var world = new World("BakedSnapshotControls"))
            {
                var em = world.EntityManager;
                Assert.Throws<InvalidOperationException>(() => SkirmishBakedActorSnapshotExporter.Capture(em));
                var owner = em.CreateEntity(); var first = em.CreateEntity(typeof(Prefab)); var second = em.CreateEntity(typeof(Prefab));
                em.AddComponentData(first, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Rifle") });
                em.AddComponentData(second, new UnitSourcePrefabKey { Value = new FixedString64Bytes("rifle") });
                em.AddComponentData(first, new UnitIdleWanderComponent { RandomState = 0xfedcba98u });
                var entries = em.AddBuffer<UnitPrefabRegistryEntry>(owner);
                entries.Add(new UnitPrefabRegistryEntry { Prefab = first }); entries.Add(new UnitPrefabRegistryEntry { Prefab = second });
                var snapshot = SkirmishBakedActorSnapshotExporter.Capture(em);
                Assert.IsFalse((bool)snapshot["registry_entries"][0]["selected_by_native_lookup"]);
                Assert.IsTrue((bool)snapshot["registry_entries"][1]["selected_by_native_lookup"]);
                var actor = SkirmishBakedActorSnapshotExporter.CaptureActor(em, first);
                Assert.AreEqual(0xfedcba98u, (uint)actor["components"]["UnitIdleWanderComponent"]["RandomState"]);
                Assert.AreEqual(Newtonsoft.Json.Linq.JTokenType.Null, actor["components"]["UnitFootprint"].Type);
                em.RemoveComponent<Prefab>(second);
                Assert.Throws<InvalidOperationException>(() => SkirmishBakedActorSnapshotExporter.Capture(em));
            }
            Debug.Log("[SkirmishBakedActorSnapshotTests] result=Passed controls=4 fixtureOnly=1");
            SkirmishBakedActorSnapshotExporter.Run();
        }
    }
}
#endif
