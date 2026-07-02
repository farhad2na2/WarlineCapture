using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    [UpdateAfter(typeof(AIEconomySystem))]
    [UpdateBefore(typeof(UnitPathfindingSystem))]
    public partial struct AIFactionControlSystem : ISystem
    {
        private const float LogIntervalSeconds = 10f;
        private const float ControlRefreshSeconds = 0.5f;
        private EntityQuery _buildingRuntimeBoundaryQuery;
        private EntityQuery _runtimeDiagnosticsQuery;
        private EntityQuery _diagnosticLogQueueQuery;
        private float _nextControlRefreshTime;

        public void OnCreate(ref SystemState state)
        {
            _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                ComponentType.ReadOnly<BuildingRuntimeFactionSummary>());
            _runtimeDiagnosticsQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
            _diagnosticLogQueueQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<AIDiagnosticLogComponent>());
            state.RequireForUpdate<FactionControlConfigTag>();
            state.RequireForUpdate<Faction>();
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0)
                return;

            double elapsedTime = SystemAPI.Time.ElapsedTime;
            float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
            if (now < _nextControlRefreshTime)
                return;

            _nextControlRefreshTime = now + ControlRefreshSeconds;

            bool shouldLogDiagnostics = ShouldQueueDiagnostics(_runtimeDiagnosticsQuery);
            Entity diagnosticQueueEntity = shouldLogDiagnostics ? EnsureDiagnosticQueue(ref state) : Entity.Null;
            DynamicBuffer<FactionControlEntry> controls = SystemAPI.GetSingletonBuffer<FactionControlEntry>();
            using NativeArray<FactionControlEntry> controlSnapshot = controls.ToNativeArray(Allocator.TempJob);
            using NativeArray<int> controlledUnitCounts = new NativeArray<int>(controlSnapshot.Length, Allocator.TempJob);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new ApplyFactionControlJob
            {
                Controls = controlSnapshot,
                ControlledUnitCounts = controlledUnitCounts,
                UnitGridLookup = SystemAPI.GetComponentLookup<UnitGrid>(true),
                StaticGridBlockerLookup = SystemAPI.GetComponentLookup<StaticGridBlocker>(true),
                AIControlledLookup = SystemAPI.GetComponentLookup<AIControlledTag>(true),
                ManualControlledLookup = SystemAPI.GetComponentLookup<ManualControlledTag>(true),
                ManualMoveOrderLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true),
                ManualMoveGroupLookup = SystemAPI.GetComponentLookup<ManualMoveGroupMemberTag>(true),
                UnitPathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true),
                UnitPathRetryCooldownLookup = SystemAPI.GetComponentLookup<UnitPathRetryCooldown>(true),
                EngageTargetLookup = SystemAPI.GetComponentLookup<EngageTarget>(true),
                AICombatOrderLookup = SystemAPI.GetComponentLookup<AICombatOrderTag>(true),
                UnitPathFollowLookup = SystemAPI.GetComponentLookup<UnitPathFollow>(true),
                UnitPathRangeLookup = SystemAPI.GetComponentLookup<UnitPathRange>(true),
                AutoWanderMoveLookup = SystemAPI.GetComponentLookup<AutoWanderMoveTag>(true),
                Ecb = ecb
            }.Schedule(state.Dependency);
            state.Dependency.Complete();

            for (int controlIndex = 0; controlIndex < controls.Length; controlIndex++)
            {
                FactionControlEntry control = controls[controlIndex];
                bool aiControlled = control.AIControlled != 0;
                int controlledUnits = controlledUnitCounts[controlIndex];

                bool shouldLog = shouldLogDiagnostics && now - control.LastLogTime >= LogIntervalSeconds;
                if (shouldLog)
                {
                    control.LastLogTime = now;
                    controls[controlIndex] = control;
                    TryGetFactionBuildingCount(ref state, control.FactionId, out int controlledBuildings);
                    EnqueueDiagnostic(
                        ref state,
                        diagnosticQueueEntity,
                        $"[AIControlMode] faction={control.FactionId} mode={(aiControlled ? "Auto" : "Manual")} controlledUnits={controlledUnits} controlledBuildings={controlledBuildings}");
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private bool TryGetFactionBuildingCount(ref SystemState state, byte factionId, out int buildingCount)
        {
            buildingCount = 0;
            if (_buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
                return false;

            Entity entity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
            if (!state.EntityManager.HasBuffer<BuildingRuntimeFactionSummary>(entity))
                return false;

            DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
                state.EntityManager.GetBuffer<BuildingRuntimeFactionSummary>(entity, true);
            for (int i = 0; i < summaries.Length; i++)
            {
                BuildingRuntimeFactionSummary summary = summaries[i];
                if (summary.FactionId != factionId)
                    continue;

                buildingCount = summary.BuildingCount;
                return true;
            }

            return false;
        }

        private static bool ShouldQueueDiagnostics(EntityQuery runtimeDiagnosticsQuery)
        {
            if (InitialUnitsRuntimeState.VerboseAILogs)
                return true;

            return runtimeDiagnosticsQuery.CalculateEntityCount() == 1 &&
                runtimeDiagnosticsQuery.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
        }

        private Entity EnsureDiagnosticQueue(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            if (_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
            {
                Entity queueEntity = em.CreateEntity(typeof(AIDiagnosticLogQueueComponent));
                em.SetName(queueEntity, "AIDiagnosticLogQueue");
                em.AddBuffer<AIDiagnosticLogComponent>(queueEntity);
                return queueEntity;
            }

            return _diagnosticLogQueueQuery.GetSingletonEntity();
        }

        private void EnqueueDiagnostic(ref SystemState state, Entity queueEntity, FixedString512Bytes message)
        {
            EntityManager em = state.EntityManager;
            DynamicBuffer<AIDiagnosticLogComponent> logs = em.GetBuffer<AIDiagnosticLogComponent>(queueEntity);
            logs.Add(new AIDiagnosticLogComponent { Message = message });
        }

        [BurstCompile]
        [WithAll(typeof(Faction))]
        private partial struct ApplyFactionControlJob : IJobEntity
        {
            [ReadOnly] public NativeArray<FactionControlEntry> Controls;
            public NativeArray<int> ControlledUnitCounts;
            [ReadOnly] public ComponentLookup<UnitGrid> UnitGridLookup;
            [ReadOnly] public ComponentLookup<StaticGridBlocker> StaticGridBlockerLookup;
            [ReadOnly] public ComponentLookup<AIControlledTag> AIControlledLookup;
            [ReadOnly] public ComponentLookup<ManualControlledTag> ManualControlledLookup;
            [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveOrderLookup;
            [ReadOnly] public ComponentLookup<ManualMoveGroupMemberTag> ManualMoveGroupLookup;
            [ReadOnly] public ComponentLookup<UnitPathRequest> UnitPathRequestLookup;
            [ReadOnly] public ComponentLookup<UnitPathRetryCooldown> UnitPathRetryCooldownLookup;
            [ReadOnly] public ComponentLookup<EngageTarget> EngageTargetLookup;
            [ReadOnly] public ComponentLookup<AICombatOrderTag> AICombatOrderLookup;
            [ReadOnly] public ComponentLookup<UnitPathFollow> UnitPathFollowLookup;
            [ReadOnly] public ComponentLookup<UnitPathRange> UnitPathRangeLookup;
            [ReadOnly] public ComponentLookup<AutoWanderMoveTag> AutoWanderMoveLookup;
            public EntityCommandBuffer Ecb;

            private void Execute(Entity entity, in Faction faction)
            {
                for (int controlIndex = 0; controlIndex < Controls.Length; controlIndex++)
                {
                    FactionControlEntry control = Controls[controlIndex];
                    if (faction.Id != control.FactionId)
                        continue;

                    if (UnitGridLookup.HasComponent(entity) && !StaticGridBlockerLookup.HasComponent(entity))
                        ControlledUnitCounts[controlIndex]++;

                    if (control.AIControlled != 0)
                        ApplyAIControl(entity);
                    else
                        ApplyManualControl(entity);

                    return;
                }
            }

            private void ApplyAIControl(Entity entity)
            {
                AddIfMissing<AIControlledTag>(entity, AIControlledLookup);
                RemoveIfPresent<ManualControlledTag>(entity, ManualControlledLookup);
                RemoveIfPresent<ManualMoveOrderTag>(entity, ManualMoveOrderLookup);
                RemoveIfPresent<ManualMoveGroupMemberTag>(entity, ManualMoveGroupLookup);
                RemoveIfPresent<UnitPathRequest>(entity, UnitPathRequestLookup);
                RemoveIfPresent<UnitPathRetryCooldown>(entity, UnitPathRetryCooldownLookup);

                if (!EngageTargetLookup.HasComponent(entity))
                    return;

                EngageTarget target = EngageTargetLookup[entity];
                if (target.IsCommanded != 0)
                    Ecb.RemoveComponent<EngageTarget>(entity);
            }

            private void ApplyManualControl(Entity entity)
            {
                AddIfMissing<ManualControlledTag>(entity, ManualControlledLookup);
                RemoveIfPresent<AIControlledTag>(entity, AIControlledLookup);

                if (!AICombatOrderLookup.HasComponent(entity))
                    return;

                RemoveIfPresent<AICombatOrderTag>(entity, AICombatOrderLookup);
                RemoveIfPresent<EngageTarget>(entity, EngageTargetLookup);
                RemoveIfPresent<UnitPathRequest>(entity, UnitPathRequestLookup);
                RemoveIfPresent<UnitPathFollow>(entity, UnitPathFollowLookup);
                RemoveIfPresent<UnitPathRange>(entity, UnitPathRangeLookup);
                RemoveIfPresent<AutoWanderMoveTag>(entity, AutoWanderMoveLookup);
            }

            private void AddIfMissing<T>(Entity entity, ComponentLookup<T> lookup)
                where T : unmanaged, IComponentData
            {
                if (!lookup.HasComponent(entity))
                    Ecb.AddComponent<T>(entity);
            }

            private void RemoveIfPresent<T>(Entity entity, ComponentLookup<T> lookup)
                where T : unmanaged, IComponentData
            {
                if (lookup.HasComponent(entity))
                    Ecb.RemoveComponent<T>(entity);
            }
        }
    }
}
