using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialSpawnResourceSystem
{
    public void ApplyInitialTotals(EntityManager em, InitialUnitsSpawnConfig config)
    {
        Entity economyEntity = Entity.Null;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<FactionEconomy>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<FactionEconomy> economyType = em.GetComponentTypeHandle<FactionEconomy>(false);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref economyType);
            for (int i = 0; i < entities.Length; i++)
            {
                FactionEconomy economy = economies[i];
                if (!FactionIdentitySystem.IsPlayerControlled(economy.FactionId))
                    continue;

                economyEntity = entities[i];
                economy.Money = math.max(0, config.InitialDollars);
                economies[i] = economy;
                return;
            }
        }

        economyEntity = em.CreateEntity(typeof(FactionEconomy), typeof(FactionEconomyPolicy));
        em.SetComponentData(economyEntity, new FactionEconomy
        {
            FactionId = FactionIdentitySystem.PlayerFactionId,
            Money = math.max(0, config.InitialDollars)
        });
        em.SetComponentData(economyEntity, new FactionEconomyPolicy
        {
            Enabled = 0,
            IncomeMultiplier = 1f
        });
    }
}
