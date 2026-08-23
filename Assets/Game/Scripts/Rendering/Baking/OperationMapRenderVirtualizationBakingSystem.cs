using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Game.Rendering
{
    [BakingVersion("WarlineCapture", 1)]
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [UpdateAfter(typeof(OperationMapRenderMaterialBaseColorBakingSystem))]
    public partial struct OperationMapRenderVirtualizationBakingSystem : ISystem
    {
        private EntityQuery _databaseQuery;
        private EntityQuery _sourceRowQuery;
        private EntityQuery _additionalRenderQuery;
        private EntityQuery _buildingOwnerQuery;
        private EntityQuery _slotQuery;

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
            _additionalRenderQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<AdditionalEntityParent>(),
                    ComponentType.ReadOnly<RenderMeshUnmanaged>()
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
            _slotQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities |
                          EntityQueryOptions.IncludePrefab |
                          EntityQueryOptions.IgnoreComponentEnabledState
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            int databaseCount = _databaseQuery.CalculateEntityCount();
            if (databaseCount == 0 &&
                _sourceRowQuery.IsEmptyIgnoreFilter &&
                _buildingOwnerQuery.IsEmptyIgnoreFilter)
            {
                return;
            }
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
                List<OperationMapRenderEligibleSourceRowBakingComponent>>();
            var expectedParts = new HashSet<SourcePartKey>();
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
                SourcePartKey partKey = SourcePartKey.From(row);
                if (!expectedParts.Add(partKey))
                {
                    throw new InvalidOperationException(
                        "Eligible logical rows contain a duplicate owner/path/submesh " +
                        "identity.");
                }
                SourceRowKey rendererKey = SourceRowKey.From(row);
                if (!expected.TryGetValue(rendererKey, out var rendererRows))
                {
                    rendererRows = new List<
                        OperationMapRenderEligibleSourceRowBakingComponent>();
                    expected.Add(rendererKey, rendererRows);
                }
                rendererRows.Add(row);
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
            RequireDeterministicStateOwnerIndices(expectedBuildingStateOwners);

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

            var matched = new HashSet<SourcePartKey>();
            var matchedRenderers = new HashSet<SourceRowKey>();
            var residentKeys = new HashSet<SourceRowKey>();
            var residentRows =
                new List<OperationMapRenderResidentSourceRowComponent>();
            var virtualizedOwnerClasses = new Dictionary<OwnerKey, byte>();
            var virtualizedOwnerEntities = new Dictionary<OwnerKey, Entity>();
            var bakingOnlyRenderEntities = new HashSet<Entity>();
            var buildingSourceRowCounts = new Dictionary<OwnerKey, int>();
            var matchedBuildingRowCounts = new Dictionary<OwnerKey, int>();
            var additionalRenderEntities = new Dictionary<Entity, List<Entity>>();
            using (NativeArray<Entity> additionalEntities =
                   _additionalRenderQuery.ToEntityArray(Allocator.Temp))
            {
                for (int index = 0; index < additionalEntities.Length; index++)
                {
                    Entity additionalEntity = additionalEntities[index];
                    Entity primary = entityManager.GetComponentData<AdditionalEntityParent>(
                        additionalEntity).Parent;
                    if (!additionalRenderEntities.TryGetValue(primary, out var renderEntities))
                    {
                        renderEntities = new List<Entity>();
                        additionalRenderEntities.Add(primary, renderEntities);
                    }
                    renderEntities.Add(additionalEntity);
                }
            }
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
                    if (!expected.TryGetValue(key, out var rows))
                    {
                        if (!residentKeys.Add(key))
                        {
                            throw new InvalidOperationException(
                                "Resident source rows contain a duplicate owner/path identity.");
                        }
                        if (!entityManager.Exists(source.RenderEntity))
                        {
                            throw new InvalidOperationException(
                                "Resident source row references a missing render entity.");
                        }
                        residentRows.Add(
                            new OperationMapRenderResidentSourceRowComponent
                            {
                                RenderEntity = source.RenderEntity,
                                OwnerIdentity = source.OwnerIdentity,
                                RendererPathIdentity = source.RendererPathIdentity
                            });
                        continue;
                    }

                    OperationMapRenderEligibleSourceRowBakingComponent row = rows[0];
                    bool requiresStateOwner = row.RequiresStateOwner == 1;
                    if (source.IsGeneratedOwner > 1)
                    {
                        throw new InvalidOperationException(
                            "Eligible source row has an invalid identity namespace.");
                    }
                    if (requiresStateOwner
                            ? source.IsRenderOnlyOwner != 0
                            : source.IsRenderOnlyOwner != 1)
                    {
                        throw new InvalidOperationException(
                            "Eligible source ownership does not match its logical state policy.");
                    }
                    byte ownerClass = (byte)(
                        (source.IsGeneratedOwner == 1 ? 2 : 0) |
                        (requiresStateOwner ? 1 : 0));
                    if (virtualizedOwnerClasses.TryGetValue(
                            sourceOwnerKey,
                            out byte existingOwnerClass) &&
                        existingOwnerClass != ownerClass)
                    {
                        throw new InvalidOperationException(
                            "One virtualized owner has inconsistent identity or gameplay roles.");
                    }
                    virtualizedOwnerClasses[sourceOwnerKey] = ownerClass;
                    if (virtualizedOwnerEntities.TryGetValue(
                            sourceOwnerKey,
                            out Entity existingOwnerEntity) &&
                        existingOwnerEntity != sourceOwner)
                    {
                        throw new InvalidOperationException(
                            "One virtualized owner identity resolves to more than one owner entity.");
                    }
                    virtualizedOwnerEntities[sourceOwnerKey] = sourceOwner;
                    if (!matchedRenderers.Add(key))
                    {
                        throw new InvalidOperationException(
                            "More than one converted source render entity matched one logical " +
                            "renderer.");
                    }
                    for (int expectedRowIndex = 0;
                         expectedRowIndex < rows.Count;
                         expectedRowIndex++)
                    {
                        OperationMapRenderEligibleSourceRowBakingComponent expectedRow =
                            rows[expectedRowIndex];
                        if (expectedRow.RequiresStateOwner != row.RequiresStateOwner ||
                            expectedRow.StateOwnerIndex != row.StateOwnerIndex ||
                            expectedRow.RequiredVisualState != row.RequiredVisualState)
                        {
                            throw new InvalidOperationException(
                                "One logical renderer has inconsistent state ownership across " +
                                "its submeshes.");
                        }
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
                    if (!entityManager.Exists(convertedRenderEntity))
                    {
                        throw new InvalidOperationException(
                            "Eligible source row does not reference exactly one baking-time " +
                            "render entity.");
                    }
                    var convertedRenderEntities = new List<Entity>();
                    if (entityManager.HasComponent<RenderMeshUnmanaged>(convertedRenderEntity))
                    {
                        convertedRenderEntities.Add(convertedRenderEntity);
                    }
                    if (additionalRenderEntities.TryGetValue(
                            convertedRenderEntity,
                            out var additionalEntitiesForSource))
                    {
                        convertedRenderEntities.AddRange(additionalEntitiesForSource);
                    }
                    if (convertedRenderEntities.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Eligible source row does not resolve to a converted render " +
                            "entity or exact additional render entities.");
                    }

                    var rendererMatchedParts = new HashSet<SourcePartKey>();
                    for (int renderIndex = 0;
                         renderIndex < convertedRenderEntities.Count;
                         renderIndex++)
                    {
                        Entity renderEntity = convertedRenderEntities[renderIndex];
                        IReadOnlyList<SourcePartKey> entityParts = RequireExactRenderParts(
                            entityManager,
                            renderEntity,
                            rows);
                        for (int partIndex = 0; partIndex < entityParts.Count; partIndex++)
                        {
                            SourcePartKey entityPart = entityParts[partIndex];
                            if (!rendererMatchedParts.Add(entityPart) ||
                                !matched.Add(entityPart))
                            {
                                throw new InvalidOperationException(
                                    "More than one converted render entity matched one " +
                                    "logical submesh row.");
                            }
                        }
                        commandBuffer.AddComponent<
                            OperationMapRenderEligibleSourceComponent>(renderEntity);
                        commandBuffer.AddComponent<BakingOnlyEntity>(renderEntity);
                        bakingOnlyRenderEntities.Add(renderEntity);
                    }
                    if (rendererMatchedParts.Count != rows.Count)
                    {
                        throw new InvalidOperationException(
                            $"Converted source renderer matched {rendererMatchedParts.Count}/" +
                            $"{rows.Count} logical submesh rows.");
                    }
                }
            }

            if (matched.Count != expectedParts.Count)
            {
                throw new InvalidOperationException(
                    $"Logical/source stripping parity failed: matched {matched.Count}/" +
                    $"{expectedParts.Count} eligible rows.");
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

            DynamicBuffer<OperationMapRenderResidentSourceRowComponent>
                packedResidentRows =
                    entityManager.GetBuffer<
                        OperationMapRenderResidentSourceRowComponent>(
                        databaseEntity);
            packedResidentRows.Clear();
            for (int index = 0; index < residentRows.Count; index++)
                packedResidentRows.Add(residentRows[index]);

            int slotCount = _slotQuery.CalculateEntityCount();
            if (slotCount <= 0)
            {
                throw new InvalidOperationException(
                    "Virtualized packed readiness requires at least one proxy slot.");
            }
            int acceptedBuildingCount = 0;
            int acceptedRenderOnlyCount = 0;
            int generatedBuildingCount = 0;
            int generatedRenderOnlyCount = 0;
            int retainedAcceptedBuildingCount = 0;
            int retainedAcceptedRenderOnlyCount = 0;
            int retainedGeneratedBuildingCount = 0;
            int retainedGeneratedRenderOnlyCount = 0;
            foreach (KeyValuePair<OwnerKey, byte> owner in virtualizedOwnerClasses)
            {
                if (!virtualizedOwnerEntities.TryGetValue(
                        owner.Key,
                        out Entity ownerEntity) ||
                    !entityManager.Exists(ownerEntity))
                {
                    throw new InvalidOperationException(
                        "Virtualized identity accounting cannot resolve its owner entity.");
                }
                bool generatedOwner = (owner.Value & 2) != 0;
                bool hasExpectedIdentity = generatedOwner
                    ? entityManager.HasComponent<DenseCityPresentationIdentity>(ownerEntity)
                    : entityManager.HasComponent<OperationMapEntityPresentationIdentity>(
                        ownerEntity);
                if (!hasExpectedIdentity)
                {
                    throw new InvalidOperationException(
                        "Virtualized identity accounting cannot resolve its owner identity component.");
                }
                bool retainsIdentity =
                    !bakingOnlyRenderEntities.Contains(ownerEntity) &&
                    !entityManager.HasComponent<Prefab>(ownerEntity) &&
                    !entityManager.HasComponent<Disabled>(ownerEntity);
                switch (owner.Value)
                {
                    case 0:
                        acceptedRenderOnlyCount++;
                        if (retainsIdentity)
                            retainedAcceptedRenderOnlyCount++;
                        break;
                    case 1:
                        acceptedBuildingCount++;
                        if (retainsIdentity)
                            retainedAcceptedBuildingCount++;
                        break;
                    case 2:
                        generatedRenderOnlyCount++;
                        if (retainsIdentity)
                            retainedGeneratedRenderOnlyCount++;
                        break;
                    case 3:
                        generatedBuildingCount++;
                        if (retainsIdentity)
                            retainedGeneratedBuildingCount++;
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Virtualized owner classification is invalid.");
                }
            }
            commandBuffer.AddComponent(
                databaseEntity,
                new OperationMapRenderPackedReadinessComponent
                {
                    ResidencyMode = 1,
                    EligibleSourceRowCount = expectedParts.Count,
                    EligibleSourceRendererCount = matchedRenderers.Count,
                    ResidentSourceRowCount = residentRows.Count,
                    ProxySlotCount = slotCount,
                    VirtualizedAcceptedBuildingIdentityCount =
                        acceptedBuildingCount,
                    VirtualizedAcceptedRenderOnlyIdentityCount =
                        acceptedRenderOnlyCount,
                    VirtualizedGeneratedBuildingIdentityCount =
                        generatedBuildingCount,
                    VirtualizedGeneratedRenderOnlyIdentityCount =
                        generatedRenderOnlyCount,
                    RetainedVirtualizedAcceptedBuildingIdentityCount =
                        retainedAcceptedBuildingCount,
                    RetainedVirtualizedAcceptedRenderOnlyIdentityCount =
                        retainedAcceptedRenderOnlyCount,
                    RetainedVirtualizedGeneratedBuildingIdentityCount =
                        retainedGeneratedBuildingCount,
                    RetainedVirtualizedGeneratedRenderOnlyIdentityCount =
                        retainedGeneratedRenderOnlyCount
                });
            commandBuffer.Playback(entityManager);
        }

        private static IReadOnlyList<SourcePartKey> RequireExactRenderParts(
            EntityManager entityManager,
            Entity renderEntity,
            IReadOnlyList<OperationMapRenderEligibleSourceRowBakingComponent> expectedRows)
        {
            if (!entityManager.Exists(renderEntity) ||
                !entityManager.HasComponent<RenderMeshUnmanaged>(renderEntity))
            {
                throw new InvalidOperationException(
                    "A converted additional source entity is missing its exact baking-time " +
                    "render representation.");
            }
            RenderMeshUnmanaged renderMesh =
                entityManager.GetComponentData<RenderMeshUnmanaged>(renderEntity);
            OperationMapRenderEligibleSourceRowBakingComponent row = default;
            int matchingRowCount = 0;
            for (int index = 0; index < expectedRows.Count; index++)
            {
                OperationMapRenderEligibleSourceRowBakingComponent candidate =
                    expectedRows[index];
                if (renderMesh.mesh != candidate.Mesh ||
                    renderMesh.materialForSubMesh != candidate.Material)
                    continue;
                row = candidate;
                matchingRowCount++;
            }
            if (matchingRowCount != 1)
            {
                throw new InvalidOperationException(
                    $"Converted source render assets match {matchingRowCount} logical " +
                    "submesh rows; exact submesh ownership is required.");
            }
            return new[] { SourcePartKey.From(row) };
        }

        internal static void RequireDeterministicStateOwnerIndices(
            IReadOnlyDictionary<OwnerKey, int> stateOwners)
        {
            if (stateOwners == null)
                throw new ArgumentNullException(nameof(stateOwners));
            if (stateOwners.Count == 0)
                return;

            var orderedOwners = new List<OwnerKey>(stateOwners.Keys);
            orderedOwners.Sort();
            for (int index = 0; index < orderedOwners.Count; index++)
            {
                int stateOwnerIndex = stateOwners[orderedOwners[index]];
                if (stateOwnerIndex != index)
                {
                    throw new InvalidOperationException(
                        "Virtualized building state-owner indices must be contiguous and " +
                        "match unsigned stable owner identity order.");
                }
            }
        }
    }

    internal readonly struct OwnerKey : IEquatable<OwnerKey>, IComparable<OwnerKey>
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

        public int CompareTo(OwnerKey other)
        {
            int comparison = _low.CompareTo(other._low);
            return comparison != 0 ? comparison : _high.CompareTo(other._high);
        }

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

    internal readonly struct SourcePartKey : IEquatable<SourcePartKey>
    {
        private readonly SourceRowKey _renderer;
        private readonly ushort _subMeshIndex;

        private SourcePartKey(SourceRowKey renderer, ushort subMeshIndex)
        {
            _renderer = renderer;
            _subMeshIndex = subMeshIndex;
        }

        internal static SourcePartKey From(
            OperationMapRenderEligibleSourceRowBakingComponent row) =>
            new(SourceRowKey.From(row), row.SubMeshIndex);

        public bool Equals(SourcePartKey other) =>
            _renderer.Equals(other._renderer) && _subMeshIndex == other._subMeshIndex;

        public override bool Equals(object obj) =>
            obj is SourcePartKey other && Equals(other);

        public override int GetHashCode() =>
            unchecked((_renderer.GetHashCode() * 397) ^ _subMeshIndex);
    }
}
