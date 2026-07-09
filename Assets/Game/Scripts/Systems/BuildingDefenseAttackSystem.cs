using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [UpdateAfter(typeof(UnitAttackSystem))]
    [UpdateBefore(typeof(UnitDeathSystem))]
    public partial struct BuildingDefenseAttackSystem : ISystem
    {
        private const float DamageHealthBarVisibleSeconds = 2f;
        private EntityQuery _targetQuery;

        public void OnCreate(ref SystemState state)
        {
            _targetQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<UnitAirMovement>());
            state.RequireForUpdate<BuildingDefenseWeapon>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            state.Dependency.Complete();

            using NativeArray<Entity> targets = _targetQuery.ToEntityArray(Allocator.Temp);
            if (targets.Length == 0)
                return;

            float deltaTime = SystemAPI.Time.DeltaTime;
            float now = (float)SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var pendingTraceTargetAdds = new NativeParallelHashSet<Entity>(32, Allocator.Temp);
            var pendingRecentAttackerAdds = new NativeParallelHashSet<Entity>(64, Allocator.Temp);
            var pendingHealthBarVisibilityAdds = new NativeParallelHashSet<Entity>(64, Allocator.Temp);

            foreach (var (weaponRef, healthRef, factionRef, transformRef, traceRef, slots, entity) in
                     SystemAPI.Query<RefRO<BuildingDefenseWeapon>, RefRO<UnitHealth>, RefRO<Faction>, RefRO<LocalTransform>, RefRW<UnitAttackTraceComponent>, DynamicBuffer<BuildingDefenseAttackSlot>>()
                         .WithAll<RuntimeBuildingCombatTag>()
                         .WithEntityAccess())
            {
                if (healthRef.ValueRO.Current <= 0)
                    continue;

                DynamicBuffer<BuildingDefenseAttackSlot> slotBuffer = slots;
                BuildingDefenseWeapon weapon = weaponRef.ValueRO;
                int slotCount = math.min(math.max((int)weapon.MaxConcurrentAttacks, 1), 4);
                EnsureSlotCount(slotBuffer, slotCount);

                FindBestTargets(
                    em,
                    targets,
                    entity,
                    factionRef.ValueRO.Id,
                    transformRef.ValueRO.Position,
                    weapon.Range,
                    out Entity target0,
                    out Entity target1,
                    out Entity target2,
                    out Entity target3);

                if (target0 == Entity.Null)
                {
                    TickSlotCooldowns(slotBuffer, slotCount, deltaTime);
                    continue;
                }

                for (int i = 0; i < slotCount; i++)
                {
                    BuildingDefenseAttackSlot slot = slotBuffer[i];
                    slot.CooldownRemaining = math.max(0f, slot.CooldownRemaining - deltaTime);
                    if (slot.CooldownRemaining > 0f)
                    {
                        slotBuffer[i] = slot;
                        continue;
                    }

                    Entity target = ResolveTargetForSlot(i, target0, target1, target2, target3);
                    if (target == Entity.Null || !IsLiveEnemyTarget(em, target, factionRef.ValueRO.Id, transformRef.ValueRO.Position, weapon.Range))
                    {
                        slotBuffer[i] = slot;
                        continue;
                    }

                    slot.Target = target;
                    slot.CooldownRemaining = math.max(0.01f, weapon.CooldownSeconds);
                    slot.ShotCounter++;
                    slotBuffer[i] = slot;

                    FireShot(
                        em,
                        ecb,
                        entity,
                        target,
                        transformRef.ValueRO.Position,
                        weapon,
                        slot.ShotCounter,
                        ref traceRef.ValueRW,
                        now,
                        ref pendingTraceTargetAdds,
                        ref pendingRecentAttackerAdds,
                        ref pendingHealthBarVisibilityAdds);
                }
            }

            ecb.Playback(em);
            ecb.Dispose();
            pendingTraceTargetAdds.Dispose();
            pendingRecentAttackerAdds.Dispose();
            pendingHealthBarVisibilityAdds.Dispose();
        }

        private static void EnsureSlotCount(DynamicBuffer<BuildingDefenseAttackSlot> slots, int slotCount)
        {
            while (slots.Length < slotCount)
            {
                slots.Add(new BuildingDefenseAttackSlot
                {
                    Target = Entity.Null,
                    CooldownRemaining = 0f,
                    ShotCounter = 0
                });
            }
        }

        private static void TickSlotCooldowns(DynamicBuffer<BuildingDefenseAttackSlot> slots, int slotCount, float deltaTime)
        {
            for (int i = 0; i < slotCount; i++)
            {
                BuildingDefenseAttackSlot slot = slots[i];
                slot.CooldownRemaining = math.max(0f, slot.CooldownRemaining - deltaTime);
                slot.Target = Entity.Null;
                slots[i] = slot;
            }
        }

        private static void FireShot(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity source,
            Entity target,
            float3 sourcePosition,
            BuildingDefenseWeapon weapon,
            int shotCounter,
            ref UnitAttackTraceComponent trace,
            float now,
            ref NativeParallelHashSet<Entity> pendingTraceTargetAdds,
            ref NativeParallelHashSet<Entity> pendingRecentAttackerAdds,
            ref NativeParallelHashSet<Entity> pendingHealthBarVisibilityAdds)
        {
            if (!em.Exists(target) || !em.HasComponent<UnitHealth>(target))
                return;

            UnitHealth targetHealth = em.GetComponentData<UnitHealth>(target);
            if (targetHealth.Current <= 0)
                return;

            float3 targetPosition = em.HasComponent<LocalTransform>(target)
                ? em.GetComponentData<LocalTransform>(target).Position
                : sourcePosition;

            int tracerInterval = math.max(1, weapon.TracerEveryNthShot);
            bool showTracer = shotCounter % tracerInterval == 0;
            if (showTracer)
            {
                trace.TimeRemaining = math.max(0.01f, weapon.TraceVisibleSeconds);
                trace.Phase = math.frac(trace.Phase + 0.371f);
                SetTraceTarget(em, ecb, source, target, targetPosition, ref pendingTraceTargetAdds);
                EnqueueAttackVfxRequest(ecb, em, UnitAttackVfxRequestKind.MuzzleFlash, source, target, sourcePosition, targetPosition);
            }

            targetHealth.Current = math.max(0, targetHealth.Current - math.max(0, weapon.Damage));
            em.SetComponentData(target, targetHealth);

            UnitAttackSystem.TryEmitUnitUnderAttackAudio(
                em,
                target,
                now,
                source,
                math.max(0, weapon.Damage),
                nameof(BuildingDefenseAttackSystem));
            SetRecentAttacker(em, ecb, target, source, sourcePosition, ref pendingRecentAttackerAdds);
            SetDamageHealthBarVisibility(em, ecb, target, ref pendingHealthBarVisibilityAdds);

            if (showTracer)
                EnqueueAttackVfxRequest(ecb, em, UnitAttackVfxRequestKind.Impact, source, target, sourcePosition, targetPosition);
        }

        private static void SetTraceTarget(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity source,
            Entity target,
            float3 targetPosition,
            ref NativeParallelHashSet<Entity> pendingTraceTargetAdds)
        {
            int2 targetCell = new((int)math.round(targetPosition.x), (int)math.round(targetPosition.z));
            var engage = new EngageTarget
            {
                Target = target,
                Cell = targetCell,
                Position = targetPosition,
                IsCommanded = 0
            };

            if (em.HasComponent<EngageTarget>(source))
                em.SetComponentData(source, engage);
            else if (pendingTraceTargetAdds.Add(source))
                ecb.AddComponent(source, engage);
        }

        private static void SetRecentAttacker(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity target,
            Entity attacker,
            float3 attackerPosition,
            ref NativeParallelHashSet<Entity> pendingRecentAttackerAdds)
        {
            var recent = new RecentAttacker
            {
                Attacker = attacker,
                Cell = new int2((int)math.round(attackerPosition.x), (int)math.round(attackerPosition.z)),
                Position = attackerPosition
            };

            if (em.HasComponent<RecentAttacker>(target))
                em.SetComponentData(target, recent);
            else if (pendingRecentAttackerAdds.Add(target))
                ecb.AddComponent(target, recent);
        }

        private static void SetDamageHealthBarVisibility(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity target,
            ref NativeParallelHashSet<Entity> pendingHealthBarVisibilityAdds)
        {
            var visibility = new RecentDamageHealthBarVisibility
            {
                TimeRemaining = DamageHealthBarVisibleSeconds
            };

            if (em.HasComponent<RecentDamageHealthBarVisibility>(target))
                em.SetComponentData(target, visibility);
            else if (pendingHealthBarVisibilityAdds.Add(target))
                ecb.AddComponent(target, visibility);
        }

        private static void EnqueueAttackVfxRequest(
            EntityCommandBuffer ecb,
            EntityManager em,
            UnitAttackVfxRequestKind kind,
            Entity source,
            Entity target,
            float3 sourcePosition,
            float3 targetPosition)
        {
            if (!UnitAttackSystem.TryBuildAttackVfxRequest(em, kind, source, target, sourcePosition, targetPosition, out UnitAttackVfxRequest request))
                return;

            Entity requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, request);
        }

        private static void FindBestTargets(
            EntityManager em,
            NativeArray<Entity> targets,
            Entity source,
            byte sourceFaction,
            float3 sourcePosition,
            float range,
            out Entity target0,
            out Entity target1,
            out Entity target2,
            out Entity target3)
        {
            target0 = Entity.Null;
            target1 = Entity.Null;
            target2 = Entity.Null;
            target3 = Entity.Null;
            float dist0 = float.MaxValue;
            float dist1 = float.MaxValue;
            float dist2 = float.MaxValue;
            float dist3 = float.MaxValue;

            float rangeSq = math.max(0f, range) * math.max(0f, range);
            for (int i = 0; i < targets.Length; i++)
            {
                Entity candidate = targets[i];
                if (candidate == source ||
                    !TryGetLiveEnemyPosition(em, candidate, sourceFaction, out float3 candidatePosition))
                {
                    continue;
                }

                float3 delta = candidatePosition - sourcePosition;
                delta.y = 0f;
                float distSq = math.lengthsq(delta);
                if (distSq > rangeSq)
                    continue;

                InsertCandidate(
                    candidate,
                    distSq,
                    ref target0,
                    ref target1,
                    ref target2,
                    ref target3,
                    ref dist0,
                    ref dist1,
                    ref dist2,
                    ref dist3);
            }
        }

        private static bool IsLiveEnemyTarget(EntityManager em, Entity target, byte sourceFaction, float3 sourcePosition, float range)
        {
            if (!TryGetLiveEnemyPosition(em, target, sourceFaction, out float3 targetPosition))
                return false;

            float3 delta = targetPosition - sourcePosition;
            delta.y = 0f;
            float rangeSq = math.max(0f, range) * math.max(0f, range);
            return math.lengthsq(delta) <= rangeSq;
        }

        private static bool TryGetLiveEnemyPosition(EntityManager em, Entity candidate, byte sourceFaction, out float3 position)
        {
            position = default;
            if (candidate == Entity.Null ||
                !em.Exists(candidate) ||
                !em.HasComponent<Faction>(candidate) ||
                !em.HasComponent<UnitHealth>(candidate) ||
                !em.HasComponent<LocalTransform>(candidate) ||
                em.HasComponent<UnitAirMovement>(candidate) ||
                em.HasComponent<DebugFireTargetTag>(candidate))
            {
                return false;
            }

            if (em.GetComponentData<Faction>(candidate).Id == sourceFaction)
                return false;

            UnitHealth health = em.GetComponentData<UnitHealth>(candidate);
            if (health.Current <= 0)
                return false;

            position = em.GetComponentData<LocalTransform>(candidate).Position;
            return true;
        }

        private static void InsertCandidate(
            Entity candidate,
            float distSq,
            ref Entity target0,
            ref Entity target1,
            ref Entity target2,
            ref Entity target3,
            ref float dist0,
            ref float dist1,
            ref float dist2,
            ref float dist3)
        {
            if (distSq < dist0)
            {
                target3 = target2;
                dist3 = dist2;
                target2 = target1;
                dist2 = dist1;
                target1 = target0;
                dist1 = dist0;
                target0 = candidate;
                dist0 = distSq;
            }
            else if (distSq < dist1)
            {
                target3 = target2;
                dist3 = dist2;
                target2 = target1;
                dist2 = dist1;
                target1 = candidate;
                dist1 = distSq;
            }
            else if (distSq < dist2)
            {
                target3 = target2;
                dist3 = dist2;
                target2 = candidate;
                dist2 = distSq;
            }
            else if (distSq < dist3)
            {
                target3 = candidate;
                dist3 = distSq;
            }
        }

        private static Entity ResolveTargetForSlot(int slotIndex, Entity target0, Entity target1, Entity target2, Entity target3)
        {
            Entity selected = slotIndex switch
            {
                0 => target0,
                1 => target1,
                2 => target2,
                3 => target3,
                _ => Entity.Null
            };

            return selected != Entity.Null ? selected : target0;
        }
    }
}
