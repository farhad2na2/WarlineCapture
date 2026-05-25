using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class MissionCameraSystem
{
    private const float M01PlayableStartOrthographicSize = 0.96f;
    private const float M01PlayableCameraHeight = 10f;

    public delegate bool TryResolveFactionSpawnCell(byte factionId, out int2 spawnCell);

    public bool FocusInitialCamera(
        World world,
        SelectionUiCameraSystem selectionUiCameraSystem,
        Camera worldCamera,
        TacticalMapRuntimeLoader loader,
        TryResolveFactionSpawnCell resolveFactionSpawnCell,
        byte fallbackFactionId)
    {
        return FocusCameraOnM01CameraStart(selectionUiCameraSystem, worldCamera, loader) ||
            FocusCameraOnConfiguredFactionBase(world, selectionUiCameraSystem, fallbackFactionId, resolveFactionSpawnCell);
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

    private bool FocusCameraOnConfiguredFactionBase(
        World world,
        SelectionUiCameraSystem selectionUiCameraSystem,
        byte factionId,
        TryResolveFactionSpawnCell resolveFactionSpawnCell)
    {
        if (selectionUiCameraSystem == null ||
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

        selectionUiCameraSystem.FollowCameraGroundCenterTo(focusWorldPosition);
        return true;
    }

    private bool FocusCameraOnM01CameraStart(
        SelectionUiCameraSystem selectionUiCameraSystem,
        Camera worldCamera,
        TacticalMapRuntimeLoader loader)
    {
        if (selectionUiCameraSystem == null ||
            !Chapter01M01PlayableRuntime.TryGetCameraStartWorld(loader, out Vector3 cameraStartWorld))
        {
            return false;
        }

        ApplyM01ProductionCameraPose(worldCamera, loader, cameraStartWorld);
        selectionUiCameraSystem.FollowCameraGroundCenterTo(cameraStartWorld);
        selectionUiCameraSystem.MoveCameraGroundCenterTo(cameraStartWorld);
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
