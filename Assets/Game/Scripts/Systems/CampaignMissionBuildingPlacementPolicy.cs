using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal static class CampaignMissionBuildingPlacementPolicy
    {
        internal static bool IsAllowed(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingDefinition building,
            RectInt placement)
        {
            return !source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager) ||
                   IsAllowed(entityManager, source.BuildingGameplayEcsQueryCompositionSystemHelper, building, placement);
        }

        internal static bool IsAllowed(
            EntityManager entityManager,
            BuildingGameplayEcsQueryCompositionSystemHelper queries,
            BuildingDefinition building,
            RectInt placement)
        {
            queries.EnsureEntityQueries(entityManager);
            EntityQuery missionQuery = queries.CampaignMissionQuery;
            int missionCount = missionQuery.CalculateEntityCount();
            if (missionCount == 0)
                return true;
            if (missionCount != 1)
                return false;

            Entity missionEntity = missionQuery.GetSingletonEntity();
            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(missionEntity);
            if (runtime.Phase == MissionPhaseKind.None || runtime.MissionId.IsEmpty)
                return true;

            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(missionEntity);
            if (!catalog.Blob.IsCreated || runtime.SourceVersion == 0 ||
                runtime.SourceVersion != catalog.SourceVersion)
                return false;

            ref CampaignMissionCatalogBlob catalogBlob = ref catalog.Blob.Value;
            for (int index = 0; index < catalogBlob.Missions.Length; index++)
            {
                ref CampaignMissionDefinitionBlob mission = ref catalogBlob.Missions[index];
                if (!mission.MissionId.Equals(runtime.MissionId))
                    continue;
                if (mission.MissionRuntimeEnabled == 0)
                    return true;
                return IsAllowedForMission(
                    entityManager, queries.OperationMapQuery, runtime, ref mission, building, placement);
            }

            return false;
        }

        private static bool IsAllowedForMission(
            EntityManager entityManager,
            EntityQuery operationMapQuery,
            in CampaignMissionRuntimeComponent runtime,
            ref CampaignMissionDefinitionBlob mission,
            BuildingDefinition building,
            RectInt placement)
        {
            if (!IsCatalogBuilding(ref mission.BuildCatalog, building) ||
                mission.BuildZone.AnchorId.IsEmpty || mission.BuildZone.HalfWidthCells <= 0 ||
                mission.BuildZone.HalfHeightCells <= 0 || operationMapQuery.CalculateEntityCount() != 1)
                return false;

            Entity mapEntity = operationMapQuery.GetSingletonEntity();
            ActiveOperationMapComponent active =
                entityManager.GetComponentData<ActiveOperationMapComponent>(mapEntity);
            OperationMapMetadataComponent metadata =
                entityManager.GetComponentData<OperationMapMetadataComponent>(mapEntity);
            if (!active.MissionId.Equals(runtime.MissionId) ||
                !active.ScenarioId.Equals(runtime.ScenarioId) ||
                !active.OperationMapId.Equals(runtime.OperationMapId) ||
                metadata.Generation != active.Generation || !metadata.Blob.IsCreated ||
                !metadata.Blob.Value.OperationMapId.Equals(active.OperationMapId))
                return false;

            ref OperationMapBlob map = ref metadata.Blob.Value;
            if (!OperationMapMetadataUtility.TryFindAnchor(
                    ref map, mission.BuildZone.AnchorId, out OperationMapAnchorBlob anchor) ||
                anchor.Kind != OperationMapAnchorKind.Build ||
                map.Grid.Dimensions.x <= 0 || map.Grid.Dimensions.y <= 0 ||
                !math.isfinite(map.Grid.CellSize) || map.Grid.CellSize <= 0f)
                return false;

            GridConfig grid = new()
            {
                Width = map.Grid.Dimensions.x,
                Height = map.Grid.Dimensions.y,
                CellSize = map.Grid.CellSize,
                Origin = map.Grid.Origin
            };
            int2 center = GridUtils.WorldToCell(in grid, anchor.Position);
            RectInt zone = new(
                center.x - mission.BuildZone.HalfWidthCells,
                center.y - mission.BuildZone.HalfHeightCells,
                mission.BuildZone.HalfWidthCells * 2,
                mission.BuildZone.HalfHeightCells * 2);
            return placement.xMin >= zone.xMin && placement.yMin >= zone.yMin &&
                   placement.xMax <= zone.xMax && placement.yMax <= zone.yMax;
        }

        private static bool IsCatalogBuilding(
            ref BlobArray<CampaignMissionBuildEntryBlob> catalog,
            BuildingDefinition building)
        {
            if (building?.Prefab == null)
                return false;

            FixedString64Bytes prefabId = new(building.Prefab.name);
            for (int index = 0; index < catalog.Length; index++)
            {
                if (catalog[index].MaxCount > 0 && catalog[index].BuildingConfigId.Equals(prefabId))
                    return true;
            }

            return false;
        }
    }
}
