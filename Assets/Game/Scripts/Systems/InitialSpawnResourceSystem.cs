using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialSpawnResourceSystem
{
    public void ApplyInitialTotals(EntityManager em, InitialUnitsSpawnConfig config)
    {
        Entity economyEntity = Entity.Null;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<FactionEconomy>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            FactionEconomy economy = em.GetComponentData<FactionEconomy>(entities[i]);
            if (economy.FactionId != 0)
                continue;

            economyEntity = entities[i];
            economy.Money = math.max(0, config.InitialDollars);
            em.SetComponentData(economyEntity, economy);
            return;
        }

        economyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
        em.SetComponentData(economyEntity, new FactionEconomy
        {
            FactionId = 0,
            Money = math.max(0, config.InitialDollars)
        });
        em.SetComponentData(economyEntity, new FactionEconomyPolicy
        {
            Enabled = 0,
            IncomeMultiplier = 1f
        });
    }
}
