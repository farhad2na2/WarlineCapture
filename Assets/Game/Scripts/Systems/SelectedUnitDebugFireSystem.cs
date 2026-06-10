using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.InputSystem;

[UpdateBefore(typeof(UnitAttackSystem))]
public partial struct SelectedUnitDebugFireSystem : ISystem
{
    private const int DebugTargetHealth = 1_000_000_000;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete();
        ApplyDebugFire(state.EntityManager, SystemAPI.GetSingleton<GridConfig>(), IsDebugFireHeld());
    }

    public static void ApplyDebugFire(EntityManager em, GridConfig grid, bool fireHeld)
    {
        CleanupInactiveSources(em, fireHeld);
        CleanupOrphanTargets(em);

        if (!fireHeld)
            return;

        using EntityQuery selectedQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitCombat>(),
            ComponentType.ReadOnly<UnitAttack>(),
            ComponentType.ReadOnly<UnitAttackCooldownComponent>(),
            ComponentType.ReadOnly<UnitAttackTraceComponent>(),
            ComponentType.ReadOnly<UnitAttackAnimationComponent>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<LocalTransform>());

        using NativeArray<Entity> selected = selectedQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < selected.Length; i++)
            EnsureDebugFireForSelectedUnit(em, grid, selected[i]);
    }

    private static bool IsDebugFireHeld()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.fKey.isPressed;
#else
        return false;
#endif
    }

    private static void EnsureDebugFireForSelectedUnit(EntityManager em, GridConfig grid, Entity entity)
    {
        if (!IsEligibleDebugFireSource(em, entity))
            return;

        UnitAttack attack = em.GetComponentData<UnitAttack>(entity);
        LocalTransform sourceTransform = em.GetComponentData<LocalTransform>(entity);
        Entity target = EnsureDebugTarget(em, entity);
        float3 targetPosition = ResolveTargetPosition(em, grid, entity, sourceTransform, attack);
        int2 targetCell = ResolveCell(grid, targetPosition);

        em.SetComponentData(target, LocalTransform.FromPosition(targetPosition));
        em.SetComponentData(target, new UnitHealth { Current = DebugTargetHealth, Max = DebugTargetHealth });

        EngageTarget debugEngage = new()
        {
            Target = target,
            Cell = targetCell,
            Position = targetPosition,
            IsCommanded = 1
        };

        if (em.HasComponent<EngageTarget>(entity))
            em.SetComponentData(entity, debugEngage);
        else
            em.AddComponentData(entity, debugEngage);

        TryArmGroundMissileLauncherDebugFire(em, entity, target, targetCell, targetPosition);
    }

    private static void TryArmGroundMissileLauncherDebugFire(
        EntityManager em,
        Entity launcherEntity,
        Entity target,
        int2 targetCell,
        float3 targetPosition)
    {
        if (!em.HasComponent<GroundMissileLauncherComponent>(launcherEntity) ||
            !em.HasComponent<GroundMissileLauncherStateComponent>(launcherEntity))
        {
            return;
        }

        GroundMissileLauncherComponent launcher = em.GetComponentData<GroundMissileLauncherComponent>(launcherEntity);
        GroundMissileLauncherStateComponent launcherState = em.GetComponentData<GroundMissileLauncherStateComponent>(launcherEntity);
        if (launcherState.Phase != (byte)GroundMissileLauncherPhase.Idle)
            return;

        int rocketCount = em.HasBuffer<GroundMissileLauncherRocketVisualComponent>(launcherEntity)
            ? em.GetBuffer<GroundMissileLauncherRocketVisualComponent>(launcherEntity).Length
            : 0;
        int nextRocketSlot = rocketCount > 0
            ? (launcherState.SelectedRocketSlot + 1 + rocketCount) % rocketCount
            : -1;

        launcherState.Phase = (byte)GroundMissileLauncherPhase.Preparing;
        launcherState.TargetEntity = target;
        launcherState.TargetCell = targetCell;
        launcherState.TargetWorldPosition = targetPosition;
        launcherState.Timer = GroundMissileLauncherTiming.PrepareAndHoldSeconds(launcher.PrepareSeconds);
        launcherState.SelectedRocketSlot = nextRocketSlot;
        em.SetComponentData(launcherEntity, launcherState);

        if (em.HasComponent<UnitAttackCooldownComponent>(launcherEntity))
        {
            em.SetComponentData(launcherEntity, new UnitAttackCooldownComponent
            {
                CooldownRemaining = math.max(
                    0.01f,
                    GroundMissileLauncherTiming.FullAttackCycleSeconds(launcher.PrepareSeconds, launcher.ReloadSeconds))
            });
        }

        if (em.HasComponent<UnitAttackTraceComponent>(launcherEntity))
        {
            UnitAttackTraceComponent trace = em.GetComponentData<UnitAttackTraceComponent>(launcherEntity);
            trace.TimeRemaining = 0f;
            em.SetComponentData(launcherEntity, trace);
        }

        if (em.HasComponent<UnitAttackAnimationComponent>(launcherEntity))
        {
            float attackAnimationSeconds = em.HasComponent<UnitAnimationSettings>(launcherEntity)
                ? math.max(0.01f, em.GetComponentData<UnitAnimationSettings>(launcherEntity).AttackAnimationSeconds)
                : 0.25f;
            em.SetComponentData(launcherEntity, new UnitAttackAnimationComponent
            {
                TimeRemaining = attackAnimationSeconds
            });
        }
    }

    private static Entity EnsureDebugTarget(EntityManager em, Entity source)
    {
        if (em.HasComponent<SelectedUnitDebugFireState>(source))
        {
            SelectedUnitDebugFireState state = em.GetComponentData<SelectedUnitDebugFireState>(source);
            if (em.Exists(state.Target) && em.HasComponent<DebugFireTargetTag>(state.Target))
                return state.Target;

            state.Target = CreateDebugTarget(em, source);
            em.SetComponentData(source, state);
            return state.Target;
        }

        SelectedUnitDebugFireState newState = default;
        if (em.HasComponent<EngageTarget>(source))
        {
            EngageTarget previous = em.GetComponentData<EngageTarget>(source);
            newState.PreviousTarget = previous.Target;
            newState.PreviousCell = previous.Cell;
            newState.PreviousPosition = previous.Position;
            newState.PreviousIsCommanded = previous.IsCommanded;
            newState.HadPreviousTarget = 1;
        }

        newState.Target = CreateDebugTarget(em, source);
        em.AddComponentData(source, newState);
        return newState.Target;
    }

    private static Entity CreateDebugTarget(EntityManager em, Entity source)
    {
        Entity target = em.CreateEntity(
            typeof(DebugFireTargetTag),
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(target, new DebugFireTargetTag { Source = source });
        em.SetComponentData(target, new Faction { Id = FactionIdentitySystem.EnemyFactionId });
        em.SetComponentData(target, new UnitHealth { Current = DebugTargetHealth, Max = DebugTargetHealth });
        em.SetComponentData(target, LocalTransform.Identity);
        return target;
    }

    private static void CleanupInactiveSources(EntityManager em, bool fireHeld)
    {
        using EntityQuery activeQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitDebugFireState>());
        using NativeArray<Entity> activeSources = activeQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < activeSources.Length; i++)
        {
            Entity source = activeSources[i];
            if (fireHeld && IsEligibleDebugFireSource(em, source))
                continue;

            CleanupSource(em, source);
        }
    }

    private static void CleanupSource(EntityManager em, Entity source)
    {
        if (!em.Exists(source) || !em.HasComponent<SelectedUnitDebugFireState>(source))
            return;

        SelectedUnitDebugFireState state = em.GetComponentData<SelectedUnitDebugFireState>(source);
        if (em.Exists(state.Target))
            em.DestroyEntity(state.Target);

        if (state.HadPreviousTarget != 0 && IsRestorableTarget(em, state.PreviousTarget))
        {
            EngageTarget restored = new()
            {
                Target = state.PreviousTarget,
                Cell = state.PreviousCell,
                Position = state.PreviousPosition,
                IsCommanded = state.PreviousIsCommanded
            };

            if (em.HasComponent<EngageTarget>(source))
                em.SetComponentData(source, restored);
            else
                em.AddComponentData(source, restored);
        }
        else if (em.HasComponent<EngageTarget>(source))
        {
            em.RemoveComponent<EngageTarget>(source);
        }

        em.RemoveComponent<SelectedUnitDebugFireState>(source);
    }

    private static void CleanupOrphanTargets(EntityManager em)
    {
        using EntityQuery targetQuery = em.CreateEntityQuery(ComponentType.ReadOnly<DebugFireTargetTag>());
        using NativeArray<Entity> targets = targetQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < targets.Length; i++)
        {
            Entity target = targets[i];
            DebugFireTargetTag tag = em.GetComponentData<DebugFireTargetTag>(target);
            if (!em.Exists(tag.Source) ||
                !em.HasComponent<SelectedUnitDebugFireState>(tag.Source) ||
                em.GetComponentData<SelectedUnitDebugFireState>(tag.Source).Target != target)
            {
                em.DestroyEntity(target);
            }
        }
    }

    private static bool IsEligibleDebugFireSource(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) ||
            !em.HasComponent<SelectedUnitTag>(entity) ||
            !em.HasComponent<UnitCombat>(entity) ||
            !em.HasComponent<UnitAttack>(entity) ||
            !em.HasComponent<UnitAttackCooldownComponent>(entity) ||
            !em.HasComponent<UnitAttackTraceComponent>(entity) ||
            !em.HasComponent<UnitAttackAnimationComponent>(entity) ||
            !em.HasComponent<UnitHealth>(entity) ||
            !em.HasComponent<LocalTransform>(entity) ||
            em.HasComponent<StaticGridBlocker>(entity) ||
            em.HasComponent<UnitDeathAnimationComponent>(entity))
        {
            return false;
        }

        UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
        UnitAttack attack = em.GetComponentData<UnitAttack>(entity);
        UnitHealth health = em.GetComponentData<UnitHealth>(entity);
        return combat.CanAttack != 0 && attack.Range > 0f && health.Current > 0;
    }

    private static bool IsRestorableTarget(EntityManager em, Entity target)
    {
        return target != Entity.Null &&
               em.Exists(target) &&
               em.HasComponent<LocalTransform>(target) &&
               (!em.HasComponent<UnitHealth>(target) || em.GetComponentData<UnitHealth>(target).Current > 0);
    }

    private static float3 ResolveTargetPosition(
        EntityManager em,
        GridConfig grid,
        Entity source,
        LocalTransform sourceTransform,
        UnitAttack attack)
    {
        float3 forward = math.rotate(sourceTransform.Rotation, new float3(0f, 0f, 1f));
        forward.y = 0f;
        forward = math.normalizesafe(forward, new float3(0f, 0f, 1f));

        float minDistance = math.max(0.25f, grid.CellSize);
        float maxDistance = math.max(minDistance, attack.Range * 0.95f);
        float distance = math.clamp(attack.Range * 0.85f, minDistance, maxDistance);
        if (em.HasComponent<GroundMissileLauncherComponent>(source))
        {
            if (TryResolveEnemyBaseDebugTargetPosition(em, grid, source, sourceTransform.Position, out float3 enemyBasePosition))
                return enemyBasePosition;

            distance = ResolveMissileLauncherDebugDistance(em.GetComponentData<GroundMissileLauncherComponent>(source), grid, minDistance, maxDistance, distance);
        }

        return sourceTransform.Position + forward * distance;
    }

    private static bool TryResolveEnemyBaseDebugTargetPosition(
        EntityManager em,
        GridConfig grid,
        Entity source,
        float3 sourcePosition,
        out float3 targetPosition)
    {
        targetPosition = default;
        byte sourceFaction = em.HasComponent<Faction>(source)
            ? em.GetComponentData<Faction>(source).Id
            : FactionIdentitySystem.PlayerFactionId;
        Entity bestEntity = Entity.Null;
        float bestScore = float.NegativeInfinity;

        using EntityQuery buildingQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
            ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<LocalTransform>());
        using NativeArray<Entity> buildings = buildingQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < buildings.Length; i++)
        {
            Entity candidate = buildings[i];
            Faction faction = em.GetComponentData<Faction>(candidate);
            if (faction.Id == sourceFaction || FactionIdentitySystem.IsNeutral(faction.Id))
                continue;

            UnitHealth health = em.GetComponentData<UnitHealth>(candidate);
            if (health.Current <= 0)
                continue;

            RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(candidate);
            LocalTransform transform = em.GetComponentData<LocalTransform>(candidate);
            float score = ScoreEnemyBuildingDebugTarget(em, candidate, info, sourcePosition, transform.Position);
            if (score <= bestScore)
                continue;

            bestEntity = candidate;
            bestScore = score;
            targetPosition = transform.Position;
        }

        if (bestEntity != Entity.Null)
            return true;

        targetPosition = ResolveEnemySideFallbackPosition(grid, sourcePosition, sourceFaction);
        return true;
    }

    private static float ScoreEnemyBuildingDebugTarget(
        EntityManager em,
        Entity candidate,
        RuntimeBuildingCombatInfo info,
        float3 sourcePosition,
        float3 candidatePosition)
    {
        float score = info.IsWall == 0 && info.IsGate == 0 ? 10000f : 0f;
        int footprintArea = math.max(1, info.FootprintCells.x) * math.max(1, info.FootprintCells.y);
        score += math.min(footprintArea, 4096);
        if (HasBaseLikeName(em, candidate))
            score += 100000f;

        float3 delta = candidatePosition - sourcePosition;
        delta.y = 0f;
        score += math.sqrt(math.lengthsq(delta)) * 0.01f;
        return score;
    }

    private static bool HasBaseLikeName(EntityManager em, Entity candidate)
    {
        if (em.HasComponent<UnitSourcePrefabKey>(candidate))
        {
            string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(candidate).Value.ToString();
            if (ContainsBaseLikeToken(sourceKey))
                return true;
        }

        if (em.HasComponent<UnitDisplayInfo>(candidate))
        {
            string displayName = em.GetComponentData<UnitDisplayInfo>(candidate).Name.ToString();
            if (ContainsBaseLikeToken(displayName))
                return true;
        }

        return false;
    }

    private static bool ContainsBaseLikeToken(string value)
    {
        return !string.IsNullOrEmpty(value) &&
               (value.IndexOf("base", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("command", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("hq", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("headquarters", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("core", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static float3 ResolveEnemySideFallbackPosition(GridConfig grid, float3 sourcePosition, byte sourceFaction)
    {
        float xFactor = FactionIdentitySystem.IsPlayerControlled(sourceFaction) ? 0.78f : 0.22f;
        float zFactor = 0.5f;
        float3 position = new(
            grid.Origin.x + math.max(1, grid.Width - 1) * grid.CellSize * xFactor,
            sourcePosition.y,
            grid.Origin.z + math.max(1, grid.Height - 1) * grid.CellSize * zFactor);
        return position;
    }

    private static float ResolveMissileLauncherDebugDistance(
        GroundMissileLauncherComponent launcher,
        GridConfig grid,
        float minDistance,
        float maxDistance,
        float fallbackDistance)
    {
        float missileMin = math.max(minDistance, launcher.MinRange + math.max(0.1f, grid.CellSize));
        float missileMax = launcher.MaxRange > 0f
            ? math.min(maxDistance, launcher.MaxRange * 0.95f)
            : maxDistance;
        if (missileMin > missileMax)
            return fallbackDistance;

        float visibleDebugDistance = missileMin + math.max(grid.CellSize * 6f, launcher.DamageRadius * 1.5f);
        return math.clamp(visibleDebugDistance, missileMin, missileMax);
    }

    private static int2 ResolveCell(GridConfig grid, float3 position)
    {
        int2 cell = GridUtils.WorldToCell(grid, position);
        return new int2(
            math.clamp(cell.x, 0, math.max(0, grid.Width - 1)),
            math.clamp(cell.y, 0, math.max(0, grid.Height - 1)));
    }
}
