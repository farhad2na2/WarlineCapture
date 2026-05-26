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
        TryResolveFactionSpawnCell resolveFactionSpawnCell,
        byte fallbackFactionId)
    {
        return FocusCameraOnM01CameraStart(world, selectionUiCameraSystem, worldCamera) ||
            FocusCameraOnConfiguredFactionBase(world, selectionUiCameraSystem, fallbackFactionId, resolveFactionSpawnCell);
    }

    public bool ApplyM01ProductionCameraPoseForCurrentAspect(World world, Camera worldCamera)
    {
        if (!Chapter01M01PlayableRuntime.TryGetCameraStartWorld(world, out Vector3 cameraStartWorld))
            return false;

        ApplyM01ProductionCameraPose(worldCamera, cameraStartWorld);
        return true;
    }

    public void ApplyM01ProductionCameraPoseIfActive(World world, Camera worldCamera)
    {
        ApplyM01ProductionCameraPoseForCurrentAspect(world, worldCamera);
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
        World world,
        SelectionUiCameraSystem selectionUiCameraSystem,
        Camera worldCamera)
    {
        if (selectionUiCameraSystem == null ||
            !Chapter01M01PlayableRuntime.TryGetCameraStartWorld(world, out Vector3 cameraStartWorld))
        {
            return false;
        }

        ApplyM01ProductionCameraPose(worldCamera, cameraStartWorld);
        selectionUiCameraSystem.FollowCameraGroundCenterTo(cameraStartWorld);
        selectionUiCameraSystem.MoveCameraGroundCenterTo(cameraStartWorld);
        ApplyM01ProductionCameraPose(worldCamera, cameraStartWorld);
        return true;
    }

    private void ApplyM01ProductionCameraPose(
        Camera worldCamera,
        Vector3 cameraStartWorld)
    {
        if (worldCamera == null)
            return;

        worldCamera.orthographic = true;
        worldCamera.orthographicSize = M01PlayableStartOrthographicSize;
        worldCamera.nearClipPlane = Mathf.Min(worldCamera.nearClipPlane, 0.05f);
        worldCamera.farClipPlane = Mathf.Max(worldCamera.farClipPlane, M01PlayableCameraHeight + 10f);
        worldCamera.transform.SetPositionAndRotation(
            new Vector3(cameraStartWorld.x, M01PlayableCameraHeight, cameraStartWorld.z),
            Quaternion.Euler(90f, 0f, 0f));
    }
}
