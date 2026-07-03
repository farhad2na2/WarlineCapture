using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingRuntimeCreationCompositionSystemHelper
    {
        private const float RuntimeBuildingMaxSurfaceHeightDelta = 0.5f;
        private const float RuntimeBuildingMaxSurfaceSlopeDegrees = 45f;

        public delegate bool TryGetGridDelegate(out GridConfig grid);
        public delegate RectInt ResolvePlacementRectDelegate(BuildingDefinition definition, Vector2Int originCell, GridConfig grid);
        public delegate bool ShouldBlockPathingDelegate(BuildingDefinition definition);
        public delegate void RemoveOverlappingBlockersDelegate(Vector2Int originCell, Vector2Int footprintCells);
        public delegate Entity CreateBlockerEntityDelegate(BuildingDefinition definition, Vector2Int originCell, Vector2Int footprintCells);
        public delegate Entity CreateCombatEntityDelegate(int runtimeBuildingId, Vector2Int originCell, BuildingDefinition definition, byte ownerFactionId, Quaternion worldRotation);
        public delegate void RedirectUnitsDelegate(RectInt occupiedRect);
        public delegate void AddDeferredRedirectFootprintDelegate(RectInt occupiedRect);
        public delegate void RuntimeBuildingAction(RuntimeBuildingEntity building);
        public delegate void RuntimeAction();

        public readonly struct Context
        {
            public readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem;
            public readonly BuildingPlacementInteractionCompositionSystemHelper RuntimeLinkInteractionSystem;
            public readonly BuildingPlacementInteractionCompositionSystemHelper.Context RuntimeLinkInteractionContext;
            public readonly RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinks;
            public readonly bool DeferSideEffects;
            public readonly BuildingRuntimeOwnershipCompositionSystemHelper.TryGetEntityManagerDelegate TryGetEntityManager;
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
                RuntimeBuildingCollection<RuntimeBuildingEntity> runtimeBuildingSystem,
                BuildingPlacementInteractionCompositionSystemHelper runtimeLinkInteractionSystem,
                BuildingPlacementInteractionCompositionSystemHelper.Context runtimeLinkInteractionContext,
                RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks,
                bool deferSideEffects,
                BuildingRuntimeOwnershipCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
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
                RuntimeLinkInteractionSystem = runtimeLinkInteractionSystem;
                RuntimeLinkInteractionContext = runtimeLinkInteractionContext;
                RuntimeBuildingEntityLinks = runtimeBuildingEntityLinks;
                DeferSideEffects = deferSideEffects;
                TryGetEntityManager = tryGetEntityManager;
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

        private readonly BuildingSurfacePlacementUtilitySystemHelper _surfacePlacementSystem = new();
        private BuildingFoundationVisualPresentationSystemHelper _foundationVisualPresentationHelper;

        public RuntimeBuildingEntity RegisterRuntimeBuilding(
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

            EntityManager entityManager = default;
            bool hasEntityManager = context.TryGetEntityManager != null && context.TryGetEntityManager(out entityManager);
            MapAuthoredBuildingVisualComponent mapAuthoredVisual = instance.GetComponent<MapAuthoredBuildingVisualComponent>();
            bool preserveAuthoredTransform = mapAuthoredVisual != null && mapAuthoredVisual.PreserveAuthoredTransform;
            bool hasSurfaceResult = false;
            BuildingSurfacePlacementUtilitySystemHelper.Result surfaceResult = default;
            RectInt occupiedRect = new(originCell, definition.FootprintCells);
            if (context.TryGetGrid != null &&
                context.ResolvePlacementRect != null &&
                context.TryGetGrid(out GridConfig grid))
            {
                occupiedRect = context.ResolvePlacementRect(definition, originCell, grid);
                hasSurfaceResult = TryEvaluateRuntimeBuildingSurface(context, definition, originCell, out surfaceResult);
                if (hasSurfaceResult && !preserveAuthoredTransform)
                    FoundationVisualPresentationHelper?.ApplyVisualFoundation(instance, surfaceResult);
            }

            bool pathBlocking = context.ShouldBlockPathing == null || context.ShouldBlockPathing(definition);
            if (removeOverlappingBlockers && pathBlocking)
                context.RemoveOverlappingBlockers?.Invoke(originCell, definition.FootprintCells);

            Entity blockerEntity = pathBlocking && context.CreateBlockerEntity != null
                ? context.CreateBlockerEntity(definition, originCell, definition.FootprintCells)
                : Entity.Null;
            Entity combatEntity = context.CreateCombatEntity != null
                ? context.CreateCombatEntity(buildingId, originCell, definition, 0, instance.transform.rotation)
                : Entity.Null;
            if (hasEntityManager && hasSurfaceResult && !preserveAuthoredTransform)
                FoundationVisualPresentationHelper?.ApplyCombatEntityFoundation(entityManager, combatEntity, surfaceResult, _surfacePlacementSystem);

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

            var building = new RuntimeBuildingEntity
            {
                Id = buildingId,
                Definition = definition,
                Instance = instance,
                OriginCell = originCell,
                CombatEntity = combatEntity,
                BlockerEntity = blockerEntity,
                ProductionSpawnLocalPositions = definition.ProductionSpawnLocalPositions,
                ProducedUnits = new List<Entity>(),
                PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>(),
                StoredOilBarrels = 0f,
                StoredFuelBarrels = 0f
            };
            if (building.ProductionSpawnLocalPositions != null && building.ProductionSpawnLocalPositions.Length > 0)
                building.ProducedUnitSlots = new Entity[building.ProductionSpawnLocalPositions.Length];

            context.InitializeVisuals?.Invoke(building);
            AttachRuntimeLink(
                context.RuntimeLinkInteractionSystem,
                context.RuntimeLinkInteractionContext,
                context.RuntimeBuildingEntityLinks,
                building);
            context.RuntimeBuildingSystem.AddBuilding(building.Id, building);

            if (context.DeferSideEffects)
                context.MarkPendingMarkerRefresh?.Invoke();
            else
                context.RefreshMarkers?.Invoke();

            return building;
        }

        private bool TryEvaluateRuntimeBuildingSurface(
            Context context,
            BuildingDefinition definition,
            Vector2Int originCell,
            out BuildingSurfacePlacementUtilitySystemHelper.Result surfaceResult)
        {
            surfaceResult = default;
            if (context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager entityManager) ||
                !_surfacePlacementSystem.TryEvaluateFootprint(
                    entityManager,
                    originCell,
                    definition.FootprintCells,
                    RuntimeBuildingMaxSurfaceHeightDelta,
                    RuntimeBuildingMaxSurfaceSlopeDegrees,
                    out surfaceResult))
            {
                return false;
            }

            return true;
        }

        private static void AttachRuntimeLink(
            BuildingPlacementInteractionCompositionSystemHelper interactionSystem,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            RuntimeBuildingEntityLinkRegistry registry,
            RuntimeBuildingEntity building)
        {
            if (interactionSystem == null || building?.Instance == null)
                return;

            RuntimeBuildingEntityLink link = building.Instance.GetComponent<RuntimeBuildingEntityLink>();
            if (link == null)
                link = building.Instance.AddComponent<RuntimeBuildingEntityLink>();

            link.Configure(interactionSystem, interactionContext, registry, building.Id, building.CombatEntity, building.BlockerEntity);
        }

        private BuildingFoundationVisualPresentationSystemHelper FoundationVisualPresentationHelper =>
            _foundationVisualPresentationHelper ??= ResolveBuildingFoundationVisualPresentationHelper();

        private static BuildingFoundationVisualPresentationSystemHelper ResolveBuildingFoundationVisualPresentationHelper()
        {
            return new BuildingFoundationVisualPresentationSystemHelper();
        }
    }
}
