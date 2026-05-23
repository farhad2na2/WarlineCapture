using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class MissionStartupSystem
{
    private const float M01PlayableStartOrthographicSize = 0.96f;
    private const float M01PlayableCameraHeight = 10f;

    public delegate bool TryResolveFactionSpawnCell(byte factionId, out int2 spawnCell);

    public readonly struct Result
    {
        public readonly bool ActiveFixedTacticalMission;

        public Result(bool activeFixedTacticalMission)
        {
            ActiveFixedTacticalMission = activeFixedTacticalMission;
        }
    }

    public Result Initialize(
        World world,
        Chapter01MissionTacticalRuntimeBinder tacticalBinder,
        Camera worldCamera,
        DayNightSystem dayNight,
        IReadOnlyList<GameObject> legacyVisualRootsDisabledForM01)
    {
        tacticalBinder?.TryApplyActiveMission(worldCamera);
        TacticalMapRuntimeLoader loader = tacticalBinder != null ? tacticalBinder.TacticalMapLoader : null;
        UpdateActiveMission(world, loader);
        bool activeFixedTacticalMission = Chapter01M01PlayableRuntime.IsActiveMission();
        ApplyM01ProductionSceneVisibility(legacyVisualRootsDisabledForM01, activeFixedTacticalMission);
        ApplyFixedTacticalMissionGuardrails(dayNight, activeFixedTacticalMission);
        DisableGenericAIPlansForFixedTacticalMission(world, activeFixedTacticalMission);
        return new Result(activeFixedTacticalMission);
    }

    public void UpdateActiveMission(World world, TacticalMapRuntimeLoader loader)
    {
        Chapter01M01PlayableRuntime.TryInitializeActiveMission(world, loader, out _);
    }

    public bool FocusInitialCamera(
        World world,
        RTSSelectionSystem selection,
        Camera worldCamera,
        TacticalMapRuntimeLoader loader,
        TryResolveFactionSpawnCell resolveFactionSpawnCell,
        byte fallbackFactionId)
    {
        return FocusCameraOnM01CameraStart(selection, worldCamera, loader) ||
            FocusCameraOnConfiguredFactionBase(world, selection, fallbackFactionId, resolveFactionSpawnCell);
    }

    public bool ApplyM01ProductionCameraPoseForCurrentAspect(Camera worldCamera, TacticalMapRuntimeLoader loader)
    {
        if (!Chapter01M01PlayableRuntime.TryGetCameraStartWorld(loader, out Vector3 cameraStartWorld))
            return false;

        Vector3 cameraCenter = TryResolveM01ProductionFrameCenter(loader, out Vector3 productionFrameCenter)
            ? productionFrameCenter
            : cameraStartWorld;
        ApplyM01ProductionCameraPose(worldCamera, loader, cameraCenter);
        return true;
    }

    public void ApplyM01ProductionCameraPoseIfActive(Camera worldCamera, TacticalMapRuntimeLoader loader)
    {
        ApplyM01ProductionCameraPoseForCurrentAspect(worldCamera, loader);
    }

    private void ApplyFixedTacticalMissionGuardrails(DayNightSystem dayNight, bool activeFixedTacticalMission)
    {
        if (dayNight == null)
            return;

        dayNight.SetRuntimeVisualsEnabled(!activeFixedTacticalMission);
    }

    public void DisableGenericAIPlansForFixedTacticalMission(World world, bool activeFixedTacticalMission)
    {
        if (!activeFixedTacticalMission || world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        DisableAIBuildPlans(em);
        DisableAIProductionPlans(em);
        DisableAISquadPlans(em);
    }

    private void DisableAIBuildPlans(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIBuildPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AIBuildPlan>(entity))
                continue;

            AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(entity);
            plan.Enabled = 0;
            em.SetComponentData(entity, plan);
        }
    }

    private void DisableAIProductionPlans(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIProductionPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AIProductionPlan>(entity))
                continue;

            AIProductionPlan plan = em.GetComponentData<AIProductionPlan>(entity);
            plan.Enabled = 0;
            em.SetComponentData(entity, plan);
        }
    }

    private void DisableAISquadPlans(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AISquadPlan>());
        using var entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<AISquadPlan>(entity))
                continue;

            AISquadPlan plan = em.GetComponentData<AISquadPlan>(entity);
            plan.Enabled = 0;
            em.SetComponentData(entity, plan);
        }
    }

    private void ApplyM01ProductionSceneVisibility(
        IReadOnlyList<GameObject> legacyVisualRootsDisabledForM01,
        bool activeFixedTacticalMission)
    {
        if (legacyVisualRootsDisabledForM01 == null)
            return;

        for (int i = 0; i < legacyVisualRootsDisabledForM01.Count; i++)
        {
            GameObject visualRoot = legacyVisualRootsDisabledForM01[i];
            if (visualRoot != null)
                visualRoot.SetActive(!activeFixedTacticalMission);
        }
    }

    private bool FocusCameraOnConfiguredFactionBase(
        World world,
        RTSSelectionSystem selection,
        byte factionId,
        TryResolveFactionSpawnCell resolveFactionSpawnCell)
    {
        if (selection == null ||
            resolveFactionSpawnCell == null ||
            !resolveFactionSpawnCell(factionId, out int2 spawnCell))
        {
            return false;
        }

        Vector3 focusWorldPosition = new(spawnCell.x, 0f, spawnCell.y);
        if (world != null && world.IsCreated)
        {
            EntityManager em = world.EntityManager;
            using EntityQuery gridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            if (!gridQuery.IsEmptyIgnoreFilter)
            {
                Entity gridEntity = gridQuery.GetSingletonEntity();
                GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
                focusWorldPosition = GridUtils.CellToWorldCenter(grid, spawnCell);
            }
        }

        selection.FollowCameraGroundCenterTo(focusWorldPosition);
        return true;
    }

    private bool FocusCameraOnM01CameraStart(
        RTSSelectionSystem selection,
        Camera worldCamera,
        TacticalMapRuntimeLoader loader)
    {
        if (selection == null ||
            !Chapter01M01PlayableRuntime.TryGetCameraStartWorld(loader, out Vector3 cameraStartWorld))
        {
            return false;
        }

        ApplyM01ProductionCameraPose(worldCamera, loader, cameraStartWorld);
        selection.FollowCameraGroundCenterTo(cameraStartWorld);
        selection.MoveCameraGroundCenterTo(cameraStartWorld);
        ApplyM01ProductionCameraPose(worldCamera, loader, cameraStartWorld);
        return true;
    }

    private void ApplyM01ProductionCameraPose(
        Camera worldCamera,
        TacticalMapRuntimeLoader loader,
        Vector3 cameraStartWorld)
    {
        if (worldCamera == null)
            return;

        worldCamera.orthographic = true;
        worldCamera.orthographicSize = ResolveM01ProductionOrthographicSize(worldCamera, loader);
        worldCamera.nearClipPlane = Mathf.Min(worldCamera.nearClipPlane, 0.05f);
        worldCamera.farClipPlane = Mathf.Max(worldCamera.farClipPlane, M01PlayableCameraHeight + 10f);
        cameraStartWorld = ClampM01CameraCenterToTacticalMap(worldCamera, loader, cameraStartWorld);
        worldCamera.transform.SetPositionAndRotation(
            new Vector3(cameraStartWorld.x, M01PlayableCameraHeight, cameraStartWorld.z),
            Quaternion.Euler(90f, 0f, 0f));
    }

    private float ResolveM01ProductionOrthographicSize(Camera worldCamera, TacticalMapRuntimeLoader loader)
    {
        TacticalMapDefinition definition = loader != null ? loader.Definition : null;
        if (definition == null || worldCamera == null || worldCamera.aspect <= 0.0001f)
            return M01PlayableStartOrthographicSize;

        float widthFitOrthographicSize = definition.VisibleWorldSize.x / (2f * worldCamera.aspect);
        return Mathf.Clamp(widthFitOrthographicSize, 0.72f, M01PlayableStartOrthographicSize);
    }

    private bool TryResolveM01ProductionFrameCenter(TacticalMapRuntimeLoader loader, out Vector3 cameraCenter)
    {
        cameraCenter = default;
        if (loader == null)
            return false;

        bool hasAny = false;
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;
        IncludeM01FrameAnchor(loader, Chapter01M01PlayableRuntime.PlayerSpawnAnchorId, ref min, ref max, ref hasAny);
        IncludeM01FrameAnchor(loader, Chapter01M01PlayableRuntime.EnemySpawnAnchorId, ref min, ref max, ref hasAny);
        IncludeM01FrameAnchor(loader, Chapter01M01PlayableRuntime.DecorCommandPointEntityId, ref min, ref max, ref hasAny);
        IncludeM01FrameAnchor(loader, Chapter01M01PlayableRuntime.ObjectiveAnchorId, ref min, ref max, ref hasAny);
        if (!hasAny)
            return false;

        cameraCenter = (min + max) * 0.5f;
        cameraCenter.y = 0f;
        return true;
    }

    private void IncludeM01FrameAnchor(
        TacticalMapRuntimeLoader loader,
        string anchorId,
        ref Vector3 min,
        ref Vector3 max,
        ref bool hasAny)
    {
        if (loader == null || !loader.TryGetAnchorWorldPosition(anchorId, out Vector3 world))
            return;

        if (!hasAny)
        {
            min = world;
            max = world;
            hasAny = true;
            return;
        }

        min = Vector3.Min(min, world);
        max = Vector3.Max(max, world);
    }

    private Vector3 ClampM01CameraCenterToTacticalMap(
        Camera worldCamera,
        TacticalMapRuntimeLoader loader,
        Vector3 cameraCenter)
    {
        TacticalMapDefinition definition = loader != null ? loader.Definition : null;
        if (definition == null || worldCamera == null || !worldCamera.orthographic)
            return cameraCenter;

        float halfHeight = worldCamera.orthographicSize;
        float halfWidth = halfHeight * worldCamera.aspect;
        float xMin = definition.WorldOrigin.x + halfWidth;
        float xMax = definition.WorldOrigin.x + definition.VisibleWorldSize.x - halfWidth;
        float zMin = definition.WorldOrigin.y + halfHeight;
        float zMax = definition.WorldOrigin.y + definition.VisibleWorldSize.y - halfHeight;
        float mapCenterX = definition.WorldOrigin.x + definition.VisibleWorldSize.x * 0.5f;
        float mapCenterZ = definition.WorldOrigin.y + definition.VisibleWorldSize.y * 0.5f;

        cameraCenter.x = xMin <= xMax
            ? Mathf.Clamp(cameraCenter.x, xMin, xMax)
            : mapCenterX;
        cameraCenter.z = zMin <= zMax
            ? Mathf.Clamp(cameraCenter.z, zMin, zMax)
            : mapCenterZ;
        return cameraCenter;
    }
}
