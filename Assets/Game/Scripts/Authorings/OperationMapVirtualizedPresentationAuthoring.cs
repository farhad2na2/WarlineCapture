using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Authoring
{
    /// <summary>
    /// Candidate-only root for the virtualized presentation database and its one shared,
    /// deterministically ordered mesh/material array.
    /// Source render ownership changes only when an explicit source presentation root is
    /// assigned and every eligible row matches the generated logical database.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OperationMapVirtualizedPresentationAuthoring : MonoBehaviour
    {
        [SerializeField] private OperationMapRenderDatabaseBakeConfig databaseConfig;
        [SerializeField] private GameObject sourcePresentationRoot;
        [SerializeField, Min(0)] private int mapGeneration;

        public OperationMapRenderDatabaseBakeConfig DatabaseConfig => databaseConfig;
        public GameObject SourcePresentationRoot => sourcePresentationRoot;
        public int MapGeneration => mapGeneration;

        public bool TryValidate(out string error)
        {
            if (databaseConfig == null)
            {
                error = "Virtualized presentation root requires a generated database config.";
                return false;
            }

            if (!databaseConfig.TryValidateSchema(out error))
                return false;

            error = null;
            return true;
        }

        [BakingVersion("WarlineCapture", 2)]
        private sealed class Baker : Baker<OperationMapVirtualizedPresentationAuthoring>
        {
            public override void Bake(OperationMapVirtualizedPresentationAuthoring authoring)
            {
                if (!OperationMapRenderMeshArrayBuilder.TryBuild(
                        authoring.DatabaseConfig,
                        out RenderMeshArray renderMeshArray,
                        out _))
                {
                    return;
                }
                if (!OperationMapRenderProxySlotBuilder.TryBuild(
                        authoring.DatabaseConfig,
                        out OperationMapRenderProxySlotBakeDescriptor[] slotDescriptors,
                        out _))
                {
                    return;
                }
                if (!OperationMapRenderDatabaseBlobBuilder.TryBuild(
                        authoring.DatabaseConfig,
                        out BlobAssetReference<OperationMapRenderDatabaseBlob> databaseBlob,
                        out _))
                {
                    return;
                }
                AddBlobAsset(ref databaseBlob, out _);

                DependsOn(authoring.DatabaseConfig);
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new OperationMapRenderDatabaseComponent
                {
                    Blob = databaseBlob,
                    ContentHash = new FixedString128Bytes(authoring.DatabaseConfig.ContentHash),
                    SchemaVersion = authoring.DatabaseConfig.SchemaVersion,
                    MapGeneration = authoring.mapGeneration
                });
                AddComponent(entity, new OperationMapRenderVirtualizationStateComponent());
                AddComponent(entity, new OperationMapRenderVirtualizationMetricsComponent());
                AddComponent(
                    entity,
                    new OperationMapRenderSlotCommandStateComponent());
                AddComponent(
                    entity,
                    new OperationMapRenderStateChangeSequenceComponent());
                AddComponent(entity, new OperationMapRenderStateSyncStateComponent());
                AddBuffer<OperationMapRenderStateChangeComponent>(entity);
                AddBuffer<OperationMapRenderCanonicalStateComponent>(entity);
                DynamicBuffer<OperationMapRenderSlotCommandComponent> commands =
                    AddBuffer<OperationMapRenderSlotCommandComponent>(entity);
                AddSharedComponentManaged(entity, renderMeshArray);
                if (authoring.SourcePresentationRoot != null)
                {
                    AddBuffer<OperationMapRenderResidentSourceRowComponent>(entity);
                    OperationMapRenderSourceBakingMarkerBuilder.AddMarkers(
                        this,
                        authoring.SourcePresentationRoot,
                        authoring.DatabaseConfig,
                        entity);
                }

                MaterialMeshInfo initialMaterialMeshInfo =
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0, 0);
                for (int index = 0; index < slotDescriptors.Length; index++)
                {
                    OperationMapRenderProxySlotBakeDescriptor descriptor =
                        slotDescriptors[index];
                    commands.Add(new OperationMapRenderSlotCommandComponent
                    {
                        SlotIndex = descriptor.SlotIndex,
                        LogicalRowIndex = -1,
                        PlacementIndex = -1,
                        PartIndex = -1,
                        PoolBucketIndex = -1,
                        AssignmentGeneration = 0,
                        Assigned = 0
                    });
                    // Renderable retains a world-space LocalToWorld while avoiding LocalTransform/Parent
                    // on these fixed, hierarchy-free presentation slots.
                    Entity slotEntity = CreateAdditionalEntity(TransformUsageFlags.Renderable);
                    AddSharedComponentManaged(slotEntity, renderMeshArray);
                    AddSharedComponent(slotEntity, descriptor.FilterSettings);
                    AddComponent(slotEntity, initialMaterialMeshInfo);
                    SetComponentEnabled<MaterialMeshInfo>(slotEntity, false);
                    AddComponent(slotEntity, new LocalToWorld { Value = float4x4.identity });
                    AddComponent(slotEntity, new RenderBounds
                    {
                        Value = new AABB
                        {
                            Center = float3.zero,
                            Extents = float3.zero
                        }
                    });
                    AddComponent<WorldToLocal_Tag>(slotEntity);
                    AddComponent<BlendProbeTag>(slotEntity);
                    AddComponent(slotEntity, new URPMaterialPropertyBaseColor
                    {
                        Value = new float4(1f)
                    });
                    AddComponent(slotEntity, new OperationMapRenderProxySlotComponent
                    {
                        SlotIndex = descriptor.SlotIndex,
                        PoolBucketIndex = descriptor.PoolBucketIndex,
                        PlacementIndex = -1,
                        PartIndex = -1,
                        AssignmentGeneration = 0
                    });
                }
            }
        }
    }
}
