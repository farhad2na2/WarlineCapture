using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public partial struct AIEconomySystem : ISystem
{
    private const float MinSellBarrels = 1f;
    private const float LogIntervalSeconds = 10f;
    private EntityQuery _buildingPlacementRuntimeQuery;
    private EntityQuery _buildingRuntimeBoundaryQuery;
    private EntityQuery _diagnosticLogQueueQuery;

    public void OnCreate(ref SystemState state)
    {
        _buildingPlacementRuntimeQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildingPlacementRuntimeComponent>());
        _buildingRuntimeBoundaryQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<BuildingRuntimeBoundaryTag>(),
            ComponentType.ReadOnly<BuildingRuntimeFactionSummary>());
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
            float sellInterval = Mathf.Max(1f, policy.SellIntervalSeconds);
            if (now - economy.LastSellTime >= sellInterval)
            {
                float oilToSell = Mathf.Floor(storedOil);
                float fuelToSell = Mathf.Floor(storedFuel);
                if (oilToSell >= MinSellBarrels || fuelToSell >= MinSellBarrels)
                {
                    BuildingPlacementSystem buildingPlacement = GetBuildingPlacement(ref state);
                    if (buildingPlacement != null)
                    {
                        buildingPlacement.SellFactionResources(economy.FactionId, oilToSell, fuelToSell, out soldOil, out soldFuel);
                        revenue = Mathf.RoundToInt(soldOil * Mathf.Max(0, policy.OilSellPrice) + soldFuel * Mathf.Max(0, policy.FuelSellPrice));
                        storedOil = Mathf.Max(0f, storedOil - soldOil);
                        storedFuel = Mathf.Max(0f, storedFuel - soldFuel);
                        economy.Money = Mathf.Max(0, economy.Money + revenue);
                    }
                }

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

    private bool TryGetFactionResourceEconomy(ref SystemState state, byte factionId, out BuildingRuntimeFactionSummary snapshot)
    {
        snapshot = default;
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

            snapshot = summary;
            return true;
        }

        return false;
    }

    private BuildingPlacementSystem GetBuildingPlacement(ref SystemState state)
    {
        if (_buildingPlacementRuntimeQuery.IsEmptyIgnoreFilter)
            return null;

        Entity entity = _buildingPlacementRuntimeQuery.GetSingletonEntity();
        return state.EntityManager.GetComponentObject<BuildingPlacementRuntimeComponent>(entity).BuildingPlacement;
    }

    private bool ShouldQueueDiagnostics(ref SystemState state)
    {
        if (Application.isBatchMode)
            return true;

        return SystemAPI.HasSingleton<RuntimeDiagnosticsStateComponent>() &&
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
