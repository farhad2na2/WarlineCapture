using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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
        public byte ShowImpactVfx; // only shots that showed a tracer trigger impact VFX
    }

    private enum StandardAttackPlanKind : byte
    {
        None = 0,
        ClearTarget = 1,
        Windup = 2,
        Shot = 3
    }

    private struct StandardAttackCandidate
    {
        public Entity Attacker;
        public Entity Target;
        public EngageTarget Engage;
        public UnitAttackCooldownComponent AttackState;
        public UnitAttackTraceComponent TraceState;
        public UnitAttackAnimationComponent AttackAnimationState;
        public UnitAttack Attack;
        public int SelfHealth;
        public int TargetHealth;
        public float3 AttackerPosition;
        public float3 TargetPosition;
        public int2 AttackerCell;
        public int2 TargetCell;
        public float SelfCombatRadius;
        public float TargetCombatRadius;
        public float AttackAnimationSeconds;
        public byte TargetExists;
        public byte TargetHasHealth;
        public byte TargetHasTransform;
        public byte TargetIsStaticGridBlocker;
        public byte IsDebugFireTarget;
        public byte HasAnimationOrder;
        public byte AircraftAttackWindowBlocked;
    }

    private struct StandardAttackPlan
    {
        public StandardAttackPlanKind Kind;
        public UnitAttackCooldownComponent BaseAttackState;
        public UnitAttackTraceComponent BaseTraceState;
        public UnitAttackAnimationComponent BaseAttackAnimationState;
        public UnitAttackCooldownComponent AttackState;
        public UnitAttackTraceComponent TraceState;
        public UnitAttackAnimationComponent AttackAnimationState;
        public EngageTarget Engage;
        public int Damage;
        public int2 AttackerCell;
        public byte ShowTracer;
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
        using NativeList<StandardAttackCandidate> standardAttackCandidates = new(InitialAttackScratchCapacity, Allocator.TempJob);
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

            if (!em.HasComponent<GroundMissileLauncherComponent>(entity))
            {
                Entity target = engageRw.Target;
                bool hasTarget = target != Entity.Null;
                bool targetExists = hasTarget && em.Exists(target);
                bool targetHasHealth = targetExists && em.HasComponent<UnitHealth>(target);
                bool targetHasTransform = targetExists && em.HasComponent<LocalTransform>(target);
                bool targetIsStaticGridBlocker = targetExists && em.HasComponent<StaticGridBlocker>(target);
                bool standardIsDebugFireTarget = targetExists && IsDebugFireTargetForSource(em, target, entity);
                float3 targetPosition = targetHasTransform ? em.GetComponentData<LocalTransform>(target).Position : float3.zero;

                standardAttackCandidates.Add(new StandardAttackCandidate
                {
                    Attacker = entity,
                    Target = target,
                    Engage = engageRw,
                    AttackState = stateRw,
                    TraceState = traceRw,
                    AttackAnimationState = attackAnimRw,
                    Attack = attackRo,
                    SelfHealth = selfHealth.ValueRO.Current,
                    TargetHealth = targetHasHealth ? em.GetComponentData<UnitHealth>(target).Current : 0,
                    AttackerPosition = selfTransform.ValueRO.Position,
                    TargetPosition = targetPosition,
                    AttackerCell = GridUtils.WorldToCell(grid, selfTransform.ValueRO.Position),
                    TargetCell = engageRw.Cell,
                    SelfCombatRadius = footprintLookup.HasComponent(entity)
                        ? GetCombatRadius(footprintLookup[entity].Size, grid.CellSize)
                        : 0f,
                    TargetCombatRadius = targetExists && footprintLookup.HasComponent(target)
                        ? GetCombatRadius(footprintLookup[target].Size, grid.CellSize)
                        : 0f,
                    AttackAnimationSeconds = animationSettingsRo.AttackAnimationSeconds,
                    TargetExists = (byte)(targetExists ? 1 : 0),
                    TargetHasHealth = (byte)(targetHasHealth ? 1 : 0),
                    TargetHasTransform = (byte)(targetHasTransform ? 1 : 0),
                    TargetIsStaticGridBlocker = (byte)(targetIsStaticGridBlocker ? 1 : 0),
                    IsDebugFireTarget = (byte)(standardIsDebugFireTarget ? 1 : 0),
                    HasAnimationOrder = (byte)(em.HasBuffer<UnitAnimationOrderEntry>(entity) ? 1 : 0),
                    AircraftAttackWindowBlocked = (byte)(IsRunwayAircraftAttackWindowBlocked(
                        em,
                        entity,
                        selfTransform.ValueRO,
                        attackRo,
                        targetPosition,
                        targetHasTransform) ? 1 : 0)
                });
                continue;
            }

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

            // Wind-up: if the unit was idle (gun down), raise the weapon first and fire
            // the first bullet a moment later so shots line up with the shoot animation.
            // Gated on the animation-order buffer so logic-only tests are unaffected.
            if (attackAnimRw.TimeRemaining <= 0f && em.HasBuffer<UnitAnimationOrderEntry>(entity))
            {
                attackAnimRw.TimeRemaining = math.max(0.01f, animationSettingsRo.AttackAnimationSeconds);
                stateRw.CooldownRemaining = math.clamp(attackRo.CooldownSeconds * 0.5f, 0.05f, 0.18f);
                continue;
            }

            stateRw.CooldownRemaining = math.max(0.01f, attackRo.CooldownSeconds);
            traceRw.ShotCounter++;
            int tracerInterval = math.max(1, attackRo.TracerEveryNthShot);
            bool tracerShown = traceRw.ShotCounter % tracerInterval == 0;
            if (tracerShown)
            {
                traceRw.TimeRemaining = math.max(0.01f, attackRo.TraceVisibleSeconds);
                traceRw.Phase = math.frac(traceRw.Phase + 0.371f);
            }
            // Keep the shoot animation alive through the next shot while continuously
            // firing, so the unit doesn't flicker back to idle/aim between shots.
            attackAnimRw.TimeRemaining = math.max(
                math.max(0.01f, animationSettingsRo.AttackAnimationSeconds),
                attackRo.CooldownSeconds + 0.15f);
            // Muzzle flash only on shots that show a tracer, so flash, tracer,
            // and impact always appear together as one visible shot.
            if (tracerShown)
            {
                EnqueueAttackVfxRequest(
                    ecb,
                    UnitAttackVfxRequestKind.MuzzleFlash,
                    entity,
                    engageRw.Target,
                    selfTransform.ValueRO.Position,
                    em.GetComponentData<LocalTransform>(engageRw.Target).Position);
            }
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
                effect.ShowImpactVfx |= (byte)(tracerShown ? 1 : 0);
                _aggregatedEffects[engageRw.Target] = effect;
            }
            else
            {
                _aggregatedEffects.Add(engageRw.Target, new AggregatedTargetEffect
                {
                    TotalDamage = attackRo.Damage,
                    Attacker = entity,
                    AttackerCell = attackerCell,
                    AttackerPosition = selfTransform.ValueRO.Position,
                    ShowImpactVfx = (byte)(tracerShown ? 1 : 0)
                });
            }
        }

        ProcessStandardAttackPlans(em, ecb, grid, dt, standardAttackCandidates);

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

            if (pending.ShowImpactVfx != 0 && em.HasComponent<LocalTransform>(target))
            {
                EnqueueAttackVfxRequest(
                    ecb,
                    UnitAttackVfxRequestKind.Impact,
                    pending.Attacker,
                    target,
                    pending.AttackerPosition,
                    em.GetComponentData<LocalTransform>(target).Position);
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

    private void ProcessStandardAttackPlans(
        EntityManager em,
        EntityCommandBuffer ecb,
        GridConfig grid,
        float dt,
        NativeList<StandardAttackCandidate> candidates)
    {
        if (candidates.Length == 0)
            return;

        using NativeArray<StandardAttackPlan> plans = new(candidates.Length, Allocator.TempJob);
        new PlanStandardAttacksJob
        {
            Candidates = candidates.AsArray(),
            Plans = plans,
            DeltaTime = dt,
            CellSize = grid.CellSize
        }.Schedule(candidates.Length, 64).Complete();

        for (int i = 0; i < plans.Length; i++)
        {
            StandardAttackCandidate candidate = candidates[i];
            StandardAttackPlan plan = plans[i];
            if (!em.Exists(candidate.Attacker))
                continue;

            if (em.HasComponent<UnitAttackCooldownComponent>(candidate.Attacker))
                em.SetComponentData(candidate.Attacker, plan.BaseAttackState);
            if (em.HasComponent<UnitAttackTraceComponent>(candidate.Attacker))
                em.SetComponentData(candidate.Attacker, plan.BaseTraceState);
            if (em.HasComponent<UnitAttackAnimationComponent>(candidate.Attacker))
                em.SetComponentData(candidate.Attacker, plan.BaseAttackAnimationState);

            if (plan.Kind == StandardAttackPlanKind.ClearTarget)
            {
                if (em.HasComponent<EngageTarget>(candidate.Attacker))
                    em.SetComponentData(candidate.Attacker, plan.Engage);
                continue;
            }

            if (plan.Kind == StandardAttackPlanKind.None)
                continue;

            if (plan.Kind == StandardAttackPlanKind.Windup)
            {
                if (em.HasComponent<UnitAttackCooldownComponent>(candidate.Attacker))
                    em.SetComponentData(candidate.Attacker, plan.AttackState);
                if (em.HasComponent<UnitAttackAnimationComponent>(candidate.Attacker))
                    em.SetComponentData(candidate.Attacker, plan.AttackAnimationState);
                continue;
            }

            int targetPredictedHealth = _predictedHealth.TryGetValue(candidate.Target, out int existingPredictedHealth)
                ? existingPredictedHealth
                : candidate.TargetHealth;
            if (targetPredictedHealth <= 0)
            {
                EngageTarget cleared = candidate.Engage;
                cleared.Target = Entity.Null;
                if (em.HasComponent<EngageTarget>(candidate.Attacker))
                    em.SetComponentData(candidate.Attacker, cleared);
                continue;
            }

            if (em.HasComponent<UnitAttackCooldownComponent>(candidate.Attacker))
                em.SetComponentData(candidate.Attacker, plan.AttackState);
            if (em.HasComponent<UnitAttackTraceComponent>(candidate.Attacker))
                em.SetComponentData(candidate.Attacker, plan.TraceState);
            if (em.HasComponent<UnitAttackAnimationComponent>(candidate.Attacker))
                em.SetComponentData(candidate.Attacker, plan.AttackAnimationState);

            if (plan.ShowTracer != 0)
            {
                EnqueueAttackVfxRequest(
                    ecb,
                    UnitAttackVfxRequestKind.MuzzleFlash,
                    candidate.Attacker,
                    candidate.Target,
                    candidate.AttackerPosition,
                    candidate.TargetPosition);
            }

            if (plan.Damage <= 0)
                continue;

            _predictedHealth[candidate.Target] = math.max(0, targetPredictedHealth - plan.Damage);
            if (_aggregatedEffects.TryGetValue(candidate.Target, out AggregatedTargetEffect effect))
            {
                effect.TotalDamage += plan.Damage;
                effect.Attacker = candidate.Attacker;
                effect.AttackerCell = plan.AttackerCell;
                effect.AttackerPosition = candidate.AttackerPosition;
                effect.ShowImpactVfx |= plan.ShowTracer;
                _aggregatedEffects[candidate.Target] = effect;
            }
            else
            {
                _aggregatedEffects.Add(candidate.Target, new AggregatedTargetEffect
                {
                    TotalDamage = plan.Damage,
                    Attacker = candidate.Attacker,
                    AttackerCell = plan.AttackerCell,
                    AttackerPosition = candidate.AttackerPosition,
                    ShowImpactVfx = plan.ShowTracer
                });
            }
        }
    }

    [BurstCompile]
    private struct PlanStandardAttacksJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<StandardAttackCandidate> Candidates;
        public NativeArray<StandardAttackPlan> Plans;
        public float DeltaTime;
        public float CellSize;

        public void Execute(int index)
        {
            StandardAttackCandidate candidate = Candidates[index];
            UnitAttackCooldownComponent baseAttackState = candidate.AttackState;
            UnitAttackTraceComponent baseTraceState = candidate.TraceState;
            UnitAttackAnimationComponent baseAnimationState = candidate.AttackAnimationState;
            baseAttackState.CooldownRemaining -= DeltaTime;
            baseTraceState.TimeRemaining = math.max(0f, baseTraceState.TimeRemaining - DeltaTime);

            StandardAttackPlan plan = new()
            {
                BaseAttackState = baseAttackState,
                BaseTraceState = baseTraceState,
                BaseAttackAnimationState = baseAnimationState,
                AttackState = baseAttackState,
                TraceState = baseTraceState,
                AttackAnimationState = baseAnimationState,
                Engage = candidate.Engage,
                AttackerCell = candidate.AttackerCell
            };

            if (candidate.SelfHealth <= 0 ||
                candidate.Engage.Target == Entity.Null)
            {
                Plans[index] = plan;
                return;
            }

            if (candidate.TargetExists == 0 ||
                candidate.TargetHasHealth == 0 ||
                candidate.TargetHasTransform == 0 ||
                (candidate.TargetIsStaticGridBlocker != 0 && candidate.IsDebugFireTarget == 0) ||
                candidate.TargetHealth <= 0)
            {
                plan.Kind = StandardAttackPlanKind.ClearTarget;
                plan.Engage.Target = Entity.Null;
                Plans[index] = plan;
                return;
            }

            float attackRange = math.max(0f, candidate.Attack.Range);
            if (attackRange <= 0f)
            {
                Plans[index] = plan;
                return;
            }

            if (candidate.AircraftAttackWindowBlocked != 0)
            {
                Plans[index] = plan;
                return;
            }

            float3 delta = candidate.TargetPosition - candidate.AttackerPosition;
            delta.y = 0f;
            float range = attackRange + candidate.SelfCombatRadius + candidate.TargetCombatRadius + CellSize * 0.25f;
            if (math.lengthsq(delta) > range * range ||
                baseAttackState.CooldownRemaining > 0f)
            {
                Plans[index] = plan;
                return;
            }

            if (baseAnimationState.TimeRemaining <= 0f && candidate.HasAnimationOrder != 0)
            {
                plan.Kind = StandardAttackPlanKind.Windup;
                plan.AttackAnimationState.TimeRemaining = math.max(0.01f, candidate.AttackAnimationSeconds);
                plan.AttackState.CooldownRemaining = math.clamp(candidate.Attack.CooldownSeconds * 0.5f, 0.05f, 0.18f);
                Plans[index] = plan;
                return;
            }

            plan.Kind = StandardAttackPlanKind.Shot;
            plan.AttackState.CooldownRemaining = math.max(0.01f, candidate.Attack.CooldownSeconds);
            plan.TraceState.ShotCounter++;
            int tracerInterval = math.max(1, candidate.Attack.TracerEveryNthShot);
            bool tracerShown = plan.TraceState.ShotCounter % tracerInterval == 0;
            if (tracerShown)
            {
                plan.TraceState.TimeRemaining = math.max(0.01f, candidate.Attack.TraceVisibleSeconds);
                plan.TraceState.Phase = math.frac(plan.TraceState.Phase + 0.371f);
            }

            plan.AttackAnimationState.TimeRemaining = math.max(
                math.max(0.01f, candidate.AttackAnimationSeconds),
                candidate.Attack.CooldownSeconds + 0.15f);
            plan.Damage = candidate.Attack.Damage;
            plan.ShowTracer = (byte)(tracerShown ? 1 : 0);
            Plans[index] = plan;
        }
    }

    private static bool IsRunwayAircraftAttackWindowBlocked(
        EntityManager em,
        Entity entity,
        LocalTransform transform,
        UnitAttack attack,
        float3 targetPosition,
        bool hasTargetPosition)
    {
        if (!hasTargetPosition ||
            !em.HasComponent<UnitAirComponent>(entity) ||
            !em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            return false;
        }

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(entity);
        if (airState.UsesRunway == 0 ||
            !FixedWingRunwayUnitUtility.IsFixedWingRunwayUnit(em.GetComponentData<UnitSourcePrefabKey>(entity).Value))
        {
            return false;
        }

        if (airState.Airborne == 0 || airState.AttackRunActive == 0)
            return true;

        return !IsFacingOrNearAttackTarget(transform, targetPosition, attack.Range);
    }

    private static bool IsFacingOrNearAttackTarget(LocalTransform transform, float3 targetPosition, float attackRange)
    {
        float3 toTarget = targetPosition - transform.Position;
        toTarget.y = 0f;
        float distanceSq = math.lengthsq(toTarget);
        if (distanceSq <= 1e-4f)
            return true;

        float distance = math.sqrt(distanceSq);
        float3 targetDirection = toTarget / distance;
        float3 forward = math.mul(transform.Rotation, new float3(0f, 0f, 1f));
        forward.y = 0f;
        forward = math.normalizesafe(forward, targetDirection);
        float alignment = math.dot(forward, targetDirection);
        if (alignment >= 0.88f)
            return true;

        float nearDistance = math.clamp(math.max(0f, attackRange) * 0.45f, 12f, 45f);
        return distance <= nearDistance && alignment >= 0.15f;
    }

    private static float GetCombatRadius(int2 footprintSize, float cellSize)
    {
        int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
        float halfWidth = math.max(0f, (clamped.x - 1) * 0.5f * cellSize);
        float halfDepth = math.max(0f, (clamped.y - 1) * 0.5f * cellSize);
        return math.max(halfWidth, halfDepth);
    }

    private static void EnqueueAttackVfxRequest(
        EntityCommandBuffer ecb,
        UnitAttackVfxRequestKind kind,
        Entity source,
        Entity target,
        float3 sourcePosition,
        float3 targetPosition)
    {
        Entity requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new UnitAttackVfxRequest
        {
            Kind = (byte)kind,
            Source = source,
            Target = target,
            SourcePosition = sourcePosition,
            TargetPosition = targetPosition
        });
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

[UpdateAfter(typeof(UnitAttackSystem))]
[UpdateBefore(typeof(UnitDeathSystem))]
public partial class UnitAttackVfxRequestSystem : SystemBase
{
    private const int MaxMuzzleFlashOriginCount = 4;

    protected override void OnCreate()
    {
        RequireForUpdate<UnitAttackVfxRequest>();
    }

    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (request, entity) in SystemAPI
                     .Query<RefRO<UnitAttackVfxRequest>>()
                     .WithEntityAccess())
        {
            UnitAttackVfxRequest value = request.ValueRO;
            switch ((UnitAttackVfxRequestKind)value.Kind)
            {
                case UnitAttackVfxRequestKind.MuzzleFlash:
                    PlayMuzzleFlash(em, value);
                    break;
                case UnitAttackVfxRequestKind.Impact:
                    PlayImpact(em, value);
                    break;
            }

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static void PlayMuzzleFlash(EntityManager em, UnitAttackVfxRequest request)
    {
        if (request.Source == Entity.Null ||
            !em.Exists(request.Source) ||
            !em.HasComponent<UnitMuzzleFlashVfxReference>(request.Source))
        {
            return;
        }

        UnitMuzzleFlashVfxReference muzzleVfx = em.GetComponentData<UnitMuzzleFlashVfxReference>(request.Source);
        GameObject muzzlePrefab = muzzleVfx.Prefab.Value;
        if (muzzlePrefab == null)
            return;

        LocalTransform sourceTransform = em.HasComponent<LocalTransform>(request.Source)
            ? em.GetComponentData<LocalTransform>(request.Source)
            : LocalTransform.FromPosition(request.SourcePosition);

        float3 muzzlePosition = sourceTransform.Position;
        if (em.HasComponent<UnitTurretReference>(request.Source))
        {
            UnitTurretReference turretRef = em.GetComponentData<UnitTurretReference>(request.Source);
            if (em.Exists(turretRef.Turret) && em.HasComponent<LocalToWorld>(turretRef.Turret))
                muzzlePosition = em.GetComponentData<LocalToWorld>(turretRef.Turret).Position;
        }

        muzzlePosition.y += math.max(0f, muzzleVfx.HeightOffset);
        Quaternion rotation = ResolveLookRotation(em, request.Target, request.TargetPosition, sourceTransform.Position, sourceTransform.Rotation);
        float forwardOffset = math.max(0f, muzzleVfx.ForwardOffset);
        if (forwardOffset > 0f)
            muzzlePosition += (float3)(rotation * Vector3.forward) * forwardOffset;

        UnitAttackTraceOriginPattern originPattern = em.HasComponent<UnitAttackTraceOriginPattern>(request.Source)
            ? em.GetComponentData<UnitAttackTraceOriginPattern>(request.Source)
            : default;
        int originCount = ResolveMuzzleFlashOriginCount(originPattern);
        Vector3 sideRight = ResolveMuzzleFlashSideRight(sourceTransform.Rotation, request.TargetPosition - sourceTransform.Position);
        for (int originIndex = 0; originIndex < originCount; originIndex++)
        {
            float sideSign = ResolveMuzzleFlashSideSign(originIndex, originCount);
            float3 sideOffset = (float3)sideRight * (sideSign * math.max(0f, originPattern.LateralOffset));
            UnitAttackImpactVfxView.Play(muzzlePrefab, (Vector3)(muzzlePosition + sideOffset), rotation);
        }
    }

    private static void PlayImpact(EntityManager em, UnitAttackVfxRequest request)
    {
        if (request.Source == Entity.Null ||
            !em.Exists(request.Source) ||
            !em.HasComponent<UnitAttackImpactVfxReference>(request.Source))
        {
            return;
        }

        UnitAttackImpactVfxReference impactVfx = em.GetComponentData<UnitAttackImpactVfxReference>(request.Source);
        GameObject impactPrefab = impactVfx.Prefab.Value;
        if (impactPrefab == null)
            return;

        float3 targetPosition = request.TargetPosition;
        if (request.Target != Entity.Null &&
            em.Exists(request.Target) &&
            em.HasComponent<LocalTransform>(request.Target))
        {
            targetPosition = em.GetComponentData<LocalTransform>(request.Target).Position;
        }

        float3 toAttacker = request.SourcePosition - targetPosition;
        toAttacker.y = 0f;
        Quaternion impactRotation = math.lengthsq(toAttacker) > 1e-4f
            ? Quaternion.LookRotation((Vector3)toAttacker)
            : Quaternion.identity;
        UnitAttackImpactVfxView.Play(impactPrefab, targetPosition, impactRotation);
    }

    private static Quaternion ResolveLookRotation(
        EntityManager em,
        Entity target,
        float3 fallbackTargetPosition,
        float3 sourcePosition,
        quaternion fallbackRotation)
    {
        float3 targetPosition = fallbackTargetPosition;
        if (target != Entity.Null &&
            em.Exists(target) &&
            em.HasComponent<LocalTransform>(target))
        {
            targetPosition = em.GetComponentData<LocalTransform>(target).Position;
        }

        float3 toTarget = targetPosition - sourcePosition;
        toTarget.y = 0f;
        return math.lengthsq(toTarget) > 1e-4f
            ? Quaternion.LookRotation((Vector3)toTarget)
            : new Quaternion(
                fallbackRotation.value.x,
                fallbackRotation.value.y,
                fallbackRotation.value.z,
                fallbackRotation.value.w);
    }

    private static int ResolveMuzzleFlashOriginCount(UnitAttackTraceOriginPattern pattern)
    {
        if (pattern.OriginCount <= 1 || pattern.LateralOffset <= 0f)
            return 1;

        return math.clamp(pattern.OriginCount, 1, MaxMuzzleFlashOriginCount);
    }

    private static float ResolveMuzzleFlashSideSign(int index, int count)
    {
        if (count <= 1)
            return 0f;
        if (count == 2)
            return index == 0 ? -1f : 1f;

        return math.lerp(-1f, 1f, index / (float)(count - 1));
    }

    private static Vector3 ResolveMuzzleFlashSideRight(quaternion sourceRotation, float3 aim)
    {
        Quaternion rotation = new(sourceRotation.value.x, sourceRotation.value.y, sourceRotation.value.z, sourceRotation.value.w);
        Vector3 right = rotation * Vector3.right;
        right.y = 0f;
        if (right.sqrMagnitude > 1e-5f)
            return right.normalized;

        Vector3 flatAim = (Vector3)aim;
        flatAim.y = 0f;
        if (flatAim.sqrMagnitude <= 1e-5f)
            return Vector3.right;

        return Vector3.Cross(Vector3.up, flatAim).normalized;
    }
}

[UpdateAfter(typeof(GroundMissileImpactSystem))]
[UpdateAfter(typeof(AirMissileImpactSystem))]
public partial class CombatGameObjectVfxPlaybackSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<CombatGameObjectVfxRequest>();
    }

    protected override void OnUpdate()
    {
        EntityManager em = EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (request, entity) in SystemAPI
                     .Query<RefRO<CombatGameObjectVfxRequest>>()
                     .WithEntityAccess())
        {
            CombatGameObjectVfxRequest value = request.ValueRO;
            GameObject prefab = value.Prefab.Value != null ? value.Prefab.Value : value.FallbackPrefab.Value;
            if (prefab != null)
            {
                Quaternion rotation = ToUnityQuaternion(value.Rotation);
                switch ((CombatGameObjectVfxRequestKind)value.Kind)
                {
                    case CombatGameObjectVfxRequestKind.Play:
                        UnitAttackImpactVfxView.Play(prefab, value.Position, rotation);
                        break;
                    case CombatGameObjectVfxRequestKind.TimedLoop:
                        UnitAttackImpactVfxView.PlayTimedLoop(
                            prefab,
                            value.Position,
                            rotation,
                            value.EmitSeconds,
                            value.ActiveSeconds);
                        break;
                }
            }

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static Quaternion ToUnityQuaternion(quaternion rotation)
    {
        return new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
    }
}

internal static class CombatGameObjectVfxRequests
{
    public static void Enqueue(
        EntityCommandBuffer ecb,
        UnityObjectRef<GameObject> prefab,
        float3 position,
        quaternion rotation,
        CombatGameObjectVfxRequestKind kind,
        float emitSeconds = 0f,
        float activeSeconds = 0f,
        UnityObjectRef<GameObject> fallbackPrefab = default)
    {
        Entity request = ecb.CreateEntity();
        ecb.AddComponent(request, new CombatGameObjectVfxRequest
        {
            Kind = (byte)kind,
            Prefab = prefab,
            FallbackPrefab = fallbackPrefab,
            Position = position,
            Rotation = rotation,
            EmitSeconds = emitSeconds,
            ActiveSeconds = activeSeconds
        });
    }
}
