using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingProductionContextCompositionSystemHelper
    {
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
                source.ResourceMaterials,
                source.MaxQueuedUnitProductions,
                source.ProductionSystem,
                CreateProductionQueueContext(source),
                source.RunwaySystem,
                BuildingDefinitionPrefabSystemHelper.GetProductionPrefab,
                BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds,
                source.BeginPlacementForConfiguredSpawnable,
                source.TrySpendMaterials,
                source.RefundMaterials,
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
                        (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager)),
                source.EvaluateConstructionResources,
                source.TransportSystem == null || source.ProductionSystem == null
                    ? null
                    : (Entity producer, int requestId, GameObject unitPrefab, float3 dropPosition, float now) =>
                    {
                        BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings settings =
                            source.ProductionSystem.ResolveProductionTransportSettings(
                                unitPrefab,
                                source.DefinitionSystem.ConfiguredUnitSpawnPrefabs,
                                source.DefinitionSystem.UnitSpawnPrefabsByKey,
                                BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds);
                        return source.TransportSystem.UpdateCanonicalOperationMapProductionDelivery(
                            CreateProductionTransportContext(source),
                            producer,
                            requestId,
                            unitPrefab,
                            settings,
                            dropPosition,
                            now);
                    },
                source.TransportSystem == null
                    ? null
                    : now => source.TransportSystem.UpdateCanonicalOperationMapProductionDeliveryLifecycle(now),
                source.TryResolveUnitResourceCosts,
                source.TrySpendConstructionResources,
                source.TryRestoreConstructionResources);
        }
    }
}
