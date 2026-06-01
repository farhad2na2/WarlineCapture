#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Scripts.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Transforms;

[InitializeOnLoad]
public static class RuntimeGroundingScreenshotProbe
{
    private const string ActiveKey = "WarlineCapture.RuntimeGroundingScreenshotProbe.Active";
    private const string StageKey = "WarlineCapture.RuntimeGroundingScreenshotProbe.Stage";
    private const string FileTriggerPath = "/private/tmp/warline-run-grounding-proof";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string ScreenshotPath = "/private/tmp/warline_grounding_boots_game_camera_runtime_20260530_2255.png";
    private const string ReportPath = "/private/tmp/warline_grounding_boots_game_camera_runtime_20260530_2255.json";
    private const double StartupTimeoutSeconds = 45d;
    private const double MatchTimeoutSeconds = 60d;
    private const double WarmupSeconds = 10d;
    private const int ScreenshotWidth = 1280;
    private const int ScreenshotHeight = 900;

    private static double s_stageStartTime;
    private static bool s_clickedDeploy;
    private static bool s_finished;
    private static string s_result = string.Empty;
    private static string s_detail = string.Empty;
    private static GameObject s_proofRoot;
    private static SoldierProbeData s_pendingData;
    private static MapSurfaceComponent s_pendingSurface;
    private static Entity s_pendingSoldier;

    static RuntimeGroundingScreenshotProbe()
    {
        if (File.Exists(FileTriggerPath))
        {
            File.Delete(FileTriggerPath);
            EditorApplication.update += RunWhenEditorIsReady;
            return;
        }

        if (SessionState.GetInt(ActiveKey, 0) == 1)
            Attach();
    }

    public static void Run()
    {
        s_stageStartTime = EditorApplication.timeSinceStartup;
        s_clickedDeploy = false;
        s_finished = false;
        s_result = string.Empty;
        s_detail = string.Empty;
        CleanupProofObjects();

        SessionState.SetInt(ActiveKey, 1);
        SessionState.SetInt(StageKey, 0);
        Attach();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void RunWhenEditorIsReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        EditorApplication.update -= RunWhenEditorIsReady;
        Debug.Log("[RuntimeGroundingScreenshotProbe] file trigger accepted");
        Run();
    }

    private static void Attach()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= HandleLog;
        Application.logMessageReceived += HandleLog;
    }

    private static void Detach()
    {
        EditorApplication.update -= Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= HandleLog;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(StageKey, 1);
            s_stageStartTime = EditorApplication.timeSinceStartup;
        }
        else if (state == PlayModeStateChange.EnteredEditMode && s_finished)
        {
            SessionState.SetInt(ActiveKey, 0);
            SessionState.SetInt(StageKey, 0);
            Detach();
            EditorApplication.Exit(s_result == "completed" ? 0 : 1);
        }
    }

    private static void Update()
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
            return;

        int stage = SessionState.GetInt(StageKey, 0);
        double now = EditorApplication.timeSinceStartup;
        if (stage == 1)
        {
            if (TryClickDeploy())
            {
                SessionState.SetInt(StageKey, 2);
                s_stageStartTime = now;
                return;
            }

            if (now - s_stageStartTime > StartupTimeoutSeconds)
                Finish("timeout_waiting_for_menu", "DeployCommandButton/MenuView game-start path was not available.");
        }
        else if (stage == 2)
        {
            if (now - s_stageStartTime > MatchTimeoutSeconds)
            {
                Finish("timeout_waiting_for_match", "Match scene, map surface, or soldier entities did not become available.");
                return;
            }

            if (now - s_stageStartTime < WarmupSeconds)
                return;

            if (!CanCapture(out string reason))
                return;

            try
            {
                CaptureProof();
            }
            catch (Exception ex)
            {
                Finish("exception", ex.ToString());
            }
        }
        else if (stage == 3)
        {
            if (now - s_stageStartTime < 0.5d)
                return;

            if (File.Exists(ScreenshotPath))
                File.Delete(ScreenshotPath);

            ScreenCapture.CaptureScreenshot(ScreenshotPath);
            SessionState.SetInt(StageKey, 4);
            s_stageStartTime = now;
        }
        else if (stage == 4)
        {
            if (File.Exists(ScreenshotPath))
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world != null && world.IsCreated)
                    WriteReport("completed", world.EntityManager, s_pendingSoldier, s_pendingSurface, s_pendingData);

                Finish("completed", "Screenshot and grounding report captured.");
                return;
            }

            if (now - s_stageStartTime > 10d)
            {
                Finish("screenshot_timeout", $"ScreenCapture did not produce {ScreenshotPath}.");
            }
        }
    }

    private static bool TryClickDeploy()
    {
        Scene menuScene = SceneManager.GetSceneByName("Menu");
        if (!menuScene.IsValid() || !menuScene.isLoaded)
            return false;

        foreach (GameObject root in menuScene.GetRootGameObjects())
        {
            WarlineCaptureShellRouteButtonView routeButton = FindComponentInTree<WarlineCaptureShellRouteButtonView>(root.transform, IsDeployCommandButton);
            if (routeButton == null)
                continue;

            routeButton.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
            s_clickedDeploy = true;
            return true;
        }

        MenuView menu = FindMenuView(menuScene);
        if (menu == null)
            return false;

        if (menu.buttonGame != null)
            menu.buttonGame.onClick.Invoke();
        else
            menu.RequestGameStart();

        s_clickedDeploy = true;
        return true;
    }

    private static bool CanCapture(out string reason)
    {
        reason = string.Empty;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            reason = "world_missing";
            return false;
        }

        EntityManager em = world.EntityManager;
        using EntityQuery surfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        if (surfaceQuery.IsEmptyIgnoreFilter)
        {
            reason = "surface_missing";
            return false;
        }

        MapSurfaceComponent surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
        if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated)
        {
            reason = "surface_blob_missing";
            return false;
        }

        using EntityQuery unitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<UnitSurfaceComponent>());
        if (unitQuery.IsEmptyIgnoreFilter)
        {
            reason = "soldiers_missing";
            return false;
        }

        using NativeArray<Entity> entities = unitQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (IsGroundSoldier(em, entity) &&
                em.GetComponentData<UnitSurfaceComponent>(entity).HasSurface != 0)
            {
                return true;
            }
        }

        reason = "grounded_soldiers_missing";
        return false;
    }

    private static void CaptureProof()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        EntityManager em = world.EntityManager;
        using EntityQuery surfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        MapSurfaceComponent surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
        if (!TryChooseSoldier(em, surface, out Entity soldier, out SoldierProbeData data))
        {
            Finish("soldier_not_found", "No grounded character soldier could be sampled.");
            return;
        }

        Vector3 soldierPosition = new(data.Position.x, data.Position.y, data.Position.z);
        Vector3 groundPosition = new(data.Position.x, data.ExpectedGroundY, data.Position.z);
        CleanupProofObjects();
        s_proofRoot = new GameObject("RuntimeGroundingProof");
        Camera camera = ResolveGameplayCamera();
        if (camera == null)
            camera = CreateProofCamera(s_proofRoot.transform);

        AimProofCamera(camera, soldierPosition, groundPosition);
        s_pendingData = data;
        s_pendingSurface = surface;
        s_pendingSoldier = soldier;
        if (Application.isBatchMode)
        {
            WriteReport("completed", em, soldier, surface, data);
            Finish("completed", "Runtime grounding data report captured.");
            return;
        }

        SessionState.SetInt(StageKey, 3);
        s_stageStartTime = EditorApplication.timeSinceStartup;
    }

    private static bool TryChooseSoldier(EntityManager em, MapSurfaceComponent surface, out Entity soldier, out SoldierProbeData data)
    {
        soldier = Entity.Null;
        data = default;
        using EntityQuery unitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<UnitSurfaceComponent>());
        using NativeArray<Entity> entities = unitQuery.ToEntityArray(Allocator.Temp);

        float bestScore = float.NegativeInfinity;
        float bestUncoveredScore = float.NegativeInfinity;
        Entity bestUncoveredSoldier = Entity.Null;
        SoldierProbeData bestUncoveredData = default;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!IsGroundSoldier(em, entity))
                continue;

            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            UnitSurfaceComponent unitSurface = em.GetComponentData<UnitSurfaceComponent>(entity);
            if (unitSurface.HasSurface == 0)
                continue;
            bool coveredByNonGround = IsCoveredByNonGroundRenderer(transform.Position) ||
                IsOccludedFromProofCamera(transform.Position);

            float offset = em.HasComponent<UnitGroundOffsetComponent>(entity)
                ? em.GetComponentData<UnitGroundOffsetComponent>(entity).Value
                : 0f;
            float expectedY = unitSurface.LastSampledHeight + offset;
            bool hasSceneOverlay = TryResolveSceneOverlay(em, transform.Position, out MapSurfaceSceneOverlay sceneOverlay, out int sceneOverlayCount);
            bool hasOverlay = TryResolveRuntimeOverlay(em, transform.Position, out BuildingRuntimeSurfaceOverlay overlay, out int overlayCount);
            float edgeScore = EstimateNearbyHeightRange(surface, transform.Position);
            float heightScore = math.abs(unitSurface.LastSampledHeight) * 0.1f;
            float visualScore = HasVisualReference(em, entity) ? 1000f : 0f;
            float runwayDistance = TryFindNearestRunway(transform.Position, out string runwayName, out Bounds runwayBounds)
                ? DistanceToBoundsXZ(transform.Position, runwayBounds)
                : float.PositiveInfinity;
            float runwayScore = math.isfinite(runwayDistance) ? math.max(0f, 10000f - runwayDistance * 100f) : 0f;
            float sceneOverlayScore = hasSceneOverlay ? 200000f + math.max(0f, sceneOverlay.Height - unitSurface.LastSampledHeight) * 1000f : 0f;
            float overlayScore = hasOverlay ? 100000f + math.max(0f, overlay.Height - unitSurface.LastSampledHeight) * 1000f : 0f;
            float score = sceneOverlayScore + overlayScore + runwayScore + visualScore + edgeScore + heightScore;
            SoldierProbeData candidateData = new SoldierProbeData
            {
                Position = transform.Position,
                ExpectedGroundY = expectedY,
                SampledHeight = unitSurface.LastSampledHeight,
                GroundOffset = offset,
                YError = transform.Position.y - expectedY,
                SurfaceId = unitSurface.SurfaceId,
                LayerId = unitSurface.LayerId,
                SourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString(),
                NearbyHeightRange = edgeScore,
                HasVisualReference = visualScore > 0f,
                NearestRunwayName = runwayName ?? string.Empty,
                NearestRunwayDistance = runwayDistance,
                NearestRunwayMinY = runwayName != null ? runwayBounds.min.y : 0f,
                NearestRunwayMaxY = runwayName != null ? runwayBounds.max.y : 0f,
                RuntimeOverlayCount = overlayCount,
                HasRuntimeOverlay = hasOverlay,
                RuntimeOverlayHeight = hasOverlay ? overlay.Height : 0f,
                RuntimeOverlayHeightDelta = hasOverlay ? overlay.Height - unitSurface.LastSampledHeight : 0f,
                SceneOverlayCount = sceneOverlayCount,
                HasSceneOverlay = hasSceneOverlay,
                SceneOverlayHeight = hasSceneOverlay ? sceneOverlay.Height : 0f,
                SceneOverlayHeightDelta = hasSceneOverlay ? sceneOverlay.Height - unitSurface.LastSampledHeight : 0f
            };
            if (!coveredByNonGround && score > bestUncoveredScore)
            {
                bestUncoveredScore = score;
                bestUncoveredSoldier = entity;
                bestUncoveredData = candidateData;
            }
            if (score <= bestScore)
                continue;

            bestScore = score;
            soldier = entity;
            data = candidateData;
        }

        if (bestUncoveredSoldier != Entity.Null)
        {
            soldier = bestUncoveredSoldier;
            data = bestUncoveredData;
            return true;
        }

        return soldier != Entity.Null;
    }

    private static bool IsCoveredByNonGroundRenderer(float3 position)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (IsCoveredByNonGroundRendererRecursive(roots[i].transform, position))
                    return true;
            }
        }

        return false;
    }

    private static bool IsOccludedFromProofCamera(float3 position)
    {
        Camera camera = ResolveGameplayCamera();
        if (camera == null)
            return false;

        Vector3 soldierPosition = new(position.x, position.y, position.z);
        Vector3 target = soldierPosition + Vector3.up * 0.25f;
        Quaternion preservedRotation = camera.transform.rotation;
        Vector3 forward = preservedRotation * Vector3.forward;
        Vector3 cameraPosition = target - forward * 28f;
        Vector3 toTarget = target - cameraPosition;
        float maxDistance = Mathf.Max(0f, toTarget.magnitude - 0.5f);
        if (maxDistance <= 0.01f)
            return false;

        Ray ray = new(cameraPosition, toTarget.normalized);
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (IsOccludedByNonGroundRendererRecursive(roots[i].transform, ray, maxDistance, soldierPosition))
                    return true;
            }
        }

        return false;
    }

    private static bool IsOccludedByNonGroundRendererRecursive(Transform transform, Ray ray, float maxDistance, Vector3 target)
    {
        if (transform == null)
            return false;

        Renderer renderer = transform.GetComponent<Renderer>();
        if (renderer != null && IsNonGroundOccluder(renderer, ray, maxDistance, target))
            return true;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (IsOccludedByNonGroundRendererRecursive(transform.GetChild(i), ray, maxDistance, target))
                return true;
        }

        return false;
    }

    private static bool IsNonGroundOccluder(Renderer renderer, Ray ray, float maxDistance, Vector3 target)
    {
        Bounds bounds = renderer.bounds;
        if (bounds.Contains(target))
            return false;
        if (!bounds.IntersectRay(ray, out float distance) || distance > maxDistance)
            return false;

        string name = renderer.transform.name;
        if (name.IndexOf("Runway", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Road", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        return bounds.size.y > 0.5f;
    }

    private static bool IsCoveredByNonGroundRendererRecursive(Transform transform, float3 position)
    {
        if (transform == null)
            return false;

        Renderer renderer = transform.GetComponent<Renderer>();
        if (renderer != null && IsNonGroundCoverRenderer(renderer, position))
            return true;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (IsCoveredByNonGroundRendererRecursive(transform.GetChild(i), position))
                return true;
        }

        return false;
    }

    private static bool IsNonGroundCoverRenderer(Renderer renderer, float3 position)
    {
        Bounds bounds = renderer.bounds;
        if (position.x < bounds.min.x || position.x > bounds.max.x ||
            position.z < bounds.min.z || position.z > bounds.max.z ||
            bounds.max.y < position.y + 1.2f)
        {
            return false;
        }

        string name = renderer.transform.name;
        if (name.IndexOf("Runway", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Road", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        return true;
    }

    private static bool TryResolveSceneOverlay(
        EntityManager em,
        float3 position,
        out MapSurfaceSceneOverlay overlay,
        out int overlayCount)
    {
        overlay = default;
        overlayCount = 0;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<MapSurfaceComponent>(),
            ComponentType.ReadOnly<MapSurfaceSceneOverlay>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity surfaceEntity = query.GetSingletonEntity();
        if (!em.HasBuffer<MapSurfaceSceneOverlay>(surfaceEntity))
            return false;

        DynamicBuffer<MapSurfaceSceneOverlay> overlays = em.GetBuffer<MapSurfaceSceneOverlay>(surfaceEntity, true);
        overlayCount = overlays.Length;
        bool found = false;
        float bestHeight = float.NegativeInfinity;
        for (int i = 0; i < overlays.Length; i++)
        {
            MapSurfaceSceneOverlay candidate = overlays[i];
            if (!ContainsOverlay(candidate, position))
                continue;
            if (found && candidate.Height <= bestHeight)
                continue;

            overlay = candidate;
            bestHeight = candidate.Height;
            found = true;
        }

        return found;
    }

    private static bool TryResolveRuntimeOverlay(
        EntityManager em,
        float3 position,
        out BuildingRuntimeSurfaceOverlay overlay,
        out int overlayCount)
    {
        overlay = default;
        overlayCount = 0;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingRuntimeSurfaceOverlay>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity boundaryEntity = query.GetSingletonEntity();
        if (!em.HasBuffer<BuildingRuntimeSurfaceOverlay>(boundaryEntity))
            return false;

        DynamicBuffer<BuildingRuntimeSurfaceOverlay> overlays = em.GetBuffer<BuildingRuntimeSurfaceOverlay>(boundaryEntity, true);
        overlayCount = overlays.Length;
        bool found = false;
        float bestHeight = float.NegativeInfinity;
        for (int i = 0; i < overlays.Length; i++)
        {
            BuildingRuntimeSurfaceOverlay candidate = overlays[i];
            if (!ContainsOverlay(candidate, position))
                continue;
            if (found && candidate.Height <= bestHeight)
                continue;

            overlay = candidate;
            bestHeight = candidate.Height;
            found = true;
        }

        return found;
    }

    private static bool ContainsOverlay(BuildingRuntimeSurfaceOverlay overlay, float3 position)
    {
        quaternion inverseRotation = math.inverse(overlay.Rotation);
        float3 local = math.mul(inverseRotation, position - overlay.Center);
        return math.abs(local.x) <= overlay.HalfExtents.x &&
               math.abs(local.z) <= overlay.HalfExtents.y;
    }

    private static bool ContainsOverlay(MapSurfaceSceneOverlay overlay, float3 position)
    {
        quaternion inverseRotation = math.inverse(overlay.Rotation);
        float3 local = math.mul(inverseRotation, position - overlay.Center);
        return math.abs(local.x) <= overlay.HalfExtents.x &&
               math.abs(local.z) <= overlay.HalfExtents.y;
    }

    private static bool TryFindNearestRunway(float3 position, out string runwayName, out Bounds runwayBounds)
    {
        runwayName = null;
        runwayBounds = default;
        bool found = false;
        float bestDistance = float.PositiveInfinity;
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                FindNearestRunwayRecursive(roots[i].transform, position, ref found, ref bestDistance, ref runwayName, ref runwayBounds);
        }

        return found;
    }

    private static void FindNearestRunwayRecursive(
        Transform transform,
        float3 position,
        ref bool found,
        ref float bestDistance,
        ref string runwayName,
        ref Bounds runwayBounds)
    {
        if (transform == null)
            return;

        if (transform.name.IndexOf("Runway", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Renderer renderer = transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                float distance = DistanceToBoundsXZ(position, bounds);
                if (distance < bestDistance)
                {
                    found = true;
                    bestDistance = distance;
                    runwayName = transform.name;
                    runwayBounds = bounds;
                }
            }
        }

        for (int i = 0; i < transform.childCount; i++)
            FindNearestRunwayRecursive(transform.GetChild(i), position, ref found, ref bestDistance, ref runwayName, ref runwayBounds);
    }

    private static float DistanceToBoundsXZ(float3 position, Bounds bounds)
    {
        float dx = math.max(math.max(bounds.min.x - position.x, 0f), position.x - bounds.max.x);
        float dz = math.max(math.max(bounds.min.z - position.z, 0f), position.z - bounds.max.z);
        return math.sqrt(dx * dx + dz * dz);
    }

    private static bool HasVisualReference(EntityManager em, Entity entity)
    {
        return
            em.HasComponent<UnitModelInstanceReference>(entity) ||
            em.HasComponent<UnitDetailedVisualReference>(entity) ||
            em.HasComponent<UnitMidLodInstanceReference>(entity) ||
            em.HasComponent<UnitLowLodInstanceReference>(entity);
    }

    private static float EstimateNearbyHeightRange(MapSurfaceComponent surface, float3 worldPosition)
    {
        if (!surface.SurfaceBlob.IsCreated || surface.CellSize <= 0f)
            return 0f;

        int2 center = (int2)math.floor(new float2(
            (worldPosition.x - surface.GridOrigin.x) / surface.CellSize,
            (worldPosition.z - surface.GridOrigin.z) / surface.CellSize));
        float minHeight = float.PositiveInfinity;
        float maxHeight = float.NegativeInfinity;
        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        for (int y = -2; y <= 2; y++)
        {
            for (int x = -2; x <= 2; x++)
            {
                int2 cell = center + new int2(x, y);
                if ((uint)cell.x >= (uint)surface.Dimensions.x ||
                    (uint)cell.y >= (uint)surface.Dimensions.y)
                {
                    continue;
                }

                int index = cell.x + cell.y * surface.Dimensions.x;
                MapSurfaceCell surfaceCell = blob.Cells[index];
                if (surfaceCell.SurfaceCount == 0)
                    continue;

                MapSurfaceSample sample = blob.Samples[surfaceCell.FirstSurfaceIndex];
                minHeight = math.min(minHeight, sample.Height);
                maxHeight = math.max(maxHeight, sample.Height);
            }
        }

        return math.isfinite(minHeight) && math.isfinite(maxHeight)
            ? maxHeight - minHeight
            : 0f;
    }

    private static bool IsGroundSoldier(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) ||
            em.HasComponent<UnitAirMovement>(entity) ||
            !em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            return false;
        }

        string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
        return sourceKey.StartsWith("Unit_Chr_", StringComparison.Ordinal);
    }

    private static GameObject CreateGroundRing(Transform parent, Vector3 position)
    {
        GameObject ring = new("SampledGroundHeightRing");
        ring.transform.SetParent(parent, false);
        ring.transform.position = position + Vector3.up * 0.035f;
        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = 0.035f;
        line.material = new Material(Shader.Find("Sprites/Default"))
        {
            color = new Color(0f, 1f, 0.1f, 0.95f)
        };
        line.positionCount = 48;
        const float radius = 0.55f;
        for (int i = 0; i < line.positionCount; i++)
        {
            float a = i / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }

        return ring;
    }

    private static Camera CreateProofCamera(Transform parent)
    {
        GameObject cameraObject = new("ProofCamera");
        cameraObject.transform.SetParent(parent, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 34f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 5000f;
        camera.depth = 500f;
        camera.cullingMask = ~0;
        return camera;
    }

    private static void AimProofCamera(Camera camera, Vector3 soldierPosition, Vector3 groundPosition)
    {
        if (camera == null)
            return;

        Vector3 target = Vector3.Lerp(soldierPosition, groundPosition, 0.35f) + Vector3.up * 0.25f;
        Quaternion preservedRotation = camera.transform.rotation;
        Vector3 forward = preservedRotation * Vector3.forward;
        float distance = 28f;
        camera.transform.SetPositionAndRotation(target - forward * distance, preservedRotation);
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 5000f;
    }

    private static Camera ResolveGameplayCamera()
    {
        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
            return main;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Camera camera = FindComponentInTree<Camera>(roots[r].transform, static candidate => candidate != null && candidate.isActiveAndEnabled);
                if (camera != null)
                    return camera;
            }
        }

        return null;
    }

    private static void RenderCamera(Camera camera, string path, UnitPrefabRegistryAuthoringConfig registryConfig)
    {
        RenderTexture target = new(ScreenshotWidth, ScreenshotHeight, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4
        };
        RenderTexture previous = RenderTexture.active;
        RenderTexture previousCameraTarget = camera.targetTexture;
        Texture2D texture = new(ScreenshotWidth, ScreenshotHeight, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = target;
            RenderTexture.active = target;
            using (UnitImpostorRenderSystem impostors = new())
            {
                impostors.Init(camera, camera.gameObject.layer, registryConfig);
                impostors.LateUpdate();
            }
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, ScreenshotWidth, ScreenshotHeight), 0, 0);
            texture.Apply(false);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousCameraTarget;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static UnitPrefabRegistryAuthoringConfig ResolveUnitPrefabRegistryConfig()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                MatchSceneView view = FindComponentInTree<MatchSceneView>(roots[r].transform, static candidate => candidate != null);
                if (view != null && view.BuildingPlacementConfig != null)
                    return view.BuildingPlacementConfig.UnitPrefabRegistryConfig;
            }
        }

        return null;
    }

    private static void WriteReport(string result, EntityManager em, Entity soldier, MapSurfaceComponent surface, SoldierProbeData data)
    {
        StringBuilder json = new();
        json.AppendLine("{");
        AppendJson(json, "result", result, comma: true);
        AppendJson(json, "clickedDeploy", s_clickedDeploy, comma: true);
        AppendJson(json, "screenshot", ScreenshotPath, comma: true);
        AppendJson(json, "sourceKey", data.SourceKey, comma: true);
        AppendJson(json, "hasVisualReference", data.HasVisualReference, comma: true);
        AppendJson(json, "entity", soldier.ToString(), comma: true);
        AppendJson(json, "position", Format(data.Position), comma: true);
        AppendJson(json, "sampledHeight", data.SampledHeight, comma: true);
        AppendJson(json, "groundOffset", data.GroundOffset, comma: true);
        AppendJson(json, "expectedGroundY", data.ExpectedGroundY, comma: true);
        AppendJson(json, "entityYMinusExpectedGroundY", data.YError, comma: true);
        AppendJson(json, "surfaceId", data.SurfaceId, comma: true);
        AppendJson(json, "layerId", data.LayerId, comma: true);
        AppendJson(json, "nearbyHeightRange", data.NearbyHeightRange, comma: true);
        AppendJson(json, "nearestRunwayName", data.NearestRunwayName, comma: true);
        AppendJson(json, "nearestRunwayDistance", data.NearestRunwayDistance, comma: true);
        AppendJson(json, "nearestRunwayMinY", data.NearestRunwayMinY, comma: true);
        AppendJson(json, "nearestRunwayMaxY", data.NearestRunwayMaxY, comma: true);
        AppendJson(json, "runtimeOverlayCount", data.RuntimeOverlayCount, comma: true);
        AppendJson(json, "hasRuntimeOverlay", data.HasRuntimeOverlay, comma: true);
        AppendJson(json, "runtimeOverlayHeight", data.RuntimeOverlayHeight, comma: true);
        AppendJson(json, "runtimeOverlayHeightDelta", data.RuntimeOverlayHeightDelta, comma: true);
        AppendJson(json, "sceneOverlayCount", data.SceneOverlayCount, comma: true);
        AppendJson(json, "hasSceneOverlay", data.HasSceneOverlay, comma: true);
        AppendJson(json, "sceneOverlayHeight", data.SceneOverlayHeight, comma: true);
        AppendJson(json, "sceneOverlayHeightDelta", data.SceneOverlayHeightDelta, comma: true);
        AppendSurfaceEntityJson(json, em);
        AppendSoldierCohortJson(json, em);
        AppendJson(json, "surfaceDimensions", $"{surface.Dimensions.x}x{surface.Dimensions.y}", comma: true);
        AppendJson(json, "surfaceCellSize", surface.CellSize, comma: true);
        AppendJson(json, "surfaceGridOrigin", Format(surface.GridOrigin), comma: false);
        json.AppendLine("}");
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, json.ToString());
        Debug.Log($"[RuntimeGroundingScreenshotProbe] result={result} screenshot={ScreenshotPath} report={ReportPath} source={data.SourceKey} y={data.Position.y:F3} expected={data.ExpectedGroundY:F3} error={data.YError:F4}");
    }

    private static void AppendSurfaceEntityJson(StringBuilder json, EntityManager em)
    {
        using EntityQuery surfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        using NativeArray<Entity> surfaceEntities = surfaceQuery.ToEntityArray(Allocator.Temp);
        AppendJson(json, "surfaceEntityCount", surfaceEntities.Length, comma: true);
        bool hasRuntimeTag = false;
        bool hasFlatFallbackTag = false;
        int sceneOverlayBufferLength = 0;
        for (int i = 0; i < surfaceEntities.Length; i++)
        {
            Entity entity = surfaceEntities[i];
            hasRuntimeTag |= em.HasComponent<MapSurfaceRuntimeBakedBlobTag>(entity);
            hasFlatFallbackTag |= em.HasComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(entity);
            if (em.HasBuffer<MapSurfaceSceneOverlay>(entity))
                sceneOverlayBufferLength += em.GetBuffer<MapSurfaceSceneOverlay>(entity, true).Length;
        }

        AppendJson(json, "surfaceHasRuntimeBakedTag", hasRuntimeTag, comma: true);
        AppendJson(json, "surfaceHasFlatFallbackTag", hasFlatFallbackTag, comma: true);
        AppendJson(json, "sceneOverlayBufferLength", sceneOverlayBufferLength, comma: true);
    }

    private static void AppendSoldierCohortJson(StringBuilder json, EntityManager em)
    {
        json.AppendLine("  \"soldierCohort\": [");
        using EntityQuery unitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<UnitSurfaceComponent>());
        using NativeArray<Entity> entities = unitQuery.ToEntityArray(Allocator.Temp);
        bool wroteAny = false;
        int written = 0;
        const int MaxSoldiers = 96;
        for (int i = 0; i < entities.Length && written < MaxSoldiers; i++)
        {
            Entity entity = entities[i];
            if (!IsGroundSoldier(em, entity))
                continue;

            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            UnitSurfaceComponent surface = em.GetComponentData<UnitSurfaceComponent>(entity);
            float offset = em.HasComponent<UnitGroundOffsetComponent>(entity)
                ? em.GetComponentData<UnitGroundOffsetComponent>(entity).Value
                : 0f;
            float expectedY = surface.LastSampledHeight + offset;
            bool hasSceneOverlay = TryResolveSceneOverlay(em, transform.Position, out MapSurfaceSceneOverlay sceneOverlay, out int sceneOverlayCount);
            bool hasRuntimeOverlay = TryResolveRuntimeOverlay(em, transform.Position, out BuildingRuntimeSurfaceOverlay runtimeOverlay, out int runtimeOverlayCount);
            bool hasVisualRoot = TryResolveVisualRootY(em, entity, out float visualRootY);

            if (wroteAny)
                json.AppendLine(",");
            json.Append("    {");
            json.Append("\"entity\":\"").Append(EscapeJson(entity.ToString())).Append("\",");
            json.Append("\"sourceKey\":\"").Append(EscapeJson(em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString())).Append("\",");
            json.Append("\"position\":\"").Append(Format(transform.Position)).Append("\",");
            json.Append("\"sampledHeight\":").Append(surface.LastSampledHeight.ToString("F4", CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"groundOffset\":").Append(offset.ToString("F4", CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"expectedY\":").Append(expectedY.ToString("F4", CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"entityYMinusExpected\":").Append((transform.Position.y - expectedY).ToString("F4", CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"hasVisualRoot\":").Append(hasVisualRoot ? "true" : "false").Append(",");
            json.Append("\"visualRootY\":").Append(visualRootY.ToString("F4", CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"visualRootYMinusExpected\":").Append((hasVisualRoot ? visualRootY - expectedY : 0f).ToString("F4", CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"surfaceId\":").Append(surface.SurfaceId.ToString(CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"layerId\":").Append(surface.LayerId.ToString(CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"sceneOverlayCount\":").Append(sceneOverlayCount.ToString(CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"hasSceneOverlay\":").Append(hasSceneOverlay ? "true" : "false").Append(",");
            json.Append("\"sceneOverlayHeightDelta\":").Append((hasSceneOverlay ? sceneOverlay.Height - surface.LastSampledHeight : 0f).ToString("F4", CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"runtimeOverlayCount\":").Append(runtimeOverlayCount.ToString(CultureInfo.InvariantCulture)).Append(",");
            json.Append("\"hasRuntimeOverlay\":").Append(hasRuntimeOverlay ? "true" : "false").Append(",");
            json.Append("\"runtimeOverlayHeightDelta\":").Append((hasRuntimeOverlay ? runtimeOverlay.Height - surface.LastSampledHeight : 0f).ToString("F4", CultureInfo.InvariantCulture));
            json.Append("}");
            wroteAny = true;
            written++;
        }

        json.AppendLine();
        json.AppendLine("  ],");
    }

    private static bool TryResolveVisualRootY(EntityManager em, Entity unit, out float visualRootY)
    {
        visualRootY = 0f;
        if (!em.HasComponent<UnitModelInstanceReference>(unit))
            return false;

        Entity model = em.GetComponentData<UnitModelInstanceReference>(unit).Instance;
        if (model == Entity.Null || !em.Exists(model) || !em.HasComponent<LocalToWorld>(model))
            return false;

        visualRootY = em.GetComponentData<LocalToWorld>(model).Position.y;
        return true;
    }

    private static void Finish(string result, string detail)
    {
        if (s_finished)
            return;

        s_finished = true;
        s_result = result;
        s_detail = detail;
        if (result != "completed")
            WriteFailureReport(result, detail);

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
        else
            EditorApplication.Exit(result == "completed" ? 0 : 1);
    }

    private static void CleanupProofObjects()
    {
        if (s_proofRoot != null)
            UnityEngine.Object.DestroyImmediate(s_proofRoot);

        s_proofRoot = null;
    }

    private static void WriteFailureReport(string result, string detail)
    {
        StringBuilder json = new();
        json.AppendLine("{");
        AppendJson(json, "result", result, comma: true);
        AppendJson(json, "clickedDeploy", s_clickedDeploy, comma: true);
        AppendJson(json, "detail", detail, comma: false);
        json.AppendLine("}");
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, json.ToString());
        Debug.LogError($"[RuntimeGroundingScreenshotProbe] result={result} detail={detail}");
    }

    private static bool IsDeployCommandButton(WarlineCaptureShellRouteButtonView routeButton)
    {
        return routeButton != null &&
               routeButton.name == "DeployCommandButton" &&
               routeButton.Intent == UiShellRouteIntent.EnterMatch &&
               routeButton.Route == WarlineCaptureRoute.Match;
    }

    private static MenuView FindMenuView(Scene menuScene)
    {
        foreach (GameObject root in menuScene.GetRootGameObjects())
        {
            MenuView menu = FindComponentInTree<MenuView>(root.transform, static candidate => candidate != null);
            if (menu != null)
                return menu;
        }

        return null;
    }

    private static T FindComponentInTree<T>(Transform root, Func<T, bool> predicate)
        where T : Component
    {
        if (root == null)
            return null;

        T component = root.GetComponent<T>();
        if (component != null && predicate(component))
            return component;

        for (int i = 0; i < root.childCount; i++)
        {
            T child = FindComponentInTree(root.GetChild(i), predicate);
            if (child != null)
                return child;
        }

        return null;
    }

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
            return;

        if (type == LogType.Exception || type == LogType.Error)
            s_detail += condition + "\n";
    }

    private static string Format(float3 value)
    {
        return $"{value.x.ToString("F3", CultureInfo.InvariantCulture)},{value.y.ToString("F3", CultureInfo.InvariantCulture)},{value.z.ToString("F3", CultureInfo.InvariantCulture)}";
    }

    private static void AppendJson(StringBuilder json, string name, string value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": \"").Append(EscapeJson(value)).Append(comma ? "\"," : "\"").AppendLine();
    }

    private static void AppendJson(StringBuilder json, string name, bool value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value ? "true" : "false").Append(comma ? "," : string.Empty).AppendLine();
    }

    private static void AppendJson(StringBuilder json, string name, int value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture)).Append(comma ? "," : string.Empty).AppendLine();
    }

    private static void AppendJson(StringBuilder json, string name, float value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(value.ToString("F4", CultureInfo.InvariantCulture)).Append(comma ? "," : string.Empty).AppendLine();
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal);
    }

    private struct SoldierProbeData
    {
        public float3 Position;
        public float ExpectedGroundY;
        public float SampledHeight;
        public float GroundOffset;
        public float YError;
        public int SurfaceId;
        public int LayerId;
        public string SourceKey;
        public float NearbyHeightRange;
        public bool HasVisualReference;
        public string NearestRunwayName;
        public float NearestRunwayDistance;
        public float NearestRunwayMinY;
        public float NearestRunwayMaxY;
        public int RuntimeOverlayCount;
        public bool HasRuntimeOverlay;
        public float RuntimeOverlayHeight;
        public float RuntimeOverlayHeightDelta;
        public int SceneOverlayCount;
        public bool HasSceneOverlay;
        public float SceneOverlayHeight;
        public float SceneOverlayHeightDelta;
    }
}
#endif
