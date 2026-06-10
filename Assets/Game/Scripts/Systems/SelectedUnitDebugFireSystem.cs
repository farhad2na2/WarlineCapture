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
            IsCommanded = 0
        };

        if (em.HasComponent<EngageTarget>(entity))
            em.SetComponentData(entity, debugEngage);
        else
            em.AddComponentData(entity, debugEngage);
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
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(target, new DebugFireTargetTag { Source = source });
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
            distance = ResolveMissileLauncherDebugDistance(em.GetComponentData<GroundMissileLauncherComponent>(source), grid, minDistance, maxDistance, distance);

        return sourceTransform.Position + forward * distance;
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

        return math.clamp(math.max(fallbackDistance, missileMin), missileMin, missileMax);
    }

    private static int2 ResolveCell(GridConfig grid, float3 position)
    {
        int2 cell = GridUtils.WorldToCell(grid, position);
        return new int2(
            math.clamp(cell.x, 0, math.max(0, grid.Width - 1)),
            math.clamp(cell.y, 0, math.max(0, grid.Height - 1)));
    }
}
