using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingUiCompositionSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public BuildingUiContextSystem.Source CreateSource(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementQuerySystem.Context> createBuildingPlacementQueryContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext)
    {
        return source.BuildingUiContextSystem.CreateSource(
            source.RuntimeResourceSystem,
            source.BuildingDefinitionSystem,
            source.RuntimeBuildingSystem,
            source.BuildingProductionSystem,
            source.BuildingProductionRequestBoundary,
            () => source.BuildingProductionContextSystem.CreateProductionRequestContext(
                source.BuildingProductionCompositionSystem.CreateRuntimeContextSource(
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
            () => source.BuildingPlacementLifecycleSystem.HasPendingBuildingPlacement,
            () => source.BuildingPlacementLifecycleSystem.CanConfirmBuildingPlacement,
            () => source.BuildingPlacementQuerySystem.GetPlacementStatusText(source.BuildingPlacementLifecycleSystem.ActivePlacement),
            () => source.BuildingPlacementQuerySystem.GetSelectedBuildingLabel(createBuildingPlacementQueryContext(source)),
            () => source.BuildingPlacementLifecycleSystem.ActivePlacementCost,
            () => source.BuildingPlacementQuerySystem.GetActivePlacementDurationSeconds(source.BuildingPlacementLifecycleSystem.ActivePlacement),
            () => source.BuildingPlacementQuerySystem.GetSelectedBuildingDisplayName(createBuildingPlacementQueryContext(source)),
            () => source.BuildingPlacementQuerySystem.GetSelectedBuildingDescription(createBuildingPlacementQueryContext(source)),
            (out int current, out int max) => source.BuildingPlacementQuerySystem.TryGetSelectedBuildingHealth(
                createBuildingPlacementQueryContext(source),
                out current,
                out max),
            (out GameObject prefab) => source.BuildingPlacementQuerySystem.TryGetSelectedBuildingPreviewPrefab(
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
                camera != null ? camera : source.BuildingPlacementStartupSystem.WorldCamera,
                Screen.width,
                Screen.height),
            (Entity unitEntity, out GameObject prefab) =>
            {
                RuntimeUnitPrefabSystem.Context runtimeUnitPrefabContext =
                    BuildingRuntimeResourcePrefabContextSystem.CreateRuntimeUnitPrefabContext(
                        source.BuildingRuntimeResourcePrefabContextSystem,
                        BuildingRuntimeResourcePrefabCompositionSystem.Create(
                            source.BuildingRuntimeResourcePrefabCompositionSystem,
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
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementQuerySystem.Context> createBuildingPlacementQueryContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext)
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
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementInteractionSystem.Context interactionContext,
        MaterialPropertyBlock markerPropertyBlock,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementQuerySystem.Context> createBuildingPlacementQueryContext,
        Func<BuildingGameplayCompositionSourceSystem, BuildingSelectionSystem.Context> createBuildingSelectionContext)
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
        BuildingGameplayCompositionSourceSystem source,
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
        BuildingGameplayCompositionSourceSystem source,
        FixedString64Bytes sourceKey,
        out GameObject prefab)
    {
        prefab = null;
        return source?.BuildingDefinitionSystem != null &&
               sourceKey.Length > 0 &&
               source.BuildingDefinitionSystem.TryResolveConfiguredUnitSpawnPrefab(sourceKey.ToString(), out prefab) &&
               prefab != null;
    }

    private static bool EnqueueAndProcessConfirmBuildingPlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementCommandSystem.Context context)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingPlacementCommandSystem.EnqueueAndProcessConfirmBuildingPlacement(entityManager, context)
            : ConfirmBuildingPlacementWithoutEntityManager(context);
    }

    private static void EnqueueAndProcessCancelBuildingPlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementCommandSystem.Context context)
    {
        if (source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager))
            source.BuildingPlacementCommandSystem.EnqueueAndProcessCancelBuildingPlacement(entityManager, context);
        else
            CancelBuildingPlacementWithoutEntityManager(context);
    }

    private static bool EnqueueAndProcessRotateBuildingPlacement(
        BuildingGameplayCompositionSourceSystem source,
        BuildingPlacementCommandSystem.Context context)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingPlacementCommandSystem.EnqueueAndProcessRotateBuildingPlacement(entityManager, context)
            : RotateBuildingPlacementWithoutEntityManager(context);
    }

    private static bool ConfirmBuildingPlacementWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        return context.SessionSystem != null &&
               context.SessionSystem.ConfirmBuildingPlacement(context.SessionContext);
    }

    private static void CancelBuildingPlacementWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        context.SessionSystem?.CancelBuildingPlacement(context.SessionContext);
    }

    private static bool RotateBuildingPlacementWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        return context.SessionSystem != null &&
               context.SessionSystem.RotateBuildingPlacement(context.SessionContext);
    }
}
