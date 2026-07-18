using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    [UpdateAfter(typeof(AISquadSystem))]
    public partial struct AITargetingSystem : ISystem
    {
        private const float LogIntervalSeconds = 6f;
        private const float TargetRefreshSeconds = 0.5f;
        private EntityQuery _squadQuery;
        private EntityQuery _targetQuery;
        private EntityQuery _targetPriorityQuery;
        private EntityQuery _runtimeDiagnosticsQuery;
        private EntityQuery _diagnosticLogQueueQuery;
        private EntityTypeHandle _entityType;
        private ComponentTypeHandle<AISquad> _squadType;
        private ComponentTypeHandle<Faction> _factionType;
        private ComponentTypeHandle<UnitGrid> _unitGridType;
        private ComponentTypeHandle<UnitHealth> _unitHealthType;
        private ComponentTypeHandle<AITargetPrioritySetting> _targetPriorityType;
        private ComponentLookup<UnitAttack> _unitAttackLookup;
        private ComponentLookup<UnitCombat> _unitCombatLookup;
        private ComponentLookup<StaticGridBlocker> _staticGridBlockerLookup;
        private ComponentLookup<GridBlockerSize> _gridBlockerSizeLookup;
        private ComponentLookup<UnitResourceHauler> _resourceHaulerLookup;
        private ComponentLookup<FuelLogisticsOilSourceTag> _fuelLogisticsOilSourceLookup;
        private ComponentLookup<FuelLogisticsRefineryInputTag> _fuelLogisticsRefineryInputLookup;
        private ComponentLookup<FuelLogisticsRefineryOutputTag> _fuelLogisticsRefineryOutputLookup;
        private ComponentLookup<FuelLogisticsFuelStorageTag> _fuelLogisticsFuelStorageLookup;
        private NativeList<TargetingDiagnosticEvent> _diagnosticEvents;
        private float _nextTargetRefreshTime;

        private enum TargetReason : byte
        {
            None,
            Threat,
            Economy,
            Unit,
            Units,
            Production
        }

        public void OnCreate(ref SystemState state)
        {
            _squadQuery = state.GetEntityQuery(ComponentType.ReadWrite<AISquad>());
            _targetQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitHealth>());
            _targetPriorityQuery = state.GetEntityQuery(ComponentType.ReadOnly<AITargetPrioritySetting>());
            _runtimeDiagnosticsQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
            _diagnosticLogQueueQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<AIDiagnosticLogComponent>());
            _entityType = state.GetEntityTypeHandle();
            _squadType = state.GetComponentTypeHandle<AISquad>(false);
            _factionType = state.GetComponentTypeHandle<Faction>(true);
            _unitGridType = state.GetComponentTypeHandle<UnitGrid>(true);
            _unitHealthType = state.GetComponentTypeHandle<UnitHealth>(true);
            _targetPriorityType = state.GetComponentTypeHandle<AITargetPrioritySetting>(true);
            _unitAttackLookup = state.GetComponentLookup<UnitAttack>(true);
            _unitCombatLookup = state.GetComponentLookup<UnitCombat>(true);
            _staticGridBlockerLookup = state.GetComponentLookup<StaticGridBlocker>(true);
            _gridBlockerSizeLookup = state.GetComponentLookup<GridBlockerSize>(true);
            _resourceHaulerLookup = state.GetComponentLookup<UnitResourceHauler>(true);
            _fuelLogisticsOilSourceLookup = state.GetComponentLookup<FuelLogisticsOilSourceTag>(true);
            _fuelLogisticsRefineryInputLookup = state.GetComponentLookup<FuelLogisticsRefineryInputTag>(true);
            _fuelLogisticsRefineryOutputLookup = state.GetComponentLookup<FuelLogisticsRefineryOutputTag>(true);
            _fuelLogisticsFuelStorageLookup = state.GetComponentLookup<FuelLogisticsFuelStorageTag>(true);
            _diagnosticEvents = new NativeList<TargetingDiagnosticEvent>(Allocator.Persistent);
            state.RequireForUpdate<AISquad>();
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_diagnosticEvents.IsCreated)
                _diagnosticEvents.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0)
                return;

            double elapsedTime = SystemAPI.Time.ElapsedTime;
            float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
            if (now < _nextTargetRefreshTime)
                return;

            _nextTargetRefreshTime = now + TargetRefreshSeconds;

            bool shouldLog = ShouldQueueDiagnostics(_runtimeDiagnosticsQuery);
            Entity diagnosticQueueEntity = shouldLog ? EnsureDiagnosticQueue(ref state) : Entity.Null;

            _entityType.Update(ref state);
            _squadType.Update(ref state);
            _factionType.Update(ref state);
            _unitGridType.Update(ref state);
            _unitHealthType.Update(ref state);
            _targetPriorityType.Update(ref state);
            _unitAttackLookup.Update(ref state);
            _unitCombatLookup.Update(ref state);
            _staticGridBlockerLookup.Update(ref state);
            _gridBlockerSizeLookup.Update(ref state);
            _resourceHaulerLookup.Update(ref state);
            _fuelLogisticsOilSourceLookup.Update(ref state);
            _fuelLogisticsRefineryInputLookup.Update(ref state);
            _fuelLogisticsRefineryOutputLookup.Update(ref state);
            _fuelLogisticsFuelStorageLookup.Update(ref state);

            using NativeArray<ArchetypeChunk> targetChunks = _targetQuery.ToArchetypeChunkArray(Allocator.TempJob);
            using NativeArray<ArchetypeChunk> targetPriorityChunks = _targetPriorityQuery.ToArchetypeChunkArray(Allocator.TempJob);
            _diagnosticEvents.Clear();

            JobHandle assignTargetsHandle = new AssignTargetsJob
            {
                Now = now,
                ShouldLog = (byte)(shouldLog ? 1 : 0),
                TargetChunks = targetChunks,
                TargetPriorityChunks = targetPriorityChunks,
                EntityType = _entityType,
                SquadType = _squadType,
                FactionType = _factionType,
                UnitGridType = _unitGridType,
                UnitHealthType = _unitHealthType,
                TargetPriorityType = _targetPriorityType,
                UnitAttackLookup = _unitAttackLookup,
                UnitCombatLookup = _unitCombatLookup,
                StaticGridBlockerLookup = _staticGridBlockerLookup,
                GridBlockerSizeLookup = _gridBlockerSizeLookup,
                ResourceHaulerLookup = _resourceHaulerLookup,
                FuelLogisticsOilSourceLookup = _fuelLogisticsOilSourceLookup,
                FuelLogisticsRefineryInputLookup = _fuelLogisticsRefineryInputLookup,
                FuelLogisticsRefineryOutputLookup = _fuelLogisticsRefineryOutputLookup,
                FuelLogisticsFuelStorageLookup = _fuelLogisticsFuelStorageLookup,
                Diagnostics = _diagnosticEvents
            }.Schedule(_squadQuery, state.Dependency);
            assignTargetsHandle.Complete();

            if (!shouldLog)
                return;

            for (int i = 0; i < _diagnosticEvents.Length; i++)
            {
                TargetingDiagnosticEvent diagnostic = _diagnosticEvents[i];
                if (diagnostic.HasTarget == 0)
                {
                    EnqueueDiagnostic(ref state, diagnosticQueueEntity, $"[AITarget] faction={diagnostic.FactionId} squad={diagnostic.SquadId} result=NoTarget");
                    continue;
                }

                EnqueueDiagnostic(ref state, diagnosticQueueEntity, $"[AITarget] faction={diagnostic.FactionId} squad={diagnostic.SquadId} target={diagnostic.TargetKind} score={diagnostic.Score} reason={TargetReasonLabel(diagnostic.Reason)} targetFaction={diagnostic.TargetFactionId} targetCell={diagnostic.TargetCell}");
            }
        }

        private static bool ShouldQueueDiagnostics(EntityQuery runtimeDiagnosticsQuery)
        {
            return runtimeDiagnosticsQuery.CalculateEntityCount() == 1 &&
                runtimeDiagnosticsQuery.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
        }

        private Entity EnsureDiagnosticQueue(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            if (!_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
                return _diagnosticLogQueueQuery.GetSingletonEntity();

            Entity queueEntity = em.CreateEntity(typeof(AIDiagnosticLogQueueComponent));
            em.SetName(queueEntity, "AIDiagnosticLogQueue");
            em.AddBuffer<AIDiagnosticLogComponent>(queueEntity);
            return queueEntity;
        }

        private void EnqueueDiagnostic(ref SystemState state, Entity queueEntity, FixedString512Bytes message)
        {
            if (queueEntity == Entity.Null)
            {
                queueEntity = EnsureDiagnosticQueue(ref state);
            }

            DynamicBuffer<AIDiagnosticLogComponent> logs = state.EntityManager.GetBuffer<AIDiagnosticLogComponent>(queueEntity);
            logs.Add(new AIDiagnosticLogComponent { Message = message });
        }

        private struct TargetingDiagnosticEvent
        {
            public byte HasTarget;
            public byte FactionId;
            public int SquadId;
            public AITargetKind TargetKind;
            public int Score;
            public TargetReason Reason;
            public byte TargetFactionId;
            public int2 TargetCell;
        }

        [BurstCompile]
        private struct AssignTargetsJob : IJobChunk
        {
            public float Now;
            public byte ShouldLog;
            [ReadOnly] public NativeArray<ArchetypeChunk> TargetChunks;
            [ReadOnly] public NativeArray<ArchetypeChunk> TargetPriorityChunks;
            [ReadOnly] public EntityTypeHandle EntityType;
            public ComponentTypeHandle<AISquad> SquadType;
            [ReadOnly] public ComponentTypeHandle<Faction> FactionType;
            [ReadOnly] public ComponentTypeHandle<UnitGrid> UnitGridType;
            [ReadOnly] public ComponentTypeHandle<UnitHealth> UnitHealthType;
            [ReadOnly] public ComponentTypeHandle<AITargetPrioritySetting> TargetPriorityType;
            [ReadOnly] public ComponentLookup<UnitAttack> UnitAttackLookup;
            [ReadOnly] public ComponentLookup<UnitCombat> UnitCombatLookup;
            [ReadOnly] public ComponentLookup<StaticGridBlocker> StaticGridBlockerLookup;
            [ReadOnly] public ComponentLookup<GridBlockerSize> GridBlockerSizeLookup;
            [ReadOnly] public ComponentLookup<UnitResourceHauler> ResourceHaulerLookup;
            [ReadOnly] public ComponentLookup<FuelLogisticsOilSourceTag> FuelLogisticsOilSourceLookup;
            [ReadOnly] public ComponentLookup<FuelLogisticsRefineryInputTag> FuelLogisticsRefineryInputLookup;
            [ReadOnly] public ComponentLookup<FuelLogisticsRefineryOutputTag> FuelLogisticsRefineryOutputLookup;
            [ReadOnly] public ComponentLookup<FuelLogisticsFuelStorageTag> FuelLogisticsFuelStorageLookup;
            public NativeList<TargetingDiagnosticEvent> Diagnostics;

            public void Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                NativeArray<AISquad> squads = chunk.GetNativeArray(ref SquadType);
                for (int i = 0; i < squads.Length; i++)
                {
                    AISquad squad = squads[i];
                    if (squad.Purpose != (byte)AISquadPurpose.Attack)
                        continue;

                    AITargetPriority priority = ResolveTargetPriority(TargetPriorityChunks, ref TargetPriorityType, squad.FactionId);
                    if (!TrySelectTarget(
                            TargetChunks,
                            EntityType,
                            ref FactionType,
                            ref UnitGridType,
                            ref UnitHealthType,
                            UnitAttackLookup,
                            UnitCombatLookup,
                            StaticGridBlockerLookup,
                            GridBlockerSizeLookup,
                            ResourceHaulerLookup,
                            FuelLogisticsOilSourceLookup,
                            FuelLogisticsRefineryInputLookup,
                            FuelLogisticsRefineryOutputLookup,
                            FuelLogisticsFuelStorageLookup,
                            squad,
                            priority,
                            out Entity target,
                            out int2 targetCell,
                            out byte targetFaction,
                            out AITargetKind kind,
                            out int score,
                            out TargetReason reason))
                    {
                        if (Now - squad.LastLogTime >= LogIntervalSeconds)
                        {
                            squad.LastLogTime = Now;
                            squads[i] = squad;
                            if (ShouldLog != 0)
                            {
                                Diagnostics.Add(new TargetingDiagnosticEvent
                                {
                                    HasTarget = 0,
                                    FactionId = squad.FactionId,
                                    SquadId = squad.SquadId
                                });
                            }
                        }

                        continue;
                    }

                    bool changed =
                        squad.TargetEntity != target ||
                        squad.TargetCell.x != targetCell.x ||
                        squad.TargetCell.y != targetCell.y ||
                        squad.TargetScore != score ||
                        squad.TargetKind != (byte)kind;

                    squad.TargetEntity = target;
                    squad.TargetFactionId = targetFaction;
                    squad.TargetKind = (byte)kind;
                    squad.TargetCell = targetCell;
                    squad.TargetScore = score;

                    if (changed || Now - squad.LastLogTime >= LogIntervalSeconds)
                    {
                        squad.LastLogTime = Now;
                        squads[i] = squad;
                        if (ShouldLog != 0)
                        {
                            Diagnostics.Add(new TargetingDiagnosticEvent
                            {
                                HasTarget = 1,
                                FactionId = squad.FactionId,
                                SquadId = squad.SquadId,
                                TargetKind = kind,
                                Score = score,
                                Reason = reason,
                                TargetFactionId = targetFaction,
                                TargetCell = targetCell
                            });
                        }
                    }
                    else
                    {
                        squads[i] = squad;
                    }
                }
            }
        }

        private static bool TrySelectTarget(
            NativeArray<ArchetypeChunk> targetChunks,
            EntityTypeHandle entityType,
            ref ComponentTypeHandle<Faction> factionType,
            ref ComponentTypeHandle<UnitGrid> unitGridType,
            ref ComponentTypeHandle<UnitHealth> unitHealthType,
            ComponentLookup<UnitAttack> unitAttackLookup,
            ComponentLookup<UnitCombat> unitCombatLookup,
            ComponentLookup<StaticGridBlocker> staticGridBlockerLookup,
            ComponentLookup<GridBlockerSize> gridBlockerSizeLookup,
            ComponentLookup<UnitResourceHauler> resourceHaulerLookup,
            ComponentLookup<FuelLogisticsOilSourceTag> fuelLogisticsOilSourceLookup,
            ComponentLookup<FuelLogisticsRefineryInputTag> fuelLogisticsRefineryInputLookup,
            ComponentLookup<FuelLogisticsRefineryOutputTag> fuelLogisticsRefineryOutputLookup,
            ComponentLookup<FuelLogisticsFuelStorageTag> fuelLogisticsFuelStorageLookup,
            AISquad squad,
            AITargetPriority priority,
            out Entity bestTarget,
            out int2 bestCell,
            out byte bestFaction,
            out AITargetKind bestKind,
            out int bestScore,
            out TargetReason bestReason)
        {
            bestTarget = Entity.Null;
            bestCell = squad.TargetCell;
            bestFaction = squad.TargetFactionId;
            bestKind = AITargetKind.None;
            bestScore = int.MinValue;
            bestReason = TargetReason.None;

            for (int chunkIndex = 0; chunkIndex < targetChunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = targetChunks[chunkIndex];
                NativeArray<Entity> targets = chunk.GetNativeArray(entityType);
                NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);
                NativeArray<UnitGrid> grids = chunk.GetNativeArray(ref unitGridType);
                NativeArray<UnitHealth> healths = chunk.GetNativeArray(ref unitHealthType);

                for (int i = 0; i < targets.Length; i++)
                {
                    Entity target = targets[i];
                    Faction faction = factions[i];
                    if (faction.Id == FactionIdentity.NeutralFactionId ||
                        faction.Id == squad.FactionId)
                        continue;

                    UnitHealth health = healths[i];
                    if (health.Current <= 0)
                        continue;

                    UnitGrid grid = grids[i];
                    AITargetKind kind = ResolveTargetKind(
                        unitAttackLookup,
                        unitCombatLookup,
                        staticGridBlockerLookup,
                        gridBlockerSizeLookup,
                        target);
                    int score = ScoreTarget(
                        resourceHaulerLookup,
                        fuelLogisticsOilSourceLookup,
                        fuelLogisticsRefineryInputLookup,
                        fuelLogisticsRefineryOutputLookup,
                        fuelLogisticsFuelStorageLookup,
                        target,
                        kind,
                        priority,
                        squad.RallyCell,
                        grid.Cell,
                        health,
                        out TargetReason reason);
                    if (score <= bestScore)
                        continue;

                    bestTarget = target;
                    bestCell = grid.Cell;
                    bestFaction = faction.Id;
                    bestKind = kind;
                    bestScore = score;
                    bestReason = reason;
                }
            }

            return bestTarget != Entity.Null;
        }

        private static AITargetKind ResolveTargetKind(
            ComponentLookup<UnitAttack> unitAttackLookup,
            ComponentLookup<UnitCombat> unitCombatLookup,
            ComponentLookup<StaticGridBlocker> staticGridBlockerLookup,
            ComponentLookup<GridBlockerSize> gridBlockerSizeLookup,
            Entity target)
        {
            if (unitAttackLookup.HasComponent(target) || unitCombatLookup.HasComponent(target))
                return AITargetKind.Threat;
            if (staticGridBlockerLookup.HasComponent(target) || gridBlockerSizeLookup.HasComponent(target))
                return AITargetKind.Building;
            return AITargetKind.Unit;
        }

        private static AITargetPriority ResolveTargetPriority(
            NativeArray<ArchetypeChunk> chunks,
            ref ComponentTypeHandle<AITargetPrioritySetting> targetPriorityType,
            byte factionId)
        {
            if (!chunks.IsCreated)
                return AITargetPriority.Balanced;

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<AITargetPrioritySetting> settings = chunks[chunkIndex].GetNativeArray(ref targetPriorityType);
                for (int i = 0; i < settings.Length; i++)
                {
                    AITargetPrioritySetting setting = settings[i];
                    if (setting.FactionId == factionId)
                        return (AITargetPriority)setting.Priority;
                }
            }

            return AITargetPriority.Balanced;
        }

        private static int ScoreTarget(
            ComponentLookup<UnitResourceHauler> resourceHaulerLookup,
            ComponentLookup<FuelLogisticsOilSourceTag> fuelLogisticsOilSourceLookup,
            ComponentLookup<FuelLogisticsRefineryInputTag> fuelLogisticsRefineryInputLookup,
            ComponentLookup<FuelLogisticsRefineryOutputTag> fuelLogisticsRefineryOutputLookup,
            ComponentLookup<FuelLogisticsFuelStorageTag> fuelLogisticsFuelStorageLookup,
            Entity target,
            AITargetKind kind,
            AITargetPriority priority,
            int2 origin,
            int2 targetCell,
            UnitHealth health,
            out TargetReason reason)
        {
            int distance = math.abs(targetCell.x - origin.x) + math.abs(targetCell.y - origin.y);
            int healthValue = math.clamp(health.Max / 10, 0, 30);
            int score = 100 - math.min(distance, 100) + healthValue;

            switch (kind)
            {
                case AITargetKind.Threat:
                    score += 45;
                    reason = TargetReason.Threat;
                    break;
                case AITargetKind.Building:
                    score += 35;
                    reason = TargetReason.Economy;
                    break;
                default:
                    score += 10;
                    reason = TargetReason.Unit;
                    break;
            }

            bool isResourceHauler = resourceHaulerLookup.HasComponent(target);
            if (isResourceHauler)
            {
                score += 20;
                reason = TargetReason.Economy;
            }

            bool isFuelLogisticsInfrastructure =
                fuelLogisticsOilSourceLookup.HasComponent(target) ||
                fuelLogisticsRefineryInputLookup.HasComponent(target) ||
                fuelLogisticsRefineryOutputLookup.HasComponent(target) ||
                fuelLogisticsFuelStorageLookup.HasComponent(target);
            if (isFuelLogisticsInfrastructure)
            {
                score += 20;
                reason = TargetReason.Economy;
            }

            switch (priority)
            {
                case AITargetPriority.Units:
                    if (kind == AITargetKind.Unit || kind == AITargetKind.Threat)
                    {
                        score += 35;
                        reason = kind == AITargetKind.Threat ? TargetReason.Threat : TargetReason.Units;
                    }
                    else if (kind == AITargetKind.Building)
                    {
                        score -= 10;
                    }
                    break;
                case AITargetPriority.Economy:
                    if (isResourceHauler || isFuelLogisticsInfrastructure)
                    {
                        score += 50;
                        reason = TargetReason.Economy;
                    }
                    else if (kind == AITargetKind.Building)
                    {
                        score += 25;
                        reason = TargetReason.Economy;
                    }
                    break;
                case AITargetPriority.Production:
                    if (kind == AITargetKind.Building)
                    {
                        score += 45;
                        reason = TargetReason.Production;
                    }
                    else if (kind == AITargetKind.Unit)
                    {
                        score -= 10;
                    }
                    break;
            }

            return score;
        }

        private static string TargetReasonLabel(TargetReason reason)
        {
            return reason switch
            {
                TargetReason.Threat => "Threat",
                TargetReason.Economy => "Economy",
                TargetReason.Unit => "Unit",
                TargetReason.Units => "Units",
                TargetReason.Production => "Production",
                _ => "None"
            };
        }
    }
}
