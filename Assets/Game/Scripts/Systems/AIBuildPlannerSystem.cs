using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;
using FactionEconomyRecord = Game.Runtime.AIBuildPlanningReadSystemHelper.FactionEconomyRecord;

namespace Game.Runtime
{
    [UpdateAfter(typeof(AIFactionControlSystem))]
    public partial struct AIBuildPlannerSystem : ISystem
    {
        private const float LogIntervalSeconds = 10f;
        private int _nextBuildSpawnRequestId;
        private EntityQuery _buildingRuntimeBoundaryQuery;
        private EntityQuery _runtimeDiagnosticsQuery;
        private EntityQuery _diagnosticLogQueueQuery;
        private EntityQuery _planQuery;
        private EntityQuery _economyQuery;
        private EntityQuery _gridQuery;
        private EntityTypeHandle _entityType;
        private ComponentTypeHandle<FactionEconomy> _economyType;
        private ComponentTypeHandle<FactionTacticalMaterialsComponent> _materialsType;

        internal enum BuildDecisionResult : byte
        {
            None = 0,
            Pending = 1,
            MissingConfig = 2,
            InsufficientFunds = 3,
            InsufficientMaterials = 4,
            InsufficientCreditsAndMaterials = 5,
            InvalidResources = 6,
            Request = 7
        }

        internal struct BuildDecision
        {
            public BuildDecisionResult Result;
            public int EntryIndex;
            public FixedString128Bytes BuildingId;
            public BuildingConfiguredSpawnableReadModel Spawnable;
            public int Cost;
            public int MaterialsCost;
            public int2 PreferredOrigin;
        }

        public void OnCreate(ref SystemState state)
        {
            _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                ComponentType.ReadOnly<BuildingConfiguredSpawnableReadModel>(),
                ComponentType.ReadOnly<BuildingRuntimeFactionSummary>(),
                ComponentType.ReadOnly<BuildingRuntimeOwnedBuildingSummary>(),
                ComponentType.ReadWrite<BuildingRuntimeSpawnRequest>());
            _runtimeDiagnosticsQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
            _diagnosticLogQueueQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
                ComponentType.ReadWrite<AIDiagnosticLogComponent>());
            _planQuery = state.GetEntityQuery(ComponentType.ReadWrite<AIBuildPlan>(), ComponentType.ReadOnly<AIBuildPlanEntry>());
            _economyQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<FactionEconomy>(),
                ComponentType.ReadOnly<FactionTacticalMaterialsComponent>());
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            _entityType = state.GetEntityTypeHandle();
            _economyType = state.GetComponentTypeHandle<FactionEconomy>(true);
            _materialsType = state.GetComponentTypeHandle<FactionTacticalMaterialsComponent>(true);
            state.RequireForUpdate(_buildingRuntimeBoundaryQuery);
            state.RequireForUpdate<AIBuildPlan>();
            state.RequireForUpdate<FactionEconomy>();
            state.RequireForUpdate<FactionTacticalMaterialsComponent>();
            state.RequireForUpdate(_gridQuery);
            state.RequireForUpdate<RuntimeGameplayStateComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0)
                return;

            if (!TryGetBuildingRuntimeStateEntity(ref state, out Entity boundaryEntity))
                return;

            double elapsedTime = SystemAPI.Time.ElapsedTime;
            float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
            Entity gridEntity = _gridQuery.GetSingletonEntity();
            GridConfig grid = state.EntityManager.GetComponentData<GridConfig>(gridEntity);
            bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
            DynamicBuffer<FactionControlEntry> controls = hasControls
                ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true)
                : default;
            bool shouldLog = ShouldQueueDiagnostics(_runtimeDiagnosticsQuery);

            EntityManager em = state.EntityManager;
            _entityType.Update(ref state);
            _economyType.Update(ref state);
            _materialsType.Update(ref state);
            using NativeArray<ArchetypeChunk> planChunks = _planQuery.ToArchetypeChunkArray(Allocator.Temp);
            using NativeList<Entity> planEntities = new(_planQuery.CalculateEntityCount(), Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < planChunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = planChunks[chunkIndex].GetNativeArray(_entityType);
                planEntities.AddRange(entities);
            }

            NativeList<FactionEconomyRecord> economyRecords =
                AIBuildPlanningReadSystemHelper.BuildFactionEconomyRecords(
                    _economyQuery, _entityType, _economyType, _materialsType);
            try
            {
                for (int i = 0; i < planEntities.Length; i++)
                {
                    Entity planEntity = planEntities[i];
                    AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(planEntity);
                    if (plan.Enabled == 0 ||
                        !AIBuildPlanningReadSystemHelper.IsFactionAIControlled(
                            plan.FactionId, hasControls, controls))
                    {
                        ClearMaterialsRecoveryNeed(em, planEntity, plan.FactionId, now);
                        continue;
                    }

                    if (!AIBuildPlanningReadSystemHelper.TryFindEconomyRecord(
                            economyRecords,
                            plan.FactionId,
                            out int economyRecordIndex,
                            out FactionEconomyRecord economyRecord))
                    {
                        ClearMaterialsRecoveryNeed(em, planEntity, plan.FactionId, now);
                        continue;
                    }

                    Entity economyEntity = economyRecord.Entity;
                    FactionEconomy economy = economyRecord.Economy;
                    FactionTacticalMaterialsComponent materials = economyRecord.Materials;
                    bool hasUnsettledRequest = ProcessCompletedSpawnRequests(
                        ref state,
                        boundaryEntity,
                        planEntity,
                        ref plan,
                        ref economy,
                        ref materials,
                        shouldLog);
                    em.SetComponentData(economyEntity, economy);
                    if (AIBuildPlanningReadSystemHelper.HasMaterialsChanged(
                            economyRecord.Materials,
                            materials))
                        em.SetComponentData(economyEntity, materials);
                    economyRecords[economyRecordIndex] = new FactionEconomyRecord(economyEntity, economy, materials);

                    if (hasUnsettledRequest)
                    {
                        ClearMaterialsRecoveryNeed(em, planEntity, plan.FactionId, now);
                        plan.LastBuildTime = now;
                        em.SetComponentData(planEntity, plan);
                        continue;
                    }

                    float interval = math.max(0.1f, plan.BuildIntervalSeconds);
                    if (now - plan.LastBuildTime < interval)
                    {
                        em.SetComponentData(planEntity, plan);
                        continue;
                    }

                    DynamicBuffer<AIBuildPlanEntry> entries = em.GetBuffer<AIBuildPlanEntry>(planEntity, true);
                    if (entries.Length == 0)
                    {
                        ClearMaterialsRecoveryNeed(em, planEntity, plan.FactionId, now);
                        LogNoPlanIfNeeded(ref state, ref plan, now, shouldLog);
                        em.SetComponentData(planEntity, plan);
                        continue;
                    }

                    if (plan.BaseCenterCell.x <= 0 && plan.BaseCenterCell.y <= 0)
                    {
                        plan.BaseCenterCell =
                            AIBuildPlanningPolicySystemHelper.ResolveDefaultBaseCenter(plan.FactionId, grid);
                    }

                    BuildDecision decision = SelectBuildDecision(
                        entries,
                        em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity, true),
                        em.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(boundaryEntity, true),
                        em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity, true),
                        plan,
                        economy,
                        materials);
                    bool handledDecision = decision.Result != BuildDecisionResult.None;
                    switch (decision.Result)
                    {
                        case BuildDecisionResult.Pending:
                            ClearMaterialsRecoveryNeed(em, planEntity, plan.FactionId, now);
                            plan.LastBuildTime = now;
                            break;

                        case BuildDecisionResult.MissingConfig:
                            ClearMaterialsRecoveryNeed(em, planEntity, plan.FactionId, now);
                            plan.NextBuildIndex = decision.EntryIndex + 1;
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.BuildingId.ToString()} result=MissingConfig");
                            break;

                        case BuildDecisionResult.InsufficientFunds:
                            ClearMaterialsRecoveryNeed(em, planEntity, plan.FactionId, now);
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} cost={decision.Cost} materialsCost={decision.MaterialsCost} result=InsufficientFunds money={economy.Money} materials={materials.Current}");
                            break;

                        case BuildDecisionResult.InsufficientMaterials:
                            PublishMaterialsRecoveryNeed(
                                em,
                                planEntity,
                                plan.FactionId,
                                decision.Cost,
                                decision.MaterialsCost,
                                materials,
                                now);
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} cost={decision.Cost} materialsCost={decision.MaterialsCost} result=InsufficientMaterials money={economy.Money} materials={materials.Current}");
                            break;

                        case BuildDecisionResult.InsufficientCreditsAndMaterials:
                            PublishMaterialsRecoveryNeed(
                                em,
                                planEntity,
                                plan.FactionId,
                                decision.Cost,
                                decision.MaterialsCost,
                                materials,
                                now);
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} cost={decision.Cost} materialsCost={decision.MaterialsCost} result=InsufficientCreditsAndMaterials money={economy.Money} materials={materials.Current}");
                            break;

                        case BuildDecisionResult.InvalidResources:
                            ClearMaterialsRecoveryNeed(em, planEntity, plan.FactionId, now);
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} result=InvalidResources");
                            break;

                        case BuildDecisionResult.Request:
                            ClearMaterialsRecoveryNeed(em, planEntity, plan.FactionId, now);
                            if (FactionConstructionResourceUtilitySystemHelper.TrySpend(
                                    ref economy,
                                    ref materials,
                                    decision.Cost,
                                    decision.MaterialsCost) != FactionConstructionResourceMutationResult.Applied)
                            {
                                plan.LastBuildTime = now;
                                break;
                            }

                            EnqueueSpawnRequest(
                                ref state,
                                boundaryEntity,
                                planEntity,
                                plan.FactionId,
                                decision.BuildingId,
                                decision.EntryIndex,
                                decision.PreferredOrigin,
                                decision.Cost,
                                decision.MaterialsCost,
                                decision.Spawnable.DisplayName);
                            em.SetComponentData(economyEntity, economy);
                            if (decision.MaterialsCost > 0)
                                em.SetComponentData(economyEntity, materials);
                            economyRecords[economyRecordIndex] = new FactionEconomyRecord(economyEntity, economy, materials);
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} cell={decision.PreferredOrigin} cost={decision.Cost} materialsCost={decision.MaterialsCost} result=Requested");
                            break;
                    }

                    if (!handledDecision)
                    {
                        ClearMaterialsRecoveryNeed(em, planEntity, plan.FactionId, now);
                        if (now - plan.LastLogTime >= LogIntervalSeconds)
                        {
                            plan.LastLogTime = now;
                            if (shouldLog)
                            {
                                AIBuildPlanningReadSystemHelper.TryGetFactionBuildingCount(
                                    ref state,
                                    boundaryEntity,
                                    plan.FactionId,
                                    out int ownedBuildings);
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} result=Complete ownedBuildings={ownedBuildings}");
                            }
                        }
                    }

                    em.SetComponentData(planEntity, plan);
                }
            }
            finally
            {
                economyRecords.Dispose();
            }

        }

        internal static void PublishMaterialsRecoveryNeed(
            EntityManager em,
            Entity planEntity,
            byte factionId,
            int requiredCredits,
            int requiredMaterials,
            in FactionTacticalMaterialsComponent materials,
            float now)
        {
            AIMaterialsRecoveryNeedMutationSystemHelper.Publish(
                em,
                planEntity,
                factionId,
                requiredCredits,
                requiredMaterials,
                materials,
                now);
        }

        internal static void ClearMaterialsRecoveryNeed(
            EntityManager em,
            Entity planEntity,
            byte factionId,
            float now)
        {
            AIMaterialsRecoveryNeedMutationSystemHelper.Clear(em, planEntity, factionId, now);
        }

        [BurstCompile]
        internal static BuildDecision SelectBuildDecision(
            DynamicBuffer<AIBuildPlanEntry> entries,
            DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
            DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> ownedSummaries,
            DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests,
            AIBuildPlan plan,
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials)
        {
            return AIBuildPlanningPolicySystemHelper.SelectBuildDecision(
                entries,
                spawnables,
                ownedSummaries,
                spawnRequests,
                plan,
                economy,
                materials);
        }

        private bool TryGetBuildingRuntimeStateEntity(ref SystemState state, out Entity entity)
        {
            entity = Entity.Null;
            if (_buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
                return false;

            entity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
            return entity != Entity.Null && state.EntityManager.Exists(entity);
        }

        private void EnqueueSpawnRequest(
            ref SystemState state,
            Entity boundaryEntity,
            Entity planEntity,
            byte factionId,
            FixedString128Bytes buildingId,
            int entryIndex,
            int2 preferredOrigin,
            int cost,
            int materialsCost,
            FixedString128Bytes displayName)
        {
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                state.EntityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
            requests.Add(new BuildingRuntimeSpawnRequest
            {
                RequestId = ++_nextBuildSpawnRequestId,
                FactionId = factionId,
                HasOwnerFaction = 1,
                BuildingId = buildingId,
                PreferredOrigin = preferredOrigin,
                Status = BuildingRuntimeSpawnRequest.Pending,
                PlanEntity = planEntity,
                EntryIndex = entryIndex,
                Cost = cost,
                MaterialsCost = materialsCost,
                DisplayName = displayName
            });
        }

        private bool ProcessCompletedSpawnRequests(
            ref SystemState state,
            Entity boundaryEntity,
            Entity planEntity,
            ref AIBuildPlan plan,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            bool shouldLog)
        {
            if (!state.EntityManager.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
                return false;

            bool hasUnsettledRequest = false;
            DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                state.EntityManager.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
            for (int i = requests.Length - 1; i >= 0; i--)
            {
                BuildingRuntimeSpawnRequest request = requests[i];
                if (request.PlanEntity != planEntity ||
                    request.Status == BuildingRuntimeSpawnRequest.Pending)
                {
                    continue;
                }

                if (request.Status == BuildingRuntimeSpawnRequest.Succeeded)
                {
                    plan.NextBuildIndex = request.EntryIndex + 1;
                }
                else
                {
                    FactionConstructionResourceMutationResult rollback =
                        FactionConstructionResourceUtilitySystemHelper.TryRollback(
                            ref economy,
                            ref materials,
                            request.Cost,
                            request.MaterialsCost);
                    if (rollback != FactionConstructionResourceMutationResult.Applied)
                    {
                        hasUnsettledRequest = true;
                        if (shouldLog)
                            EnqueueDiagnostic(ref state, $"[AIBuild] faction={request.FactionId} building={request.DisplayName.ToString()} result=RollbackPending reason={(byte)rollback}");
                        continue;
                    }

                    if (request.ResultCode == BuildingRuntimeSpawnRequest.MissingConfig)
                        plan.NextBuildIndex = request.EntryIndex + 1;
                }

                if (shouldLog)
                {
                    EnqueueDiagnostic(
                        ref state,
                        $"[AIBuild] faction={request.FactionId} building={request.DisplayName.ToString()} cell={request.ActualOrigin} cost={request.Cost} materialsCost={request.MaterialsCost} result={AIBuildPlanningReadSystemHelper.SpawnResultLabel(request)}");
                }

                requests.RemoveAt(i);
            }

            return hasUnsettledRequest;
        }

        private static bool ShouldQueueDiagnostics(EntityQuery runtimeDiagnosticsQuery)
        {
            if (InitialUnitsRuntimeState.VerboseAILogs)
                return true;

            return runtimeDiagnosticsQuery.CalculateEntityCount() == 1 &&
                runtimeDiagnosticsQuery.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
        }

        private void EnqueueDiagnostic(ref SystemState state, FixedString512Bytes message)
        {
            EntityManager em = state.EntityManager;
            Entity queueEntity;
            if (_diagnosticLogQueueQuery.IsEmptyIgnoreFilter)
            {
                queueEntity = em.CreateEntity(typeof(AIDiagnosticLogQueueComponent));
                em.SetName(queueEntity, "AIDiagnosticLogQueue");
                em.AddBuffer<AIDiagnosticLogComponent>(queueEntity);
            }
            else
            {
                queueEntity = _diagnosticLogQueueQuery.GetSingletonEntity();
            }

            DynamicBuffer<AIDiagnosticLogComponent> logs = em.GetBuffer<AIDiagnosticLogComponent>(queueEntity);
            logs.Add(new AIDiagnosticLogComponent { Message = message });
        }

        private void LogNoPlanIfNeeded(ref SystemState state, ref AIBuildPlan plan, float now, bool shouldLog)
        {
            if (now - plan.LastLogTime < LogIntervalSeconds)
                return;

            plan.LastLogTime = now;
            if (shouldLog)
                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} result=NoPlan");
        }

    }
}
