#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Game.Scripts.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CharacterVisualAdornmentsProofCapture
{
    private const string ActiveKey = "WarlineCapture.CharacterVisualAdornmentsProof.Active";
    private const string StageKey = "WarlineCapture.CharacterVisualAdornmentsProof.Stage";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string InitialUnitsConfigPath = "Assets/Game/Configs/Scene/MatchSubScene_InitialUnitsSpawner_Config.asset";
    private const string MainMatchSubScenePath = "Assets/Game/Scenes/Match/MatchSubScene.unity";
    private const string ShadowMatchSubScenePath = "Assets/Game/Scenes/Match/Match/MatchSubScene.unity";
    private const string ReportPath = "/private/tmp/warline_soldier_click_selection_proof.txt";
    private const string ScreenshotPath = "/private/tmp/warline_soldier_click_selection_proof_close.png";
    private const double StartupTimeoutSeconds = 45d;
    private const double MatchTimeoutSeconds = 75d;
    private const double WarmupSeconds = 12d;
    private const double VisualTimeoutSeconds = 15d;
    private const double VisualSettleSeconds = 1.5d;
    private const int RequiredCharacters = 1;
    private const int RequiredVehicles = 0;

    private static readonly Entity[] SelectedCharacters = new Entity[RequiredCharacters];
    private static readonly Entity[] SelectedVehicles = new Entity[RequiredVehicles];
    private static double s_stageStartTime;
    private static bool s_clickedDeploy;
    private static bool s_finished;
    private static string s_result = string.Empty;
    private static string s_visualReason = string.Empty;
    private static int s_clickResolvedCharacters;
    private static Vector3 s_selectedCharacterWorldPosition;
    private static Vector2 s_selectionClickScreenPosition;
    private static string s_directRuntimeClickLookup = string.Empty;
    private static Mouse s_proofMouse;

    static CharacterVisualAdornmentsProofCapture()
    {
        if (SessionState.GetInt(ActiveKey, 0) == 1)
            Attach();
    }

    [MenuItem("WarlineCapture/Run Character Visual Adornments Proof")]
    public static void Run()
    {
        Array.Fill(SelectedCharacters, Entity.Null);
        Array.Fill(SelectedVehicles, Entity.Null);
        s_stageStartTime = EditorApplication.timeSinceStartup;
        s_clickedDeploy = false;
        s_finished = false;
        s_result = string.Empty;
        s_visualReason = string.Empty;
        s_clickResolvedCharacters = 0;
        s_selectedCharacterWorldPosition = Vector3.zero;
        s_selectionClickScreenPosition = Vector2.zero;
        s_directRuntimeClickLookup = string.Empty;
        File.Delete(ReportPath);
        File.Delete(ScreenshotPath);

        SessionState.SetInt(ActiveKey, 1);
        SessionState.SetInt(StageKey, 0);
        ForceImportIfExists(MainMatchSubScenePath);
        ForceImportIfExists(ShadowMatchSubScenePath);
        Attach();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void ForceImportIfExists(string path)
    {
        if (string.IsNullOrWhiteSpace(AssetDatabase.AssetPathToGUID(path)))
            return;

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
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
        try
        {
            if (stage == 1)
            {
                if (TryClickDeploy())
                {
                    SessionState.SetInt(StageKey, 2);
                    s_stageStartTime = now;
                    return;
                }

                if (now - s_stageStartTime > StartupTimeoutSeconds)
                    Finish("timeout_waiting_for_menu", "Deploy button or fallback MenuView start path was not available.");
            }
            else if (stage == 2)
            {
                if (now - s_stageStartTime < WarmupSeconds)
                    return;

                if (!TryChooseProofUnit(out string selectReason))
                {
                    if (now - s_stageStartTime > MatchTimeoutSeconds)
                        Finish("timeout_waiting_for_match_units", selectReason);
                    return;
                }

                FocusCameraOnSelection();
                SessionState.SetInt(StageKey, 3);
                s_stageStartTime = now;
            }
            else if (stage == 3)
            {
                if (now - s_stageStartTime < 0.5d)
                    return;

                if (!TryQueueRuntimeFocusCommand(out string clickReason))
                {
                    if (now - s_stageStartTime > VisualTimeoutSeconds)
                        Finish("runtime_focus_command_failed", clickReason);
                    return;
                }

                SessionState.SetInt(StageKey, 5);
                s_stageStartTime = now;
            }
            else if (stage == 4)
            {
                if (now - s_stageStartTime < 0.2d)
                    return;

                if (!TryQueueRuntimeMouseRelease(out string releaseReason))
                {
                    if (now - s_stageStartTime > VisualTimeoutSeconds)
                        Finish("runtime_click_release_failed", releaseReason);
                    return;
                }

                SessionState.SetInt(StageKey, 5);
                s_stageStartTime = now;
            }
            else if (stage == 5)
            {
                if (HasExpectedVisuals(out string visualReason))
                {
                    s_visualReason = visualReason;
                    SessionState.SetInt(StageKey, 6);
                    s_stageStartTime = now;
                    return;
                }

                if (now - s_stageStartTime > VisualTimeoutSeconds)
                {
                    WriteReport("visuals_incomplete", visualReason);
                    CaptureScreenshot();
                    SessionState.SetInt(StageKey, 7);
                    s_stageStartTime = now;
                }
            }
            else if (stage == 6)
            {
                if (now - s_stageStartTime < VisualSettleSeconds)
                    return;

                WriteReport("completed", s_visualReason);
                CaptureScreenshot();
                SessionState.SetInt(StageKey, 7);
                s_stageStartTime = now;
            }
            else if (stage == 7)
            {
                if (File.Exists(ScreenshotPath))
                    Finish("completed", "Report and screenshot written.");
                else if (now - s_stageStartTime > 10d)
                    Finish("screenshot_timeout", "ScreenCapture did not produce the proof screenshot.");
            }
        }
        catch (Exception ex)
        {
            Finish("exception", ex.ToString());
        }
    }

    private static bool TryChooseProofUnit(out string reason)
    {
        reason = string.Empty;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            reason = "Default ECS world is not available.";
            return false;
        }

        EntityManager em = world.EntityManager;
        Camera camera = ResolveActiveGameCamera();
        if (camera == null)
        {
            reason = "Active game camera is not available.";
            return false;
        }

        if (!TryGetGrid(em, out GridConfig grid))
        {
            reason = "GridConfig is not available.";
            return false;
        }

        var clickLookup = new FocusableUnitLookupSystem();
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitMovementBehavior>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<Faction>());
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        Entity bestCharacter = Entity.Null;
        float bestCharacterScore = float.MinValue;
        int characterCount = 0;
        int clickResolvedCharacters = 0;
        int vehicleCount = 0;
        for (int i = 0; i < entities.Length && (characterCount < RequiredCharacters || vehicleCount < RequiredVehicles); i++)
        {
            Entity entity = entities[i];
            if (em.HasComponent<Prefab>(entity) ||
                em.HasComponent<StaticGridBlocker>(entity) ||
                em.HasComponent<UnitTransportPassenger>(entity) ||
                em.GetComponentData<UnitHealth>(entity).Current <= 0 ||
                !FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id))
            {
                continue;
            }

            UnitMovementBehavior movement = em.GetComponentData<UnitMovementBehavior>(entity);
            if (movement.UsesVehicleMotion == 0)
            {
                if (bestCharacter != Entity.Null && RequiredCharacters <= 0)
                    continue;

                if (!TryResolveClickedUnit(em, camera, grid, clickLookup, entity, out Entity clickedCharacter) ||
                    clickedCharacter != entity)
                {
                    continue;
                }

                clickResolvedCharacters++;
                Vector3 screen = camera.WorldToScreenPoint(em.GetComponentData<LocalToWorld>(clickedCharacter).Position);
                float edgePenalty = Mathf.Abs(screen.x - Screen.width * 0.7f) + Mathf.Abs(screen.y - Screen.height * 0.5f);
                float score = -edgePenalty;
                if (screen.x > Screen.width * 0.45f)
                    score += Screen.width;
                if (score > bestCharacterScore)
                {
                    bestCharacterScore = score;
                    bestCharacter = clickedCharacter;
                }
            }
            else
            {
                if (vehicleCount >= RequiredVehicles || em.HasComponent<UnitAirMovement>(entity))
                    continue;

                SelectedVehicles[vehicleCount++] = entity;
            }
        }

        if (bestCharacter != Entity.Null && RequiredCharacters > 0)
        {
            SelectedCharacters[0] = bestCharacter;
            characterCount = 1;
        }

        if (characterCount < RequiredCharacters || vehicleCount < RequiredVehicles)
        {
            reason = $"Found characters={characterCount}/{RequiredCharacters} vehicles={vehicleCount}/{RequiredVehicles}.";
            return false;
        }

        s_clickResolvedCharacters = characterCount;
        reason = $"Chose characters={SelectedCharacters.Length} vehicles={SelectedVehicles.Length}.";
        return true;
    }

    private static bool TryGetGrid(EntityManager em, out GridConfig grid)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        if (query.IsEmptyIgnoreFilter)
        {
            grid = default;
            return false;
        }

        grid = em.GetComponentData<GridConfig>(query.GetSingletonEntity());
        return true;
    }

    private static bool TryResolveClickedUnit(
        EntityManager em,
        Camera camera,
        GridConfig grid,
        FocusableUnitLookupSystem lookup,
        Entity target,
        out Entity clicked)
    {
        clicked = Entity.Null;
        if (camera == null ||
            target == Entity.Null ||
            !em.Exists(target) ||
            !em.HasComponent<LocalToWorld>(target))
        {
            return false;
        }

        Vector3 screen = camera.WorldToScreenPoint(em.GetComponentData<LocalToWorld>(target).Position);
        if (screen.z <= 0f)
            return false;

        Ray ray = camera.ScreenPointToRay(screen);
        Plane plane = new(Vector3.up, new Vector3(0f, grid.Origin.y, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        Vector3 worldPoint = ray.GetPoint(distance);
        int2 clickedCell = GridUtils.WorldToCell(grid, worldPoint);
        if (!GridUtils.InBounds(clickedCell, grid.Width, grid.Height))
            return false;

        return lookup.TryGetClickedUnitEntity(em, camera, clickedCell, new Vector2(screen.x, screen.y), out clicked);
    }

    private static bool TryQueueRuntimeMousePress(out string reason)
    {
        reason = string.Empty;
        if (!TryResolveSelectionScreenPosition(out Vector2 screenPosition))
        {
            reason = "Could not resolve selected unit screen position for runtime click press.";
            return false;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
            mouse = InputSystem.AddDevice<Mouse>("WarlineCaptureProofMouse");
        s_proofMouse = mouse;

        InputSystem.QueueStateEvent(s_proofMouse, new MouseState
        {
            position = screenPosition,
            buttons = 1
        });
        InputSystem.Update();

        s_selectionClickScreenPosition = screenPosition;
        s_directRuntimeClickLookup = ResolveDirectRuntimeClickLookup(screenPosition);
        reason = $"Queued runtime mouse press at {screenPosition.x:F1},{screenPosition.y:F1}.";
        return true;
    }

    private static bool TryQueueRuntimeFocusCommand(out string reason)
    {
        reason = string.Empty;
        if (!TryResolveSelectionScreenPosition(out Vector2 screenPosition))
        {
            reason = "Could not resolve selected unit screen position for runtime focus command.";
            return false;
        }

        s_selectionClickScreenPosition = screenPosition;
        s_directRuntimeClickLookup = ResolveDirectRuntimeClickLookup(screenPosition);
        var input = new RtsSelectionInputSystem();
        if (!input.QueueFocusUnitCommandRequest(screenPosition, Time.frameCount))
        {
            reason = $"Could not enqueue runtime FocusUnit command at {screenPosition.x:F1},{screenPosition.y:F1}.";
            return false;
        }

        reason = $"Queued runtime FocusUnit command at {screenPosition.x:F1},{screenPosition.y:F1}.";
        return true;
    }

    private static string ResolveDirectRuntimeClickLookup(Vector2 screenPosition)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return "world-missing";

        EntityManager em = world.EntityManager;
        Camera camera = ResolveActiveGameCamera();
        if (camera == null)
            return "camera-missing";

        var lookup = new FocusableUnitLookupSystem();
        if (!lookup.TryGetClickedUnitEntityByScreenDistance(em, camera, screenPosition, 54f, out Entity entity))
            return "miss";

        string source = em.HasComponent<UnitSourcePrefabKey>(entity)
            ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
            : em.GetName(entity);
        bool selected = em.HasComponent<SelectedUnitTag>(entity);
        byte faction = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
        return $"hit entity={entity} source={source} faction={faction} selected={selected}";
    }

    private static bool TryQueueRuntimeMouseRelease(out string reason)
    {
        reason = string.Empty;
        if (s_proofMouse == null)
        {
            reason = "Proof mouse was not available for runtime click release.";
            return false;
        }

        InputSystem.QueueStateEvent(s_proofMouse, new MouseState
        {
            position = s_selectionClickScreenPosition,
            buttons = 0
        });
        InputSystem.Update();

        reason = $"Queued runtime mouse release at {s_selectionClickScreenPosition.x:F1},{s_selectionClickScreenPosition.y:F1}.";
        return true;
    }

    private static bool HasExpectedVisuals(out string reason)
    {
        reason = string.Empty;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            reason = "Default ECS world is not available.";
            return false;
        }

        EntityManager em = world.EntityManager;
        int characterMarkers = CountMarkerInstances(em, SelectedCharacters);
        int vehicleMarkers = CountMarkerInstances(em, SelectedVehicles);
        int characterTintTargets = CountFactionTintTargetsInSelectedTrees(em, SelectedCharacters);
        int selectedCharacters = CountSelectedUnits(em, SelectedCharacters);
        int visibleCharacterDetails = CountVisibleCharacterDetails(em, SelectedCharacters);
        bool complete = characterMarkers >= RequiredCharacters &&
                        vehicleMarkers >= RequiredVehicles &&
                        selectedCharacters >= RequiredCharacters &&
                        characterTintTargets > 0 &&
                        visibleCharacterDetails >= RequiredCharacters;
        reason = $"selectedCharacters={selectedCharacters}/{RequiredCharacters} characterMarkers={characterMarkers}/{RequiredCharacters} vehicleMarkers={vehicleMarkers}/{RequiredVehicles} characterTintTargets={characterTintTargets} visibleCharacterDetails={visibleCharacterDetails}/{RequiredCharacters}";
        return complete;
    }

    private static int CountSelectedUnits(EntityManager em, Entity[] units)
    {
        int count = 0;
        for (int i = 0; i < units.Length; i++)
        {
            Entity unit = units[i];
            if (unit != Entity.Null &&
                em.Exists(unit) &&
                em.HasComponent<SelectedUnitTag>(unit))
            {
                count++;
            }
        }

        return count;
    }

    private static int CountMarkerInstances(EntityManager em, Entity[] units)
    {
        int count = 0;
        for (int i = 0; i < units.Length; i++)
        {
            Entity unit = units[i];
            if (unit != Entity.Null &&
                em.Exists(unit) &&
                em.HasComponent<UnitSelectionMarkerInstanceReference>(unit) &&
                em.Exists(em.GetComponentData<UnitSelectionMarkerInstanceReference>(unit).Instance))
            {
                count++;
            }
        }

        return count;
    }

    private static int CountHealthBarInstances(EntityManager em, Entity[] units)
    {
        int count = 0;
        for (int i = 0; i < units.Length; i++)
        {
            Entity unit = units[i];
            if (unit != Entity.Null &&
                em.Exists(unit) &&
                em.HasComponent<UnitHealthBarInstanceReference>(unit) &&
                em.Exists(em.GetComponentData<UnitHealthBarInstanceReference>(unit).Instance))
            {
                count++;
            }
        }

        return count;
    }

    private static int CountVisibleCharacterDetails(EntityManager em, Entity[] units)
    {
        int count = 0;
        for (int i = 0; i < units.Length; i++)
        {
            Entity unit = units[i];
            if (unit != Entity.Null &&
                em.Exists(unit) &&
                HasVisibleDetailRenderable(em, unit))
            {
                count++;
            }
        }

        return count;
    }

    private static bool HasVisibleDetailRenderable(EntityManager em, Entity unit)
    {
        Entity detailRoot = ResolveDetailRoot(em, unit);
        return detailRoot != Entity.Null &&
               em.Exists(detailRoot) &&
               CountRenderableEntitiesInTree(em, detailRoot, requireVisible: true) > 0;
    }

    private static Entity ResolveDetailRoot(EntityManager em, Entity unit)
    {
        if (unit == Entity.Null || !em.Exists(unit))
            return Entity.Null;

        if (em.HasComponent<UnitDetailedVisualReference>(unit))
        {
            Entity root = em.GetComponentData<UnitDetailedVisualReference>(unit).Root;
            if (root != Entity.Null && em.Exists(root))
                return root;
        }

        if (em.HasComponent<UnitModelInstanceReference>(unit))
        {
            Entity root = em.GetComponentData<UnitModelInstanceReference>(unit).Instance;
            if (root != Entity.Null && em.Exists(root))
                return root;
        }

        return unit;
    }

    private static int CountRenderableEntitiesInTree(EntityManager em, Entity root, bool requireVisible)
    {
        if (root == Entity.Null || !em.Exists(root))
            return 0;

        int count = 0;
        bool renderable = em.HasComponent<MaterialMeshInfo>(root);
        bool hidden = em.HasComponent<Disabled>(root) ||
                      em.HasComponent<DisableRendering>(root) ||
                      em.HasComponent<UnitRenderBudgetCulledTag>(root);
        if (renderable && (!requireVisible || !hidden))
            count++;

        if (!em.HasBuffer<Child>(root))
            return count;

        DynamicBuffer<Child> children = em.GetBuffer<Child>(root);
        for (int i = 0; i < children.Length; i++)
            count += CountRenderableEntitiesInTree(em, children[i].Value, requireVisible);
        return count;
    }

    private static int CountHiddenRenderableEntitiesInTree(EntityManager em, Entity root)
    {
        if (root == Entity.Null || !em.Exists(root))
            return 0;

        int count = 0;
        bool renderable = em.HasComponent<MaterialMeshInfo>(root);
        bool hidden = em.HasComponent<Disabled>(root) ||
                      em.HasComponent<DisableRendering>(root) ||
                      em.HasComponent<UnitRenderBudgetCulledTag>(root);
        if (renderable && hidden)
            count++;

        if (!em.HasBuffer<Child>(root))
            return count;

        DynamicBuffer<Child> children = em.GetBuffer<Child>(root);
        for (int i = 0; i < children.Length; i++)
            count += CountHiddenRenderableEntitiesInTree(em, children[i].Value);
        return count;
    }

    private static int CountFactionTintTargetsInSelectedTrees(EntityManager em, Entity[] units)
    {
        int count = 0;
        for (int i = 0; i < units.Length; i++)
            count += CountFactionTintTargetsInTree(em, units[i]);
        return count;
    }

    private static int CountFactionTintTargetsInTree(EntityManager em, Entity root)
    {
        if (root == Entity.Null || !em.Exists(root))
            return 0;

        int count = em.HasComponent<FactionTintTarget>(root) ? 1 : 0;
        if (!em.HasBuffer<Child>(root))
            return count;

        DynamicBuffer<Child> children = em.GetBuffer<Child>(root);
        for (int i = 0; i < children.Length; i++)
            count += CountFactionTintTargetsInTree(em, children[i].Value);
        return count;
    }

    private static void FocusCameraOnSelection()
    {
        Camera camera = ResolveActiveGameCamera();
        if (camera == null)
            return;

        if (!TryResolveSelectionCenter(out Vector3 center))
            return;

        Vector3 lookTarget = center + new Vector3(0f, 0.35f, 0f);
        Vector3 offset = new(0f, 10.5f, -4.75f);
        camera.transform.position = center + offset;
        camera.transform.rotation = Quaternion.LookRotation((lookTarget - camera.transform.position).normalized, Vector3.up);
        camera.nearClipPlane = 0.1f;
        camera.fieldOfView = 28f;
    }

    private static bool TryResolveSelectionScreenPosition(out Vector2 screenPosition)
    {
        screenPosition = default;
        Camera camera = ResolveActiveGameCamera();
        if (camera == null)
            return false;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        Entity unit = SelectedCharacters.Length > 0 ? SelectedCharacters[0] : Entity.Null;
        if (unit == Entity.Null || !em.Exists(unit) || !em.HasComponent<LocalToWorld>(unit))
            return false;

        Vector3 worldPosition = em.GetComponentData<LocalToWorld>(unit).Position;
        Vector3 screen = camera.WorldToScreenPoint(worldPosition + Vector3.up * 0.85f);
        if (screen.z <= 0f)
            return false;

        screenPosition = new Vector2(screen.x, screen.y);
        return true;
    }

    private static bool TryResolveSelectionCenter(out Vector3 center)
    {
        center = Vector3.zero;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        int count = 0;
        AccumulatePositions(em, SelectedCharacters, ref center, ref count);
        if (count == 0)
            return false;

        center /= count;
        s_selectedCharacterWorldPosition = center;
        return true;
    }

    private static void AccumulatePositions(EntityManager em, Entity[] units, ref Vector3 total, ref int count)
    {
        for (int i = 0; i < units.Length; i++)
        {
            Entity unit = units[i];
            if (unit == Entity.Null || !em.Exists(unit))
                continue;

            if (em.HasComponent<LocalToWorld>(unit))
            {
                float3 position = em.GetComponentData<LocalToWorld>(unit).Position;
                total += new Vector3(position.x, position.y, position.z);
                count++;
            }
            else if (em.HasComponent<LocalTransform>(unit))
            {
                float3 position = em.GetComponentData<LocalTransform>(unit).Position;
                total += new Vector3(position.x, position.y, position.z);
                count++;
            }
        }
    }

    private static void WriteReport(string result, string detail)
    {
        StringBuilder report = new();
        report.AppendLine("Character Visual Adornments Proof");
        report.AppendLine($"result={result}");
        report.AppendLine($"detail={detail}");
        report.AppendLine($"clickedDeploy={s_clickedDeploy}");
        report.AppendLine($"clickResolvedCharacters={s_clickResolvedCharacters}/{RequiredCharacters}");
        report.AppendLine($"runtimeClickScreen={s_selectionClickScreenPosition.x:F1},{s_selectionClickScreenPosition.y:F1}");
        report.AppendLine($"directLookupAtRuntimeClick={s_directRuntimeClickLookup}");
        report.AppendLine($"selectedCharacterWorld={s_selectedCharacterWorldPosition.x:F2},{s_selectedCharacterWorldPosition.y:F2},{s_selectedCharacterWorldPosition.z:F2}");
        AppendWorldVisualSummary(report);
        AppendUnitReport("character", SelectedCharacters, report);
        AppendUnitReport("vehicle", SelectedVehicles, report);
        report.AppendLine($"screenshot={ScreenshotPath}");
        File.WriteAllText(ReportPath, report.ToString());
    }

    private static void AppendWorldVisualSummary(StringBuilder report)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        using EntityQuery sourceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSourcePrefabKey>());
        using NativeArray<Entity> sourceEntities = sourceQuery.ToEntityArray(Allocator.Temp);

        int markerPrefabRefs = 0;
        int healthPrefabRefs = 0;
        int sourcePrefabs = 0;
        int sourcePrefabMarkerRefs = 0;
        int sourcePrefabHealthRefs = 0;
        int selectedSourceKeyPrefabCount = 0;
        int selectedSourceKeyMarkerRefs = 0;
        int selectedSourceKeyHealthRefs = 0;
        FixedString64Bytes selectedSourceKey = ResolveFirstSelectedSourceKey(em);
        int sharedVisualReferenceCount = 0;
        bool sharedHasMarker = false;
        bool sharedHasHealth = false;
        int spawnConfigReferenceCount = 0;
        bool spawnConfigHasMarker = false;
        bool spawnConfigHasHealth = false;
        using (EntityQuery sharedQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSharedVisualPrefabReferences>()))
        {
            using NativeArray<Entity> sharedEntities = sharedQuery.ToEntityArray(Allocator.Temp);
            sharedVisualReferenceCount = sharedEntities.Length;
            if (sharedEntities.Length > 0)
            {
                UnitSharedVisualPrefabReferences shared = em.GetComponentData<UnitSharedVisualPrefabReferences>(sharedEntities[0]);
                sharedHasMarker = shared.SelectionMarkerPrefab != Entity.Null && em.Exists(shared.SelectionMarkerPrefab);
                sharedHasHealth = shared.HealthBarPrefab != Entity.Null && em.Exists(shared.HealthBarPrefab);
            }
        }
        using (EntityQuery spawnConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>()))
        {
            using NativeArray<Entity> spawnConfigEntities = spawnConfigQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < spawnConfigEntities.Length; i++)
            {
                InitialUnitsSpawnConfig spawnConfig = em.GetComponentData<InitialUnitsSpawnConfig>(spawnConfigEntities[i]);
                bool marker = spawnConfig.UnitSelectionMarkerPrefab != Entity.Null && em.Exists(spawnConfig.UnitSelectionMarkerPrefab);
                bool health = spawnConfig.UnitHealthBarPrefab != Entity.Null && em.Exists(spawnConfig.UnitHealthBarPrefab);
                if (marker || health)
                    spawnConfigReferenceCount++;
                spawnConfigHasMarker |= marker;
                spawnConfigHasHealth |= health;
            }
        }

        for (int i = 0; i < sourceEntities.Length; i++)
        {
            Entity entity = sourceEntities[i];
            bool hasMarker = em.HasComponent<UnitSelectionMarkerPrefabReference>(entity);
            bool hasHealth = em.HasComponent<UnitHealthBarPrefabReference>(entity);
            if (hasMarker)
                markerPrefabRefs++;
            if (hasHealth)
                healthPrefabRefs++;

            bool isPrefab = em.HasComponent<Prefab>(entity);
            if (isPrefab)
            {
                sourcePrefabs++;
                if (hasMarker)
                    sourcePrefabMarkerRefs++;
                if (hasHealth)
                    sourcePrefabHealthRefs++;
            }

            if (selectedSourceKey.Length > 0 &&
                em.GetComponentData<UnitSourcePrefabKey>(entity).Value.Equals(selectedSourceKey))
            {
                selectedSourceKeyPrefabCount += isPrefab ? 1 : 0;
                if (hasMarker)
                    selectedSourceKeyMarkerRefs++;
                if (hasHealth)
                    selectedSourceKeyHealthRefs++;
            }
        }

        report.Append("worldVisualRefs=");
        InitialUnitsSpawnerAuthoringConfig initialConfig =
            AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(InitialUnitsConfigPath);
        report.Append("initialConfigMarker=");
        report.Append(initialConfig != null && initialConfig.UnitSelectionMarkerPrefab != null);
        report.Append(" initialConfigHealth=");
        report.Append(initialConfig != null && initialConfig.UnitHealthBarPrefab != null);
        report.Append(" ");
        report.Append("shared=");
        report.Append(sharedVisualReferenceCount);
        report.Append(" sharedMarker=");
        report.Append(sharedHasMarker);
        report.Append(" sharedHealth=");
        report.Append(sharedHasHealth);
        report.Append(" ");
        report.Append("spawnConfigRefs=");
        report.Append(spawnConfigReferenceCount);
        report.Append(" spawnConfigMarker=");
        report.Append(spawnConfigHasMarker);
        report.Append(" spawnConfigHealth=");
        report.Append(spawnConfigHasHealth);
        report.Append(" ");
        report.Append("sources=");
        report.Append(sourceEntities.Length);
        report.Append(" markerRefs=");
        report.Append(markerPrefabRefs);
        report.Append(" healthRefs=");
        report.Append(healthPrefabRefs);
        report.Append(" prefabs=");
        report.Append(sourcePrefabs);
        report.Append(" prefabMarkerRefs=");
        report.Append(sourcePrefabMarkerRefs);
        report.Append(" prefabHealthRefs=");
        report.Append(sourcePrefabHealthRefs);
        report.Append(" selectedSourceKey=");
        report.Append(selectedSourceKey.ToString());
        report.Append(" selectedSourcePrefabs=");
        report.Append(selectedSourceKeyPrefabCount);
        report.Append(" selectedMarkerRefs=");
        report.Append(selectedSourceKeyMarkerRefs);
        report.Append(" selectedHealthRefs=");
        report.Append(selectedSourceKeyHealthRefs);
        report.AppendLine();
    }

    private static FixedString64Bytes ResolveFirstSelectedSourceKey(EntityManager em)
    {
        for (int i = 0; i < SelectedCharacters.Length; i++)
        {
            Entity entity = SelectedCharacters[i];
            if (entity != Entity.Null && em.Exists(entity) && em.HasComponent<UnitSourcePrefabKey>(entity))
                return em.GetComponentData<UnitSourcePrefabKey>(entity).Value;
        }

        for (int i = 0; i < SelectedVehicles.Length; i++)
        {
            Entity entity = SelectedVehicles[i];
            if (entity != Entity.Null && em.Exists(entity) && em.HasComponent<UnitSourcePrefabKey>(entity))
                return em.GetComponentData<UnitSourcePrefabKey>(entity).Value;
        }

        return default;
    }

    private static void AppendUnitReport(string label, Entity[] units, StringBuilder report)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        for (int i = 0; i < units.Length; i++)
        {
            Entity entity = units[i];
            report.Append(label);
            report.Append(i);
            report.Append('=');
            report.Append(entity);
            if (entity == Entity.Null || !em.Exists(entity))
            {
                report.AppendLine(" missing");
                continue;
            }

            report.Append(" selected=");
            report.Append(em.HasComponent<SelectedUnitTag>(entity));
            report.Append(" unitMove=");
            report.Append(em.HasComponent<UnitMove>(entity));
            report.Append(" unitGrid=");
            report.Append(em.HasComponent<UnitGrid>(entity));
            report.Append(" unitFootprint=");
            report.Append(em.HasComponent<UnitFootprint>(entity));
            report.Append(" localToWorld=");
            report.Append(em.HasComponent<LocalToWorld>(entity));
            report.Append(" disabled=");
            report.Append(em.HasComponent<Disabled>(entity));
            report.Append(" passenger=");
            report.Append(em.HasComponent<UnitTransportPassenger>(entity));
            report.Append(" health=");
            report.Append(em.HasComponent<UnitHealth>(entity));
            report.Append(" behavior=");
            report.Append(em.HasComponent<UnitMovementBehavior>(entity));
            report.Append(" markerPrefab=");
            report.Append(em.HasComponent<UnitSelectionMarkerPrefabReference>(entity));
            report.Append(" markerInstance=");
            report.Append(em.HasComponent<UnitSelectionMarkerInstanceReference>(entity));
            report.Append(" healthPrefab=");
            report.Append(em.HasComponent<UnitHealthBarPrefabReference>(entity));
            report.Append(" healthInstance=");
            report.Append(em.HasComponent<UnitHealthBarInstanceReference>(entity));
            report.Append(" tintTargets=");
            report.Append(CountFactionTintTargetsInTree(em, entity));
            report.Append(" culledUnit=");
            report.Append(em.HasComponent<UnitRenderBudgetCulledUnitTag>(entity));
            report.Append(" renderVisual=");
            report.Append(em.HasComponent<UnitRenderVisualState>(entity)
                ? ((UnitRenderVisualKind)em.GetComponentData<UnitRenderVisualState>(entity).Current).ToString()
                : "none");
            Entity detailRoot = ResolveDetailRoot(em, entity);
            report.Append(" detailRoot=");
            report.Append(detailRoot);
            report.Append(" detailRootExists=");
            report.Append(detailRoot != Entity.Null && em.Exists(detailRoot));
            if (detailRoot != Entity.Null && em.Exists(detailRoot))
            {
                report.Append(" detailDisabled=");
                report.Append(em.HasComponent<Disabled>(detailRoot));
                report.Append(" detailDisableRendering=");
                report.Append(em.HasComponent<DisableRendering>(detailRoot));
                report.Append(" detailCulled=");
                report.Append(em.HasComponent<UnitRenderBudgetCulledTag>(detailRoot));
                report.Append(" detailRenderables=");
                report.Append(CountRenderableEntitiesInTree(em, detailRoot, requireVisible: false));
                report.Append(" visibleDetailRenderables=");
                report.Append(CountRenderableEntitiesInTree(em, detailRoot, requireVisible: true));
                report.Append(" hiddenDetailRenderables=");
                report.Append(CountHiddenRenderableEntitiesInTree(em, detailRoot));
                if (em.HasComponent<LocalToWorld>(detailRoot))
                {
                    float3 detailPosition = em.GetComponentData<LocalToWorld>(detailRoot).Position;
                    report.Append(" detailWorld=");
                    report.Append($"{detailPosition.x:F2},{detailPosition.y:F2},{detailPosition.z:F2}");
                }
            }

            if (em.HasComponent<LocalToWorld>(entity))
            {
                float3 unitPosition = em.GetComponentData<LocalToWorld>(entity).Position;
                report.Append(" unitWorld=");
                report.Append($"{unitPosition.x:F2},{unitPosition.y:F2},{unitPosition.z:F2}");
            }

            if (em.HasComponent<Faction>(entity))
            {
                report.Append(" faction=");
                report.Append(em.GetComponentData<Faction>(entity).Id);
            }

            if (em.HasComponent<UnitSourcePrefabKey>(entity))
            {
                report.Append(" sourceKey=");
                report.Append(em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString());
            }

            report.AppendLine();
        }
    }

    private static void CaptureScreenshot()
    {
        Camera camera = ResolveActiveGameCamera();
        if (camera == null)
            return;

        const int width = 1280;
        const int height = 720;
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = new(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply(false);
            File.WriteAllBytes(ScreenshotPath, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(texture);
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
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

        MenuView menu = FindComponentInScene<MenuView>(menuScene);
        if (menu == null)
            return false;

        if (menu.buttonGame != null)
            menu.buttonGame.onClick.Invoke();
        else
            menu.RequestGameStart();

        s_clickedDeploy = true;
        return true;
    }

    private static bool IsDeployCommandButton(WarlineCaptureShellRouteButtonView routeButton)
    {
        return routeButton != null &&
               routeButton.name == "DeployCommandButton" &&
               routeButton.Intent == UiShellRouteIntent.EnterMatch &&
               routeButton.Route == WarlineCaptureRoute.Match;
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
                Camera camera = FindComponentInTree<Camera>(root.transform, static c => c != null && c.enabled);
                if (camera != null)
                    return camera;
            }
        }

        return null;
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

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
            return;

        if (type == LogType.Exception || type == LogType.Error)
            Debug.Log($"[CharacterVisualAdornmentsProof:ObservedLog] {condition}");
    }

    private static void Finish(string result, string detail)
    {
        if (s_finished)
            return;

        s_finished = true;
        s_result = result;
        if (!File.Exists(ReportPath))
            WriteReport(result, detail);

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
        else
            EditorApplication.Exit(result == "completed" ? 0 : 1);
    }
}
#endif
