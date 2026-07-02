using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingProductionContextCompositionSystemHelper
    {
        public readonly struct Source
        {
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly Camera WorldCamera;
            public readonly BuildingDefinitionPrefabSystemHelper DefinitionSystem;
            public readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;
            public readonly BuildingProductionUpdateCompositionSystemHelper ProductionUpdateSystem;
            public readonly BuildingProductionTransportPresentationSystemHelper TransportSystem;
            public readonly BuildingProductionTransportBridgeCompositionSystemHelper TransportBridgeSystem;
            public readonly BuildingProductionSlotUtilitySystemHelper ProductionSlotSystem;
            public readonly BuildingRunwaySystem RunwaySystem;
            public readonly BuildingVisualSystem VisualSystem;
            public readonly BuildingSpawnCompositionSystemHelper SpawnSystem;
            public readonly BuildingSpawnCompositionSystemHelper.Context SpawnContext;
            public readonly int ResourceDollars;
            public readonly int MaxQueuedUnitProductions;
            public readonly BuildingProductionRequestSystemHelper.BeginPlacementForConfiguredSpawnableDelegate BeginPlacementForConfiguredSpawnable;
            public readonly BuildingProductionRequestSystemHelper.TrySpendDollarsDelegate TrySpendDollars;
            public readonly BuildingProductionRequestSystemHelper.RefundDollarsDelegate RefundDollars;
            public readonly BuildingProductionRequestSystemHelper.SetActivePlacementCostDelegate SetActivePlacementCost;
            public readonly BuildingProductionRequestSystemHelper.TryQueuePlayerUnitDelegate TryQueuePlayerUnit;
            public readonly BuildingProductionRequestSystemHelper.SelectRuntimeBuildingDelegate SelectRuntimeBuilding;
            public readonly BuildingProductionRequestSystemHelper.RuntimeGameplayAction SuppressNextWorldClick;
            public readonly BuildingProductionRequestSystemHelper.RuntimeGameplayAction RefreshBuildingMarkers;
            public readonly BuildingProductionRequestSystemHelper.RuntimeGameplayAction ClearFocusedUnit;
            public readonly BuildingProductionTransportBridgeCompositionSystemHelper.BooleanQuery IsBuildDrawerOpen;
            public readonly BuildingProductionRequestSystemHelper.CameraFocusAction SmoothMoveCameraGroundCenterTo;
            public readonly BuildingProductionRequestSystemHelper.ResolveBuildingFocusWorldPositionDelegate ResolveBuildingFocusWorldPosition;
            public readonly BuildingProductionRequestSystemHelper.RecordUnitOrderedDelegate RecordUnitOrdered;
            public readonly BuildingProductionRequestSystemHelper.LogWarningDelegate LogWarning;
            public readonly BuildingProductionRequestSystemHelper.CountFactionUnitsDelegate CountPendingProductionsForFaction;
            public readonly BuildingProductionRequestSystemHelper.CountFactionUnitsDelegate CountRuntimeProducedUnitsForFaction;
            public readonly ResourceHaulerUtilitySystemHelper ResourceHaulerUtilitySystemHelper;
            public readonly FactionResourceCompositionSystemHelper FactionResourceCompositionSystemHelper;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetGridDataDelegate TryGetGridData;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.EnsureEntityQueriesDelegate EnsureEntityQueries;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.GetEntityQueryDelegate GetHaulerUnitsQuery;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.GetEntityQueryDelegate GetSelectedUnitsQuery;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetRuntimeBuildingDelegate TryGetRuntimeBuilding;
            public readonly BuildingResourceHaulerBridgeCompositionSystemHelper.GetEffectivePlacementRectDelegate GetEffectivePlacementRect;
            public readonly BuildingProductionTransportPresentationSystemHelper.PrepareTransportDropVisualDelegate PrepareTransportDropVisual;

            public Source(
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                Camera worldCamera,
                BuildingDefinitionPrefabSystemHelper definitionSystem,
                BuildingProductionQueueCompositionSystemHelper productionSystem,
                BuildingProductionUpdateCompositionSystemHelper productionUpdateSystem,
                BuildingProductionTransportPresentationSystemHelper transportSystem,
                BuildingProductionTransportBridgeCompositionSystemHelper transportBridgeSystem,
                BuildingProductionSlotUtilitySystemHelper productionSlotSystem,
                BuildingRunwaySystem runwaySystem,
                BuildingVisualSystem visualSystem,
                BuildingSpawnCompositionSystemHelper spawnSystem,
                BuildingSpawnCompositionSystemHelper.Context spawnContext,
                int resourceDollars,
                int maxQueuedUnitProductions,
                BuildingProductionRequestSystemHelper.BeginPlacementForConfiguredSpawnableDelegate beginPlacementForConfiguredSpawnable,
                BuildingProductionRequestSystemHelper.TrySpendDollarsDelegate trySpendDollars,
                BuildingProductionRequestSystemHelper.RefundDollarsDelegate refundDollars,
                BuildingProductionRequestSystemHelper.SetActivePlacementCostDelegate setActivePlacementCost,
                BuildingProductionRequestSystemHelper.TryQueuePlayerUnitDelegate tryQueuePlayerUnit,
                BuildingProductionRequestSystemHelper.SelectRuntimeBuildingDelegate selectRuntimeBuilding,
                BuildingProductionRequestSystemHelper.RuntimeGameplayAction suppressNextWorldClick,
                BuildingProductionRequestSystemHelper.RuntimeGameplayAction refreshBuildingMarkers,
                BuildingProductionRequestSystemHelper.RuntimeGameplayAction clearFocusedUnit,
                BuildingProductionTransportBridgeCompositionSystemHelper.BooleanQuery isBuildDrawerOpen,
                BuildingProductionRequestSystemHelper.CameraFocusAction smoothMoveCameraGroundCenterTo,
                BuildingProductionRequestSystemHelper.ResolveBuildingFocusWorldPositionDelegate resolveBuildingFocusWorldPosition,
                BuildingProductionRequestSystemHelper.RecordUnitOrderedDelegate recordUnitOrdered,
                BuildingProductionRequestSystemHelper.LogWarningDelegate logWarning,
                BuildingProductionRequestSystemHelper.CountFactionUnitsDelegate countPendingProductionsForFaction,
                BuildingProductionRequestSystemHelper.CountFactionUnitsDelegate countRuntimeProducedUnitsForFaction,
                ResourceHaulerUtilitySystemHelper resourceHaulerSystem,
                FactionResourceCompositionSystemHelper factionResourceSystem,
                BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
                BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetGridDataDelegate tryGetGridData,
                BuildingResourceHaulerBridgeCompositionSystemHelper.EnsureEntityQueriesDelegate ensureEntityQueries,
                BuildingResourceHaulerBridgeCompositionSystemHelper.GetEntityQueryDelegate getHaulerUnitsQuery,
                BuildingResourceHaulerBridgeCompositionSystemHelper.GetEntityQueryDelegate getSelectedUnitsQuery,
                BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
                BuildingResourceHaulerBridgeCompositionSystemHelper.GetEffectivePlacementRectDelegate getEffectivePlacementRect,
                BuildingProductionTransportPresentationSystemHelper.PrepareTransportDropVisualDelegate prepareTransportDropVisual = null)
            {
                RuntimeBuildings = runtimeBuildings;
                WorldCamera = worldCamera;
                DefinitionSystem = definitionSystem;
                ProductionSystem = productionSystem;
                ProductionUpdateSystem = productionUpdateSystem;
                TransportSystem = transportSystem;
                TransportBridgeSystem = transportBridgeSystem;
                ProductionSlotSystem = productionSlotSystem;
                RunwaySystem = runwaySystem;
                VisualSystem = visualSystem;
                SpawnSystem = spawnSystem;
                SpawnContext = spawnContext;
                ResourceDollars = resourceDollars;
                MaxQueuedUnitProductions = Mathf.Max(0, maxQueuedUnitProductions);
                BeginPlacementForConfiguredSpawnable = beginPlacementForConfiguredSpawnable;
                TrySpendDollars = trySpendDollars;
                RefundDollars = refundDollars;
                SetActivePlacementCost = setActivePlacementCost;
                TryQueuePlayerUnit = tryQueuePlayerUnit;
                SelectRuntimeBuilding = selectRuntimeBuilding;
                SuppressNextWorldClick = suppressNextWorldClick;
                RefreshBuildingMarkers = refreshBuildingMarkers;
                ClearFocusedUnit = clearFocusedUnit;
                IsBuildDrawerOpen = isBuildDrawerOpen;
                SmoothMoveCameraGroundCenterTo = smoothMoveCameraGroundCenterTo;
                ResolveBuildingFocusWorldPosition = resolveBuildingFocusWorldPosition;
                RecordUnitOrdered = recordUnitOrdered;
                LogWarning = logWarning;
                CountPendingProductionsForFaction = countPendingProductionsForFaction;
                CountRuntimeProducedUnitsForFaction = countRuntimeProducedUnitsForFaction;
                ResourceHaulerUtilitySystemHelper = resourceHaulerSystem;
                FactionResourceCompositionSystemHelper = factionResourceSystem;
                TryGetEntityManager = tryGetEntityManager;
                TryGetGridData = tryGetGridData;
                EnsureEntityQueries = ensureEntityQueries;
                GetHaulerUnitsQuery = getHaulerUnitsQuery;
                GetSelectedUnitsQuery = getSelectedUnitsQuery;
                TryGetRuntimeBuilding = tryGetRuntimeBuilding;
                GetEffectivePlacementRect = getEffectivePlacementRect;
                PrepareTransportDropVisual = prepareTransportDropVisual;
            }
        }

        public Source CreateSource(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Camera worldCamera,
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            BuildingProductionUpdateCompositionSystemHelper productionUpdateSystem,
            BuildingProductionTransportPresentationSystemHelper transportSystem,
            BuildingProductionTransportBridgeCompositionSystemHelper transportBridgeSystem,
            BuildingProductionSlotUtilitySystemHelper productionSlotSystem,
            BuildingRunwaySystem runwaySystem,
            BuildingVisualSystem visualSystem,
            BuildingSpawnCompositionSystemHelper spawnSystem,
            BuildingSpawnCompositionSystemHelper.Context spawnContext,
            int resourceDollars,
            int maxQueuedUnitProductions,
            BuildingProductionRequestSystemHelper.BeginPlacementForConfiguredSpawnableDelegate beginPlacementForConfiguredSpawnable,
            BuildingProductionRequestSystemHelper.TrySpendDollarsDelegate trySpendDollars,
            BuildingProductionRequestSystemHelper.RefundDollarsDelegate refundDollars,
            BuildingProductionRequestSystemHelper.SetActivePlacementCostDelegate setActivePlacementCost,
            BuildingProductionRequestSystemHelper.TryQueuePlayerUnitDelegate tryQueuePlayerUnit,
            BuildingProductionRequestSystemHelper.SelectRuntimeBuildingDelegate selectRuntimeBuilding,
            BuildingProductionRequestSystemHelper.RuntimeGameplayAction suppressNextWorldClick,
            BuildingProductionRequestSystemHelper.RuntimeGameplayAction refreshBuildingMarkers,
            BuildingProductionRequestSystemHelper.RuntimeGameplayAction clearFocusedUnit,
            BuildingProductionTransportBridgeCompositionSystemHelper.BooleanQuery isBuildDrawerOpen,
            BuildingProductionRequestSystemHelper.CameraFocusAction smoothMoveCameraGroundCenterTo,
            BuildingProductionRequestSystemHelper.ResolveBuildingFocusWorldPositionDelegate resolveBuildingFocusWorldPosition,
            BuildingProductionRequestSystemHelper.RecordUnitOrderedDelegate recordUnitOrdered,
            BuildingProductionRequestSystemHelper.LogWarningDelegate logWarning,
            BuildingProductionRequestSystemHelper.CountFactionUnitsDelegate countPendingProductionsForFaction,
            BuildingProductionRequestSystemHelper.CountFactionUnitsDelegate countRuntimeProducedUnitsForFaction,
            ResourceHaulerUtilitySystemHelper resourceHaulerSystem,
            FactionResourceCompositionSystemHelper factionResourceSystem,
            BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetGridDataDelegate tryGetGridData,
            BuildingResourceHaulerBridgeCompositionSystemHelper.EnsureEntityQueriesDelegate ensureEntityQueries,
            BuildingResourceHaulerBridgeCompositionSystemHelper.GetEntityQueryDelegate getHaulerUnitsQuery,
            BuildingResourceHaulerBridgeCompositionSystemHelper.GetEntityQueryDelegate getSelectedUnitsQuery,
            BuildingResourceHaulerBridgeCompositionSystemHelper.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding,
            BuildingResourceHaulerBridgeCompositionSystemHelper.GetEffectivePlacementRectDelegate getEffectivePlacementRect,
            BuildingProductionTransportPresentationSystemHelper.PrepareTransportDropVisualDelegate prepareTransportDropVisual = null)
        {
            return new Source(
                runtimeBuildings,
                worldCamera,
                definitionSystem,
                productionSystem,
                productionUpdateSystem,
                transportSystem,
                transportBridgeSystem,
                productionSlotSystem,
                runwaySystem,
                visualSystem,
                spawnSystem,
                spawnContext,
                resourceDollars,
                maxQueuedUnitProductions,
                beginPlacementForConfiguredSpawnable,
                trySpendDollars,
                refundDollars,
                setActivePlacementCost,
                tryQueuePlayerUnit,
                selectRuntimeBuilding,
                suppressNextWorldClick,
                refreshBuildingMarkers,
                clearFocusedUnit,
                isBuildDrawerOpen,
                smoothMoveCameraGroundCenterTo,
                resolveBuildingFocusWorldPosition,
                recordUnitOrdered,
                logWarning,
                countPendingProductionsForFaction,
                countRuntimeProducedUnitsForFaction,
                resourceHaulerSystem,
                factionResourceSystem,
                tryGetEntityManager,
                tryGetGridData,
                ensureEntityQueries,
                getHaulerUnitsQuery,
                getSelectedUnitsQuery,
                tryGetRuntimeBuilding,
                getEffectivePlacementRect,
                prepareTransportDropVisual);
        }

        public BuildingProductionUpdateCompositionSystemHelper.Context CreateProductionUpdateContext(Source source)
        {
            source.ProductionSystem?.PrewarmPendingProductionPool();
            source.TransportSystem?.PrewarmConfiguredProductionTransportPools(
                source.ProductionSystem,
                source.DefinitionSystem?.ConfiguredUnitSpawnPrefabs,
                source.DefinitionSystem?.UnitSpawnPrefabsByKey,
                BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds,
                source.VisualSystem);
            source.TransportSystem?.PrewarmProductionTransportPools(
                source.RuntimeBuildings,
                source.VisualSystem);

            return new BuildingProductionUpdateCompositionSystemHelper.Context(
                source.RuntimeBuildings,
                source.ProductionSystem,
                source.TransportSystem,
                CreateProductionTransportContext(source));
        }

        public BuildingProductionTransportPresentationSystemHelper.Context CreateProductionTransportContext(Source source)
        {
            return new BuildingProductionTransportPresentationSystemHelper.Context(
                source.RuntimeBuildings,
                source.WorldCamera,
                source.ProductionSystem,
                source.VisualSystem,
                source.RunwaySystem,
                source.TransportBridgeSystem,
                CreateProductionTransportBridgeContext(source),
                source.PrepareTransportDropVisual);
        }

        public BuildingProductionTransportBridgeCompositionSystemHelper.Context CreateProductionTransportBridgeContext(Source source)
        {
            return new BuildingProductionTransportBridgeCompositionSystemHelper.Context(
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                    source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
                entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
                source.SpawnSystem,
                source.SpawnContext,
                () => source.IsBuildDrawerOpen?.Invoke() == true,
                worldPosition => source.SmoothMoveCameraGroundCenterTo?.Invoke(worldPosition));
        }

        public BuildingProductionRequestSystemHelper.Context CreateProductionRequestContext(Source source)
        {
            source.ProductionSystem?.PrewarmPendingProductionPool();
            source.ProductionSystem?.PrewarmProductionTransportSettings(
                source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
                source.DefinitionSystem.UnitSpawnPrefabsByKey,
                BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds);

            return new BuildingProductionRequestSystemHelper.Context(
                source.RuntimeBuildings,
                source.DefinitionSystem.ConfiguredSpawnableDefinitions,
                source.DefinitionSystem.ConfiguredDefinitionsByPrefab,
                source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
                source.DefinitionSystem.UnitSpawnPrefabsByKey,
                source.ResourceDollars,
                source.MaxQueuedUnitProductions,
                source.ProductionSystem,
                CreateProductionQueueContext(source),
                source.RunwaySystem,
                BuildingDefinitionPrefabSystemHelper.GetProductionPrefab,
                BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds,
                source.BeginPlacementForConfiguredSpawnable,
                source.TrySpendDollars,
                source.RefundDollars,
                source.SetActivePlacementCost,
                source.TryQueuePlayerUnit,
                source.SelectRuntimeBuilding,
                source.SuppressNextWorldClick,
                source.RefreshBuildingMarkers,
                source.ClearFocusedUnit,
                source.SmoothMoveCameraGroundCenterTo,
                source.ResolveBuildingFocusWorldPosition,
                source.RecordUnitOrdered,
                source.LogWarning,
                source.CountPendingProductionsForFaction,
                source.CountRuntimeProducedUnitsForFaction,
                source.DefinitionSystem.TryGetConfiguredUnitReadModel,
                source.TryGetEntityManager == null
                    ? null
                    : (BuildingProductionRequestSystemHelper.TryGetEntityManagerDelegate)(
                        (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager)));
        }

        public BuildingProductionQueueCompositionSystemHelper.QueueContext CreateProductionQueueContext(Source source)
        {
            return new BuildingProductionQueueCompositionSystemHelper.QueueContext(
                source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
                source.DefinitionSystem.UnitSpawnPrefabsByKey,
                source.ProductionSlotSystem,
                BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds,
                BuildingDefinitionPrefabSystemHelper.RuntimeBuildingMatchesId,
                source.SpawnContext.TryGetRuntimeBoundaryEntity == null
                    ? null
                    : (BuildingProductionQueueCompositionSystemHelper.TryGetRuntimeBoundaryEntityDelegate)(
                        (EntityManager em, out Entity entity) => source.SpawnContext.TryGetRuntimeBoundaryEntity(em, out entity)));
        }

        public bool TryQueuePlayerUnitProduction(
            Source source,
            RuntimeBuildingEntity building,
            int productionIndex,
            GameObject spawnUnitPrefab,
            float now)
        {
            if (!source.TryGetEntityManager(out EntityManager entityManager))
                return false;

            return source.ProductionSystem.TryQueuePlayerUnitFromBuilding(
                CreateProductionQueueContext(source),
                building,
                productionIndex,
                spawnUnitPrefab,
                entityManager,
                now);
        }

        public BuildingResourceHaulerBridgeCompositionSystemHelper.Context CreateResourceHaulerBridgeContext(Source source)
        {
            return new BuildingResourceHaulerBridgeCompositionSystemHelper.Context(
                source.RuntimeBuildings,
                source.ResourceHaulerUtilitySystemHelper,
                source.FactionResourceCompositionSystemHelper,
                source.TryGetEntityManager,
                source.TryGetGridData,
                source.EnsureEntityQueries,
                source.GetHaulerUnitsQuery,
                source.GetSelectedUnitsQuery,
                source.TryGetRuntimeBuilding,
                building => source.ResolveBuildingFocusWorldPosition(building),
                source.GetEffectivePlacementRect);
        }
    }
}
