using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingRuntimeBoundaryPublishSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    public readonly struct Context
    {
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly BuildingRuntimeBoundarySystem BoundarySystem;
        public readonly BuildingDefinitionSystem DefinitionSystem;
        public readonly BuildingRuntimeSpawnSystem RuntimeSpawnSystem;
        public readonly BuildingRuntimeSpawnSystem.Context RuntimeSpawnContext;
        public readonly BuildingProductionRequestSystem ProductionRequestSystem;
        public readonly BuildingProductionRequestSystem.Context ProductionRequestContext;
        public readonly BuildingRuntimeQuerySystem RuntimeQuerySystem;
        public readonly BuildingRuntimeQuerySystem.Context RuntimeQueryContext;
        public readonly FactionResourceSystem FactionResourceSystem;
        public readonly Func<EntityQuery> GetBoundaryQuery;
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;

        public Context(
            TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureEntityQueries,
            BuildingRuntimeBoundarySystem boundarySystem,
            BuildingDefinitionSystem definitionSystem,
            BuildingRuntimeSpawnSystem runtimeSpawnSystem,
            BuildingRuntimeSpawnSystem.Context runtimeSpawnContext,
            BuildingProductionRequestSystem productionRequestSystem,
            BuildingProductionRequestSystem.Context productionRequestContext,
            BuildingRuntimeQuerySystem runtimeQuerySystem,
            BuildingRuntimeQuerySystem.Context runtimeQueryContext,
            FactionResourceSystem factionResourceSystem,
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
            FactionResourceSystem = factionResourceSystem;
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
            context.FactionResourceSystem,
            em,
            boundaryQuery,
            context.RuntimeBuildings,
            Time.time,
            Time.frameCount);
    }
}
