using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingRuntimePublishCompositionSystemHelper
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    public readonly struct Context
    {
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly BuildingRuntimeProcessingCompositionSystemHelper BoundarySystem;
        public readonly BuildingDefinitionPrefabSystemHelper DefinitionSystem;
        public readonly BuildingRuntimeSpawnCompositionSystemHelper RuntimeSpawnSystem;
        public readonly BuildingRuntimeSpawnCompositionSystemHelper.Context RuntimeSpawnContext;
        public readonly BuildingProductionRequestSystemHelper ProductionRequestSystem;
        public readonly BuildingProductionRequestSystemHelper.Context ProductionRequestContext;
        public readonly BuildingRuntimeReadModelCompositionSystemHelper RuntimeQuerySystem;
        public readonly BuildingRuntimeReadModelCompositionSystemHelper.Context RuntimeQueryContext;
        public readonly FactionResourceCompositionSystemHelper FactionResourceCompositionSystemHelper;
        public readonly Func<EntityQuery> GetBoundaryQuery;
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;

        public Context(
            TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureEntityQueries,
            BuildingRuntimeProcessingCompositionSystemHelper boundarySystem,
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
            BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext,
            BuildingProductionRequestSystemHelper productionRequestSystem,
            BuildingProductionRequestSystemHelper.Context productionRequestContext,
            BuildingRuntimeReadModelCompositionSystemHelper runtimeQuerySystem,
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
            FactionResourceCompositionSystemHelper factionResourceSystem,
            Func<EntityQuery> getBoundaryQuery,
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings)
        {
            TryGetEntityManager = tryGetEntityManager;
            EnsureEntityQueries = ensureEntityQueries;
            BoundarySystem = boundarySystem;
            DefinitionSystem = definitionSystem;
            RuntimeSpawnSystem = runtimeSpawnSystem;
            RuntimeSpawnContext = runtimeSpawnContext;
            ProductionRequestSystem = productionRequestSystem;
            ProductionRequestContext = productionRequestContext;
            RuntimeQuerySystem = runtimeQuerySystem;
            RuntimeQueryContext = runtimeQueryContext;
            FactionResourceCompositionSystemHelper = factionResourceSystem;
            GetBoundaryQuery = getBoundaryQuery;
            RuntimeBuildings = runtimeBuildings;
        }
    }

    public void Update(Context context)
    {
        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
            return;

        context.EnsureEntityQueries?.Invoke(em);
        EntityQuery boundaryQuery = context.GetBoundaryQuery != null ? context.GetBoundaryQuery() : default;
        context.BoundarySystem?.Update(
            context.DefinitionSystem,
            context.RuntimeSpawnSystem,
            context.RuntimeSpawnContext,
            context.ProductionRequestSystem,
            context.ProductionRequestContext,
            context.RuntimeQuerySystem,
            context.RuntimeQueryContext,
            context.FactionResourceCompositionSystemHelper,
            em,
            boundaryQuery,
            context.RuntimeBuildings,
            UnityEngine.Time.time,
            UnityEngine.Time.frameCount);
    }
}
