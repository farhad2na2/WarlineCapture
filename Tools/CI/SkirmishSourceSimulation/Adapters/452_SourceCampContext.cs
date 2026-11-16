using System;using System.Collections.Generic;using Game.Components;using Unity.Entities;using UnityEngine;namespace Game.Runtime{internal sealed partial class SourceCampAvailabilityKernel{
 internal delegate bool TryGetEntityManagerDelegate(out EntityManager em);internal delegate SourceUnitPrefab GetProductionPrefabDelegate(RuntimeDefinition definition,int index);internal delegate bool TryResolveUnitResourceCostsDelegate(SourceUnitPrefab prefab,int fallbackMaterials,out int credits,out int materials);
 // Same retained world/current source context only. No snapshot counters or result assertions.
 internal sealed partial class Context{
 internal IReadOnlyDictionary<int,RuntimeBuildingEntity> RuntimeBuildings;
 internal IReadOnlyList<RuntimeDefinition> ConfiguredSpawnableDefinitions;
 internal IReadOnlyDictionary<SourceUnitPrefab,RuntimeDefinition> ConfiguredDefinitionsByPrefab;
 internal IReadOnlyList<SourceUnitPrefab> UnitSpawnPrefabs;internal IReadOnlyDictionary<string,SourceUnitPrefab> UnitSpawnPrefabsByKey;
 internal int ResourceMaterials,MaxQueuedUnitProductions;
 internal BuildingProductionQueueCompositionSystemHelper ProductionSystem;
 internal BuildingProductionQueueCompositionSystemHelper.QueueContext ProductionQueueContext;
 internal SourceCampRunwayReads RunwaySystem;internal GetProductionPrefabDelegate GetProductionPrefab;
 internal BuildingProductionQueueCompositionSystemHelper.TryGetPrefabLocalBoundsDelegate TryGetPrefabLocalBounds;
 internal Action<string> LogWarning;internal TryGetEntityManagerDelegate TryGetEntityManager;
 internal Func<int,int,FactionConstructionResourceMutationResult> EvaluateConstructionResources;
 internal TryResolveUnitResourceCostsDelegate TryResolveUnitResourceCosts;
 }
}
// Explicit source runway collaborator; unknown active runway branch throws in outer admission.
internal sealed class SourceCampUnknownRunwayReads:SourceCampRunwayReads{internal override bool HasAvailableAirportRunway(IReadOnlyDictionary<int,RuntimeBuildingEntity> buildings,byte faction)=>throw new NotSupportedException("Active source runway read domain unsupported/unknown");}
internal abstract class SourceCampRunwayReads{internal abstract bool HasAvailableAirportRunway(IReadOnlyDictionary<int,RuntimeBuildingEntity> buildings,byte faction);}
}
namespace Game.Runtime
{
    // Exact no-runway branch of BuildingRunwaySystem.HasAvailableAirportRunway.
    // A live, complete registry is required; a runway-bearing definition remains unsupported
    // because this host has not established the native instance/lifecycle domain.
    internal sealed class SourceCampProvenNoRunwayReads : SourceCampRunwayReads
    {
        private readonly System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> registry;
        internal SourceCampProvenNoRunwayReads(System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> registry, bool completeLiveRegistry)
        {
            if (!completeLiveRegistry || registry == null)
                throw new System.NotSupportedException("Complete same-world live building registry required");
            this.registry = registry;
        }
        internal override bool HasAvailableAirportRunway(System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings, byte faction)
        {
            if (!object.ReferenceEquals(buildings, registry))
                throw new System.NotSupportedException("Same live building registry required");
            foreach (var pair in buildings)
            {
                var building = pair.Value;
                if (building == null || building.IsDestroyed) continue;
                if (building.Definition == null || !building.Definition.SourceMetadataComplete)
                    throw new System.NotSupportedException("Complete source building definition required");
                if (building.Definition.HasRunway)
                    throw new System.NotSupportedException("Active runway native instance domain unsupported");
            }
            return false;
        }
    }
}
