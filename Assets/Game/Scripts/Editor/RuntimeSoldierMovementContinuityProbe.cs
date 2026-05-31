#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Scripts.UI;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RuntimeSoldierMovementContinuityProbe
{
    private const string ActiveKey = "WarlineCapture.RuntimeSoldierMovementContinuityProbe.Active";
    private const string StageKey = "WarlineCapture.RuntimeSoldierMovementContinuityProbe.Stage";
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string OutputPath = "/private/tmp/warlinecapture-soldier-movement-continuity.json";
    private const double StartupTimeoutSeconds = 45d;
    private const double MatchTimeoutSeconds = 75d;
    private const double WarmupSeconds = 12d;
    private const double PathWaitTimeoutSeconds = 8d;
    private const double SampleSeconds = 6d;
    private const float StallDistanceThreshold = 0.0025f;
    private const int MaxSamples = 720;

    private static readonly List<MovementSample> Samples = new(MaxSamples);
    private static double s_stageStartTime;
    private static double s_sampleStartTime;
    private static Entity s_soldier;
    private static int2 s_goal;
    private static float3 s_previousPosition;
    private static float3 s_previousVisualPosition;
    private static float s_previousAnimTime;
    private static float s_previousRenderFrame;
    private static int s_stallFrames;
    private static int s_visualStallFrames;
    private static int s_animRepeatFrames;
    private static bool s_hasPrevious;
    private static bool s_clickedDeploy;
    private static bool s_finished;
    private static string s_lastStartReason;

    static RuntimeSoldierMovementContinuityProbe()
    {
        if (SessionState.GetInt(ActiveKey, 0) == 1)
            Attach();
    }

    public static void Run()
    {
        Samples.Clear();
        s_stageStartTime = EditorApplication.timeSinceStartup;
        s_sampleStartTime = 0d;
        s_soldier = Entity.Null;
        s_goal = default;
        s_previousPosition = default;
        s_previousVisualPosition = default;
        s_previousAnimTime = 0f;
        s_previousRenderFrame = -1f;
        s_stallFrames = 0;
        s_visualStallFrames = 0;
        s_animRepeatFrames = 0;
        s_hasPrevious = false;
        s_clickedDeploy = false;
        s_finished = false;
        s_lastStartReason = string.Empty;

        SessionState.SetInt(ActiveKey, 1);
        SessionState.SetInt(StageKey, 0);
        Attach();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
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
            EditorApplication.Exit(0);
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
                    Finish("timeout_waiting_for_menu", "Deploy button or MenuView game-start path was not available.");
            }
            else if (stage == 2)
            {
                if (now - s_stageStartTime > MatchTimeoutSeconds)
                {
                    Finish("timeout_waiting_for_match", string.IsNullOrEmpty(s_lastStartReason) ? "Soldier entity/grid did not become available." : s_lastStartReason);
                    return;
                }

                if (now - s_stageStartTime < WarmupSeconds)
                    return;

                if (!TryStartSoldierMove(out string reason))
                {
                    s_lastStartReason = reason;
                    return;
                }

                SessionState.SetInt(StageKey, 4);
                s_sampleStartTime = now;
                s_hasPrevious = false;
            }
            else if (stage == 3)
            {
                if (TryHasActivePath(out string reason))
                {
                    SessionState.SetInt(StageKey, 4);
                    s_sampleStartTime = now;
                    s_hasPrevious = false;
                    return;
                }

                if (now - s_stageStartTime > PathWaitTimeoutSeconds)
                    Finish("path_not_started", reason);
            }
            else if (stage == 4)
            {
                if (!TrySampleSoldier(now, out string reason))
                {
                    Finish("sampling_failed", reason);
                    return;
                }

                if (now - s_sampleStartTime >= SampleSeconds || Samples.Count >= MaxSamples)
                    Finish("completed", "Movement continuity sample captured.");
            }
        }
        catch (Exception ex)
        {
            Finish("exception", ex.ToString());
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

    private static bool TryStartSoldierMove(out string reason)
    {
        reason = string.Empty;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            reason = "default_world_not_ready";
            return false;
        }

        EntityManager em = world.EntityManager;
        Entity gridEntity;
        GridConfig grid;
        DynamicBuffer<GridWalkable> walkable;
        try
        {
            grid = GetSingleton<GridConfig>(em, out gridEntity);
            walkable = em.GetBuffer<GridWalkable>(gridEntity);
        }
        catch (Exception ex)
        {
            reason = "grid_not_ready:" + ex.GetType().Name;
            return false;
        }

        if (!TryFindSoldierWithMoveGoal(em, gridEntity, grid, walkable, out Entity soldier, out UnitGrid unitGrid, out int2 goal))
        {
            reason = "soldier_with_clear_goal_not_ready";
            return false;
        }

        ForcePlayRequested(em);
        ApplyDirectProbePath(em, gridEntity, grid, soldier, unitGrid.Cell, goal);
        s_soldier = soldier;
        s_goal = goal;
        return true;
    }

    private static bool TryFindSoldierWithMoveGoal(
        EntityManager em,
        Entity gridEntity,
        in GridConfig grid,
        DynamicBuffer<GridWalkable> walkable,
        out Entity soldier,
        out UnitGrid unitGrid,
        out int2 goal)
    {
        soldier = Entity.Null;
        unitGrid = default;
        goal = default;

        using EntityQuery query = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<LocalTransform>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>()
            }
        });

        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!sourceKey.StartsWith("Unit_Chr_", StringComparison.Ordinal))
                continue;

            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            if (health.Current <= 0)
                continue;

            if (em.HasComponent<Faction>(entity) && em.GetComponentData<Faction>(entity).Id != 0)
                continue;

            UnitGrid candidateGrid = em.GetComponentData<UnitGrid>(entity);
            UnitFootprint footprint = em.HasComponent<UnitFootprint>(entity)
                ? em.GetComponentData<UnitFootprint>(entity)
                : new UnitFootprint { Size = new int2(1, 1) };
            byte factionId = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
            if (!TryFindMoveGoal(em, gridEntity, grid, walkable, candidateGrid.Cell, footprint.Size, factionId, out int2 candidateGoal))
                continue;

            soldier = entity;
            unitGrid = candidateGrid;
            goal = candidateGoal;
            return true;
        }

        return false;
    }

    private static bool TrySampleSoldier(double now, out string reason)
    {
        reason = string.Empty;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            reason = "default_world_not_ready";
            return false;
        }

        EntityManager em = world.EntityManager;
        if (s_soldier == Entity.Null || !em.Exists(s_soldier))
        {
            reason = "soldier_lost";
            return false;
        }

        LocalTransform transform = em.GetComponentData<LocalTransform>(s_soldier);
        UnitGrid grid = em.GetComponentData<UnitGrid>(s_soldier);
        bool hasFollow = em.HasComponent<UnitPathFollow>(s_soldier);
        UnitPathFollow follow = hasFollow ? em.GetComponentData<UnitPathFollow>(s_soldier) : default;
        bool hasRequest = em.HasComponent<UnitPathRequest>(s_soldier);
        bool hasRange = em.HasComponent<UnitPathRange>(s_soldier);
        UnitPathRange range = hasRange ? em.GetComponentData<UnitPathRange>(s_soldier) : default;
        UnitMoveVisualState visualState = em.HasComponent<UnitMoveVisualState>(s_soldier)
            ? em.GetComponentData<UnitMoveVisualState>(s_soldier)
            : default;
        UnitResolvedAnimationIndex resolvedAnimation = em.HasComponent<UnitResolvedAnimationIndex>(s_soldier)
            ? em.GetComponentData<UnitResolvedAnimationIndex>(s_soldier)
            : default;

        TryResolveVisualAnimation(em, s_soldier, out Entity visualEntity, out float3 visualPosition, out MaterialAnimationIndex materialIndex, out MaterialAnimationData materialData);

        float moveDelta = 0f;
        float visualDelta = 0f;
        bool movementStalled = false;
        bool visualStalled = false;
        bool animRepeated = false;
        if (s_hasPrevious)
        {
            float3 delta = transform.Position - s_previousPosition;
            delta.y = 0f;
            moveDelta = math.length(delta);
            float3 visualDelta3 = visualPosition - s_previousVisualPosition;
            visualDelta3.y = 0f;
            visualDelta = math.length(visualDelta3);
            bool activeMove = hasFollow || hasRequest;
            movementStalled = activeMove && moveDelta < StallDistanceThreshold;
            visualStalled = activeMove && visualDelta < StallDistanceThreshold;
            animRepeated = materialData.Time > s_previousAnimTime && math.abs(materialData.RenderConfig.x - s_previousRenderFrame) < 0.5f;
            if (movementStalled)
                s_stallFrames++;
            if (visualStalled)
                s_visualStallFrames++;
            if (animRepeated)
                s_animRepeatFrames++;
        }

        Samples.Add(new MovementSample
        {
            Frame = Time.frameCount,
            Time = (float)(now - s_sampleStartTime),
            DeltaTime = Time.deltaTime,
            Position = transform.Position,
            VisualPosition = visualPosition,
            MoveDelta = moveDelta,
            VisualDelta = visualDelta,
            Cell = grid.Cell,
            Goal = s_goal,
            HasFollow = hasFollow,
            HasRequest = hasRequest,
            PathIndex = hasFollow ? follow.PathIndex : -1,
            PathLength = hasRange ? range.Length : -1,
            MoveVisual = visualState.IsMoving,
            ResolvedAnimation = resolvedAnimation.Value,
            MaterialAnimation = materialIndex.Value,
            MaterialDataAnimation = materialData.AnimationIndex,
            MaterialTime = materialData.Time,
            RenderFrame = materialData.RenderConfig.x,
            NextRenderFrame = materialData.RenderConfig.y,
            Blend = materialData.RenderConfig.z,
            MovementStalled = movementStalled,
            VisualStalled = visualStalled,
            AnimationRepeated = animRepeated,
            VisualEntityIndex = visualEntity.Index
        });

        s_previousPosition = transform.Position;
        s_previousVisualPosition = visualPosition;
        s_previousAnimTime = materialData.Time;
        s_previousRenderFrame = materialData.RenderConfig.x;
        s_hasPrevious = true;
        return true;
    }

    private static bool TryFindSoldier(EntityManager em, out Entity soldier)
    {
        soldier = Entity.Null;
        using EntityQuery query = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<LocalTransform>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>()
            }
        });

        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!sourceKey.StartsWith("Unit_Chr_", StringComparison.Ordinal))
                continue;

            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            if (health.Current <= 0)
                continue;

            if (em.HasComponent<Faction>(entity) && em.GetComponentData<Faction>(entity).Id != 0)
                continue;

            soldier = entity;
            return true;
        }

        return false;
    }

    private static bool TryFindMoveGoal(
        EntityManager em,
        Entity gridEntity,
        in GridConfig grid,
        DynamicBuffer<GridWalkable> walkable,
        int2 current,
        int2 footprintSize,
        byte factionId,
        out int2 goal)
    {
        NativeArray<GridWalkable> walkableArray = walkable.AsNativeArray();
        DynamicBlockerData blockers = em.HasComponent<DynamicBlockerData>(gridEntity)
            ? em.GetComponentData<DynamicBlockerData>(gridEntity)
            : default;

        int2[] directions =
        {
            new(1, 0),
            new(0, 1),
            new(1, 1),
            new(-1, 0),
            new(0, -1),
            new(-1, 1),
            new(1, -1),
            new(-1, -1)
        };

        for (int distance = 48; distance >= 12; distance -= 4)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                int2 candidate = current + directions[i] * distance;
                if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                    continue;

                if (HasClearWalkableLine(
                        grid,
                        walkableArray,
                        blockers,
                        current,
                        candidate,
                        footprintSize,
                        factionId))
                {
                    goal = candidate;
                    return true;
                }
            }
        }

        goal = current;
        return false;
    }

    private static void ApplyDirectProbePath(EntityManager em, Entity gridEntity, in GridConfig grid, Entity soldier, int2 current, int2 goal)
    {
        if (em.HasComponent<EngageTarget>(soldier))
            em.RemoveComponent<EngageTarget>(soldier);
        if (em.HasComponent<UnitPathRequest>(soldier))
            em.RemoveComponent<UnitPathRequest>(soldier);
        if (em.HasComponent<UnitPathRetryCooldown>(soldier))
            em.RemoveComponent<UnitPathRetryCooldown>(soldier);
        if (em.HasComponent<UnitPathFollow>(soldier))
            em.RemoveComponent<UnitPathFollow>(soldier);
        if (em.HasComponent<UnitPathRange>(soldier))
            em.RemoveComponent<UnitPathRange>(soldier);
        if (em.HasComponent<UnitLongDistanceMove>(soldier))
            em.RemoveComponent<UnitLongDistanceMove>(soldier);
        if (em.HasComponent<AutoWanderMoveTag>(soldier))
            em.RemoveComponent<AutoWanderMoveTag>(soldier);

        PathPoolData pool = em.GetComponentData<PathPoolData>(gridEntity);
        int start = pool.Cells.Length;
        int2 delta = goal - current;
        int steps = math.max(1, math.max(math.abs(delta.x), math.abs(delta.y)));
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            int2 cell = new int2(
                (int)math.round(math.lerp(current.x, goal.x, t)),
                (int)math.round(math.lerp(current.y, goal.y, t)));
            pool.Cells.Add(cell);
        }

        SetOrAdd(em, soldier, new UnitTarget { Cell = goal });
        SetOrAdd(em, soldier, new UnitPathFollow { PathIndex = 0 });
        SetOrAdd(em, soldier, new UnitPathRange { Start = start, Length = steps });
        if (!em.HasComponent<ManualMoveOrderTag>(soldier))
            em.AddComponent<ManualMoveOrderTag>(soldier);

        LocalTransform transform = em.GetComponentData<LocalTransform>(soldier);
        transform.Position = GridUtils.CellToWorldCenter(grid, current);
        em.SetComponentData(soldier, transform);
        em.SetComponentData(soldier, new UnitGrid { Cell = current });
    }

    private static void SetOrAdd<T>(EntityManager em, Entity entity, T component)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            em.SetComponentData(entity, component);
        else
            em.AddComponentData(entity, component);
    }

    private static void ForcePlayRequested(EntityManager em)
    {
        InitialUnitsRuntimeState.PlayRequested = true;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
        if (query.CalculateEntityCount() <= 0)
            return;

        Entity stateEntity = query.GetSingletonEntity();
        RuntimeGameplayStateComponent state = em.GetComponentData<RuntimeGameplayStateComponent>(stateEntity);
        state.PlayRequested = 1;
        em.SetComponentData(stateEntity, state);
    }

    private static bool TryHasActivePath(out string reason)
    {
        reason = "path_waiting";
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            reason = "default_world_not_ready";
            return false;
        }

        EntityManager em = world.EntityManager;
        if (s_soldier == Entity.Null || !em.Exists(s_soldier))
        {
            reason = "soldier_lost";
            return false;
        }

        bool hasRequest = em.HasComponent<UnitPathRequest>(s_soldier);
        if (em.HasComponent<UnitPathFollow>(s_soldier) && em.HasComponent<UnitPathRange>(s_soldier))
            return true;

        if (!hasRequest)
            reason = "path_request_consumed_without_follow";
        return false;
    }

    private static bool HasClearWalkableLine(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        DynamicBlockerData blockers,
        int2 from,
        int2 to,
        int2 footprintSize,
        byte factionId)
    {
        int2 delta = to - from;
        int steps = math.max(math.abs(delta.x), math.abs(delta.y));
        if (steps <= 0)
            return false;

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            int2 cell = new int2(
                (int)math.round(math.lerp(from.x, to.x, t)),
                (int)math.round(math.lerp(from.y, to.y, t)));
            if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                return false;

            if (!UnitFootprintUtility.CanPlace(
                    grid,
                    walkable,
                    blockers.Blocked,
                    blockers.FriendlyPassFactionIds,
                    default,
                    cell,
                    footprintSize,
                    from,
                    factionId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryResolveVisualAnimation(
        EntityManager em,
        Entity unit,
        out Entity visualEntity,
        out float3 visualPosition,
        out MaterialAnimationIndex animationIndex,
        out MaterialAnimationData animationData)
    {
        visualEntity = Entity.Null;
        visualPosition = em.HasComponent<LocalTransform>(unit) ? em.GetComponentData<LocalTransform>(unit).Position : default;
        animationIndex = default;
        animationData = default;

        if (!em.HasComponent<UnitModelInstanceReference>(unit))
            return false;

        Entity root = em.GetComponentData<UnitModelInstanceReference>(unit).Instance;
        if (root == Entity.Null || !em.Exists(root))
            return false;

        if (em.HasComponent<LocalToWorld>(root))
            visualPosition = em.GetComponentData<LocalToWorld>(root).Position;

        return TryResolveVisualAnimationRecursive(em, root, ref visualEntity, ref animationIndex, ref animationData);
    }

    private static bool TryResolveVisualAnimationRecursive(
        EntityManager em,
        Entity entity,
        ref Entity visualEntity,
        ref MaterialAnimationIndex animationIndex,
        ref MaterialAnimationData animationData)
    {
        if (em.HasComponent<MaterialAnimationIndex>(entity) && em.HasComponent<MaterialAnimationData>(entity))
        {
            visualEntity = entity;
            animationIndex = em.GetComponentData<MaterialAnimationIndex>(entity);
            animationData = em.GetComponentData<MaterialAnimationData>(entity);
            return true;
        }

        if (!em.HasBuffer<Child>(entity))
            return false;

        DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
        for (int i = 0; i < children.Length; i++)
        {
            if (TryResolveVisualAnimationRecursive(em, children[i].Value, ref visualEntity, ref animationIndex, ref animationData))
                return true;
        }

        return false;
    }

    private static T GetSingleton<T>(EntityManager em, out Entity entity)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        entity = query.GetSingletonEntity();
        return query.GetSingleton<T>();
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

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
            return;

        if (type == LogType.Exception || type == LogType.Error)
            Debug.Log($"[RuntimeSoldierMovementContinuityProbe:ObservedLog] {condition}");
    }

    private static void Finish(string result, string detail)
    {
        if (s_finished)
            return;

        s_finished = true;
        WriteReport(result, detail);
        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
        else
            EditorApplication.Exit(result == "completed" ? 0 : 1);
    }

    private static void WriteReport(string result, string detail)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        StringBuilder json = new();
        json.AppendLine("{");
        AppendJson(json, "result", result, comma: true);
        AppendJson(json, "detail", detail, comma: true);
        AppendJson(json, "clickedDeploy", s_clickedDeploy, comma: true);
        AppendJson(json, "sampleCount", Samples.Count, comma: true);
        AppendJson(json, "movementStallFrames", s_stallFrames, comma: true);
        AppendJson(json, "visualStallFrames", s_visualStallFrames, comma: true);
        AppendJson(json, "animationRepeatFrames", s_animRepeatFrames, comma: true);
        AppendJson(json, "soldierEntityIndex", s_soldier.Index, comma: true);
        AppendJson(json, "goal", $"{s_goal.x},{s_goal.y}", comma: true);
        json.AppendLine("  \"samples\": [");
        int start = math.max(0, Samples.Count - 180);
        for (int i = start; i < Samples.Count; i++)
        {
            MovementSample sample = Samples[i];
            json.Append("    {");
            AppendJsonInline(json, "frame", sample.Frame, comma: true);
            AppendJsonInline(json, "time", sample.Time, comma: true);
            AppendJsonInline(json, "dt", sample.DeltaTime, comma: true);
            AppendJsonInline(json, "pos", Format(sample.Position), comma: true);
            AppendJsonInline(json, "visualPos", Format(sample.VisualPosition), comma: true);
            AppendJsonInline(json, "moveDelta", sample.MoveDelta, comma: true);
            AppendJsonInline(json, "visualDelta", sample.VisualDelta, comma: true);
            AppendJsonInline(json, "cell", $"{sample.Cell.x},{sample.Cell.y}", comma: true);
            AppendJsonInline(json, "hasFollow", sample.HasFollow, comma: true);
            AppendJsonInline(json, "hasRequest", sample.HasRequest, comma: true);
            AppendJsonInline(json, "pathIndex", sample.PathIndex, comma: true);
            AppendJsonInline(json, "pathLength", sample.PathLength, comma: true);
            AppendJsonInline(json, "moveVisual", sample.MoveVisual, comma: true);
            AppendJsonInline(json, "resolvedAnim", sample.ResolvedAnimation, comma: true);
            AppendJsonInline(json, "matAnim", sample.MaterialAnimation, comma: true);
            AppendJsonInline(json, "dataAnim", sample.MaterialDataAnimation, comma: true);
            AppendJsonInline(json, "matTime", sample.MaterialTime, comma: true);
            AppendJsonInline(json, "renderFrame", sample.RenderFrame, comma: true);
            AppendJsonInline(json, "nextFrame", sample.NextRenderFrame, comma: true);
            AppendJsonInline(json, "blend", sample.Blend, comma: true);
            AppendJsonInline(json, "movementStalled", sample.MovementStalled, comma: true);
            AppendJsonInline(json, "visualStalled", sample.VisualStalled, comma: true);
            AppendJsonInline(json, "animationRepeated", sample.AnimationRepeated, comma: true);
            AppendJsonInline(json, "visualEntityIndex", sample.VisualEntityIndex, comma: false);
            json.Append(i == Samples.Count - 1 ? "}\n" : "},\n");
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(OutputPath, json.ToString());
        Debug.Log($"[RuntimeSoldierMovementContinuityProbe] wrote {OutputPath} result={result} samples={Samples.Count} movementStalls={s_stallFrames} visualStalls={s_visualStallFrames} animRepeats={s_animRepeatFrames}");
    }

    private static string Format(float3 value)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:F4},{1:F4},{2:F4}", value.x, value.y, value.z);
    }

    private static void AppendJson(StringBuilder json, string key, string value, bool comma)
    {
        json.Append("  \"").Append(key).Append("\": \"").Append(Escape(value)).Append('"');
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder json, string key, bool value, bool comma)
    {
        json.Append("  \"").Append(key).Append("\": ").Append(value ? "true" : "false");
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder json, string key, int value, bool comma)
    {
        json.Append("  \"").Append(key).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture));
        json.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendJsonInline(StringBuilder json, string key, string value, bool comma)
    {
        json.Append('"').Append(key).Append("\":\"").Append(Escape(value)).Append('"');
        if (comma)
            json.Append(',');
    }

    private static void AppendJsonInline(StringBuilder json, string key, bool value, bool comma)
    {
        json.Append('"').Append(key).Append("\":").Append(value ? "true" : "false");
        if (comma)
            json.Append(',');
    }

    private static void AppendJsonInline(StringBuilder json, string key, int value, bool comma)
    {
        json.Append('"').Append(key).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
        if (comma)
            json.Append(',');
    }

    private static void AppendJsonInline(StringBuilder json, string key, float value, bool comma)
    {
        json.Append('"').Append(key).Append("\":").Append(value.ToString("R", CultureInfo.InvariantCulture));
        if (comma)
            json.Append(',');
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private struct MovementSample
    {
        public int Frame;
        public float Time;
        public float DeltaTime;
        public float3 Position;
        public float3 VisualPosition;
        public float MoveDelta;
        public float VisualDelta;
        public int2 Cell;
        public int2 Goal;
        public bool HasFollow;
        public bool HasRequest;
        public int PathIndex;
        public int PathLength;
        public byte MoveVisual;
        public byte ResolvedAnimation;
        public byte MaterialAnimation;
        public byte MaterialDataAnimation;
        public float MaterialTime;
        public float RenderFrame;
        public float NextRenderFrame;
        public float Blend;
        public bool MovementStalled;
        public bool VisualStalled;
        public bool AnimationRepeated;
        public int VisualEntityIndex;
    }
}
#endif
