using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Rendering
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    public partial struct OperationMapRenderMaterialBaseColorBakingSystem : ISystem
    {
        private const int MaximumParentDepth = 64;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private EntityQuery _ownerSectionQuery;
        private EntityQuery _renderQuery;
        private EntityTypeHandle _entityTypeHandle;
        private ComponentTypeHandle<MaterialMeshInfo> _materialMeshInfoTypeHandle;
        private SharedComponentTypeHandle<RenderMeshArray> _renderMeshArrayTypeHandle;

        public void OnCreate(ref SystemState state)
        {
            _ownerSectionQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<SceneSection>()
                },
                Any = new[]
                {
                    ComponentType.ReadOnly<OperationMapEntityPresentationIdentity>(),
                    ComponentType.ReadOnly<DenseCityPresentationIdentity>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities |
                          EntityQueryOptions.IncludePrefab
            });
            _renderQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<MaterialMeshInfo>(),
                    ComponentType.ReadOnly<RenderMeshArray>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities |
                          EntityQueryOptions.IncludePrefab |
                          EntityQueryOptions.IgnoreComponentEnabledState
            });
            _entityTypeHandle = state.GetEntityTypeHandle();
            _materialMeshInfoTypeHandle =
                state.GetComponentTypeHandle<MaterialMeshInfo>(true);
            _renderMeshArrayTypeHandle =
                state.GetSharedComponentTypeHandle<RenderMeshArray>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager entityManager = state.EntityManager;
            _entityTypeHandle.Update(ref state);
            _materialMeshInfoTypeHandle.Update(ref state);
            _renderMeshArrayTypeHandle.Update(ref state);
            var operationMapSections = new HashSet<SceneSection>();
            using (NativeArray<Entity> ownerEntities =
                   _ownerSectionQuery.ToEntityArray(Allocator.Temp))
            {
                for (int ownerIndex = 0; ownerIndex < ownerEntities.Length; ownerIndex++)
                {
                    operationMapSections.Add(
                        entityManager.GetSharedComponentManaged<SceneSection>(
                            ownerEntities[ownerIndex]));
                }
            }
            using NativeArray<ArchetypeChunk> chunks =
                _renderQuery.ToArchetypeChunkArray(Allocator.Temp);
            using var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(_entityTypeHandle);
                NativeArray<MaterialMeshInfo> materialMeshInfos =
                    chunk.GetNativeArray(ref _materialMeshInfoTypeHandle);
                int sharedComponentIndex =
                    chunk.GetSharedComponentIndex(_renderMeshArrayTypeHandle);
                RenderMeshArray renderMeshArray =
                    entityManager.GetSharedComponentManaged<RenderMeshArray>(
                        sharedComponentIndex);

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    Entity entity = entities[entityIndex];
                    if (!HasOperationMapOwner(
                            entityManager,
                            operationMapSections,
                            entity))
                        continue;

                    MaterialMeshInfo materialMeshInfo = materialMeshInfos[entityIndex];
                    if (materialMeshInfo.HasMaterialMeshIndexRange)
                    {
                        throw new InvalidOperationException(
                            $"Operation-map render entity {entity} uses an unsupported " +
                            "multi-material MaterialMeshInfo range.");
                    }

                    Material material = renderMeshArray.GetMaterial(materialMeshInfo);
                    if (material == null)
                    {
                        throw new InvalidOperationException(
                            $"Operation-map render entity {entity} has no resolvable material.");
                    }
                    if (!material.HasProperty(BaseColorId))
                        continue;

                    Color color = material.GetColor(BaseColorId).linear;
                    var component = new URPMaterialPropertyBaseColor
                    {
                        Value = new float4(color.r, color.g, color.b, color.a)
                    };
                    if (entityManager.HasComponent<URPMaterialPropertyBaseColor>(entity))
                        commandBuffer.SetComponent(entity, component);
                    else
                        commandBuffer.AddComponent(entity, component);
                }
            }

            commandBuffer.Playback(entityManager);
        }

        private static bool HasOperationMapOwner(
            EntityManager entityManager,
            HashSet<SceneSection> operationMapSections,
            Entity entity)
        {
            Entity current = entity;
            for (int depth = 0; depth < MaximumParentDepth; depth++)
            {
                if (entityManager.HasComponent<OperationMapEntityPresentationIdentity>(current) ||
                    entityManager.HasComponent<DenseCityPresentationIdentity>(current))
                    return true;
                if (!entityManager.HasComponent<Parent>(current))
                    break;
                current = entityManager.GetComponentData<Parent>(current).Value;
            }

            return entityManager.HasComponent<SceneSection>(entity) &&
                   operationMapSections.Contains(
                       entityManager.GetSharedComponentManaged<SceneSection>(entity));
        }
    }
}
