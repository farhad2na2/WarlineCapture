using Unity.Entities;
using UnityEngine;

public partial struct AIEconomySystem : ISystem
{
    private const float MinSellBarrels = 1f;
    private const float LogIntervalSeconds = 10f;
    private EntityQuery _buildingPlacementRuntimeQuery;

    public void OnCreate(ref SystemState state)
    {
        _buildingPlacementRuntimeQuery = state.GetEntityQuery(ComponentType.ReadOnly<BuildingPlacementRuntimeComponent>());
        state.RequireForUpdate<FactionEconomy>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
            return;

        BuildingPlacementSystem buildingPlacement = GetBuildingPlacement(ref state);
        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;

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

            if (buildingPlacement != null &&
                buildingPlacement.TryGetFactionResourceEconomy(economy.FactionId, out BuildingPlacementSystem.FactionResourceEconomySnapshot snapshot))
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
            if (buildingPlacement != null && now - economy.LastSellTime >= sellInterval)
            {
                float oilToSell = Mathf.Floor(storedOil);
                float fuelToSell = Mathf.Floor(storedFuel);
                if (oilToSell >= MinSellBarrels || fuelToSell >= MinSellBarrels)
                {
                    buildingPlacement.SellFactionResources(economy.FactionId, oilToSell, fuelToSell, out soldOil, out soldFuel);
                    revenue = Mathf.RoundToInt(soldOil * Mathf.Max(0, policy.OilSellPrice) + soldFuel * Mathf.Max(0, policy.FuelSellPrice));
                    storedOil = Mathf.Max(0f, storedOil - soldOil);
                    storedFuel = Mathf.Max(0f, storedFuel - soldFuel);
                    economy.Money = Mathf.Max(0, economy.Money + revenue);
                }

                economy.LastSellTime = now;
            }

            economy.Oil = storedOil;
            economy.Fuel = storedFuel;
            economy.OilIncomeRate = oilIncomeRate;
            economy.FuelIncomeRate = fuelIncomeRate;

            bool shouldLog = AILog.IsEnabled && (revenue > 0 || now - economy.LastLogTime >= LogIntervalSeconds);
            if (shouldLog)
            {
                economy.LastLogTime = now;
                AILog.Log(
                    $"[AIEconomy] faction={economy.FactionId} money={economy.Money} " +
                    $"oil={Mathf.FloorToInt(economy.Oil)} fuel={Mathf.FloorToInt(economy.Fuel)} " +
                    $"oilIncome={economy.OilIncomeRate:F1} fuelIncome={economy.FuelIncomeRate:F1} " +
                    $"soldOil={Mathf.FloorToInt(soldOil)} soldFuel={Mathf.FloorToInt(soldFuel)} revenue={revenue}");
            }

            economyRef.ValueRW = economy;
        }
    }

    private BuildingPlacementSystem GetBuildingPlacement(ref SystemState state)
    {
        if (_buildingPlacementRuntimeQuery.IsEmptyIgnoreFilter)
            return null;

        Entity entity = _buildingPlacementRuntimeQuery.GetSingletonEntity();
        return state.EntityManager.GetComponentObject<BuildingPlacementRuntimeComponent>(entity).BuildingPlacement;
    }
}
