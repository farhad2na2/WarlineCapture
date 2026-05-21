using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public static class Chapter01M01PlayableRuntime
{
    public const string PlayerSquadEntityId = "unit.player.rifle_squad_01";
    public const string EnemyPatrolEntityId = "unit.enemy.patrol_01";
    public const string PlayerSpawnAnchorId = "player_spawn.command_squad";
    public const string EnemySpawnAnchorId = "enemy_spawn.patrol_start";
    public const string CameraStartAnchorId = "camera.default_start";
    public const string ObjectiveAnchorId = "objective.destroy_patrol_group";
    public const string DecorCommandPointEntityId = Chapter01M01SpritePresenterCatalog.DecorCommandPointEntityId;
    public const string ObjectiveId = "destroy_patrol";
    public const string PatrolRouteId = "route.enemy_patrol_01";
    private const string PatrolRouteAnchorPrefix = "route.enemy_patrol_01.";
    private const float M01InfantryRunSpeedWorldUnitsPerSecond = 0.42f;
    private const float M01InfantryWalkSpeedWorldUnitsPerSecond = 0.28f;

    public readonly struct RuntimeState
    {
        public readonly bool Initialized;
        public readonly Entity PlayerSquad;
        public readonly Entity EnemyPatrol;
        public readonly Entity CommandPoint;
        public readonly Vector3 PlayerSpawnWorld;
        public readonly Vector3 EnemySpawnWorld;
        public readonly Vector3 CommandPointWorld;
        public readonly Vector3 ObjectiveWorld;
        public readonly Vector3 CameraStartWorld;

        public RuntimeState(
            bool initialized,
            Entity playerSquad,
            Entity enemyPatrol,
            Entity commandPoint,
            Vector3 playerSpawnWorld,
            Vector3 enemySpawnWorld,
            Vector3 commandPointWorld,
            Vector3 objectiveWorld,
            Vector3 cameraStartWorld)
        {
            Initialized = initialized;
            PlayerSquad = playerSquad;
            EnemyPatrol = enemyPatrol;
            CommandPoint = commandPoint;
            PlayerSpawnWorld = playerSpawnWorld;
            EnemySpawnWorld = enemySpawnWorld;
            CommandPointWorld = commandPointWorld;
            ObjectiveWorld = objectiveWorld;
            CameraStartWorld = cameraStartWorld;
        }
    }

    public readonly struct Evaluation
    {
        public readonly bool IsActiveMission;
        public readonly bool CommandSquadAlive;
        public readonly bool PatrolDestroyed;
        public readonly bool ObjectiveComplete;
        public readonly bool ResultRouteReady;

        public Evaluation(
            bool isActiveMission,
            bool commandSquadAlive,
            bool patrolDestroyed,
            bool objectiveComplete,
            bool resultRouteReady)
        {
            IsActiveMission = isActiveMission;
            CommandSquadAlive = commandSquadAlive;
            PatrolDestroyed = patrolDestroyed;
            ObjectiveComplete = objectiveComplete;
            ResultRouteReady = resultRouteReady;
        }
    }

    public static bool IsActiveMission()
    {
        return WarlineCaptureMissionSession.HasActiveMission &&
            WarlineCaptureMissionSession.ActiveMissionId == ChapterOneMissionCatalog.FirstContactMissionId;
    }

    public static bool TryInitializeActiveMission(World world, TacticalMapRuntimeLoader loader, out RuntimeState runtimeState)
    {
        runtimeState = default;
        if (!IsActiveMission() || world == null || !world.IsCreated || loader == null || loader.Definition == null)
            return false;

        TacticalMapDefinition definition = loader.Definition;
        if (definition.MissionId != WarlineCaptureMissionSession.ActiveMissionId ||
            definition.ScenarioSetupId != WarlineCaptureMissionSession.ActiveScenarioSetupId ||
            definition.LevelId != WarlineCaptureMissionSession.ActiveLevelId ||
            definition.MapId != WarlineCaptureMissionSession.ActiveIsoMapId)
        {
            return false;
        }

        if (!loader.TryGetAnchorWorldPosition(PlayerSpawnAnchorId, out Vector3 playerSpawnWorld) ||
            !loader.TryGetAnchorWorldPosition(EnemySpawnAnchorId, out Vector3 enemySpawnWorld) ||
            !loader.TryGetAnchorWorldPosition(ObjectiveAnchorId, out Vector3 objectiveWorld) ||
            !loader.TryGetAnchorWorldPosition(CameraStartAnchorId, out Vector3 cameraStartWorld))
        {
            return false;
        }

        bool hasCommandPoint = false;
        Vector3 commandPointWorld = default;
        EntityManager em = world.EntityManager;
        Entity player = ResolveOrCreateMissionUnit(
            em,
            loader,
            PlayerSquadEntityId,
            PlayerSpawnAnchorId,
            playerSpawnWorld,
            0,
            "Rifle Squad",
            createFallback: true);
        Entity enemy = ResolveOrCreateMissionUnit(
            em,
            loader,
            EnemyPatrolEntityId,
            EnemySpawnAnchorId,
            enemySpawnWorld,
            1,
            "Hostile Patrol",
            createFallback: true);

        if (player == Entity.Null || enemy == Entity.Null)
            return false;

        BindMissionIdentity(em, player, PlayerSquadEntityId, PlayerSquadEntityId, isPlayer: true);
        BindMissionIdentity(em, enemy, EnemyPatrolEntityId, ObjectiveId, isPlayer: false);
        ApplyPatrolRoute(em, loader, enemy);

        Entity commandPoint = hasCommandPoint
            ? ResolveOrCreateMissionDecor(em, loader, DecorCommandPointEntityId, commandPointWorld)
            : Entity.Null;

        runtimeState = new RuntimeState(true, player, enemy, commandPoint, playerSpawnWorld, enemySpawnWorld, commandPointWorld, objectiveWorld, cameraStartWorld);
        return true;
    }

    public static bool TryEvaluateActiveMission(World world, out Evaluation evaluation)
    {
        evaluation = default;
        if (!IsActiveMission() || world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        Entity player = FindMissionEntity(em, PlayerSquadEntityId);
        Entity enemy = FindMissionEntity(em, EnemyPatrolEntityId);
        bool playerAlive = IsAlive(em, player);
        bool patrolDestroyed = enemy == Entity.Null || !IsAlive(em, enemy);
        bool objectiveComplete = playerAlive && patrolDestroyed;
        bool resultRouteReady = objectiveComplete && WarlineCaptureMissionSession.BuildCurrentResult(GameRuntimeStats.GetSnapshot()).Victory;

        evaluation = new Evaluation(true, playerAlive, patrolDestroyed, objectiveComplete, resultRouteReady);
        return true;
    }

    public static bool ShouldStartResultFlow(World world)
    {
        return TryEvaluateActiveMission(world, out Evaluation evaluation) && evaluation.ResultRouteReady;
    }

    public static bool TryGetCameraStartWorld(TacticalMapRuntimeLoader loader, out Vector3 cameraStartWorld)
    {
        if (!IsActiveMission() || loader == null || loader.Definition == null)
        {
            cameraStartWorld = default;
            return false;
        }

        return loader.TryGetAnchorWorldPosition(CameraStartAnchorId, out cameraStartWorld);
    }

    private static Entity ResolveOrCreateMissionUnit(
        EntityManager em,
        TacticalMapRuntimeLoader loader,
        string entityId,
        string anchorId,
        Vector3 fallbackWorld,
        byte factionId,
        string displayName,
        bool createFallback)
    {
        int2 cell = ResolveAnchorCell(loader, anchorId, fallbackWorld);
        Entity existingById = FindMissionEntity(em, entityId);
        if (existingById != Entity.Null)
        {
            ApplyMissionUnitPlacement(em, existingById, fallbackWorld, cell, factionId);
            return existingById;
        }

        Entity candidate = FindNearestFactionUnit(em, factionId, fallbackWorld);
        if (candidate != Entity.Null)
        {
            ApplyMissionUnitPlacement(em, candidate, fallbackWorld, cell, factionId);
            return candidate;
        }

        if (!createFallback)
            return Entity.Null;

        return CreateFallbackMissionUnit(em, fallbackWorld, cell, factionId, displayName);
    }

    private static void BindMissionIdentity(EntityManager em, Entity entity, string entityId, string objectiveId, bool isPlayer)
    {
        SetComponent(em, entity, new MissionRuntimeEntityId { Value = new FixedString64Bytes(entityId) });
        SetComponent(em, entity, new MissionRuntimeObjectiveTarget { ObjectiveId = new FixedString64Bytes(objectiveId) });

        if (isPlayer)
        {
            if (!em.HasComponent<MissionRuntimeCommandSquadTag>(entity))
                em.AddComponent<MissionRuntimeCommandSquadTag>(entity);
            if (em.HasComponent<MissionRuntimeEnemyPatrolTag>(entity))
                em.RemoveComponent<MissionRuntimeEnemyPatrolTag>(entity);
        }
        else
        {
            if (!em.HasComponent<MissionRuntimeEnemyPatrolTag>(entity))
                em.AddComponent<MissionRuntimeEnemyPatrolTag>(entity);
            if (em.HasComponent<MissionRuntimeCommandSquadTag>(entity))
                em.RemoveComponent<MissionRuntimeCommandSquadTag>(entity);
            if (!em.HasComponent<MissionRuntimeOpeningControlProtection>(entity))
                em.AddComponent<MissionRuntimeOpeningControlProtection>(entity);
            if (em.HasComponent<UnitCombat>(entity))
            {
                UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                combat.CanAttack = 0;
                combat.AutoEngage = 0;
                em.SetComponentData(entity, combat);
            }
        }

        ApplyM01TacticalScaleAttackTrace(em, entity);
        ApplyM01InfantryMovementContract(em, entity);
        BindMissionSpritePresenter(em, entity, entityId);
    }

    private static void ApplyM01TacticalScaleAttackTrace(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<UnitAttack>(entity))
            return;

        UnitAttack attack = em.GetComponentData<UnitAttack>(entity);
        attack.TraceWidth = math.clamp(attack.TraceWidth, 0.012f, 0.035f);
        attack.TraceVisibleSeconds = math.clamp(attack.TraceVisibleSeconds, 0.05f, 0.16f);
        attack.TraceDashDensity = math.max(8f, attack.TraceDashDensity);
        em.SetComponentData(entity, attack);
    }

    private static Entity ResolveOrCreateMissionDecor(EntityManager em, TacticalMapRuntimeLoader loader, string entityId, Vector3 worldPosition)
    {
        Entity existingById = FindMissionEntity(em, entityId);
        if (existingById != Entity.Null)
        {
            int2 existingCell = ResolveAnchorCell(loader, entityId, worldPosition);
            SetComponent(em, existingById, new UnitGrid { Cell = existingCell });
            SetComponent(em, existingById, new UnitFootprint { Size = new int2(4, 3) });
            SetComponent(em, existingById, LocalTransform.FromPosition(worldPosition));
            if (em.HasComponent<LocalToWorld>(existingById))
                em.SetComponentData(existingById, new LocalToWorld { Value = float4x4.Translate(worldPosition) });
            BindMissionSpritePresenter(em, existingById, entityId);
            return existingById;
        }

        int2 cell = ResolveAnchorCell(loader, entityId, worldPosition);
        Entity entity = em.CreateEntity(
            typeof(MissionRuntimeEntityId),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(LocalTransform));
        em.SetComponentData(entity, new MissionRuntimeEntityId { Value = new FixedString64Bytes(entityId) });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(4, 3) });
        em.SetComponentData(entity, LocalTransform.FromPosition(worldPosition));
        BindMissionSpritePresenter(em, entity, entityId);
        return entity;
    }

    private static void BindMissionSpritePresenter(EntityManager em, Entity entity, string entityId)
    {
        if (!Chapter01M01SpritePresenterCatalog.TryCreatePresenter(entityId, out MissionRuntimeSpritePresenter presenter))
            return;

        SetComponent(em, entity, presenter);
        if (!em.HasComponent<MissionRuntimeSpritePresenterSuppressesLegacyModelTag>(entity))
            em.AddComponent<MissionRuntimeSpritePresenterSuppressesLegacyModelTag>(entity);
        if (em.HasComponent<UnitDestroyedVisualReference>(entity))
            em.RemoveComponent<UnitDestroyedVisualReference>(entity);
        if (em.HasComponent<UnitDestroyedVisualInitialized>(entity))
            em.RemoveComponent<UnitDestroyedVisualInitialized>(entity);
    }

    private static void ApplyPatrolRoute(EntityManager em, TacticalMapRuntimeLoader loader, Entity enemy)
    {
        if (enemy == Entity.Null || !em.Exists(enemy))
            return;

        int2 a = ResolveRouteCell(loader, "a", EnemySpawnAnchorId);
        int2 b = ResolveRouteCell(loader, "b", EnemySpawnAnchorId);
        int2 c = ResolveRouteCell(loader, "c", EnemySpawnAnchorId);
        SetComponent(em, enemy, new MissionRuntimePatrolRoute
        {
            WaypointA = a,
            WaypointB = b,
            WaypointC = c,
            WaypointCount = 3,
            CurrentWaypointIndex = 1,
            HoldAtEnd = 1
        });

        if (em.HasComponent<UnitTarget>(enemy))
            em.RemoveComponent<UnitTarget>(enemy);
        if (em.HasComponent<UnitPathRequest>(enemy))
            em.RemoveComponent<UnitPathRequest>(enemy);
        if (em.HasComponent<UnitPathFollow>(enemy))
            em.RemoveComponent<UnitPathFollow>(enemy);
        if (em.HasComponent<UnitPathRange>(enemy))
            em.RemoveComponent<UnitPathRange>(enemy);
    }

    private static int2 ResolveAnchorCell(TacticalMapRuntimeLoader loader, string anchorId, Vector3 fallbackWorld)
    {
        if (loader != null && loader.TryGetAnchorCell(anchorId, out Vector2Int anchorCell))
            return new int2(anchorCell.x, anchorCell.y);

        return new int2(Mathf.RoundToInt(fallbackWorld.x), Mathf.RoundToInt(fallbackWorld.z));
    }

    private static int2 ResolveRouteCell(TacticalMapRuntimeLoader loader, string suffix, string fallbackAnchor)
    {
        string anchorId = PatrolRouteAnchorPrefix + suffix;
        if (loader.TryGetAnchorCell(anchorId, out Vector2Int cell) || loader.TryGetAnchorCell(fallbackAnchor, out cell))
            return new int2(cell.x, cell.y);

        return int2.zero;
    }

    private static Entity CreateFallbackMissionUnit(EntityManager em, Vector3 worldPosition, int2 cell, byte factionId, string displayName)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitFootprint),
            typeof(UnitMovementBehavior),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitAttackState),
            typeof(UnitDisplayInfo),
            typeof(UnitAnimationSettings),
            typeof(UnitPrevWorldPos),
            typeof(UnitMoveVisualState),
            typeof(LocalTransform));

        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitMove
        {
            Speed = M01InfantryRunSpeedWorldUnitsPerSecond,
            WalkSpeed = M01InfantryWalkSpeedWorldUnitsPerSecond,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new UnitCombat { AggroRangeCells = 7, ChaseBreakDistance = 1.5f, CanAttack = 1, AutoEngage = 1 });
        em.SetComponentData(entity, new UnitAttack
        {
            Range = 0.35f,
            CooldownSeconds = 0.75f,
            Damage = 15,
            TraceColor = factionId == 0 ? new float4(0.35f, 0.8f, 1f, 1f) : new float4(1f, 0.25f, 0.2f, 1f),
            TraceWidth = 0.015f,
            TraceScrollSpeed = 1f,
            TraceDashDensity = 8f,
            TraceVisibleSeconds = 0.12f
        });
        em.SetComponentData(entity, new UnitAttackState { CooldownRemaining = 0f });
        em.SetComponentData(entity, new UnitDisplayInfo
        {
            Name = new FixedString64Bytes(displayName),
            Description = new FixedString128Bytes("Chapter 1 mission runtime unit")
        });
        em.SetComponentData(entity, new UnitAnimationSettings
        {
            IdleDelayMinSeconds = 2f,
            IdleDelayMaxSeconds = 5f,
            IdleWanderDistanceMin = 0f,
            IdleWanderDistanceMax = 0f,
            AttackAnimationSeconds = 0.2f,
            DeathAnimationSeconds = 0.4f
        });
        em.SetComponentData(entity, new UnitPrevWorldPos { Value = worldPosition });
        em.SetComponentData(entity, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });
        em.SetComponentData(entity, LocalTransform.FromPosition(worldPosition));
        return entity;
    }

    private static void ApplyMissionUnitPlacement(EntityManager em, Entity entity, Vector3 worldPosition, int2 cell, byte factionId)
    {
        SetComponent(em, entity, new Faction { Id = factionId });
        SetComponent(em, entity, new UnitGrid { Cell = cell });
        SetComponent(em, entity, LocalTransform.FromPosition(worldPosition));
        if (em.HasComponent<LocalToWorld>(entity))
            em.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(worldPosition) });
        SetComponent(em, entity, new UnitPrevWorldPos { Value = worldPosition });
        if (em.HasComponent<UnitHealth>(entity))
        {
            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            if (health.Max <= 0)
                health.Max = 100;
            if (health.Current <= 0)
                health.Current = health.Max;
            em.SetComponentData(entity, health);
        }
        else
        {
            em.AddComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        }

        if (em.HasComponent<UnitCombat>(entity))
        {
            UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
            combat.CanAttack = 1;
            em.SetComponentData(entity, combat);
        }
    }

    private static void ApplyM01InfantryMovementContract(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<UnitMove>(entity))
            return;

        UnitMove move = em.GetComponentData<UnitMove>(entity);
        move.Speed = M01InfantryRunSpeedWorldUnitsPerSecond;
        move.WalkSpeed = M01InfantryWalkSpeedWorldUnitsPerSecond;
        move.RoadSpeedMultiplier = 1f;
        move.ArriveDistance = math.clamp(move.ArriveDistance, 0.035f, 0.08f);
        em.SetComponentData(entity, move);
    }

    private static Entity FindMissionEntity(EntityManager em, string entityId)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissionRuntimeEntityId>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity))
                continue;

            MissionRuntimeEntityId id = em.GetComponentData<MissionRuntimeEntityId>(entity);
            if (id.Value.ToString() == entityId)
                return entity;
        }

        return Entity.Null;
    }

    private static Entity FindNearestFactionUnit(EntityManager em, byte factionId, Vector3 targetWorld)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitCombat>(),
            ComponentType.ReadOnly<UnitAttack>(),
            ComponentType.ReadOnly<LocalTransform>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

        Entity best = Entity.Null;
        float bestDistanceSq = float.MaxValue;
        float3 target = targetWorld;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity))
                continue;
            if (em.HasComponent<MissionRuntimeEntityId>(entity))
                continue;
            if (em.GetComponentData<Faction>(entity).Id != factionId)
                continue;
            if (em.GetComponentData<UnitHealth>(entity).Current <= 0)
                continue;

            float distanceSq = math.distancesq(em.GetComponentData<LocalTransform>(entity).Position, target);
            if (distanceSq >= bestDistanceSq)
                continue;

            best = entity;
            bestDistanceSq = distanceSq;
        }

        return best;
    }

    private static bool HasPendingInitialUnitSpawn(EntityManager em)
    {
        using EntityQuery pendingQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
            ComponentType.Exclude<InitialUnitsSpawnInitialized>());
        return !pendingQuery.IsEmptyIgnoreFilter;
    }

    private static bool IsAlive(EntityManager em, Entity entity)
    {
        return entity != Entity.Null &&
            em.Exists(entity) &&
            em.HasComponent<UnitHealth>(entity) &&
            em.GetComponentData<UnitHealth>(entity).Current > 0;
    }

    private static void SetComponent<T>(EntityManager em, Entity entity, T value) where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            em.SetComponentData(entity, value);
        else
            em.AddComponentData(entity, value);
    }
}
