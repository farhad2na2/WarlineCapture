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
        public delegate Entity CreateBlockerEntityDelegate(BuildingDefinition definition, Vector2Int originCell, Vector2Int footprintCells, RuntimeActorBirthContext birth);
        public delegate Entity CreateCombatEntityDelegate(int runtimeBuildingId, Vector2Int originCell, BuildingDefinition definition, byte ownerFactionId, Quaternion worldRotation, RuntimeActorBirthContext birth);
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
            public readonly System.Func<bool> DeferSideEffects;
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
                System.Func<bool> deferSideEffects,
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
            bool removeOverlappingBlockers, RuntimeActorBirthContext birth = default)
        {
            if (context.RuntimeBuildingSystem == null || definition == null || instance == null)
                return null;

            EntityManager entityManager = default;
            bool hasEntityManager = false;
            bool preserveAuthoredTransform = false;
            bool hasSurfaceResult = false;
            BuildingSurfacePlacementUtilitySystemHelper.Result surfaceResult = default;
            RectInt occupiedRect = default;
            Entity blockerEntity = Entity.Null;
            Entity combatEntity = Entity.Null;
            RuntimeBuildingEntity building = null;
            Game.Simulation.BuildingRegistrationKernel.Register(
                new Game.Simulation.BuildingRegistrationKernel.Context
                {
                    DeferSideEffects = context.DeferSideEffects,
                    AllocateId = context.RuntimeBuildingSystem.AllocateId,
                    PrepareSourceData = buildingId =>
                    {
                        instance.name = $"{definition.DisplayName}_{buildingId}";
                        hasEntityManager = context.TryGetEntityManager != null && context.TryGetEntityManager(out entityManager);
                        MapAuthoredBuildingVisualComponent mapAuthoredVisual = instance.GetComponent<MapAuthoredBuildingVisualComponent>();
                        preserveAuthoredTransform = mapAuthoredVisual != null && mapAuthoredVisual.PreserveAuthoredTransform;
                        occupiedRect = new RectInt(originCell, definition.FootprintCells);
                        if (context.TryGetGrid != null && context.ResolvePlacementRect != null &&
                            context.TryGetGrid(out GridConfig grid))
                        {
                            occupiedRect = context.ResolvePlacementRect(definition, originCell, grid);
                            hasSurfaceResult = TryEvaluateRuntimeBuildingSurface(context, definition, originCell, out surfaceResult);
                            if (hasSurfaceResult && !preserveAuthoredTransform)
                                FoundationVisuals?.ApplyVisualFoundation(instance, surfaceResult);
                        }
                    },
                    ShouldBlockPathing = () => context.ShouldBlockPathing == null || context.ShouldBlockPathing(definition),
                    RemoveOverlappingBlockers = () => context.RemoveOverlappingBlockers?.Invoke(originCell, definition.FootprintCells),
                    CreateActorsAndFoundation = (buildingId, pathBlocking) =>
                    {
                        blockerEntity = pathBlocking && context.CreateBlockerEntity != null
                            ? context.CreateBlockerEntity(definition, originCell, definition.FootprintCells, birth)
                            : Entity.Null;
                        combatEntity = context.CreateCombatEntity != null
                            ? context.CreateCombatEntity(buildingId, originCell, definition, 0, instance.transform.rotation, birth)
                            : Entity.Null;
                        if (hasEntityManager && hasSurfaceResult && !preserveAuthoredTransform)
                            FoundationVisuals?.ApplyCombatEntityFoundation(entityManager, combatEntity, surfaceResult, _surfacePlacementSystem);
                    },
                    AddDeferredRedirectFootprint = () => context.AddDeferredRedirectFootprint?.Invoke(occupiedRect),
                    RedirectUnits = () => context.RedirectUnits?.Invoke(occupiedRect),
                    CommitData = (buildingId, defer) =>
                    {
                        building = new RuntimeBuildingEntity
                        {
                            Id = buildingId,
                            Definition = definition,
                            Instance = instance,
                            OriginCell = originCell,
                            CombatEntity = combatEntity,
                            BlockerEntity = blockerEntity
                        };
                        BuildingRuntimeDataRegistrationSystemHelper.InitializeAndRegister(
                            context.RuntimeBuildingSystem, building, defer,
                            value => context.InitializeVisuals?.Invoke(value),
                            value =>
                            {
                                if (hasEntityManager) BuildingRunwayPathingSurface.Attach(entityManager, value);
                                AttachRuntimeLink(context.RuntimeLinkInteractionSystem, context.RuntimeLinkInteractionContext,
                                    context.RuntimeBuildingEntityLinks, value);
                            },
                            value =>
                            {
                                if (hasEntityManager && combatEntity != Entity.Null && entityManager.Exists(combatEntity))
                                    entityManager.AddComponentObject(combatEntity, new SkirmishNativeBuildingCheckpointRecord { Building = value });
                            },
                            () => context.MarkPendingMarkerRefresh?.Invoke(),
                            () => context.RefreshMarkers?.Invoke());
                    }
                }, removeOverlappingBlockers);
            return building;
        }

        public bool RollbackRuntimeBuildingRegistration(Context context, RuntimeBuildingEntity building)
        {
            if (context.RuntimeBuildingSystem == null || building == null ||
                !context.RuntimeBuildingSystem.ContainsBuilding(building.Id))
            {
                return false;
            }

            if (context.TryGetEntityManager != null && context.TryGetEntityManager(out EntityManager entityManager))
            {
                if (building.CombatEntity != Entity.Null && entityManager.Exists(building.CombatEntity))
                    entityManager.DestroyEntity(building.CombatEntity);
                if (building.BlockerEntity != Entity.Null && entityManager.Exists(building.BlockerEntity))
                    entityManager.DestroyEntity(building.BlockerEntity);
            }

            building.CombatEntity = Entity.Null;
            building.BlockerEntity = Entity.Null;
            bool removed = context.RuntimeBuildingSystem.RemoveBuilding(building.Id);
            if (removed)
                context.RefreshMarkers?.Invoke();
            return removed;
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

        private BuildingFoundationVisualPresentationSystemHelper FoundationVisuals =>
            _foundationVisualPresentationHelper ??= ResolveBuildingFoundationVisuals();

        private static BuildingFoundationVisualPresentationSystemHelper ResolveBuildingFoundationVisuals()
        {
            return new BuildingFoundationVisualPresentationSystemHelper();
        }
    }
}
