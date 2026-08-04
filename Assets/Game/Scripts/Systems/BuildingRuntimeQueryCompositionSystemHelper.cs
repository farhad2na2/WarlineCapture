using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    internal sealed class BuildingRuntimeQueryCompositionSystemHelper
    {
        private readonly BuildingRunwaySystem.GetPlacementFootprintDelegate _getPlacementFootprint;

        internal delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

        internal delegate bool TryGetGridDataDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            out Entity gridEntity,
            out GridConfig grid,
            out DynamicBuffer<GridRoad> roads,
            out DynamicBlockerComponent blockerData);

        internal delegate RectInt GetEffectivePlacementRectDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingDefinition definition,
            Vector2Int originCell,
            GridConfig grid,
            bool rotateVertical);

        public BuildingRuntimeQueryCompositionSystemHelper(
            BuildingRunwaySystem.GetPlacementFootprintDelegate getPlacementFootprint)
        {
            _getPlacementFootprint = getPlacementFootprint;
        }

        public bool IsHouseBuilding(BuildingGameplaySourceCompositionSystemHelper source, RuntimeBuildingEntity building)
        {
            if (building?.Definition == null)
                return false;

            if (building.Definition.Role == BuildingRole.House)
                return true;

            if (building.Definition.Role != BuildingRole.None)
                return false;

            GameObject prefab = building.Definition.Prefab;
            string prefabName = prefab != null ? prefab.name : string.Empty;
            if (source.BuildingGameplayDependencyCompositionSystemHelper.IsConfiguredHousePrefab(prefab))
                return true;

            return prefabName.IndexOf("house", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   !building.Definition.IsWall;
        }

        public bool TryResolveBuildingFocusWorldPosition(
            BuildingGameplaySourceCompositionSystemHelper source,
            RuntimeBuildingEntity building,
            TryGetEntityManagerDelegate tryGetEntityManager,
            out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            if (building == null)
                return false;

            if (building.Instance != null &&
                building.Definition != null &&
                source.BuildingGameplayGridDataCompositionSystemHelper.TryGetGridForSelection(
                    source.BuildingGameplayEcsQueryCompositionSystemHelper,
                    (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                    out GridConfig grid))
            {
                worldPosition = source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(
                    building.OriginCell,
                    building.Definition.FootprintCells,
                    grid,
                    source.BuildingPlacementStartupSystemHelper.BuildPlaneY);
                return true;
            }

            if (building.Instance == null)
                return false;

            worldPosition = building.Instance.transform.position;
            worldPosition.y = 0f;
            return true;
        }

        public bool TryGetRuntimeBuilding(
            BuildingGameplaySourceCompositionSystemHelper source,
            int id,
            out RuntimeBuildingEntity building)
        {
            if (source.RuntimeBuildingSystem.TryGetBuilding(id, out building) && building != null && !building.IsDestroyed)
                return true;

            building = null;
            return false;
        }

        public RectInt GetEffectivePlacementRect(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingDefinition definition,
            Vector2Int originCell,
            GridConfig grid,
            bool rotateVertical = false)
        {
            return source.BuildingRunwaySystem.GetEffectivePlacementRect(
                definition,
                originCell,
                grid,
                rotateVertical,
                source.BuildingPlacementStartupSystemHelper.BuildPlaneY,
                _getPlacementFootprint);
        }

        public bool OverlapsAnyRuntimeBuilding(
            BuildingGameplaySourceCompositionSystemHelper source,
            RectInt candidateRect,
            TryGetGridDataDelegate tryGetGridData,
            GetEffectivePlacementRectDelegate getEffectivePlacementRect)
        {
            if (source.RuntimeBuildingSystem.Buildings == null || source.RuntimeBuildingSystem.Buildings.Count == 0)
                return false;
            if (!tryGetGridData(source, out _, out GridConfig grid, out _, out _))
                return false;

            foreach (var entry in source.RuntimeBuildingSystem.Buildings)
            {
                RuntimeBuildingEntity building = entry.Value;
                if (building?.Definition == null || building.IsDestroyed)
                    continue;

                RectInt existingRect = getEffectivePlacementRect(source, building.Definition, building.OriginCell, grid, false);
                if (candidateRect.Overlaps(existingRect))
                    return true;
            }

            return false;
        }

        public bool OverlapsAnyLiveUnitFootprint(
            BuildingGameplaySourceCompositionSystemHelper source,
            RectInt candidateRect,
            TryGetEntityManagerDelegate tryGetEntityManager)
        {
            if (tryGetEntityManager == null || !tryGetEntityManager(out EntityManager entityManager))
                return false;

            source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(entityManager);
            return OverlapsAnyLiveUnitFootprint(
                entityManager,
                source.BuildingGameplayEcsQueryCompositionSystemHelper.LiveUnitFootprintQuery,
                candidateRect);
        }

        internal static bool OverlapsAnyLiveUnitFootprint(
            EntityManager entityManager,
            EntityQuery liveUnitFootprintQuery,
            RectInt candidateRect)
        {
            EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();
            ComponentTypeHandle<UnitGrid> unitGridType = entityManager.GetComponentTypeHandle<UnitGrid>(true);
            ComponentTypeHandle<UnitFootprint> footprintType = entityManager.GetComponentTypeHandle<UnitFootprint>(true);
            using NativeArray<ArchetypeChunk> chunks = liveUnitFootprintQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<UnitGrid> unitGrids = chunk.GetNativeArray(ref unitGridType);
                NativeArray<UnitFootprint> footprints = chunk.GetNativeArray(ref footprintType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    bool operationMapBuilding =
                        entityManager.HasComponent<OperationMapBuildingComponent>(entity);
                    bool runtimeBuildingProxy =
                        entityManager.HasComponent<RuntimeBuildingCombatTag>(entity) &&
                        !operationMapBuilding;
                    if (entityManager.HasComponent<Prefab>(entity) ||
                        (entityManager.HasComponent<StaticGridBlocker>(entity) && !operationMapBuilding) ||
                        runtimeBuildingProxy)
                    {
                        continue;
                    }

                    int2 size;
                    int2 min;
                    if (operationMapBuilding &&
                        entityManager.HasComponent<RuntimeBuildingCombatInfo>(entity))
                    {
                        RuntimeBuildingCombatInfo buildingInfo =
                            entityManager.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                        size = UnitFootprintUtility.ClampSize(buildingInfo.FootprintCells);
                        min = buildingInfo.OriginCell;
                    }
                    else
                    {
                        size = UnitFootprintUtility.ClampSize(footprints[i].Size);
                        min = UnitFootprintUtility.GetMinCell(unitGrids[i].Cell, size);
                    }

                    var unitRect = new RectInt(min.x, min.y, size.x, size.y);
                    if (candidateRect.Overlaps(unitRect))
                        return true;
                }
            }

            return false;
        }
    }
}
