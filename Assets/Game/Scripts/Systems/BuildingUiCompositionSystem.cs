using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingUiCompositionSystem
{
    public BuildingUiContextSystem.Source CreateSource(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementQueryUiSystemHelper.Context> createBuildingPlacementQueryContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        return source.BuildingUiContextSystem.CreateSource(
            source.RuntimeResourceSystem,
            source.BuildingDefinitionPrefabSystemHelper,
            source.RuntimeBuildingSystem,
            source.BuildingProductionSystem,
            source.BuildingProductionRequestBoundary,
            () => source.BuildingProductionContextCompositionSystemHelper.CreateProductionRequestContext(
                source.BuildingProductionCompositionSystemHelper.CreateRuntimeContextSource(
                    source,
                    createRuntimeContextSource,
                    createPlacementCommandContext,
                    interactionContext,
                    markerPropertyBlock)),
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId,
            () => UnityEngine.Time.frameCount,
            source.BuildingEntityManagerAccessSystem.TryGetEntityManager,
            () => UnityEngine.Time.time,
            source.RuntimeBuildingSystem.HasSelectedBuilding,
            () => source.RuntimeBuildingSystem.CurrentActiveBuildingId.HasValue,
            () => source.BuildingPlacementLifecycleCompositionSystemHelper.HasPendingBuildingPlacement,
            () => source.BuildingPlacementLifecycleCompositionSystemHelper.CanConfirmBuildingPlacement,
            () => source.BuildingPlacementQueryUiSystemHelper.GetPlacementStatusText(source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement),
            () => source.BuildingPlacementQueryUiSystemHelper.GetSelectedBuildingLabel(createBuildingPlacementQueryContext(source)),
            () => source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacementCost,
            () => source.BuildingPlacementQueryUiSystemHelper.GetActivePlacementDurationSeconds(source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement),
            () => source.BuildingPlacementQueryUiSystemHelper.GetSelectedBuildingDisplayName(createBuildingPlacementQueryContext(source)),
            () => source.BuildingPlacementQueryUiSystemHelper.GetSelectedBuildingDescription(createBuildingPlacementQueryContext(source)),
            (out int current, out int max) => source.BuildingPlacementQueryUiSystemHelper.TryGetSelectedBuildingHealth(
                createBuildingPlacementQueryContext(source),
                out current,
                out max),
            (out GameObject prefab) => source.BuildingPlacementQueryUiSystemHelper.TryGetSelectedBuildingPreviewPrefab(
                createBuildingPlacementQueryContext(source),
                out prefab),
            buildingId => source.BuildingRuntimeQuerySystem.IsRuntimeBuildingWall(
                source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                buildingId),
            buildingId => source.BuildingRuntimeQuerySystem.IsRuntimeBuildingCityGenerated(
                source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                buildingId),
            (int buildingId, out byte factionId) => source.BuildingRuntimeQuerySystem.TryGetRuntimeBuildingOwnerFaction(
                source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(createRuntimeContextSource(source)),
                buildingId,
                out factionId),
            camera => source.BuildingSelectionSystem.HasVisibleSelectableBuilding(
                createBuildingSelectionContext(source),
                camera != null ? camera : source.BuildingPlacementStartupSystemHelper.WorldCamera,
                Screen.width,
                Screen.height),
            (Entity unitEntity, out GameObject prefab) =>
            {
                RuntimeUnitPrefabSystem.Context runtimeUnitPrefabContext =
                    BuildingRuntimeResourcePrefabContextCompositionSystemHelper.CreateRuntimeUnitPrefabContext(
                        source.BuildingRuntimeResourcePrefabContextCompositionSystemHelper,
                        BuildingRuntimeResourcePrefabCompositionSystemHelper.Create(
                            source.BuildingRuntimeResourcePrefabCompositionHelper,
                            source));
                return TryResolveLiveUnitPreviewPrefab(source, runtimeUnitPrefabContext, unitEntity, out prefab);
            },
            () => EnqueueAndProcessConfirmBuildingPlacement(
                source,
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock)),
            () => EnqueueAndProcessCancelBuildingPlacement(
                source,
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock)),
            () => EnqueueAndProcessRotateBuildingPlacement(
                source,
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock)));
    }

    public BuildingUiCommandBoundary.Context CreateCommandContext(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementQueryUiSystemHelper.Context> createBuildingPlacementQueryContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        return source.BuildingUiContextSystem.CreateCommandContext(
            CreateSource(
                source,
                interactionContext,
                markerPropertyBlock,
                createRuntimeContextSource,
                createPlacementCommandContext,
                createBuildingPlacementQueryContext,
                createBuildingSelectionContext));
    }

    public BuildingUiQuerySystem.Context CreateQueryContext(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementQueryUiSystemHelper.Context> createBuildingPlacementQueryContext,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        return source.BuildingUiContextSystem.CreateQueryContext(
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
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessConfirmBuildingPlacement(entityManager, context)
            : ConfirmBuildingPlacementWithoutEntityManager(context);
    }

    private static void EnqueueAndProcessCancelBuildingPlacement(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        if (source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager))
            source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessCancelBuildingPlacement(entityManager, context);
        else
            CancelBuildingPlacementWithoutEntityManager(context);
    }

    private static bool EnqueueAndProcessRotateBuildingPlacement(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessRotateBuildingPlacement(entityManager, context)
            : RotateBuildingPlacementWithoutEntityManager(context);
    }

    private static bool ConfirmBuildingPlacementWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        return context.SessionSystem != null &&
               context.SessionSystem.ConfirmBuildingPlacement(context.SessionContext);
    }

    private static void CancelBuildingPlacementWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        context.SessionSystem?.CancelBuildingPlacement(context.SessionContext);
    }

    private static bool RotateBuildingPlacementWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
    {
        return context.SessionSystem != null &&
               context.SessionSystem.RotateBuildingPlacement(context.SessionContext);
    }
}
