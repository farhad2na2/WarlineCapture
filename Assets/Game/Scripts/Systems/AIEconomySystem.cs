using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public partial struct AIEconomySystem : ISystem
{
    private const float MinSellBarrels = 1f;
    private const float LogIntervalSeconds = 10f;
    private int _nextSellRequestId;
    private EntityQuery _buildingRuntimeBoundaryQuery;
    private EntityQuery _diagnosticLogQueueQuery;

    public void OnCreate(ref SystemState state)
    {
        _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingRuntimeFactionSummary>(),
            ComponentType.ReadWrite<BuildingFactionResourceSellRequest>());
        _diagnosticLogQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<AIDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<AIDiagnosticLogComponent>());
        state.RequireForUpdate<FactionEconomy>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        bool shouldLogDiagnostics = ShouldQueueDiagnostics(ref state);
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

            float storedOil = economy.Oil;
            float storedFuel = economy.Fuel;
            float oilIncomeRate = 0f;
            float fuelIncomeRate = 0f;

            if (TryGetFactionResourceEconomy(ref state, economy.FactionId, out BuildingRuntimeFactionSummary snapshot))
            {
                storedOil = snapshot.StoredOilBarrels;
                storedFuel = snapshot.StoredFuelBarrels;
                oilIncomeRate = snapshot.OilBarrelsPerDay * policy.IncomeMultiplier;
                fuelIncomeRate = snapshot.FuelBarrelsPerDay * policy.IncomeMultiplier;
            }

            int revenue = 0;
            float soldOil = 0f;
            float soldFuel = 0f;
            if (boundaryEntity != Entity.Null)
            {
                ProcessCompletedSellRequests(ref state, boundaryEntity, ref economy, policy, out soldOil, out soldFuel, out revenue);
            }

            float sellInterval = Mathf.Max(1f, policy.SellIntervalSeconds);
            if (now - economy.LastSellTime >= sellInterval)
            {
                float oilToSell = Mathf.Floor(storedOil);
                float fuelToSell = Mathf.Floor(storedFuel);
                if (boundaryEntity != Entity.Null &&
                    (oilToSell >= MinSellBarrels || fuelToSell >= MinSellBarrels) &&
                    !HasPendingSellRequest(ref state, boundaryEntity, economy.FactionId))
                    EnqueueSellRequest(ref state, boundaryEntity, economy.FactionId, oilToSell, fuelToSell);

                economy.LastSellTime = now;
            }

            economy.Oil = storedOil;
            economy.Fuel = storedFuel;
            economy.OilIncomeRate = oilIncomeRate;
            economy.FuelIncomeRate = fuelIncomeRate;

            bool shouldLog = shouldLogDiagnostics && (revenue > 0 || now - economy.LastLogTime >= LogIntervalSeconds);
            if (shouldLog)
            {
                economy.LastLogTime = now;
                EnqueueDiagnostic(
                    ref state,
                    diagnosticQueueEntity,
                    $"[AIEconomy] faction={economy.FactionId} money={economy.Money} " +
                    $"oil={Mathf.FloorToInt(economy.Oil)} fuel={Mathf.FloorToInt(economy.Fuel)} " +
                    $"oilIncome={economy.OilIncomeRate:F1} fuelIncome={economy.FuelIncomeRate:F1} " +
                    $"soldOil={Mathf.FloorToInt(soldOil)} soldFuel={Mathf.FloorToInt(soldFuel)} revenue={revenue}");
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

    private bool TryGetFactionResourceEconomy(ref SystemState state, byte factionId, out BuildingRuntimeFactionSummary snapshot)
    {
        snapshot = default;
        if (!TryGetBuildingRuntimeBoundaryEntity(ref state, out Entity entity))
            return false;

        if (!state.EntityManager.HasBuffer<BuildingRuntimeFactionSummary>(entity))
            return false;

        DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
            state.EntityManager.GetBuffer<BuildingRuntimeFactionSummary>(entity, true);
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeFactionSummary summary = summaries[i];
            if (summary.FactionId != factionId)
                continue;

            snapshot = summary;
            return true;
        }

        return false;
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

        revenue = Mathf.RoundToInt(
            soldOil * Mathf.Max(0, policy.OilSellPrice) +
            soldFuel * Mathf.Max(0, policy.FuelSellPrice));
        economy.Money = Mathf.Max(0, economy.Money + revenue);
    }

    private bool HasPendingSellRequest(ref SystemState state, Entity boundaryEntity, byte factionId)
    {
        if (!state.EntityManager.HasBuffer<BuildingFactionResourceSellRequest>(boundaryEntity))
            return false;

        DynamicBuffer<BuildingFactionResourceSellRequest> requests =
            state.EntityManager.GetBuffer<BuildingFactionResourceSellRequest>(boundaryEntity, true);
        for (int i = 0; i < requests.Length; i++)
        {
            BuildingFactionResourceSellRequest request = requests[i];
            if (request.FactionId == factionId &&
                request.Status == BuildingFactionResourceSellRequest.Pending)
            {
                return true;
            }
        }

        return false;
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

    private bool ShouldQueueDiagnostics(ref SystemState state)
    {
        return InitialUnitsRuntimeState.VerboseAILogs ||
            SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
            SystemAPI.GetSingleton<RuntimeDiagnosticsStateComponent>().VerboseAILogs != 0;
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
