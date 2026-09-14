using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingUiCompositionSystemHelper
    {
        public BuildingUiCommandSystemHelper.Context CreateCommandContext(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementQueryUiSystemHelper.Context> createBuildingPlacementQueryContext,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionRuntimeCompositionSystemHelper.Context> createBuildingSelectionContext)
        {
            return source.BuildingUiContextCompositionSystemHelper.CreateCommandContext(
                CreateSource(
                    source,
                    interactionContext,
                    markerPropertyBlock,
                    createRuntimeContextSource,
                    createPlacementCommandContext,
                    createBuildingPlacementQueryContext,
                    createBuildingSelectionContext));
        }

        public BuildingUiQueryUiSystemHelper.Context CreateQueryContext(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementQueryUiSystemHelper.Context> createBuildingPlacementQueryContext,
            Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionRuntimeCompositionSystemHelper.Context> createBuildingSelectionContext)
        {
            return source.BuildingUiContextCompositionSystemHelper.CreateQueryContext(
                CreateSource(
                    source,
                    interactionContext,
                    markerPropertyBlock,
                    createRuntimeContextSource,
                    createPlacementCommandContext,
                    createBuildingPlacementQueryContext,
                    createBuildingSelectionContext));
        }

        private static bool TryResolveLiveUnitPreviewPrefab(
            BuildingGameplaySourceCompositionSystemHelper source,
            RuntimeUnitPrefabSystem.Context runtimeUnitPrefabContext,
            Entity unitEntity,
            out GameObject prefab)
        {
            prefab = null;
            if (source == null ||
                unitEntity == Entity.Null ||
                runtimeUnitPrefabContext.TryGetEntityManager == null ||
                !runtimeUnitPrefabContext.TryGetEntityManager(out EntityManager em) ||
                !em.Exists(unitEntity))
            {
                return false;
            }

            runtimeUnitPrefabContext.EnsureEntityQueries?.Invoke(em);
            if (em.HasComponent<UnitRespawnPrefab>(unitEntity))
            {
                Entity prefabEntity = em.GetComponentData<UnitRespawnPrefab>(unitEntity).Prefab;
                if (prefabEntity != Entity.Null &&
                    source.RuntimeUnitPrefabSystem.TryResolveSpawnUnitSourceKey(runtimeUnitPrefabContext, prefabEntity, out FixedString64Bytes sourceKey) &&
                    TryResolveConfiguredUnitSpawnPrefab(source, sourceKey, out prefab))
                {
                    return true;
                }
            }

            if (em.HasComponent<UnitSourcePrefabKey>(unitEntity) &&
                TryResolveConfiguredUnitSpawnPrefab(source, em.GetComponentData<UnitSourcePrefabKey>(unitEntity).Value, out prefab))
            {
                return true;
            }

            if (source.RuntimeBuildingSystem?.Buildings != null)
            {
                foreach (var pair in source.RuntimeBuildingSystem.Buildings)
                {
                    RuntimeBuildingEntity building = pair.Value;
                    if (building == null)
                        continue;

                    if (building.ProducedUnitSourceKeys != null &&
                        building.ProducedUnitSourceKeys.TryGetValue(unitEntity, out FixedString64Bytes producedSourceKey) &&
                        TryResolveConfiguredUnitSpawnPrefab(source, producedSourceKey, out prefab))
                    {
                        return true;
                    }

                    if (building.ProducedUnitPrefabs == null)
                        continue;
                    if (building.ProducedUnitPrefabs.TryGetValue(unitEntity, out prefab) && prefab != null)
                        return true;
                }
            }

            return false;
        }

        private static bool TryResolveConfiguredUnitSpawnPrefab(
            BuildingGameplaySourceCompositionSystemHelper source,
            FixedString64Bytes sourceKey,
            out GameObject prefab)
        {
            prefab = null;
            return source?.BuildingDefinitionPrefabSystemHelper != null &&
                   sourceKey.Length > 0 &&
                   source.BuildingDefinitionPrefabSystemHelper.TryResolveConfiguredUnitSpawnPrefab(sourceKey.ToString(), out prefab) &&
                   prefab != null;
        }

        private static bool EnqueueAndProcessConfirmBuildingPlacement(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementCommandRequestCompositionSystemHelper.Context context) =>
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager em)
                ? source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessConfirmBuildingPlacement(em, context)
                : ConfirmBuildingPlacementWithoutEntityManager(context);

        private static void EnqueueAndProcessCancelBuildingPlacement(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
        {
            if (source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager em))
                source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessCancelBuildingPlacement(em, context);
            else
                CancelBuildingPlacementWithoutEntityManager(context);
        }

        private static bool EnqueueAndProcessRotateBuildingPlacement(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementCommandRequestCompositionSystemHelper.Context context) =>
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager em)
                ? source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessRotateBuildingPlacement(em, context)
                : RotateBuildingPlacementWithoutEntityManager(context);

        private static bool ConfirmBuildingPlacementWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context) =>
            context.SessionSystem != null && context.SessionSystem.ConfirmBuildingPlacement(context.SessionContext);

        private static void CancelBuildingPlacementWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context) =>
            context.SessionSystem?.CancelBuildingPlacement(context.SessionContext);

        private static bool RotateBuildingPlacementWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context) =>
            context.SessionSystem != null && context.SessionSystem.RotateBuildingPlacement(context.SessionContext);
    }
}
