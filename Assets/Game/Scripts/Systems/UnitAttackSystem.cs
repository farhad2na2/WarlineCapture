using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(UnitEngagedMovementSystem))]
public partial struct UnitAttackSystem : ISystem
{
    private const float DamageHealthBarVisibleSeconds = 2f;
    private const int FleeCellsMin = 12;
    private const int FleeCellsMax = 24;
    private const int InitialAttackScratchCapacity = 4096;

    private struct PendingAttack
    {
        public Entity Attacker;
        public Entity Target;
        public int Damage;
        public int2 AttackerCell;
        public float3 AttackerPosition;
    }

    private struct AggregatedTargetEffect
    {
        public int TotalDamage;
        public Entity Attacker;
        public int2 AttackerCell;
        public float3 AttackerPosition;
    }

    private NativeParallelHashMap<Entity, int> _predictedHealth;
    private NativeParallelHashMap<Entity, AggregatedTargetEffect> _aggregatedEffects;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<UnitCombat>();
        state.RequireForUpdate<UnitAttack>();
        state.RequireForUpdate<UnitAttackCooldownComponent>();
        _predictedHealth = new NativeParallelHashMap<Entity, int>(InitialAttackScratchCapacity, Allocator.Persistent);
        _aggregatedEffects = new NativeParallelHashMap<Entity, AggregatedTargetEffect>(InitialAttackScratchCapacity, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        if (_predictedHealth.IsCreated)
            _predictedHealth.Dispose();
        if (_aggregatedEffects.IsCreated)
            _aggregatedEffects.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingleton<GridConfig>();
        var em = state.EntityManager;
        var footprintLookup = SystemAPI.GetComponentLookup<UnitFootprint>(true);
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        if (!_predictedHealth.IsCreated)
            _predictedHealth = new NativeParallelHashMap<Entity, int>(InitialAttackScratchCapacity, Allocator.Persistent);
        if (!_aggregatedEffects.IsCreated)
            _aggregatedEffects = new NativeParallelHashMap<Entity, AggregatedTargetEffect>(InitialAttackScratchCapacity, Allocator.Persistent);
        _predictedHealth.Clear();
        _aggregatedEffects.Clear();
        foreach (var (engage, attackState, attackTraceState, attackAnimationState, selfTransform, attack, selfHealth, entity) in SystemAPI
                     .Query<RefRW<EngageTarget>, RefRW<UnitAttackCooldownComponent>, RefRW<UnitAttackTraceComponent>, RefRW<UnitAttackAnimationComponent>, RefRO<LocalTransform>, RefRO<UnitAttack>, RefRO<UnitHealth>>()
                     .WithNone<StaticGridBlocker>()
                     .WithNone<UnitDeathAnimationComponent>()
                     .WithEntityAccess())
        {
            if (em.HasComponent<AirMissileLauncherComponent>(entity))
                continue;

            if (em.HasComponent<UnitCombat>(entity) && em.GetComponentData<UnitCombat>(entity).CanAttack == 0)
                continue;

            ref var engageRw = ref engage.ValueRW;
            ref var stateRw = ref attackState.ValueRW;
            ref var traceRw = ref attackTraceState.ValueRW;
            ref var attackAnimRw = ref attackAnimationState.ValueRW;
            var attackRo = attack.ValueRO;
            var animationSettingsRo = em.HasComponent<UnitAnimationSettings>(entity)
                ? em.GetComponentData<UnitAnimationSettings>(entity)
                : new UnitAnimationSettings
                {
                    AttackAnimationSeconds = 0.25f
                };

            stateRw.CooldownRemaining -= dt;
            traceRw.TimeRemaining = math.max(0f, traceRw.TimeRemaining - dt);

            if (selfHealth.ValueRO.Current <= 0)
                continue;

            if (engageRw.Target == Entity.Null)
                continue;

            if (!em.Exists(engageRw.Target) || !em.HasComponent<UnitHealth>(engageRw.Target) || !em.HasComponent<LocalTransform>(engageRw.Target))
            {
                engageRw.Target = Entity.Null;
                continue;
            }

            bool isDebugFireTarget = IsDebugFireTargetForSource(em, engageRw.Target, entity);
            if ((em.HasComponent<StaticGridBlocker>(engageRw.Target) && !isDebugFireTarget) ||
                em.GetComponentData<UnitHealth>(engageRw.Target).Current <= 0)
            {
                engageRw.Target = Entity.Null;
                continue;
            }

            int targetPredictedHealth = _predictedHealth.TryGetValue(engageRw.Target, out int existingPredictedHealth)
                ? existingPredictedHealth
                : em.GetComponentData<UnitHealth>(engageRw.Target).Current;
            if (targetPredictedHealth <= 0)
            {
                engageRw.Target = Entity.Null;
                continue;
            }

            float attackRange = math.max(0f, attackRo.Range);
            if (attackRange <= 0f)
                continue;

            float3 delta = em.GetComponentData<LocalTransform>(engageRw.Target).Position - selfTransform.ValueRO.Position;
            delta.y = 0f;

            float selfCombatRadius = footprintLookup.HasComponent(entity)
                ? GetCombatRadius(footprintLookup[entity].Size, grid.CellSize)
                : 0f;
            float targetCombatRadius = footprintLookup.HasComponent(engageRw.Target)
                ? GetCombatRadius(footprintLookup[engageRw.Target].Size, grid.CellSize)
                : 0f;
            float range = attackRange + selfCombatRadius + targetCombatRadius + grid.CellSize * 0.25f;
            bool bypassMissileDebugRange = isDebugFireTarget && em.HasComponent<GroundMissileLauncherComponent>(entity);
            if (!bypassMissileDebugRange && math.lengthsq(delta) > range * range)
                continue;

            if (!bypassMissileDebugRange && stateRw.CooldownRemaining > 0f)
                continue;

            if (TryStartGroundMissileLauncherAttack(
                    em,
                    entity,
                    engageRw.Target,
                    engageRw.Cell,
                    engageRw.Position,
                    selfTransform.ValueRO.Position,
                    ref stateRw,
                    ref traceRw,
                    ref attackAnimRw,
                    animationSettingsRo,
                    bypassRangeValidation: bypassMissileDebugRange))
            {
                continue;
            }

            stateRw.CooldownRemaining = math.max(0.01f, attackRo.CooldownSeconds);
            traceRw.TimeRemaining = math.max(0.01f, attackRo.TraceVisibleSeconds);
            traceRw.Phase = math.frac(traceRw.Phase + 0.371f);
            attackAnimRw.TimeRemaining = math.max(0.01f, animationSettingsRo.AttackAnimationSeconds);
            if (attackRo.Damage <= 0)
                continue;

            int2 attackerCell = GridUtils.WorldToCell(grid, selfTransform.ValueRO.Position);
            _predictedHealth[engageRw.Target] = math.max(0, targetPredictedHealth - attackRo.Damage);
            if (_aggregatedEffects.TryGetValue(engageRw.Target, out AggregatedTargetEffect effect))
            {
                effect.TotalDamage += attackRo.Damage;
                effect.Attacker = entity;
                effect.AttackerCell = attackerCell;
                effect.AttackerPosition = selfTransform.ValueRO.Position;
                _aggregatedEffects[engageRw.Target] = effect;
            }
            else
            {
                _aggregatedEffects.Add(engageRw.Target, new AggregatedTargetEffect
                {
                    TotalDamage = attackRo.Damage,
                    Attacker = entity,
                    AttackerCell = attackerCell,
                    AttackerPosition = selfTransform.ValueRO.Position
                });
            }
        }

        foreach (var pair in _aggregatedEffects)
        {
            Entity target = pair.Key;
            AggregatedTargetEffect pending = pair.Value;
            if (!em.Exists(target) || !em.HasComponent<UnitHealth>(target))
                continue;

            UnitHealth health = em.GetComponentData<UnitHealth>(target);
            if (health.Current <= 0)
                continue;

            health.Current = math.max(0, health.Current - pending.TotalDamage);
            em.SetComponentData(target, health);

            if (em.HasComponent<RecentAttacker>(target))
            {
                em.SetComponentData(target, new RecentAttacker
                {
                    Attacker = pending.Attacker,
                    Cell = pending.AttackerCell,
                    Position = pending.AttackerPosition
                });
            }
            else
            {
                ecb.AddComponent(target, new RecentAttacker
                {
                    Attacker = pending.Attacker,
                    Cell = pending.AttackerCell,
                    Position = pending.AttackerPosition
                });
            }

            if (health.Current > 0)
            {
                TryIssueCounterEngage(em, ecb, grid, pending.Attacker, pending.AttackerPosition, target);
                TryIssueFleeOrder(em, ecb, grid, pending.AttackerPosition, target);
            }

            if (em.HasComponent<UnitAttackImpactVfxReference>(pending.Attacker) && em.HasComponent<LocalTransform>(target))
            {
                UnitAttackImpactVfxReference impactVfx = em.GetComponentObject<UnitAttackImpactVfxReference>(pending.Attacker);
                if (impactVfx?.Prefab != null)
                    UnitAttackImpactVfxRuntime.Play(impactVfx.Prefab, em.GetComponentData<LocalTransform>(target).Position);
            }

            if (em.HasComponent<RecentDamageHealthBarVisibility>(target))
            {
                em.SetComponentData(target, new RecentDamageHealthBarVisibility
                {
                    TimeRemaining = DamageHealthBarVisibleSeconds
                });
            }
            else
            {
                ecb.AddComponent(target, new RecentDamageHealthBarVisibility
                {
                    TimeRemaining = DamageHealthBarVisibleSeconds
                });
            }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static float GetCombatRadius(int2 footprintSize, float cellSize)
    {
        int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
        float halfWidth = math.max(0f, (clamped.x - 1) * 0.5f * cellSize);
        float halfDepth = math.max(0f, (clamped.y - 1) * 0.5f * cellSize);
        return math.max(halfWidth, halfDepth);
    }

    private static bool TryStartGroundMissileLauncherAttack(
        EntityManager em,
        Entity attacker,
        Entity target,
        int2 targetCell,
        float3 targetPosition,
        float3 attackerPosition,
        ref UnitAttackCooldownComponent attackState,
        ref UnitAttackTraceComponent traceState,
        ref UnitAttackAnimationComponent attackAnimationState,
        UnitAnimationSettings animationSettings,
        bool bypassRangeValidation = false)
    {
        if (!em.HasComponent<GroundMissileLauncherComponent>(attacker) ||
            !em.HasComponent<GroundMissileLauncherStateComponent>(attacker))
        {
            return false;
        }

        GroundMissileLauncherComponent launcher = em.GetComponentData<GroundMissileLauncherComponent>(attacker);
        GroundMissileLauncherStateComponent launcherState = em.GetComponentData<GroundMissileLauncherStateComponent>(attacker);
        if (em.HasComponent<GroundMissileInFlightComponent>(attacker))
            return true;
        if (launcherState.Phase != (byte)GroundMissileLauncherPhase.Idle)
            return true;

        float3 delta = targetPosition - attackerPosition;
        delta.y = 0f;
        float distance = math.length(delta);
        if (!bypassRangeValidation &&
            (distance < math.max(0f, launcher.MinRange) || distance > math.max(launcher.MinRange, launcher.MaxRange)))
        {
            return true;
        }

        int rocketCount = em.HasBuffer<GroundMissileLauncherRocketVisualComponent>(attacker)
            ? em.GetBuffer<GroundMissileLauncherRocketVisualComponent>(attacker).Length
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
        em.SetComponentData(attacker, launcherState);

        attackState.CooldownRemaining = math.max(
            0.01f,
            GroundMissileLauncherTiming.FullAttackCycleSeconds(launcher.PrepareSeconds, launcher.ReloadSeconds));
        traceState.TimeRemaining = 0f;
        attackAnimationState.TimeRemaining = math.max(0.01f, animationSettings.AttackAnimationSeconds);
        return true;
    }

    private static bool IsDebugFireTargetForSource(EntityManager em, Entity target, Entity source)
    {
        return target != Entity.Null &&
               em.Exists(target) &&
               em.HasComponent<DebugFireTargetTag>(target) &&
               em.GetComponentData<DebugFireTargetTag>(target).Source == source;
    }

    private static void TryIssueFleeOrder(
        EntityManager em,
        EntityCommandBuffer ecb,
        GridConfig grid,
        float3 attackerPosition,
        Entity target)
    {
        if (!em.Exists(target) ||
            em.HasComponent<StaticGridBlocker>(target) ||
            !em.HasComponent<UnitCombat>(target) ||
            !em.HasComponent<UnitMove>(target) ||
            !em.HasComponent<UnitFootprint>(target) ||
            !em.HasComponent<UnitGrid>(target) ||
            !em.HasComponent<LocalTransform>(target))
        {
            return;
        }

        UnitCombat targetCombat = em.GetComponentData<UnitCombat>(target);
        if (targetCombat.CanAttack != 0)
            return;

        LocalTransform targetTransform = em.GetComponentData<LocalTransform>(target);
        float3 targetPosition = targetTransform.Position;
        float3 away = targetPosition - attackerPosition;
        away.y = 0f;
        if (math.lengthsq(away) < 1e-6f)
        {
            away = math.mul(targetTransform.Rotation, new float3(0f, 0f, 1f));
            away.y = 0f;
        }

        away = math.normalizesafe(away, new float3(0f, 0f, 1f));
        int fleeCells = math.clamp(targetCombat.AggroRangeCells + 2, FleeCellsMin, FleeCellsMax);
        float fleeDistance = math.max(grid.CellSize, fleeCells * grid.CellSize);
        float3 desiredWorld = targetPosition + away * fleeDistance;
        int2 desiredCell = GridUtils.WorldToCell(grid, desiredWorld);
        desiredCell = new int2(
            math.clamp(desiredCell.x, 0, grid.Width - 1),
            math.clamp(desiredCell.y, 0, grid.Height - 1));

        if (em.HasComponent<UnitTarget>(target))
            em.SetComponentData(target, new UnitTarget { Cell = desiredCell });
        else
            ecb.AddComponent(target, new UnitTarget { Cell = desiredCell });

        if (em.HasComponent<UnitPathRequest>(target))
            em.SetComponentData(target, new UnitPathRequest { Goal = desiredCell });
        else
            ecb.AddComponent(target, new UnitPathRequest { Goal = desiredCell });

        if (em.HasComponent<EngageTarget>(target))
            ecb.RemoveComponent<EngageTarget>(target);

        if (em.HasComponent<UnitPathFollow>(target))
            ecb.RemoveComponent<UnitPathFollow>(target);
        if (em.HasComponent<UnitPathRange>(target))
            ecb.RemoveComponent<UnitPathRange>(target);
        if (em.HasComponent<AutoWanderMoveTag>(target))
            ecb.RemoveComponent<AutoWanderMoveTag>(target);
        if (!em.HasComponent<ManualMoveOrderTag>(target))
            ecb.AddComponent<ManualMoveOrderTag>(target);
    }

    private static void TryIssueCounterEngage(
        EntityManager em,
        EntityCommandBuffer ecb,
        GridConfig grid,
        Entity attacker,
        float3 attackerPosition,
        Entity target)
    {
        if (!em.Exists(target) ||
            !em.Exists(attacker) ||
            !em.HasComponent<UnitCombat>(target) ||
            !em.HasComponent<UnitAttack>(target) ||
            !em.HasComponent<LocalTransform>(target) ||
            !em.HasComponent<UnitHealth>(target))
        {
            return;
        }

        if (em.GetComponentData<UnitHealth>(target).Current <= 0)
            return;

        UnitCombat targetCombat = em.GetComponentData<UnitCombat>(target);
        bool hasActiveManualMove =
            em.HasComponent<ManualMoveOrderTag>(target) &&
            (em.HasComponent<UnitPathFollow>(target) || em.HasComponent<UnitPathRequest>(target));

        if (targetCombat.CanAttack == 0 || targetCombat.AutoEngage == 0 || hasActiveManualMove)
            return;

        if (em.HasComponent<HoldPositionOrderTag>(target) &&
            em.HasComponent<LocalTransform>(target) &&
            em.HasComponent<UnitAttack>(target))
        {
            float3 holdPosition = em.GetComponentData<LocalTransform>(target).Position;
            float3 delta = attackerPosition - holdPosition;
            delta.y = 0f;
            float attackRange = math.max(0f, em.GetComponentData<UnitAttack>(target).Range);
            if (attackRange <= 0f || math.lengthsq(delta) > attackRange * attackRange)
                return;
        }

        if (em.HasComponent<EngageTarget>(target))
        {
            EngageTarget currentEngage = em.GetComponentData<EngageTarget>(target);
            if (currentEngage.Target != Entity.Null &&
                em.Exists(currentEngage.Target) &&
                (!em.HasComponent<UnitHealth>(currentEngage.Target) || em.GetComponentData<UnitHealth>(currentEngage.Target).Current > 0))
            {
                return;
            }
        }

        int2 attackerCell = GridUtils.WorldToCell(grid, attackerPosition);
        if (em.HasComponent<EngageTarget>(target))
        {
            em.SetComponentData(target, new EngageTarget
            {
                Target = attacker,
                Cell = attackerCell,
                Position = attackerPosition,
                IsCommanded = 1
            });
        }
        else
        {
            ecb.AddComponent(target, new EngageTarget
            {
                Target = attacker,
                Cell = attackerCell,
                Position = attackerPosition,
                IsCommanded = 1
            });
        }

        if (em.HasComponent<UnitPathFollow>(target))
            ecb.RemoveComponent<UnitPathFollow>(target);
        if (em.HasComponent<UnitPathRange>(target))
            ecb.RemoveComponent<UnitPathRange>(target);
        if (em.HasComponent<UnitPathRequest>(target))
            ecb.RemoveComponent<UnitPathRequest>(target);
        if (em.HasComponent<AutoWanderMoveTag>(target))
            ecb.RemoveComponent<AutoWanderMoveTag>(target);
    }
}
