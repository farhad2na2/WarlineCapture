#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Game.Scripts.UI;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VehicleSelectionMarkerProofCapture
{
    private const string ScreenshotPath = "/private/tmp/warline_vehicle_marker_proof.png";
    private const string ReportPath = "/private/tmp/warline_vehicle_marker_proof.txt";
    private const string ActiveKey = "WarlineCapture.VehicleSelectionMarkerProof.Active";
    private const string StageKey = "WarlineCapture.VehicleSelectionMarkerProof.Stage";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const double StageTimeoutSeconds = 45d;
    private static int _pendingDelayFrames;
    private static double s_stageStartTime;
    private static Entity s_autoSelectedVehicle;

    static VehicleSelectionMarkerProofCapture()
    {
        if (SessionState.GetInt(ActiveKey, 0) == 1)
            Attach();
    }

    [MenuItem("WarlineCapture/Run Vehicle Selection Marker Proof")]
    public static void RunProof()
    {
        SessionState.SetInt(ActiveKey, 1);
        SessionState.SetInt(StageKey, 0);
        s_stageStartTime = EditorApplication.timeSinceStartup;
        s_autoSelectedVehicle = Entity.Null;
        Attach();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("WarlineCapture/Capture Vehicle Selection Marker Proof")]
    public static void Capture()
    {
        Capture(autoSelectIfNeeded: true);
    }

    private static void Capture(bool autoSelectIfNeeded)
    {
        var report = new StringBuilder();
        report.AppendLine("Vehicle Selection Marker Proof");

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            WriteReport(report, "No default ECS world exists. Enter Play Mode and select a vehicle first.");
            return;
        }

        EntityManager em = world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitMovementBehavior>(),
            ComponentType.ReadOnly<UnitHealth>());
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        int totalVehicles = 0;
        int playerVehicles = 0;
        int selectedVehicles = 0;
        int selectedWithPrefabRef = 0;
        int selectedWithMarkerInstance = 0;
        Entity firstSelectableVehicle = Entity.Null;
        Entity firstSelectedVehicle = Entity.Null;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            UnitMovementBehavior movement = em.GetComponentData<UnitMovementBehavior>(entity);
            if (movement.UsesVehicleMotion == 0 ||
                em.HasComponent<Prefab>(entity) ||
                em.HasComponent<StaticGridBlocker>(entity))
                continue;

            totalVehicles++;
            bool playerControlled = em.HasComponent<Faction>(entity) &&
                                    FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id);
            if (playerControlled)
            {
                playerVehicles++;
                if (firstSelectableVehicle == Entity.Null && em.GetComponentData<UnitHealth>(entity).Current > 0)
                    firstSelectableVehicle = entity;
            }

            if (!em.HasComponent<SelectedUnitTag>(entity))
                continue;

            selectedVehicles++;
            if (firstSelectedVehicle == Entity.Null)
                firstSelectedVehicle = entity;

            bool hasPrefabRef = em.HasComponent<VehicleSelectionMarkerPrefabReference>(entity);
            bool hasMarkerRef = em.HasComponent<VehicleSelectionMarkerInstanceReference>(entity);
            if (hasPrefabRef)
                selectedWithPrefabRef++;
            if (hasMarkerRef)
                selectedWithMarkerInstance++;

            report.Append("selectedVehicle=");
            report.Append(entity);
            report.Append(" health=");
            report.Append(em.GetComponentData<UnitHealth>(entity).Current);
            if (em.HasComponent<UnitSourcePrefabKey>(entity))
            {
                report.Append(" sourceKey=");
                report.Append(em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString());
            }

            report.Append(" hasPrefabRef=");
            report.Append(hasPrefabRef);
            report.Append(" hasMarkerRef=");
            report.Append(hasMarkerRef);

            if (hasMarkerRef)
            {
                Entity marker = em.GetComponentData<VehicleSelectionMarkerInstanceReference>(entity).Instance;
                report.Append(" marker=");
                report.Append(marker);
                report.Append(" markerExists=");
                report.Append(em.Exists(marker));
                if (em.Exists(marker))
                {
                    report.Append(" markerName=");
                    report.Append(em.GetName(marker));
                    if (em.HasComponent<LocalTransform>(marker))
                    {
                        LocalTransform markerTransform = em.GetComponentData<LocalTransform>(marker);
                        report.Append(" markerLocalPosition=");
                        report.Append(markerTransform.Position);
                        report.Append(" markerScale=");
                        report.Append(markerTransform.Scale);
                    }

                    if (em.HasComponent<SelectionMarkerVisualChild>(marker))
                    {
                        SelectionMarkerVisualChild child = em.GetComponentData<SelectionMarkerVisualChild>(marker);
                        report.Append(" visualChild=");
                        report.Append(child.Value);
                        report.Append(" visibleScale=");
                        report.Append(child.VisibleScale);
                        report.Append(" visualExists=");
                        report.Append(em.Exists(child.Value));
                        if (em.Exists(child.Value) && em.HasComponent<LocalTransform>(child.Value))
                        {
                            LocalTransform visualTransform = em.GetComponentData<LocalTransform>(child.Value);
                            report.Append(" visualLocalPosition=");
                            report.Append(visualTransform.Position);
                            report.Append(" visualScale=");
                            report.Append(visualTransform.Scale);
                        }
                    }
                }
            }

            report.AppendLine();
        }

        if (autoSelectIfNeeded && selectedVehicles == 0 && firstSelectableVehicle != Entity.Null)
        {
            em.AddComponent<SelectedUnitTag>(firstSelectableVehicle);
            report.AppendLine($"autoSelectedVehicle={firstSelectableVehicle}");
            File.WriteAllText(ReportPath, report.ToString());
            ScheduleDelayedCapture();
            Debug.Log($"[VehicleSelectionMarkerProof] auto-selected {firstSelectableVehicle}; waiting for marker system.");
            return;
        }

        report.AppendLine($"totalVehicles={totalVehicles}");
        report.AppendLine($"playerVehicles={playerVehicles}");
        report.AppendLine($"selectedVehicles={selectedVehicles}");
        report.AppendLine($"selectedWithPrefabRef={selectedWithPrefabRef}");
        report.AppendLine($"selectedWithMarkerInstance={selectedWithMarkerInstance}");
        AppendVehiclePrefabReferenceDiagnostics(em, report);
        FocusGameCameraOnEntity(em, firstSelectedVehicle, report);

        ScreenCapture.CaptureScreenshot(ScreenshotPath);
        report.AppendLine($"screenshot={ScreenshotPath}");
        WriteReport(report, $"Wrote {ReportPath} and requested screenshot {ScreenshotPath}");
    }

    private static void AppendVehiclePrefabReferenceDiagnostics(EntityManager em, StringBuilder report)
    {
        using EntityQuery prefabQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Prefab>(),
            ComponentType.ReadOnly<UnitMovementBehavior>());
        using var prefabs = prefabQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

        int vehiclePrefabs = 0;
        int vehiclePrefabsWithSourceKey = 0;
        int vehiclePrefabsWithMarkerRef = 0;
        int vehiclePrefabsWithHealthRef = 0;
        int vehiclePrefabsWithDestroyedRef = 0;
        for (int i = 0; i < prefabs.Length; i++)
        {
            Entity prefab = prefabs[i];
            if (em.GetComponentData<UnitMovementBehavior>(prefab).UsesVehicleMotion == 0)
                continue;

            vehiclePrefabs++;
            if (em.HasComponent<UnitSourcePrefabKey>(prefab))
                vehiclePrefabsWithSourceKey++;
            if (em.HasComponent<VehicleSelectionMarkerPrefabReference>(prefab))
                vehiclePrefabsWithMarkerRef++;
            if (em.HasComponent<VehicleHealthBarPrefabReference>(prefab))
                vehiclePrefabsWithHealthRef++;
            if (em.HasComponent<VehicleDestroyedVisualPrefabReference>(prefab))
                vehiclePrefabsWithDestroyedRef++;
        }

        report.AppendLine($"vehiclePrefabs={vehiclePrefabs}");
        report.AppendLine($"vehiclePrefabsWithSourceKey={vehiclePrefabsWithSourceKey}");
        report.AppendLine($"vehiclePrefabsWithMarkerRef={vehiclePrefabsWithMarkerRef}");
        report.AppendLine($"vehiclePrefabsWithHealthRef={vehiclePrefabsWithHealthRef}");
        report.AppendLine($"vehiclePrefabsWithDestroyedRef={vehiclePrefabsWithDestroyedRef}");
    }

    private static void FocusGameCameraOnEntity(EntityManager em, Entity entity, StringBuilder report)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return;

        Camera camera = ResolveActiveGameCamera();
        if (camera == null)
        {
            report.AppendLine("proofCameraFocus=missing_camera");
            return;
        }

        if (!TryResolveEntityWorldPosition(em, entity, out float3 worldPosition))
        {
            report.AppendLine("proofCameraFocus=missing_entity_transform");
            return;
        }

        Vector3 target = new(worldPosition.x, worldPosition.y, worldPosition.z);
        Vector3 forward = camera.transform.forward;
        if (forward.sqrMagnitude < 0.0001f)
        {
            report.AppendLine("proofCameraFocus=invalid_camera_forward");
            return;
        }

        float distance = 30f;
        if (em.HasComponent<UnitFootprint>(entity))
        {
            int2 footprint = em.GetComponentData<UnitFootprint>(entity).Size;
            distance = Mathf.Clamp(Mathf.Max(footprint.x, footprint.y) * 4f, 24f, 44f);
        }

        camera.transform.position = target - forward.normalized * distance;
        report.AppendLine($"proofCameraFocus=entity target={target} distance={distance:F1}");
    }

    private static bool TryResolveEntityWorldPosition(EntityManager em, Entity entity, out float3 worldPosition)
    {
        if (em.HasComponent<LocalToWorld>(entity))
        {
            worldPosition = em.GetComponentData<LocalToWorld>(entity).Position;
            return true;
        }

        if (em.HasComponent<LocalTransform>(entity))
        {
            worldPosition = em.GetComponentData<LocalTransform>(entity).Position;
            return true;
        }

        worldPosition = default;
        return false;
    }

    private static Camera ResolveActiveGameCamera()
    {
        Camera main = Camera.main;
        if (main != null && main.enabled)
            return main;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Camera camera = FindComponentInTree<Camera>(root.transform, c => c != null && c.enabled);
                if (camera != null)
                    return camera;
            }
        }

        return null;
    }

    private static void Attach()
    {
        EditorApplication.update -= UpdateProof;
        EditorApplication.update += UpdateProof;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void Detach()
    {
        EditorApplication.update -= UpdateProof;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
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
    }

    private static void UpdateProof()
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

            if (now - s_stageStartTime > StageTimeoutSeconds)
                FinishProof("timeout_waiting_for_deploy_button");
        }
        else if (stage == 2)
        {
            if (TryAutoSelectFirstPlayerVehicle(out s_autoSelectedVehicle))
            {
                SessionState.SetInt(StageKey, 3);
                s_stageStartTime = now;
                return;
            }

            if (now - s_stageStartTime > StageTimeoutSeconds)
                FinishProof("timeout_waiting_for_player_vehicle");
        }
        else if (stage == 3)
        {
            if (HasMarkerInstance(s_autoSelectedVehicle))
            {
                Capture(autoSelectIfNeeded: false);
                SessionState.SetInt(StageKey, 4);
                s_stageStartTime = now;
                return;
            }

            if (now - s_stageStartTime > StageTimeoutSeconds)
            {
                Capture(autoSelectIfNeeded: false);
                FinishProof("timeout_waiting_for_marker_instance");
            }
        }
        else if (stage == 4)
        {
            if (File.Exists(ScreenshotPath))
                FinishProof("completed");
            else if (now - s_stageStartTime > 10d)
                FinishProof("screenshot_timeout");
        }
    }

    private static bool TryAutoSelectFirstPlayerVehicle(out Entity selectedVehicle)
    {
        selectedVehicle = Entity.Null;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitMovementBehavior>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<Faction>());
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.HasComponent<Prefab>(entity) ||
                em.HasComponent<StaticGridBlocker>(entity) ||
                em.HasComponent<UnitAirMovement>(entity) ||
                em.GetComponentData<UnitMovementBehavior>(entity).UsesVehicleMotion == 0 ||
                em.GetComponentData<UnitHealth>(entity).Current <= 0 ||
                !FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id))
            {
                continue;
            }

            if (!em.HasComponent<SelectedUnitTag>(entity))
                em.AddComponent<SelectedUnitTag>(entity);

            selectedVehicle = entity;
            return true;
        }

        return false;
    }

    private static bool HasMarkerInstance(Entity vehicle)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated || vehicle == Entity.Null)
            return false;

        EntityManager em = world.EntityManager;
        return em.Exists(vehicle) &&
               em.HasComponent<VehicleSelectionMarkerInstanceReference>(vehicle) &&
               em.Exists(em.GetComponentData<VehicleSelectionMarkerInstanceReference>(vehicle).Instance);
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
            return true;
        }

        MenuView menu = FindComponentInScene<MenuView>(menuScene);
        if (menu == null)
            return false;

        if (menu.buttonGame != null)
            menu.buttonGame.onClick.Invoke();
        else
            menu.RequestGameStart();

        return true;
    }

    private static bool IsDeployCommandButton(WarlineCaptureShellRouteButtonView routeButton)
    {
        return routeButton != null &&
               routeButton.name == "DeployCommandButton" &&
               routeButton.Intent == UiShellRouteIntent.EnterMatch &&
               routeButton.Route == WarlineCaptureRoute.Match;
    }

    private static T FindComponentInScene<T>(Scene scene)
        where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = FindComponentInTree<T>(root.transform, static candidate => candidate != null);
            if (component != null)
                return component;
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

    private static void FinishProof(string result)
    {
        File.AppendAllText(ReportPath, $"{Environment.NewLine}proofResult={result}{Environment.NewLine}");
        SessionState.SetInt(ActiveKey, 0);
        SessionState.SetInt(StageKey, 0);
        Detach();
        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
    }

    private static void ScheduleDelayedCapture()
    {
        _pendingDelayFrames = 8;
        EditorApplication.update -= DelayedCaptureUpdate;
        EditorApplication.update += DelayedCaptureUpdate;
    }

    private static void DelayedCaptureUpdate()
    {
        if (_pendingDelayFrames-- > 0)
            return;

        EditorApplication.update -= DelayedCaptureUpdate;
        Capture(autoSelectIfNeeded: false);
    }

    private static void WriteReport(StringBuilder report, string message)
    {
        report.AppendLine(message);
        File.WriteAllText(ReportPath, report.ToString());
        Debug.Log($"[VehicleSelectionMarkerProof] {message}");
    }
}
#endif
