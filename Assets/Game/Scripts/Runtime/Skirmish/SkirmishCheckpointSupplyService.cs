using System.Collections.Generic;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>Preserves physical stores by owned prefab/footprint identity, never by a cross-owner numeric ID.</summary>
    public static class SkirmishCheckpointSupplyService
    {
        private static EntityQuery Stores(EntityManager em) => em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<BuildingResourceStorageComponent>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(), ComponentType.ReadOnly<UnitSourcePrefabKey>() },
            None = new[] { ComponentType.ReadOnly<OperationMapBuildingComponent>() }
        });

        public static SkirmishCheckpointSupplyStore[] Capture(EntityManager em)
        {
            using var query = Stores(em);
            using var entities = query.ToEntityArray(Allocator.Temp);
            var values = new List<SkirmishCheckpointSupplyStore>();
            foreach (var entity in entities)
            {
                var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                if (storage.OwnerFactionId != 1 && storage.OwnerFactionId != 2) continue;
                var building = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                values.Add(new SkirmishCheckpointSupplyStore
                {
                    FactionId = storage.OwnerFactionId,
                    PrefabKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString(),
                    OriginX = building.OriginCell.x, OriginZ = building.OriginCell.y,
                    Oil = storage.StoredOilBarrels, Fuel = storage.StoredFuelBarrels,
                    OilInbound = storage.ReservedOilInboundBarrels, OilOutbound = storage.ReservedOilOutboundBarrels,
                    FuelInbound = storage.ReservedFuelInboundBarrels, FuelOutbound = storage.ReservedFuelOutboundBarrels,
                    CivilianReserve = storage.CivilianFuelReserveBarrels
                });
            }
            return values.ToArray();
        }

        // Preflight every store before the checkpoint caller mutates any session state.
        // Reconstructing missing native buildings belongs to the shared building restore owner.
        public static bool CanApply(EntityManager em, SkirmishCheckpointSupplyStore[] stores)
        {
            if (stores == null) return false;
            using var query = Stores(em);
            using var entities = query.ToEntityArray(Allocator.Temp);
            int participating = 0;
            foreach (var entity in entities)
            {
                byte owner = em.GetComponentData<BuildingResourceStorageComponent>(entity).OwnerFactionId;
                if (owner == 1 || owner == 2) participating++;
            }
            if (participating != stores.Length) return false;
            var matched = new HashSet<Entity>();
            foreach (var saved in stores)
            {
                Entity entity = Find(em, entities, saved);
                if (entity == Entity.Null || !matched.Add(entity) ||
                    !Fits(saved, em.GetComponentData<BuildingResourceStorageComponent>(entity))) return false;
            }
            return true;
        }

        public static bool TryApply(EntityManager em, SkirmishCheckpointSupplyStore[] stores)
        {
            if (!CanApply(em, stores)) return false;
            using var query = Stores(em);
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var saved in stores)
            {
                Entity entity = Find(em, entities, saved);
                var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                storage.StoredOilBarrels = saved.Oil;
                storage.StoredFuelBarrels = saved.Fuel;
                storage.ReservedOilInboundBarrels = saved.OilInbound;
                storage.ReservedOilOutboundBarrels = saved.OilOutbound;
                storage.ReservedFuelInboundBarrels = saved.FuelInbound;
                storage.ReservedFuelOutboundBarrels = saved.FuelOutbound;
                storage.CivilianFuelReserveBarrels = saved.CivilianReserve;
                storage.Version++;
                em.SetComponentData(entity, storage);
            }
            return true;
        }

        public static bool Conserved(EntityManager em, SkirmishCheckpointSupplyStore[] stores)
        {
            if (!CanApply(em, stores)) return false;
            using var query = Stores(em);
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var saved in stores)
            {
                var current = em.GetComponentData<BuildingResourceStorageComponent>(Find(em, entities, saved));
                if (current.StoredOilBarrels != saved.Oil || current.StoredFuelBarrels != saved.Fuel ||
                    current.ReservedOilInboundBarrels != saved.OilInbound || current.ReservedOilOutboundBarrels != saved.OilOutbound ||
                    current.ReservedFuelInboundBarrels != saved.FuelInbound || current.ReservedFuelOutboundBarrels != saved.FuelOutbound ||
                    current.CivilianFuelReserveBarrels != saved.CivilianReserve) return false;
            }
            return true;
        }

        private static Entity Find(EntityManager em, NativeArray<Entity> entities, SkirmishCheckpointSupplyStore saved)
        {
            if (saved == null) return Entity.Null;
            Entity found = Entity.Null;
            foreach (var entity in entities)
            {
                var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                var building = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                if (storage.OwnerFactionId != saved.FactionId || building.OwnerFactionId != saved.FactionId ||
                    building.OriginCell.x != saved.OriginX || building.OriginCell.y != saved.OriginZ ||
                    em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() != saved.PrefabKey) continue;
                if (found != Entity.Null) return Entity.Null;
                found = entity;
            }
            return found;
        }

        private static bool Fits(SkirmishCheckpointSupplyStore saved, BuildingResourceStorageComponent storage) =>
            FiniteNonNegative(saved.Oil) && FiniteNonNegative(saved.Fuel) &&
            FiniteNonNegative(saved.OilInbound) && FiniteNonNegative(saved.OilOutbound) &&
            FiniteNonNegative(saved.FuelInbound) && FiniteNonNegative(saved.FuelOutbound) &&
            FiniteNonNegative(saved.CivilianReserve) &&
            saved.Oil + saved.OilInbound <= storage.OilStorageCapacity &&
            saved.Fuel + saved.FuelInbound <= storage.FuelStorageCapacity &&
            saved.OilOutbound <= saved.Oil && saved.FuelOutbound + saved.CivilianReserve <= saved.Fuel;

        private static bool FiniteNonNegative(float value) => math.isfinite(value) && value >= 0f;
    }
}
