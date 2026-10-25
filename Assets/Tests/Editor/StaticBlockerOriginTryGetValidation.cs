#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Tests.Editor
{
    public static class StaticBlockerOriginTryGetValidation
    {
        sealed class Fixture : IDisposable
        {
            internal readonly World World = new World("blocker-origin-try-get");
            internal EntityManager Em => World.EntityManager;
            internal readonly Entity Grid, Actor;
            readonly SystemHandle writer;
            internal Fixture()
            {
                Grid = Em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent));
                Em.SetComponentData(Grid, new GridConfig { Width = 8, Height = 8, CellSize = 1, Origin = float3.zero });
                Em.SetComponentData(Grid, new DynamicBlockerComponent { GridSize = 64, Counts = new NativeArray<int>(64, Allocator.Persistent), Blocked = new NativeBitArray(64, Allocator.Persistent), FriendlyPassFactionIds = new NativeArray<byte>(64, Allocator.Persistent), StorageEpoch = 1 });
                Actor = Em.CreateEntity(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize), typeof(OperationMapBuildingComponent));
                Em.SetComponentData(Actor, new UnitGrid { Cell = new int2(1, 1) });
                Em.SetComponentData(Actor, new GridBlockerSize { Size = new int2(2, 2) });
                writer = World.CreateSystem<StaticGridBlockerUpdateSystem>();
            }
            internal void Info(int x, int z)
            {
                var value = new RuntimeBuildingCombatInfo { OriginCell = new int2(x, z) };
                if (Em.HasComponent<RuntimeBuildingCombatInfo>(Actor)) Em.SetComponentData(Actor, value);
                else Em.AddComponentData(Actor, value);
            }
            internal void Tick() { writer.Update(World.Unmanaged); Em.CompleteAllTrackedJobs(); }
            internal void Expect(int x, int z)
            {
                var data = Em.GetComponentData<DynamicBlockerComponent>(Grid);
                for (int i = 0; i < 64; i++)
                {
                    bool inside = i % 8 >= x && i % 8 < x + 2 && i / 8 >= z && i / 8 < z + 2;
                    Assert.AreEqual(inside ? 1 : 0, data.Counts[i], "count " + i);
                    Assert.AreEqual(inside, data.Blocked.IsSet(i), "blocked " + i);
                }
                Assert.AreEqual(new int2(x, z), Em.GetComponentData<StaticBlockerPrevBounds>(Actor).Min);
                Assert.AreEqual(data.StorageEpoch, data.AppliedStorageEpoch);
                Assert.AreEqual(0, data.WriteInProgress);
            }
            public void Dispose()
            {
                var data = Em.GetComponentData<DynamicBlockerComponent>(Grid);
                data.Counts.Dispose(); data.Blocked.Dispose(); data.FriendlyPassFactionIds.Dispose(); World.Dispose();
            }
        }
        public static void RunFocusedValidation()
        {
            // Existing independent original-system parity and all storage-epoch controls stay unchanged.
            new SharedStaticBlockerKernelTests().NativeOriginalWriterParity();
            GridBlockerStorageEpochTests.RunFocusedValidation();
            using (var f = new Fixture())
            {
                f.Info(5, 1); f.Tick(); f.Expect(5, 1); // New authored origin.
                f.Info(3, 4); f.Tick(); f.Expect(3, 4); // Existing live update.
                f.Em.RemoveComponent<RuntimeBuildingCombatInfo>(f.Actor); f.Tick(); f.Expect(1, 1);
                f.Info(5, 5); f.Tick(); f.Expect(5, 5); // Recreated component is read live.
                f.Em.RemoveComponent<OperationMapBuildingComponent>(f.Actor); f.Tick(); f.Expect(1, 1);
            }
            using (var f = new Fixture())
            {
                f.Tick(); f.Expect(1, 1); // Authored marker without info retains the original fallback.
            }
            using (var f = new Fixture())
            {
                f.Info(5, 1); f.Em.AddComponent<Disabled>(f.Actor); f.Tick();
                Assert.IsFalse(f.Em.HasComponent<StaticBlockerPrevBounds>(f.Actor));
                f.Em.RemoveComponent<Disabled>(f.Actor); f.Tick(); f.Expect(5, 1);
            }
            Debug.Log("[StaticBlockerOriginTryGetValidation] result=Passed origin_cases=8 original_parity=Passed epochs=Passed scope=IsolatedWriterNotMissionReadiness");
        }
    }
}
#endif
