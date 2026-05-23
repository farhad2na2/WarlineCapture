using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using RuntimeBuildingData = BuildingPlacementSystem.RuntimeBuildingData;
using BuildingDefinition = BuildingPlacementSystem.BuildingDefinition;

internal sealed class BuildingRuntimeCreationSystem
{
    public delegate bool TryGetGridDelegate(out GridConfig grid);
    public delegate RectInt ResolvePlacementRectDelegate(BuildingDefinition definition, Vector2Int originCell, GridConfig grid);
    public delegate bool ShouldBlockPathingDelegate(BuildingDefinition definition);
    public delegate void RemoveOverlappingBlockersDelegate(Vector2Int originCell, Vector2Int footprintCells);
    public delegate Entity CreateBlockerEntityDelegate(BuildingDefinition definition, Vector2Int originCell, Vector2Int footprintCells);
    public delegate Entity CreateCombatEntityDelegate(Vector2Int originCell, BuildingDefinition definition, byte ownerFactionId, Quaternion worldRotation);
    public delegate void RedirectUnitsDelegate(RectInt occupiedRect);
    public delegate void AddDeferredRedirectFootprintDelegate(RectInt occupiedRect);
    public delegate void RuntimeBuildingAction(RuntimeBuildingData building);
    public delegate void RuntimeAction();

    public readonly struct Context
    {
        public readonly RuntimeBuildingSystem<RuntimeBuildingData> RuntimeBuildingSystem;
        public readonly BuildingPlacementSystem RuntimeLinkOwner;
        public readonly bool DeferSideEffects;
        public readonly TryGetGridDelegate TryGetGrid;
        public readonly ResolvePlacementRectDelegate ResolvePlacementRect;
        public readonly ShouldBlockPathingDelegate ShouldBlockPathing;
        public readonly RemoveOverlappingBlockersDelegate RemoveOverlappingBlockers;
        public readonly CreateBlockerEntityDelegate CreateBlockerEntity;
        public readonly CreateCombatEntityDelegate CreateCombatEntity;
        public readonly RedirectUnitsDelegate RedirectUnits;
        public readonly AddDeferredRedirectFootprintDelegate AddDeferredRedirectFootprint;
        public readonly RuntimeAction MarkPendingMarkerRefresh;
        public readonly RuntimeBuildingAction InitializeVisuals;
        public readonly RuntimeAction RefreshMarkers;

        public Context(
            RuntimeBuildingSystem<RuntimeBuildingData> runtimeBuildingSystem,
            BuildingPlacementSystem runtimeLinkOwner,
            bool deferSideEffects,
            TryGetGridDelegate tryGetGrid,
            ResolvePlacementRectDelegate resolvePlacementRect,
            ShouldBlockPathingDelegate shouldBlockPathing,
            RemoveOverlappingBlockersDelegate removeOverlappingBlockers,
            CreateBlockerEntityDelegate createBlockerEntity,
            CreateCombatEntityDelegate createCombatEntity,
            RedirectUnitsDelegate redirectUnits,
            AddDeferredRedirectFootprintDelegate addDeferredRedirectFootprint,
            RuntimeAction markPendingMarkerRefresh,
            RuntimeBuildingAction initializeVisuals,
            RuntimeAction refreshMarkers)
        {
            RuntimeBuildingSystem = runtimeBuildingSystem;
            RuntimeLinkOwner = runtimeLinkOwner;
            DeferSideEffects = deferSideEffects;
            TryGetGrid = tryGetGrid;
            ResolvePlacementRect = resolvePlacementRect;
            ShouldBlockPathing = shouldBlockPathing;
            RemoveOverlappingBlockers = removeOverlappingBlockers;
            CreateBlockerEntity = createBlockerEntity;
            CreateCombatEntity = createCombatEntity;
            RedirectUnits = redirectUnits;
            AddDeferredRedirectFootprint = addDeferredRedirectFootprint;
            MarkPendingMarkerRefresh = markPendingMarkerRefresh;
            InitializeVisuals = initializeVisuals;
            RefreshMarkers = refreshMarkers;
        }
    }

    public RuntimeBuildingData RegisterRuntimeBuilding(
        Context context,
        BuildingDefinition definition,
        GameObject instance,
        Vector2Int originCell,
        bool removeOverlappingBlockers)
    {
        if (context.RuntimeBuildingSystem == null || definition == null || instance == null)
            return null;

        int buildingId = context.RuntimeBuildingSystem.AllocateId();
        instance.name = $"{definition.DisplayName}_{buildingId}";

        RectInt occupiedRect = new(originCell, definition.FootprintCells);
        if (context.TryGetGrid != null &&
            context.ResolvePlacementRect != null &&
            context.TryGetGrid(out GridConfig grid))
        {
            occupiedRect = context.ResolvePlacementRect(definition, originCell, grid);
        }

        bool pathBlocking = context.ShouldBlockPathing == null || context.ShouldBlockPathing(definition);
        if (removeOverlappingBlockers && pathBlocking)
            context.RemoveOverlappingBlockers?.Invoke(originCell, definition.FootprintCells);

        Entity blockerEntity = pathBlocking && context.CreateBlockerEntity != null
            ? context.CreateBlockerEntity(definition, originCell, definition.FootprintCells)
            : Entity.Null;
        Entity combatEntity = context.CreateCombatEntity != null
            ? context.CreateCombatEntity(originCell, definition, 0, instance.transform.rotation)
            : Entity.Null;

        if (context.DeferSideEffects)
        {
            if (pathBlocking)
                context.AddDeferredRedirectFootprint?.Invoke(occupiedRect);
            context.MarkPendingMarkerRefresh?.Invoke();
        }
        else if (pathBlocking)
        {
            context.RedirectUnits?.Invoke(occupiedRect);
        }

        var building = new RuntimeBuildingData
        {
            Id = buildingId,
            Definition = definition,
            Instance = instance,
            OriginCell = originCell,
            CombatEntity = combatEntity,
            BlockerEntity = blockerEntity,
            ProductionSpawnLocalPositions = definition.ProductionSpawnLocalPositions,
            ProducedUnits = new List<Entity>(),
            PendingProductions = new List<RuntimeBuildingData.PendingProduction>(),
            StoredOilBarrels = 0f,
            StoredFuelBarrels = 0f
        };
        if (building.ProductionSpawnLocalPositions != null && building.ProductionSpawnLocalPositions.Length > 0)
            building.ProducedUnitSlots = new Entity[building.ProductionSpawnLocalPositions.Length];

        context.InitializeVisuals?.Invoke(building);
        AttachRuntimeLink(context.RuntimeLinkOwner, building);
        context.RuntimeBuildingSystem.AddBuilding(building.Id, building);

        if (context.DeferSideEffects)
            context.MarkPendingMarkerRefresh?.Invoke();
        else
            context.RefreshMarkers?.Invoke();

        return building;
    }

    private static void AttachRuntimeLink(BuildingPlacementSystem owner, RuntimeBuildingData building)
    {
        if (owner == null || building?.Instance == null)
            return;

        RuntimeBuildingEntityLink link = building.Instance.GetComponent<RuntimeBuildingEntityLink>();
        if (link == null)
            link = building.Instance.AddComponent<RuntimeBuildingEntityLink>();

        link.Configure(owner, building.Id, building.CombatEntity, building.BlockerEntity);
    }
}
