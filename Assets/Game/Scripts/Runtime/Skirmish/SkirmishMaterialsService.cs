using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public struct SkirmishSharedMaterialsInitialized : IComponentData { }

    /// <summary>Expanded production and construction use the same faction bank.
    /// Session stocks are retained only for old checkpoint/standalone fixture compatibility.</summary>
    public static class SkirmishMaterialsService
    {
        public static void Initialize(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            if (em.HasComponent<SkirmishSharedMaterialsInitialized>(session)) return;
            for (byte faction = 1; faction <= 2; faction++)
            {
                Entity bank = FindEconomy(em, faction);
                if (bank == Entity.Null)
                {
                    bank = em.CreateEntity();
                    em.AddComponentData(bank, new FactionEconomy { FactionId = faction });
                }
                var economy = em.GetComponentData<FactionEconomy>(bank);
                economy.Money = 0;
                economy.MaterialsOnlyConstruction = 1;
                em.SetComponentData(bank, economy);
                Set(em, bank, new FactionEconomyPolicy { Enabled = 0, IncomeMultiplier = 1f });
                Set(em, bank, new FactionTacticalMaterialsComponent
                {
                    FactionId = faction, Current = math.clamp(setup.MaterialsEach, 0, setup.MaterialsCapacityEach),
                    Capacity = setup.MaterialsCapacityEach, Version = 1
                });
                Set(em, bank, new FactionMaterialFabricationTelemetryComponent { FactionId = faction });
                Set(em, bank, new FactionFuelLogisticsTelemetryComponent { FactionId = faction });
                if (!em.HasComponent<MaterialFabricationEconomyEventQueueComponent>(bank))
                    em.AddComponentData(bank, new MaterialFabricationEconomyEventQueueComponent());
                if (!em.HasBuffer<MaterialFabricationEconomyEventElement>(bank))
                    em.AddBuffer<MaterialFabricationEconomyEventElement>(bank)
                        .EnsureCapacity(MaterialFabricationEconomyEventQueueComponent.Capacity);
            }
            em.AddComponent<SkirmishSharedMaterialsInitialized>(session);
        }

        public static int Read(EntityManager em, Entity session, byte faction)
        {
            if (em.HasComponent<SkirmishSharedMaterialsInitialized>(session))
            {
                Entity bank = FindEconomy(em, faction);
                return bank != Entity.Null && em.HasComponent<FactionTacticalMaterialsComponent>(bank)
                    ? em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current : 0;
            }
            if (faction == 2)
                return em.HasComponent<SkirmishEnemyStockComponent>(session)
                    ? em.GetComponentData<SkirmishEnemyStockComponent>(session).Materials : 0;
            return em.HasComponent<SkirmishEconomyStockComponent>(session)
                ? em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials : 0;
        }

        // The caller owns the reservation or checkpoint transaction. No frame-by-frame mirroring.
        public static void Write(EntityManager em, Entity session, byte faction, int amount)
        {
            if (em.HasComponent<SkirmishSharedMaterialsInitialized>(session))
            {
                Entity bank = FindEconomy(em, faction);
                if (bank == Entity.Null || !em.HasComponent<FactionTacticalMaterialsComponent>(bank))
                    throw new System.InvalidOperationException("Expanded faction materials bank is missing.");
                var materials = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
                materials.Current = math.clamp(amount, 0, materials.Capacity);
                materials.Version = materials.Version == uint.MaxValue ? 1u : materials.Version + 1u;
                em.SetComponentData(bank, materials);
                return;
            }
            if (faction == 2 && em.HasComponent<SkirmishEnemyStockComponent>(session))
            {
                var stock = em.GetComponentData<SkirmishEnemyStockComponent>(session);
                stock.Materials = amount;
                em.SetComponentData(session, stock);
            }
            else if (faction == 1 && em.HasComponent<SkirmishEconomyStockComponent>(session))
            {
                var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
                stock.Materials = amount;
                em.SetComponentData(session, stock);
            }
        }

        // Production/readiness transactions use the shared spend/refund owner.
        // Checkpoint writes remain explicit state restoration, never spending.
        public static void WriteTransaction(EntityManager em, Entity session, byte faction, int amount,
            FactionTacticalMaterialsSpendKind kind)
        {
            if (!em.HasComponent<SkirmishSharedMaterialsInitialized>(session))
            { Write(em, session, faction, amount); return; }
            Entity bank = FindEconomy(em, faction);
            if (bank == Entity.Null || !em.HasComponent<FactionTacticalMaterialsComponent>(bank))
                throw new System.InvalidOperationException("Expanded faction materials bank is missing.");
            var materials = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            int target = math.clamp(amount, 0, materials.Capacity);
            int delta = target - materials.Current;
            if (delta == 0) return;
            var result = delta < 0
                ? FactionTacticalMaterialsUtilitySystemHelper.TrySpend(ref materials, -delta, kind)
                : FactionTacticalMaterialsUtilitySystemHelper.TryRefundQueue(ref materials, delta, kind);
            if (result != FactionTacticalMaterialsMutationResult.Applied)
                throw new System.InvalidOperationException("Expanded material transaction rejected: " + result);
            em.SetComponentData(bank, materials);
        }

        public static Entity FindEconomy(EntityManager em, byte faction)
        {
            using var query = em.CreateEntityQuery(typeof(FactionEconomy));
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                if (em.GetComponentData<FactionEconomy>(entities[i]).FactionId == faction) return entities[i];
            return Entity.Null;
        }

        private static void Set<T>(EntityManager em, Entity entity, T value) where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(entity)) em.SetComponentData(entity, value);
            else em.AddComponentData(entity, value);
        }
    }
}
