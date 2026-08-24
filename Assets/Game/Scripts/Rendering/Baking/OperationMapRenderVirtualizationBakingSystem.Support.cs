using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Entities;
using Unity.Rendering;

namespace Game.Rendering
{
    public partial struct OperationMapRenderVirtualizationBakingSystem
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
