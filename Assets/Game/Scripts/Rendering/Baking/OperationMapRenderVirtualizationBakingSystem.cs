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
            for (int index = 0; index < expectedBuffer.Length; index++)
            {
                OperationMapRenderEligibleSourceRowBakingComponent row =
                    expectedBuffer[index];
                if (!expected.TryAdd(SourceRowKey.From(row), row))
                {
                    throw new InvalidOperationException(
                        "Eligible logical rows contain a duplicate owner/path identity.");
                }
            }

            var matched = new HashSet<SourceRowKey>();
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
                    SourceRowKey key = SourceRowKey.From(source);
                    if (!expected.TryGetValue(
                            key,
                            out OperationMapRenderEligibleSourceRowBakingComponent row))
                        continue;

                    if (source.IsRenderOnlyOwner != 1)
                    {
                        throw new InvalidOperationException(
                            "An eligible logical row resolved to a canonical gameplay owner.");
                    }
                    if (!matched.Add(key))
                    {
                        throw new InvalidOperationException(
                            "More than one converted source render entity matched one logical row.");
                    }
                    Entity convertedRenderEntity = source.RenderEntity;
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
            commandBuffer.Playback(entityManager);
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
