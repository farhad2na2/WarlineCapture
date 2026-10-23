using Game.Components;
using Game.Runtime;
using Game.Rendering;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

namespace Game.Composition
{
    public readonly struct RuntimeGridSourceBinding
    {
        public readonly World World;
        public readonly MatchSceneView View;
        public readonly OperationMapSceneView SourceView;
        public readonly Entity MapRoot, Grid, SceneRoot;
        public readonly int Generation;
        public readonly string SourceIdentity, SourceContent, GeneratedMetadata;
        internal RuntimeGridSourceBinding(World world, MatchSceneView view, OperationMapSceneView source,
            Entity map, Entity grid, Entity scene, int generation, string identity, string content, string generated)
        { World = world; View = view; SourceView = source; MapRoot = map; Grid = grid; SceneRoot = scene;
          Generation = generation; SourceIdentity = identity; SourceContent = content; GeneratedMetadata = generated; }
        public bool SameIdentity(in RuntimeGridSourceBinding other) => World == other.World && View == other.View &&
            SourceView == other.SourceView && MapRoot == other.MapRoot && Grid == other.Grid && SceneRoot == other.SceneRoot &&
            Generation == other.Generation && SourceIdentity == other.SourceIdentity && SourceContent == other.SourceContent &&
            GeneratedMetadata == other.GeneratedMetadata;
    }

    public sealed partial class MatchSceneView : IRuntimeActorBirthSourceProvider
    {
        private World birthProviderWorld;
        private Entity birthProviderRoot;
        private void RegisterActorBirthSourceProvider(World world)
        {
            if (operationMapRuntimeBootstrapSystem == null || !operationMapRuntimeBootstrapSystem.TryGetPublishedRoot(world, out var root)) return;
            birthProviderWorld = world; birthProviderRoot = root;
            world.EntityManager.AddComponentObject(root, new RuntimeActorBirthSourceProvider(this));
        }
        private void UnregisterActorBirthSourceProvider()
        {
            if (birthProviderWorld != null && birthProviderWorld.IsCreated && birthProviderWorld.EntityManager.Exists(birthProviderRoot))
            {
                var em = birthProviderWorld.EntityManager;
                if (em.HasComponent<RuntimeActorBirthSourceProvider>(birthProviderRoot) &&
                    ReferenceEquals(em.GetComponentObject<RuntimeActorBirthSourceProvider>(birthProviderRoot).Provider, this))
                    em.RemoveComponent<RuntimeActorBirthSourceProvider>(birthProviderRoot);
            }
            birthProviderWorld = null; birthProviderRoot = Entity.Null;
        }
        public bool TryReadBirthSource(World world, Entity actualGrid, out RuntimeActorSourceIdentity source)
        {
            source = default;
            if (this == null || world == null || world != birthProviderWorld ||
                !TryReadEntitySceneGridBinding(world, birthProviderRoot, actualGrid, out var binding, out _)) return false;
            source = new RuntimeActorSourceIdentity(world.Unmanaged.SequenceNumber, binding.MapRoot, binding.Grid, binding.SceneRoot,
                binding.Generation, EntityId.ToULong(GetEntityId()), EntityId.ToULong(binding.SourceView.GetEntityId()), binding.SourceIdentity, binding.SourceContent, binding.GeneratedMetadata, world.EntityManager.GetComponentData<GridConfig>(actualGrid));
            return true;
        }

        // Passive producer-backed read. No scene load, readiness write, singleton adoption or synthetic SceneTag.
        public bool TryReadEntitySceneGridBinding(World world, Entity mapRoot, Entity grid,
            out RuntimeGridSourceBinding binding, out string reason)
        {
            binding = default;
            reason = "NormalSourceLifecycleUnavailable";
            if (!matchRuntimeBound || world == null || !world.IsCreated || operationMapSceneUnloadStartPending ||
                operationMapSceneLoadingSystem == null || !operationMapSceneLoadingSystem.IsReady ||
                operationMapSceneLoadingSystem.IsUnloading || activeOperationMapSceneView == null ||
                operationMapSceneLoadingSystem.SceneView != activeOperationMapSceneView ||
                !activeOperationMapSceneView.gameObject.scene.isLoaded || operationMapRuntimeBootstrapSystem == null ||
                !operationMapRuntimeBootstrapSystem.TryGetPublishedRoot(world, out var actualRoot) || actualRoot != mapRoot)
                return false;
            var em = world.EntityManager;
            using var references = em.CreateEntityQuery(typeof(MatchSceneReferenceComponent));
            if (references.CalculateEntityCount() != 1 || references.GetSingleton<MatchSceneReferenceComponent>().View.Value != this)
            { reason = "NormalMatchReferenceOwnerMismatch"; return false; }
            // This is the same actual view policy used by InitializeGameplaySystemsIfNeeded.
            if (!matchBootstrapSystem.GameplayInitialized || matchBootstrapSystem.LegacyRuntimeBlockerEnabledAtStartup != false ||
                activeOperationMapSceneView.CanonicalPresentationMode != OperationMapCanonicalPresentationMode.EntityScene ||
                matchBootstrapSystem.RuntimeGridBlockers != null)
            { reason = "EntitySceneHelperDisabledPolicyUnavailable"; return false; }
            var metadata = em.GetComponentData<OperationMapMetadataComponent>(mapRoot);
            var active = em.GetComponentData<ActiveOperationMapComponent>(mapRoot);
            var physical = activeOperationMapSceneView.Definition;
            if (metadata.PhysicalSourceValidated != 1 || !metadata.Blob.IsCreated || physical == null ||
                !physical.TryValidateMetadata(out _) || !physical.TryValidateLocalContentReferences(out _))
            { reason = "LoadedPhysicalSourceUnavailable"; return false; }
            ref var map = ref metadata.Blob.Value;
            if (map.SourceIdentityHash.ToString() != physical.SourceIdentityHash || map.SourceContentHash.ToString() != physical.ContentHash ||
                map.Navigation.AuthoredSubSceneGuid.ToString() != physical.NavigationMetadata.AuthoredSubSceneGuid)
            { reason = "LoadedPhysicalSourceIdentityMismatch"; return false; }
            var guid = new Unity.Entities.Hash128(physical.NavigationMetadata.AuthoredSubSceneGuid);
            var scene = SceneSystem.GetSceneEntity(world.Unmanaged, guid);
            if (!guid.IsValid || scene == Entity.Null || !SceneSystem.IsSceneLoaded(world.Unmanaged, scene) ||
                !em.Exists(grid) || !em.HasComponent<GridConfig>(grid) || em.HasComponent<RuntimeGridBootstrapGridTag>(grid) ||
                !em.HasComponent<SceneTag>(grid) || !em.HasBuffer<ResolvedSectionEntity>(scene))
            { reason = "ActualAuthoredGridLifecycleUnavailable"; return false; }
            var section = em.GetSharedComponent<SceneTag>(grid).SceneEntity;
            if (!em.Exists(section) || !em.HasComponent<SceneSectionData>(section) || !em.HasComponent<SceneEntityReference>(section) ||
                !em.GetComponentData<SceneSectionData>(section).SceneGUID.Equals(guid) ||
                em.GetComponentData<SceneEntityReference>(section).SceneEntity != scene)
            { reason = "ActualAuthoredGridSourceMismatch"; return false; }
            bool owned = false;
            foreach (var row in em.GetBuffer<ResolvedSectionEntity>(scene, true)) if (row.SectionEntity == section) owned = true;
            if (!owned) { reason = "ActualGridSectionNotOwned"; return false; }
            binding = new RuntimeGridSourceBinding(world, this, activeOperationMapSceneView, mapRoot, grid, scene,
                active.Generation, map.SourceIdentityHash.ToString(), map.SourceContentHash.ToString(), map.GeneratedMetadataHash.ToString());
            reason = null; return true;
        }
    }
}
