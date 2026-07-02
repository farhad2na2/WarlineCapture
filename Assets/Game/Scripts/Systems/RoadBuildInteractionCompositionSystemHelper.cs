using Unity.Entities;
using UnityEngine;
using static UnityEngine.Object;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class RoadBuildInteractionCompositionSystemHelper
    {
        public delegate bool IsPointerOverUiDelegate(Vector2 screenPosition);
        public delegate bool TryGetGridCellDelegate(Vector2 screenPosition, GridConfig grid, out Vector2Int cell);

        public readonly struct Context
        {
            public readonly RoadBuildPlacementStorageCompositionSystemHelper StorageSystem;
            public readonly RoadBuildEcsCompositionSystemHelper EcsSystem;
            public readonly RoadBuildEcsCompositionSystemHelper.Context EcsContext;
            public readonly RuntimeGridBlockerPresentationSystemHelper RuntimeGridBlockers;
            public readonly RoadBuildEcsCompositionSystemHelper.TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly RoadBuildEcsCompositionSystemHelper.TryGetGridDataDelegate TryGetGridData;
            public readonly TryGetGridCellDelegate TryGetGridCell;
            public readonly IsPointerOverUiDelegate IsPointerOverUi;

            public Context(
                RoadBuildPlacementStorageCompositionSystemHelper storageSystem,
                RoadBuildEcsCompositionSystemHelper ecsSystem,
                RoadBuildEcsCompositionSystemHelper.Context ecsContext,
                RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
                RoadBuildEcsCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
                RoadBuildEcsCompositionSystemHelper.TryGetGridDataDelegate tryGetGridData,
                TryGetGridCellDelegate tryGetGridCell,
                IsPointerOverUiDelegate isPointerOverUi)
            {
                StorageSystem = storageSystem;
                EcsSystem = ecsSystem;
                EcsContext = ecsContext;
                RuntimeGridBlockers = runtimeGridBlockers;
                TryGetEntityManager = tryGetEntityManager;
                TryGetGridData = tryGetGridData;
                TryGetGridCell = tryGetGridCell;
                IsPointerOverUi = isPointerOverUi;
            }
        }

        public RuntimeBuildingEntity PlaceBuilding(Context context, BuildingPlacementLifecycleCompositionSystemHelper.PlacementState placement)
        {
            if (placement?.PreviewInstance == null || context.StorageSystem == null)
                return null;

            GameObject previewInstance = placement.PreviewInstance;
            int buildingId = context.StorageSystem.AllocateBuildingId();
            previewInstance.name = $"{placement.Definition.DisplayName}_{buildingId}";

            context.RuntimeGridBlockers?.RemoveBlockersOverlappingFootprint(
                placement.OriginCell,
                placement.Definition.FootprintCells);
            Entity blockerEntity = context.EcsSystem.CreateBlockerEntity(
                context.EcsContext,
                placement.OriginCell,
                placement.Definition.FootprintCells);
            Entity combatEntity = context.EcsSystem.CreateBuildingCombatEntity(
                context.EcsContext,
                placement.OriginCell,
                placement.Definition);

            var building = new RuntimeBuildingEntity
            {
                Id = buildingId,
                Definition = placement.Definition,
                Instance = previewInstance,
                OriginCell = placement.OriginCell,
                CombatEntity = combatEntity,
                BlockerEntity = blockerEntity
            };

            context.EcsSystem.AttachRuntimeLink(context.EcsContext, building);
            context.StorageSystem.AddBuilding(building);
            context.StorageSystem.ReleaseActivePlacementPreview();
            return building;
        }

        public void HandleBuildingSelectionClick(Context context, Vector2 screenPosition)
        {
            if (context.IsPointerOverUi != null && context.IsPointerOverUi(screenPosition))
                return;

            if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
                return;

            if (context.TryGetGridCell == null || !context.TryGetGridCell(screenPosition, grid, out Vector2Int cell))
            {
                context.StorageSystem?.ClearSelection();
                return;
            }

            foreach (var entry in context.StorageSystem.RuntimeBuildings)
            {
                Vector2Int min = entry.Value.OriginCell;
                Vector2Int size = entry.Value.Definition.FootprintCells;
                if (cell.x < min.x || cell.y < min.y || cell.x >= min.x + size.x || cell.y >= min.y + size.y)
                    continue;

                SelectBuilding(context, entry.Key);
                return;
            }

            context.StorageSystem?.ClearSelection();
        }

        public void SelectBuilding(Context context, int buildingId)
        {
            context.StorageSystem?.SelectBuilding(buildingId);
        }

        public void DeleteBuilding(Context context, int buildingId, bool destroyVisual)
        {
            if (context.StorageSystem == null ||
                !context.StorageSystem.TryGetBuilding(buildingId, out RuntimeBuildingEntity building))
                return;

            if (building.CombatEntity != Entity.Null &&
                context.TryGetEntityManager != null &&
                context.TryGetEntityManager(out EntityManager em) &&
                em.Exists(building.CombatEntity))
            {
                em.DestroyEntity(building.CombatEntity);
            }

            if (destroyVisual && building.Instance != null)
                Destroy(building.Instance);

            if (building.BlockerEntity != Entity.Null &&
                context.TryGetEntityManager != null &&
                context.TryGetEntityManager(out em) &&
                em.Exists(building.BlockerEntity))
            {
                em.DestroyEntity(building.BlockerEntity);
            }

            context.StorageSystem.RemoveBuilding(buildingId);
        }
    }
}
