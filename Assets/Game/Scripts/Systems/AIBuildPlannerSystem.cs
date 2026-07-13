using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

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

            NativeList<FactionEconomyRecord> economyRecords = BuildFactionEconomyRecords();
            try
            {
                for (int i = 0; i < planEntities.Length; i++)
                {
                    Entity planEntity = planEntities[i];
                    AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(planEntity);
                    if (plan.Enabled == 0 || !IsFactionAIControlled(plan.FactionId, hasControls, controls))
                        continue;

                    if (!TryFindEconomyRecord(economyRecords, plan.FactionId, out int economyRecordIndex, out FactionEconomyRecord economyRecord))
                        continue;

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
                    if (HasMaterialsChanged(economyRecord.Materials, materials))
                        em.SetComponentData(economyEntity, materials);
                    economyRecords[economyRecordIndex] = new FactionEconomyRecord(economyEntity, economy, materials);

                    if (hasUnsettledRequest)
                    {
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
                        LogNoPlanIfNeeded(ref state, ref plan, now, shouldLog);
                        em.SetComponentData(planEntity, plan);
                        continue;
                    }

                    if (plan.BaseCenterCell.x <= 0 && plan.BaseCenterCell.y <= 0)
                        plan.BaseCenterCell = ResolveDefaultBaseCenter(plan.FactionId, grid);

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
                            plan.LastBuildTime = now;
                            break;

                        case BuildDecisionResult.MissingConfig:
                            plan.NextBuildIndex = decision.EntryIndex + 1;
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.BuildingId.ToString()} result=MissingConfig");
                            break;

                        case BuildDecisionResult.InsufficientFunds:
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} cost={decision.Cost} materialsCost={decision.MaterialsCost} result=InsufficientFunds money={economy.Money} materials={materials.Current}");
                            break;

                        case BuildDecisionResult.InsufficientMaterials:
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} cost={decision.Cost} materialsCost={decision.MaterialsCost} result=InsufficientMaterials money={economy.Money} materials={materials.Current}");
                            break;

                        case BuildDecisionResult.InsufficientCreditsAndMaterials:
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} cost={decision.Cost} materialsCost={decision.MaterialsCost} result=InsufficientCreditsAndMaterials money={economy.Money} materials={materials.Current}");
                            break;

                        case BuildDecisionResult.InvalidResources:
                            plan.LastBuildTime = now;
                            if (shouldLog)
                                EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} building={decision.Spawnable.DisplayName.ToString()} result=InvalidResources");
                            break;

                        case BuildDecisionResult.Request:
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

                    if (!handledDecision && now - plan.LastLogTime >= LogIntervalSeconds)
                    {
                        plan.LastLogTime = now;
                        if (shouldLog)
                        {
                            TryGetFactionBuildingCount(ref state, boundaryEntity, plan.FactionId, out int ownedBuildings);
                            EnqueueDiagnostic(ref state, $"[AIBuild] faction={plan.FactionId} result=Complete ownedBuildings={ownedBuildings}");
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

        private NativeList<FactionEconomyRecord> BuildFactionEconomyRecords()
        {
            int count = _economyQuery.CalculateEntityCount();
            NativeList<FactionEconomyRecord> records = new(count, Allocator.Temp);
            using NativeArray<ArchetypeChunk> chunks = _economyQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(_entityType);
                NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref _economyType);
                NativeArray<FactionTacticalMaterialsComponent> materials = chunk.GetNativeArray(ref _materialsType);
                for (int i = 0; i < chunk.Count; i++)
                    records.Add(new FactionEconomyRecord(entities[i], economies[i], materials[i]));
            }

            return records;
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
            BuildDecision decision = default;
            if (entries.Length == 0)
                return decision;

            int attempts = math.max(1, entries.Length);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int candidateIndex = PositiveModulo(plan.NextBuildIndex + attempt, entries.Length);
                FixedString128Bytes buildingId = NormalizeBuildId(entries[candidateIndex].BuildingId);
                if (buildingId.Length == 0)
                    continue;

                if (TryGetOwnedBuildingCount(ownedSummaries, buildingId, plan.FactionId, out int ownedCount) &&
                    ownedCount > 0)
                {
                    continue;
                }

                decision.EntryIndex = candidateIndex;
                decision.BuildingId = buildingId;
                if (HasPendingSpawnRequest(spawnRequests, buildingId, plan.FactionId))
                {
                    decision.Result = BuildDecisionResult.Pending;
                    break;
                }

                if (!TryResolveSpawnableReadModel(spawnables, buildingId, out BuildingConfiguredSpawnableReadModel spawnable) ||
                    spawnable.CanRequest == 0)
                {
                    decision.Result = BuildDecisionResult.MissingConfig;
                    break;
                }

                int cost = math.max(0, spawnable.Price);
                int materialsCost = math.max(0, spawnable.MaterialsCost);
                decision.Spawnable = spawnable;
                decision.Cost = cost;
                decision.MaterialsCost = materialsCost;
                FactionConstructionResourceMutationResult affordability =
                    FactionConstructionResourceUtilitySystemHelper.Evaluate(
                        economy,
                        materials,
                        cost,
                        materialsCost);
                if (affordability != FactionConstructionResourceMutationResult.Applied)
                {
                    decision.Result = ToBuildDecisionResult(affordability);
                    break;
                }

                decision.Result = BuildDecisionResult.Request;
                decision.PreferredOrigin = ResolvePreferredOriginCell(plan.BaseCenterCell, candidateIndex);
                break;
            }

            return decision;
        }

        private static BuildDecisionResult ToBuildDecisionResult(
            FactionConstructionResourceMutationResult result)
        {
            return result switch
            {
                FactionConstructionResourceMutationResult.InsufficientCredits => BuildDecisionResult.InsufficientFunds,
                FactionConstructionResourceMutationResult.InsufficientMaterials => BuildDecisionResult.InsufficientMaterials,
                FactionConstructionResourceMutationResult.InsufficientCreditsAndMaterials => BuildDecisionResult.InsufficientCreditsAndMaterials,
                FactionConstructionResourceMutationResult.Applied => BuildDecisionResult.Request,
                _ => BuildDecisionResult.InvalidResources
            };
        }

        private static FixedString128Bytes NormalizeBuildId(FixedString64Bytes buildingId)
        {
            FixedString128Bytes source = buildingId;
            int start = 0;
            while (start < source.Length)
            {
                int whitespaceBytes = GetWhitespaceByteCount(ref source, start);
                if (whitespaceBytes == 0)
                    break;

                start += whitespaceBytes;
            }

            int end = source.Length;
            while (end > start)
            {
                int whitespaceBytes = GetTrailingWhitespaceByteCount(ref source, end);
                if (whitespaceBytes == 0)
                    break;

                end -= whitespaceBytes;
            }

            FixedString128Bytes normalized = source.Substring(start, end - start);
            return normalized.ToLowerAscii();
        }

        private static int GetTrailingWhitespaceByteCount(ref FixedString128Bytes value, int end)
        {
            int oneByteStart = end - 1;
            if (GetWhitespaceByteCount(ref value, oneByteStart) == 1)
                return 1;
            if (end >= 2 && GetWhitespaceByteCount(ref value, end - 2) == 2)
                return 2;
            return end >= 3 && GetWhitespaceByteCount(ref value, end - 3) == 3 ? 3 : 0;
        }

        private static int GetWhitespaceByteCount(ref FixedString128Bytes value, int index)
        {
            if (index < 0 || index >= value.Length)
                return 0;

            byte first = value[index];
            if (first == (byte)' ' || (first >= 0x09 && first <= 0x0d))
                return 1;

            if (index + 1 < value.Length && first == 0xc2)
            {
                byte second = value[index + 1];
                if (second == 0x85 || second == 0xa0)
                    return 2;
            }

            if (index + 2 >= value.Length)
                return 0;

            byte middle = value[index + 1];
            byte last = value[index + 2];
            if (first == 0xe1 && middle == 0x9a && last == 0x80)
                return 3;
            if (first == 0xe2 && middle == 0x80 &&
                ((last >= 0x80 && last <= 0x8a) || last == 0xa8 || last == 0xa9 || last == 0xaf))
            {
                return 3;
            }
            if (first == 0xe2 && middle == 0x81 && last == 0x9f)
                return 3;
            return first == 0xe3 && middle == 0x80 && last == 0x80 ? 3 : 0;
        }

        private static bool TryResolveSpawnableReadModel(
            DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
            FixedString128Bytes buildingId,
            out BuildingConfiguredSpawnableReadModel spawnable)
        {
            for (int i = 0; i < spawnables.Length; i++)
            {
                BuildingConfiguredSpawnableReadModel candidate = spawnables[i];
                if (!candidate.BuildingId.Equals(buildingId))
                    continue;

                spawnable = candidate;
                return true;
            }

            spawnable = default;
            return false;
        }

        private static bool TryGetOwnedBuildingCount(
            DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> ownedSummaries,
            FixedString128Bytes buildingId,
            byte factionId,
            out int count)
        {
            for (int i = 0; i < ownedSummaries.Length; i++)
            {
                BuildingRuntimeOwnedBuildingSummary summary = ownedSummaries[i];
                if (summary.FactionId != factionId || !summary.BuildingId.Equals(buildingId))
                    continue;

                count = summary.Count;
                return true;
            }

            count = 0;
            return false;
        }

        private static bool HasPendingSpawnRequest(
            DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests,
            FixedString128Bytes buildingId,
            byte factionId)
        {
            for (int i = 0; i < spawnRequests.Length; i++)
            {
                BuildingRuntimeSpawnRequest request = spawnRequests[i];
                if (request.FactionId == factionId &&
                    request.BuildingId.Equals(buildingId) &&
                    request.Status == BuildingRuntimeSpawnRequest.Pending)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindEconomyRecord(
            NativeList<FactionEconomyRecord> records,
            byte factionId,
            out int index,
            out FactionEconomyRecord record)
        {
            for (int i = 0; i < records.Length; i++)
            {
                FactionEconomyRecord candidate = records[i];
                if (candidate.Economy.FactionId != factionId)
                    continue;

                index = i;
                record = candidate;
                return true;
            }

            index = -1;
            record = default;
            return false;
        }

        private bool TryGetBuildingRuntimeStateEntity(ref SystemState state, out Entity entity)
        {
            entity = Entity.Null;
            if (_buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
                return false;

            entity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
            return entity != Entity.Null && state.EntityManager.Exists(entity);
        }

        private bool TryGetFactionBuildingCount(ref SystemState state, Entity boundaryEntity, byte factionId, out int count)
        {
            count = 0;
            if (!state.EntityManager.HasBuffer<BuildingRuntimeFactionSummary>(boundaryEntity))
                return false;

            DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
                state.EntityManager.GetBuffer<BuildingRuntimeFactionSummary>(boundaryEntity, true);
            for (int i = 0; i < summaries.Length; i++)
            {
                BuildingRuntimeFactionSummary summary = summaries[i];
                if (summary.FactionId != factionId)
                    continue;

                count = summary.BuildingCount;
                return true;
            }

            return false;
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
                        $"[AIBuild] faction={request.FactionId} building={request.DisplayName.ToString()} cell={request.ActualOrigin} cost={request.Cost} materialsCost={request.MaterialsCost} result={SpawnResultLabel(request)}");
                }

                requests.RemoveAt(i);
            }

            return hasUnsettledRequest;
        }

        private static bool HasMaterialsChanged(
            in FactionTacticalMaterialsComponent previous,
            in FactionTacticalMaterialsComponent current)
        {
            return previous.FactionId != current.FactionId ||
                   previous.Current != current.Current ||
                   previous.Capacity != current.Capacity ||
                   previous.LifetimeFabricated != current.LifetimeFabricated ||
                   previous.LifetimeImported != current.LifetimeImported ||
                   previous.LifetimeRewarded != current.LifetimeRewarded ||
                   previous.LifetimeExported != current.LifetimeExported ||
                   previous.LifetimeSpent != current.LifetimeSpent ||
                   previous.Version != current.Version;
        }

        private static bool IsFactionAIControlled(byte factionId, bool hasControls, DynamicBuffer<FactionControlEntry> controls)
        {
            if (!hasControls)
                return FactionIdentity.IsAiControlledByDefault(factionId);

            for (int i = 0; i < controls.Length; i++)
            {
                FactionControlEntry control = controls[i];
                if (control.FactionId == factionId)
                    return control.AIControlled != 0;
            }

            return FactionIdentity.IsAiControlledByDefault(factionId);
        }

        private static int2 ResolveDefaultBaseCenter(byte factionId, GridConfig grid)
        {
            int x = FactionIdentity.IsPlayerControlled(factionId) ? grid.Width / 4 : (grid.Width * 3) / 4;
            int y = grid.Height / 2;
            return new int2(math.max(0, x), math.max(0, y));
        }

        private static int2 ResolvePreferredOriginCell(int2 baseCenterCell, int entryIndex)
        {
            int ring = entryIndex / 5;
            int spacing = 14 + ring * 8;
            int2 offset = PositiveModulo(entryIndex, 5) switch
            {
                0 => new int2(0, 0),
                1 => new int2(spacing, 0),
                2 => new int2(-spacing, 0),
                3 => new int2(0, spacing),
                _ => new int2(0, -spacing)
            };

            return baseCenterCell + offset;
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0)
                return 0;

            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private static string SpawnResultLabel(BuildingRuntimeSpawnRequest request)
        {
            if (request.Status == BuildingRuntimeSpawnRequest.Succeeded)
                return "Placed";

            return request.ResultCode switch
            {
                BuildingRuntimeSpawnRequest.MissingConfig => "MissingConfig",
                BuildingRuntimeSpawnRequest.Blocked => "Blocked",
                _ => "Failed"
            };
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

        private readonly struct FactionEconomyRecord
        {
            public FactionEconomyRecord(
                Entity entity,
                FactionEconomy economy,
                FactionTacticalMaterialsComponent materials)
            {
                Entity = entity;
                Economy = economy;
                Materials = materials;
            }

            public readonly Entity Entity;
            public readonly FactionEconomy Economy;
            public readonly FactionTacticalMaterialsComponent Materials;
        }
    }
}
