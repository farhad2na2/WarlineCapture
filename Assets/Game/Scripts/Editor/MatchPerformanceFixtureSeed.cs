using System;
using Game.Components;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Editor
{
    public static class MatchPerformanceFixtureSeed
    {
        public const int TargetSourceEntityCount = 733;
        public const int TargetBuildingCount = 628;
        public const int TargetRenderVisualStateCount = 105;
        public const int TargetCulledUnitCount = 59;
        public const int WarmupFrames = 60;

        private const string FallbackBuildingSourceKey = "Building_Performance_Fixture";
        private const string FallbackCharacterSourceKey = "Unit_Chr_Soldier_Male_02_Alt_04";

        public readonly struct Result
        {
            public readonly int InitialSourceEntityCount;
            public readonly int InitialBuildingCount;
            public readonly int InitialRenderVisualStateCount;
            public readonly int InitialCulledUnitCount;
            public readonly int AddedBuildingCount;
            public readonly int AddedUnitCount;
            public readonly int FinalSourceEntityCount;
            public readonly int FinalBuildingCount;
            public readonly int FinalRenderVisualStateCount;
            public readonly int FinalCulledUnitCount;

            public Result(
                int initialSourceEntityCount,
                int initialBuildingCount,
                int initialRenderVisualStateCount,
                int initialCulledUnitCount,
                int addedBuildingCount,
                int addedUnitCount,
                int finalSourceEntityCount,
                int finalBuildingCount,
                int finalRenderVisualStateCount,
                int finalCulledUnitCount)
            {
                InitialSourceEntityCount = initialSourceEntityCount;
                InitialBuildingCount = initialBuildingCount;
                InitialRenderVisualStateCount = initialRenderVisualStateCount;
                InitialCulledUnitCount = initialCulledUnitCount;
                AddedBuildingCount = addedBuildingCount;
                AddedUnitCount = addedUnitCount;
                FinalSourceEntityCount = finalSourceEntityCount;
                FinalBuildingCount = finalBuildingCount;
                FinalRenderVisualStateCount = finalRenderVisualStateCount;
                FinalCulledUnitCount = finalCulledUnitCount;
            }

            public bool AddedEntities => AddedBuildingCount > 0 || AddedUnitCount > 0;

            public override string ToString()
            {
                return
                    $"performanceFixture=ready addedBuildings={AddedBuildingCount} addedUnits={AddedUnitCount} " +
                    $"sourceEntities={FinalSourceEntityCount} buildings={FinalBuildingCount} " +
                    $"renderStates={FinalRenderVisualStateCount} culledUnits={FinalCulledUnitCount}";
            }
        }

        public static Result Ensure(EntityManager entityManager)
        {
            int initialSourceEntityCount = Count<UnitSourcePrefabKey>(entityManager);
            int initialBuildingCount = Count<RuntimeBuildingCombatTag>(entityManager);
            int initialRenderVisualStateCount = Count<UnitRenderVisualComponent>(entityManager);
            int initialCulledUnitCount = Count<UnitRenderBudgetCulledUnitTag>(entityManager);

            int addedBuildingCount = math.max(0, TargetBuildingCount - initialBuildingCount);
            int sourceCountAfterBuildings = initialSourceEntityCount + addedBuildingCount;
            int missingSourceUnits = math.max(0, TargetSourceEntityCount - sourceCountAfterBuildings);
            int missingRenderVisualStates = math.max(0, TargetRenderVisualStateCount - initialRenderVisualStateCount);
            int missingCulledUnits = math.max(0, TargetCulledUnitCount - initialCulledUnitCount);
            int addedUnitCount = math.max(missingSourceUnits, math.max(missingRenderVisualStates, missingCulledUnits));

            if (addedBuildingCount > 0 || addedUnitCount > 0)
            {
                entityManager.CompleteAllTrackedJobs();
                FixedString64Bytes buildingSourceKey = ResolveBuildingSourceKey(entityManager);
                FixedString64Bytes characterSourceKey = ResolveCharacterSourceKey(entityManager);
                AddBuildings(entityManager, addedBuildingCount, buildingSourceKey);
                AddUnits(entityManager, addedUnitCount, missingCulledUnits, characterSourceKey);
            }

            return new Result(
                initialSourceEntityCount,
                initialBuildingCount,
                initialRenderVisualStateCount,
                initialCulledUnitCount,
                addedBuildingCount,
                addedUnitCount,
                Count<UnitSourcePrefabKey>(entityManager),
                Count<RuntimeBuildingCombatTag>(entityManager),
                Count<UnitRenderVisualComponent>(entityManager),
                Count<UnitRenderBudgetCulledUnitTag>(entityManager));
        }

        private static void AddBuildings(
            EntityManager entityManager,
            int count,
            FixedString64Bytes sourceKey)
        {
            if (count <= 0)
                return;

            EntityArchetype archetype = entityManager.CreateArchetype(
                typeof(UnitSourcePrefabKey),
                typeof(RuntimeBuildingCombatTag),
                typeof(RuntimeBuildingCombatInfo),
                typeof(Faction),
                typeof(UnitGrid),
                typeof(StaticGridBlocker),
                typeof(LocalTransform));

            for (int i = 0; i < count; i++)
            {
                int2 cell = new(128 + (i % 32) * 4, 128 + (i / 32) * 4);
                float3 position = new(cell.x, 0f, cell.y);
                Entity entity = entityManager.CreateEntity(archetype);
                entityManager.SetComponentData(entity, new UnitSourcePrefabKey { Value = sourceKey });
                entityManager.SetComponentData(entity, new RuntimeBuildingCombatInfo
                {
                    RuntimeBuildingId = int.MaxValue - i,
                    OwnerFactionId = FactionIdentity.NeutralFactionId,
                    OriginCell = cell,
                    FootprintCells = new int2(2, 2)
                });
                entityManager.SetComponentData(entity, new Faction { Id = FactionIdentity.NeutralFactionId });
                entityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
                entityManager.SetComponentData(
                    entity,
                    LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));
            }
        }

        private static void AddUnits(
            EntityManager entityManager,
            int count,
            int culledCount,
            FixedString64Bytes sourceKey)
        {
            if (count <= 0)
                return;

            ResolvePlacementBasis(out float3 nearCenter, out float3 right, out float3 forward);
            int clampedCulledCount = math.clamp(culledCount, 0, count);
            int visibleCount = count - clampedCulledCount;
            EntityArchetype visibleArchetype = entityManager.CreateArchetype(
                typeof(UnitSourcePrefabKey),
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitHealth),
                typeof(UnitRenderVisualComponent),
                typeof(LocalTransform));
            EntityArchetype culledArchetype = entityManager.CreateArchetype(
                typeof(UnitSourcePrefabKey),
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitHealth),
                typeof(UnitMovementBehavior),
                typeof(UnitRenderVisualComponent),
                typeof(UnitRenderBudgetCulledUnitTag),
                typeof(LocalTransform));

            for (int i = 0; i < count; i++)
            {
                bool culled = i >= visibleCount;
                int culledIndex = i - visibleCount;
                float lateralOffset = (i - (visibleCount - 1) * 0.5f) * 0.75f;
                float3 position = culled
                    ? nearCenter + right * (400f + (culledIndex % 16) * 6f) + forward * (180f + (culledIndex / 16) * 8f)
                    : nearCenter + right * lateralOffset;
                int2 cell = new((int)math.round(position.x), (int)math.round(position.z));
                byte visual = culled
                    ? (byte)UnitRenderVisualKind.Far
                    : (byte)UnitRenderVisualKind.Detail;

                Entity entity = entityManager.CreateEntity(culled ? culledArchetype : visibleArchetype);
                entityManager.SetComponentData(entity, new UnitSourcePrefabKey { Value = sourceKey });
                entityManager.SetComponentData(entity, new Faction { Id = FactionIdentity.NeutralFactionId });
                entityManager.SetComponentData(entity, new UnitGrid { Cell = cell });
                entityManager.SetComponentData(entity, new UnitHealth { Current = 10000, Max = 10000 });
                if (culled)
                {
                    entityManager.SetComponentData(entity, new UnitMovementBehavior
                    {
                        AllowIdleWander = 0,
                        UsesVehicleMotion = 0
                    });
                }
                entityManager.SetComponentData(entity, new UnitRenderVisualComponent
                {
                    Current = visual,
                    Desired = visual,
                    LastChangedFrame = UnityEngine.Time.frameCount
                });
                entityManager.SetComponentData(
                    entity,
                    LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));
            }
        }

        private static void ResolvePlacementBasis(out float3 center, out float3 right, out float3 forward)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (RuntimeCameraReferenceSystem.TryGetCameraSnapshot(world, out RuntimeCameraSnapshotComponent camera))
            {
                right = math.normalizesafe(math.mul(camera.Rotation, new float3(1f, 0f, 0f)), new float3(1f, 0f, 0f));
                forward = math.normalizesafe(math.mul(camera.Rotation, new float3(0f, 0f, 1f)), new float3(0f, 0f, 1f));
                center = camera.Position + forward * 20f;
                return;
            }

            center = new float3(512f, 0f, 512f);
            right = new float3(1f, 0f, 0f);
            forward = new float3(0f, 0f, 1f);
        }

        private static FixedString64Bytes ResolveBuildingSourceKey(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                    ComponentType.ReadOnly<RuntimeBuildingCombatTag>()
                }
            });
            using NativeArray<UnitSourcePrefabKey> sourceKeys =
                query.ToComponentDataArray<UnitSourcePrefabKey>(Allocator.Temp);
            return sourceKeys.Length > 0
                ? sourceKeys[0].Value
                : new FixedString64Bytes(FallbackBuildingSourceKey);
        }

        private static FixedString64Bytes ResolveCharacterSourceKey(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                None = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatTag>() }
            });
            using NativeArray<UnitSourcePrefabKey> sourceKeys =
                query.ToComponentDataArray<UnitSourcePrefabKey>(Allocator.Temp);
            for (int i = 0; i < sourceKeys.Length; i++)
            {
                string key = sourceKeys[i].Value.ToString();
                if (key.StartsWith("Unit_Chr_", StringComparison.OrdinalIgnoreCase))
                    return sourceKeys[i].Value;
            }

            return new FixedString64Bytes(FallbackCharacterSourceKey);
        }

        private static int Count<T>(EntityManager entityManager)
            where T : unmanaged, IComponentData
        {
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.CalculateEntityCount();
        }
    }
}
