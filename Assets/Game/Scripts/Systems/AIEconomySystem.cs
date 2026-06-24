using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct AIEconomySystem : ISystem
{
    private const float MinSellBarrels = 1f;
    private const float LogIntervalSeconds = 10f;
    private int _nextSellRequestId;
    private EntityQuery _buildingRuntimeBoundaryQuery;
    private EntityQuery _runtimeDiagnosticsQuery;
    private EntityQuery _diagnosticLogQueueQuery;

    private struct EconomyDecision
    {
        public float StoredOil;
        public float StoredFuel;
        public float OilIncomeRate;
        public float FuelIncomeRate;
        public float OilToSell;
        public float FuelToSell;
        public byte ShouldEnqueueSellRequest;
        public byte ShouldUpdateLastSellTime;
    }

    public void OnCreate(ref SystemState state)
    {
        _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingRuntimeFactionSummary>(),
            ComponentType.ReadWrite<BuildingFactionResourceSellRequest>());
        _runtimeDiagnosticsQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
        _diagnosticLogQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<AIDiagnosticLogComponent>());
        state.RequireForUpdate<FactionEconomy>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive == 0)
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        bool shouldLogDiagnostics = ShouldQueueDiagnostics(_runtimeDiagnosticsQuery);
        Entity diagnosticQueueEntity = shouldLogDiagnostics ? EnsureDiagnosticQueue(ref state) : Entity.Null;
        Entity boundaryEntity = TryGetBuildingRuntimeBoundaryEntity(ref state, out Entity foundBoundaryEntity)
            ? foundBoundaryEntity
            : Entity.Null;

        foreach (var (economyRef, policyRef) in SystemAPI.Query<RefRW<FactionEconomy>, RefRO<FactionEconomyPolicy>>())
        {
            FactionEconomy economy = economyRef.ValueRO;
            FactionEconomyPolicy policy = policyRef.ValueRO;
            if (policy.Enabled == 0)
                continue;

            int revenue = 0;
            float soldOil = 0f;
            float soldFuel = 0f;
            if (boundaryEntity != Entity.Null)
            {
                ProcessCompletedSellRequests(ref state, boundaryEntity, ref economy, policy, out soldOil, out soldFuel, out revenue);
            }

            EconomyDecision decision = ResolveEconomyDecision(ref state, boundaryEntity, economy, policy, now);
            if (decision.ShouldUpdateLastSellTime != 0)
            {
                if (decision.ShouldEnqueueSellRequest != 0)
                {
                    EnqueueSellRequest(
                        ref state,
                        boundaryEntity,
                        economy.FactionId,
                        decision.OilToSell,
                        decision.FuelToSell);
                }

                economy.LastSellTime = now;
            }

            economy.Oil = decision.StoredOil;
            economy.Fuel = decision.StoredFuel;
            economy.OilIncomeRate = decision.OilIncomeRate;
            economy.FuelIncomeRate = decision.FuelIncomeRate;

            bool shouldLog = shouldLogDiagnostics && (revenue > 0 || now - economy.LastLogTime >= LogIntervalSeconds);
            if (shouldLog)
            {
                economy.LastLogTime = now;
                EnqueueDiagnostic(
                    ref state,
                    diagnosticQueueEntity,
                    $"[AIEconomy] faction={economy.FactionId} money={economy.Money} " +
                    $"oil={(int)math.floor(economy.Oil)} fuel={(int)math.floor(economy.Fuel)} " +
                    $"oilIncome={economy.OilIncomeRate:F1} fuelIncome={economy.FuelIncomeRate:F1} " +
                    $"soldOil={(int)math.floor(soldOil)} soldFuel={(int)math.floor(soldFuel)} revenue={revenue}");
            }

            economyRef.ValueRW = economy;
        }
    }

    private bool TryGetBuildingRuntimeBoundaryEntity(ref SystemState state, out Entity entity)
    {
        entity = Entity.Null;
        if (_buildingRuntimeBoundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
        return entity != Entity.Null && state.EntityManager.Exists(entity);
    }

    private EconomyDecision ResolveEconomyDecision(
        ref SystemState state,
        Entity boundaryEntity,
        FactionEconomy economy,
        FactionEconomyPolicy policy,
        float now)
    {
        bool hasBoundary = boundaryEntity != Entity.Null;
        using NativeArray<BuildingRuntimeFactionSummary> summaries = hasBoundary
            ? CopyBoundaryBuffer<BuildingRuntimeFactionSummary>(state.EntityManager, boundaryEntity, Allocator.TempJob)
            : new NativeArray<BuildingRuntimeFactionSummary>(0, Allocator.TempJob);
        using NativeArray<BuildingFactionResourceSellRequest> sellRequests = hasBoundary
            ? CopyBoundaryBuffer<BuildingFactionResourceSellRequest>(state.EntityManager, boundaryEntity, Allocator.TempJob)
            : new NativeArray<BuildingFactionResourceSellRequest>(0, Allocator.TempJob);
        using NativeReference<EconomyDecision> decision = new(Allocator.TempJob);

        new ResolveEconomyDecisionJob
        {
            Summaries = summaries,
            SellRequests = sellRequests,
            Economy = economy,
            Policy = policy,
            Now = now,
            MinSellBarrels = MinSellBarrels,
            HasBoundary = hasBoundary ? (byte)1 : (byte)0,
            Decision = decision
        }.Schedule(state.Dependency).Complete();

        return decision.Value;
    }

    private void ProcessCompletedSellRequests(
        ref SystemState state,
        Entity boundaryEntity,
        ref FactionEconomy economy,
        FactionEconomyPolicy policy,
        out float soldOil,
        out float soldFuel,
        out int revenue)
    {
        soldOil = 0f;
        soldFuel = 0f;
        revenue = 0;
        if (!state.EntityManager.HasBuffer<BuildingFactionResourceSellRequest>(boundaryEntity))
            return;

        DynamicBuffer<BuildingFactionResourceSellRequest> requests =
            state.EntityManager.GetBuffer<BuildingFactionResourceSellRequest>(boundaryEntity);
        for (int i = requests.Length - 1; i >= 0; i--)
        {
            BuildingFactionResourceSellRequest request = requests[i];
            if (request.FactionId != economy.FactionId ||
                request.Status == BuildingFactionResourceSellRequest.Pending)
            {
                continue;
            }

            if (request.Status == BuildingFactionResourceSellRequest.Succeeded)
            {
                soldOil += request.SoldOilBarrels;
                soldFuel += request.SoldFuelBarrels;
            }

            requests.RemoveAt(i);
        }

        if (soldOil <= 0f && soldFuel <= 0f)
            return;

        revenue = (int)math.round(
            soldOil * math.max(0, policy.OilSellPrice) +
            soldFuel * math.max(0, policy.FuelSellPrice));
        economy.Money = math.max(0, economy.Money + revenue);
    }

    private static NativeArray<T> CopyBoundaryBuffer<T>(
        EntityManager em,
        Entity boundaryEntity,
        Allocator allocator)
        where T : unmanaged, IBufferElementData
    {
        if (!em.HasBuffer<T>(boundaryEntity))
            return new NativeArray<T>(0, allocator);

        DynamicBuffer<T> buffer = em.GetBuffer<T>(boundaryEntity, true);
        NativeArray<T> copy = new(buffer.Length, allocator);
        for (int i = 0; i < buffer.Length; i++)
            copy[i] = buffer[i];

        return copy;
    }

    [BurstCompile]
    private struct ResolveEconomyDecisionJob : IJob
    {
        [ReadOnly] public NativeArray<BuildingRuntimeFactionSummary> Summaries;
        [ReadOnly] public NativeArray<BuildingFactionResourceSellRequest> SellRequests;
        public FactionEconomy Economy;
        public FactionEconomyPolicy Policy;
        public float Now;
        public float MinSellBarrels;
        public byte HasBoundary;
        public NativeReference<EconomyDecision> Decision;

        public void Execute()
        {
            EconomyDecision decision = new()
            {
                StoredOil = Economy.Oil,
                StoredFuel = Economy.Fuel
            };

            if (TryGetFactionResourceEconomy(Economy.FactionId, out BuildingRuntimeFactionSummary snapshot))
            {
                decision.StoredOil = snapshot.StoredOilBarrels;
                decision.StoredFuel = snapshot.StoredFuelBarrels;
                decision.OilIncomeRate = snapshot.OilBarrelsPerDay * Policy.IncomeMultiplier;
                decision.FuelIncomeRate = snapshot.FuelBarrelsPerDay * Policy.IncomeMultiplier;
            }

            float sellInterval = math.max(1f, Policy.SellIntervalSeconds);
            if (Now - Economy.LastSellTime >= sellInterval)
            {
                decision.ShouldUpdateLastSellTime = 1;
                decision.OilToSell = math.floor(decision.StoredOil);
                decision.FuelToSell = math.floor(decision.StoredFuel);
                if (HasBoundary != 0 &&
                    (decision.OilToSell >= MinSellBarrels || decision.FuelToSell >= MinSellBarrels) &&
                    !HasPendingSellRequest(Economy.FactionId))
                {
                    decision.ShouldEnqueueSellRequest = 1;
                }
            }

            Decision.Value = decision;
        }

        private bool TryGetFactionResourceEconomy(
            byte factionId,
            out BuildingRuntimeFactionSummary snapshot)
        {
            for (int i = 0; i < Summaries.Length; i++)
            {
                BuildingRuntimeFactionSummary summary = Summaries[i];
                if (summary.FactionId != factionId)
                    continue;

                snapshot = summary;
                return true;
            }

            snapshot = default;
            return false;
        }

        private bool HasPendingSellRequest(byte factionId)
        {
            for (int i = 0; i < SellRequests.Length; i++)
            {
                BuildingFactionResourceSellRequest request = SellRequests[i];
                if (request.FactionId == factionId &&
                    request.Status == BuildingFactionResourceSellRequest.Pending)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void EnqueueSellRequest(ref SystemState state, Entity boundaryEntity, byte factionId, float oilToSell, float fuelToSell)
    {
        DynamicBuffer<BuildingFactionResourceSellRequest> requests =
            state.EntityManager.GetBuffer<BuildingFactionResourceSellRequest>(boundaryEntity);
        requests.Add(new BuildingFactionResourceSellRequest
        {
            RequestId = ++_nextSellRequestId,
            FactionId = factionId,
            RequestedOilBarrels = oilToSell,
            RequestedFuelBarrels = fuelToSell,
            Status = BuildingFactionResourceSellRequest.Pending
        });
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
}
