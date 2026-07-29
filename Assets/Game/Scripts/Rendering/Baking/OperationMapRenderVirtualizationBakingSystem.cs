using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Game.Rendering
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    public partial struct OperationMapRenderVirtualizationBakingSystem : ISystem
    {
        private EntityQuery _databaseQuery;
        private EntityQuery _sourceRowQuery;
        private EntityQuery _buildingOwnerQuery;

        public void OnCreate(ref SystemState state)
        {
            _databaseQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>(),
                ComponentType.ReadOnly<OperationMapRenderEligibleSourceRowBakingComponent>());
            _sourceRowQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<OperationMapRenderSourceRowBakingComponent>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities |
                          EntityQueryOptions.IncludePrefab |
                          EntityQueryOptions.IgnoreComponentEnabledState
            });
            _buildingOwnerQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<
                        OperationMapVirtualizedBuildingOwnerBakingComponent>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities |
                          EntityQueryOptions.IncludePrefab |
                          EntityQueryOptions.IgnoreComponentEnabledState
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            int databaseCount = _databaseQuery.CalculateEntityCount();
            if (databaseCount != 1)
            {
                throw new InvalidOperationException(
                    $"Source-row stripping requires exactly one render database, found {databaseCount}.");
            }

            EntityManager entityManager = state.EntityManager;
            Entity databaseEntity = _databaseQuery.GetSingletonEntity();
            DynamicBuffer<OperationMapRenderEligibleSourceRowBakingComponent> expectedBuffer =
                entityManager.GetBuffer<OperationMapRenderEligibleSourceRowBakingComponent>(
                    databaseEntity,
                    true);
            var expected = new Dictionary<
                SourceRowKey,
                OperationMapRenderEligibleSourceRowBakingComponent>(expectedBuffer.Length);
            var expectedBuildingStateOwners = new Dictionary<OwnerKey, int>();
            for (int index = 0; index < expectedBuffer.Length; index++)
            {
                OperationMapRenderEligibleSourceRowBakingComponent row =
                    expectedBuffer[index];
                bool requiresStateOwner = row.RequiresStateOwner == 1;
                if (row.RequiresStateOwner > 1 ||
                    (requiresStateOwner
                        ? row.StateOwnerIndex < 0 ||
                          row.RequiredVisualState == OperationMapRenderVisualState.Any
                        : row.StateOwnerIndex != -1 ||
                          row.RequiredVisualState != OperationMapRenderVisualState.Any))
                {
                    throw new InvalidOperationException(
                        "Eligible logical row has inconsistent building-state ownership.");
                }
                if (!expected.TryAdd(SourceRowKey.From(row), row))
                {
                    throw new InvalidOperationException(
                        "Eligible logical rows contain a duplicate owner/path identity.");
                }
                if (requiresStateOwner)
                {
                    OwnerKey owner = OwnerKey.From(row.OwnerIdentity);
                    if (expectedBuildingStateOwners.TryGetValue(
                            owner,
                            out int existingStateOwnerIndex) &&
                        existingStateOwnerIndex != row.StateOwnerIndex)
                    {
                        throw new InvalidOperationException(
                            "One virtualized building maps to more than one state-owner index.");
                    }
                    expectedBuildingStateOwners[owner] = row.StateOwnerIndex;
                }
            }

            var buildingEntities = new Dictionary<OwnerKey, Entity>();
            using (NativeArray<Entity> buildingOwnerEntities =
                   _buildingOwnerQuery.ToEntityArray(Allocator.Temp))
            {
                for (int index = 0; index < buildingOwnerEntities.Length; index++)
                {
                    Entity buildingEntity = buildingOwnerEntities[index];
                    OwnerKey owner = OwnerKey.From(
                        entityManager.GetComponentData<
                            OperationMapVirtualizedBuildingOwnerBakingComponent>(
                            buildingEntity).OwnerIdentity);
                    if (!buildingEntities.TryAdd(owner, buildingEntity))
                    {
                        throw new InvalidOperationException(
                            "More than one canonical building uses one virtualized owner identity.");
                    }
                }
            }

            var matched = new HashSet<SourceRowKey>();
            var buildingSourceRowCounts = new Dictionary<OwnerKey, int>();
            var matchedBuildingRowCounts = new Dictionary<OwnerKey, int>();
            using NativeArray<Entity> sourceOwners =
                _sourceRowQuery.ToEntityArray(Allocator.Temp);
            using var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            for (int ownerIndex = 0; ownerIndex < sourceOwners.Length; ownerIndex++)
            {
                Entity sourceOwner = sourceOwners[ownerIndex];
                DynamicBuffer<OperationMapRenderSourceRowBakingComponent> sourceRows =
                    entityManager.GetBuffer<OperationMapRenderSourceRowBakingComponent>(
                        sourceOwner,
                        true);
                for (int rowIndex = 0; rowIndex < sourceRows.Length; rowIndex++)
                {
                    OperationMapRenderSourceRowBakingComponent source =
                        sourceRows[rowIndex];
                    OwnerKey sourceOwnerKey = OwnerKey.From(source.OwnerIdentity);
                    if (expectedBuildingStateOwners.ContainsKey(sourceOwnerKey))
                    {
                        buildingSourceRowCounts.TryGetValue(
                            sourceOwnerKey,
                            out int sourceRowCount);
                        buildingSourceRowCounts[sourceOwnerKey] = sourceRowCount + 1;
                    }
                    SourceRowKey key = SourceRowKey.From(source);
                    if (!expected.TryGetValue(
                            key,
                            out OperationMapRenderEligibleSourceRowBakingComponent row))
                        continue;

                    bool requiresStateOwner = row.RequiresStateOwner == 1;
                    if (requiresStateOwner
                            ? source.IsRenderOnlyOwner != 0
                            : source.IsRenderOnlyOwner != 1)
                    {
                        throw new InvalidOperationException(
                            "Eligible source ownership does not match its logical state policy.");
                    }
                    if (!matched.Add(key))
                    {
                        throw new InvalidOperationException(
                            "More than one converted source render entity matched one logical row.");
                    }
                    Entity convertedRenderEntity = source.RenderEntity;
                    if (requiresStateOwner)
                    {
                        if (!buildingEntities.TryGetValue(
                                sourceOwnerKey,
                                out Entity buildingEntity) ||
                            convertedRenderEntity == buildingEntity)
                        {
                            throw new InvalidOperationException(
                                "A stateful source row does not resolve to a separate " +
                                "canonical gameplay building.");
                        }
                        matchedBuildingRowCounts.TryGetValue(
                            sourceOwnerKey,
                            out int matchedBuildingRowCount);
                        matchedBuildingRowCounts[sourceOwnerKey] =
                            matchedBuildingRowCount + 1;
                    }
                    if (!entityManager.Exists(convertedRenderEntity) ||
                        !entityManager.HasComponent<MaterialMeshInfo>(convertedRenderEntity) ||
                        !entityManager.HasComponent<RenderMeshUnmanaged>(convertedRenderEntity))
                    {
                        throw new InvalidOperationException(
                            "Eligible source row does not reference exactly one baking-time " +
                            "render entity.");
                    }

                    MaterialMeshInfo materialMeshInfo =
                        entityManager.GetComponentData<MaterialMeshInfo>(
                            convertedRenderEntity);
                    if (materialMeshInfo.HasMaterialMeshIndexRange ||
                        materialMeshInfo.SubMesh != row.SubMeshIndex)
                    {
                        throw new InvalidOperationException(
                            "Eligible source render submesh does not match its logical row.");
                    }
                    RenderMeshUnmanaged renderMesh =
                        entityManager.GetComponentData<RenderMeshUnmanaged>(
                            convertedRenderEntity);
                    if (renderMesh.mesh != row.Mesh ||
                        renderMesh.materialForSubMesh != row.Material)
                    {
                        throw new InvalidOperationException(
                            "Eligible source render assets do not match their logical row.");
                    }

                    commandBuffer.AddComponent<BakingOnlyEntity>(
                        convertedRenderEntity);
                }
            }

            if (matched.Count != expected.Count)
            {
                throw new InvalidOperationException(
                    $"Logical/source stripping parity failed: matched {matched.Count}/" +
                    $"{expected.Count} eligible rows.");
            }
            foreach (KeyValuePair<OwnerKey, int> stateOwner in
                     expectedBuildingStateOwners)
            {
                if (!buildingEntities.TryGetValue(
                        stateOwner.Key,
                        out Entity buildingEntity) ||
                    !entityManager.HasComponent<OperationMapBuildingComponent>(
                        buildingEntity) ||
                    !entityManager.HasComponent<OperationMapBuildingPresentation>(
                        buildingEntity) ||
                    entityManager.HasComponent<
                        OperationMapVirtualizedBuildingPresentationComponent>(
                        buildingEntity))
                {
                    throw new InvalidOperationException(
                        "Virtualized building replacement requires one canonical building " +
                        "with resident render-root ownership.");
                }
                buildingSourceRowCounts.TryGetValue(
                    stateOwner.Key,
                    out int sourceRowCount);
                matchedBuildingRowCounts.TryGetValue(
                    stateOwner.Key,
                    out int matchedRowCount);
                if (sourceRowCount == 0 || sourceRowCount != matchedRowCount)
                {
                    throw new InvalidOperationException(
                        "Virtualized building replacement requires every source render row " +
                        "to have an exact logical match.");
                }

                commandBuffer.RemoveComponent<OperationMapBuildingPresentation>(
                    buildingEntity);
                commandBuffer.AddComponent(
                    buildingEntity,
                    new OperationMapVirtualizedBuildingPresentationComponent
                    {
                        StateOwnerIndex = stateOwner.Value
                    });
            }
            commandBuffer.Playback(entityManager);
        }
    }

    internal readonly struct OwnerKey : IEquatable<OwnerKey>
    {
        private readonly ulong _low;
        private readonly ulong _high;

        private OwnerKey(OperationMapRenderIdentity128 identity)
        {
            _low = identity.Low;
            _high = identity.High;
        }

        internal static OwnerKey From(OperationMapRenderIdentity128 identity) =>
            new(identity);

        public bool Equals(OwnerKey other) =>
            _low == other._low && _high == other._high;

        public override bool Equals(object obj) =>
            obj is OwnerKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)_low ^ (int)(_low >> 32)) * 397 ^
                       (int)_high ^ (int)(_high >> 32);
            }
        }
    }

    internal readonly struct SourceRowKey : IEquatable<SourceRowKey>
    {
        private readonly ulong _ownerLow;
        private readonly ulong _ownerHigh;
        private readonly ulong _pathLow;
        private readonly ulong _pathHigh;

        private SourceRowKey(
            OperationMapRenderIdentity128 owner,
            OperationMapRenderIdentity128 path)
        {
            _ownerLow = owner.Low;
            _ownerHigh = owner.High;
            _pathLow = path.Low;
            _pathHigh = path.High;
        }

        internal static SourceRowKey From(
            OperationMapRenderSourceRowBakingComponent row) =>
            new(row.OwnerIdentity, row.RendererPathIdentity);

        internal static SourceRowKey From(
            OperationMapRenderEligibleSourceRowBakingComponent row) =>
            new(row.OwnerIdentity, row.RendererPathIdentity);

        public bool Equals(SourceRowKey other) =>
            _ownerLow == other._ownerLow &&
            _ownerHigh == other._ownerHigh &&
            _pathLow == other._pathLow &&
            _pathHigh == other._pathHigh;

        public override bool Equals(object obj) =>
            obj is SourceRowKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)_ownerLow ^ (int)(_ownerLow >> 32);
                hash = (hash * 397) ^ (int)_ownerHigh ^ (int)(_ownerHigh >> 32);
                hash = (hash * 397) ^ (int)_pathLow ^ (int)(_pathLow >> 32);
                return (hash * 397) ^ (int)_pathHigh ^ (int)(_pathHigh >> 32);
            }
        }
    }
}
