using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public struct SkirmishSharedSupplyInitialized : IComponentData { }

    /// <summary>One-time authored supply grants into the shared physical building stores.</summary>
    public static class SkirmishStartingSupplyService
    {
        public static bool Initialize(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            if (em.HasComponent<SkirmishSharedSupplyInitialized>(session)) return true;
            if (!em.HasComponent<SkirmishSharedBuildingsReady>(session)) return false;
            var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent), typeof(SkirmishAttemptOwnedComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            // Start the fabrication economy first, then stock the extraction end
            // of the haul chain. Query/archetype order must not strand all oil in a refinery.
            var grantOrder = entities.ToArray();
            System.Array.Sort(grantOrder, (left, right) =>
            {
                int order = OilGrantPriority(em, left).CompareTo(OilGrantPriority(em, right));
                return order != 0 ? order : string.CompareOrdinal(
                    em.GetComponentData<SkirmishAttemptOwnedComponent>(left).StableObjectId.ToString(),
                    em.GetComponentData<SkirmishAttemptOwnedComponent>(right).StableObjectId.ToString());
            });
            // Validate both factions before granting either side anything.
            for (byte faction = 1; faction <= 2; faction++)
            {
                float oilRoom = 0f, fuelRoom = 0f;
                foreach (var entity in entities)
                {
                    if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(id)) continue;
                    var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                    if (storage.OwnerFactionId != faction) continue;
                    oilRoom += math.max(0, storage.OilStorageCapacity - storage.StoredOilBarrels - storage.ReservedOilInboundBarrels);
                    if (IsUsableFuelStorage(storage))
                        fuelRoom += math.max(0, storage.FuelStorageCapacity - storage.StoredFuelBarrels - storage.ReservedFuelInboundBarrels);
                }
                if (oilRoom < setup.OilEach || fuelRoom < setup.UsableFuelEach) return false;
            }
            for (byte faction = 1; faction <= 2; faction++)
            {
                float oil = setup.OilEach, fuel = setup.UsableFuelEach;
                foreach (var entity in grantOrder)
                {
                    if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(id)) continue;
                    var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                    if (storage.OwnerFactionId != faction) continue;
                    float oilGrant = math.min(oil, math.max(0, storage.OilStorageCapacity - storage.StoredOilBarrels - storage.ReservedOilInboundBarrels));
                    float fuelGrant = IsUsableFuelStorage(storage)
                        ? math.min(fuel, math.max(0, storage.FuelStorageCapacity - storage.StoredFuelBarrels - storage.ReservedFuelInboundBarrels)) : 0f;
                    if (oilGrant <= 0f && fuelGrant <= 0f) continue;
                    storage.StoredOilBarrels += oilGrant;
                    storage.StoredFuelBarrels += fuelGrant;
                    storage.Version++;
                    em.SetComponentData(entity, storage);
                    oil -= oilGrant;
                    fuel -= fuelGrant;
                }
            }
            em.AddComponent<SkirmishSharedSupplyInitialized>(session);
            return true;
        }

        private static int OilGrantPriority(EntityManager em, Entity entity) =>
            em.HasComponent<MaterialFabricationComponent>(entity) ? 0 :
            em.GetComponentData<BuildingResourceStorageComponent>(entity).OilBarrelsPerDay > 0f ? 1 : 2;

        public static bool IsUsableFuelStorage(in BuildingResourceStorageComponent storage) =>
            storage.FuelStorageCapacity > 0 && storage.FuelBarrelsPerDay <= 0f && storage.OilBarrelsPerDay <= 0f;

        public static float ReadUsableFuel(EntityManager em, byte faction)
        {
            float fuel = 0f;
            using var query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
            using var stores = query.ToComponentDataArray<BuildingResourceStorageComponent>(Allocator.Temp);
            foreach (var storage in stores)
                if (storage.OwnerFactionId == faction && IsUsableFuelStorage(storage))
                    fuel += math.max(0f, storage.StoredFuelBarrels - storage.ReservedFuelOutboundBarrels - storage.CivilianFuelReserveBarrels);
            return fuel;
        }

        public static int ReadFuel(EntityManager em, Entity session, byte faction)
        {
            if (em.HasComponent<SkirmishSharedSupplyInitialized>(session))
                return (int)math.floor(ReadUsableFuel(em, faction));
            return faction == 2
                ? (em.HasComponent<SkirmishEnemyStockComponent>(session) ? em.GetComponentData<SkirmishEnemyStockComponent>(session).Fuel : 0)
                : (em.HasComponent<SkirmishEconomyStockComponent>(session) ? em.GetComponentData<SkirmishEconomyStockComponent>(session).Fuel : 0);
        }

        public static int ReadOil(EntityManager em, Entity session, byte faction)
        {
            if (!em.HasComponent<SkirmishSharedSupplyInitialized>(session))
                return faction == 2 ? em.GetComponentData<SkirmishEnemyStockComponent>(session).Oil
                    : em.GetComponentData<SkirmishEconomyStockComponent>(session).Oil;
            float oil = 0f;
            using var query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
            using var stores = query.ToComponentDataArray<BuildingResourceStorageComponent>(Allocator.Temp);
            foreach (var storage in stores)
                if (storage.OwnerFactionId == faction) oil += math.max(0f, storage.StoredOilBarrels);
            return (int)math.floor(oil);
        }

        // Production uses whole barrels. Preserve fractional movement consumption and
        // outbound/civilian reservations when applying its accepted transaction delta.
        public static bool TryWriteFuel(EntityManager em, Entity session, byte faction, int amount)
        {
            if (amount < 0) return false;
            if (!em.HasComponent<SkirmishSharedSupplyInitialized>(session))
            {
                if (faction == 2)
                {
                    var stock = em.GetComponentData<SkirmishEnemyStockComponent>(session);
                    stock.Fuel = amount; em.SetComponentData(session, stock);
                }
                else
                {
                    var stock = em.GetComponentData<SkirmishEconomyStockComponent>(session);
                    stock.Fuel = amount; em.SetComponentData(session, stock);
                }
                return true;
            }
            float delta = amount - ReadFuel(em, session, faction);
            if (delta == 0f) return true;
            using var query = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            float available = 0f;
            foreach (var entity in entities)
            {
                var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                if (storage.OwnerFactionId != faction || !IsUsableFuelStorage(storage)) continue;
                available += delta < 0f
                    ? math.max(0f, storage.StoredFuelBarrels - storage.ReservedFuelOutboundBarrels - storage.CivilianFuelReserveBarrels)
                    : math.max(0f, storage.FuelStorageCapacity - storage.StoredFuelBarrels - storage.ReservedFuelInboundBarrels);
            }
            float remaining = math.abs(delta);
            if (available < remaining) return false;
            foreach (var entity in entities)
            {
                var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                if (storage.OwnerFactionId != faction || !IsUsableFuelStorage(storage)) continue;
                float room = delta < 0f
                    ? math.max(0f, storage.StoredFuelBarrels - storage.ReservedFuelOutboundBarrels - storage.CivilianFuelReserveBarrels)
                    : math.max(0f, storage.FuelStorageCapacity - storage.StoredFuelBarrels - storage.ReservedFuelInboundBarrels);
                float changed = math.min(remaining, room);
                if (changed <= 0f) continue;
                storage.StoredFuelBarrels += delta < 0f ? -changed : changed;
                storage.Version++;
                em.SetComponentData(entity, storage);
                remaining -= changed;
                if (remaining <= 0f) break;
            }
            return true;
        }
    }
}
