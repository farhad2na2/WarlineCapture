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