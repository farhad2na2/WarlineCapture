using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    /// <summary>Mode-owned recipes on real attempt facilities; never changes Campaign assets or grants stock.</summary>
    public static class SkirmishFacilityEconomyProfile
    {
        public static void Apply(EntityManager em, Entity building, FixedString64Bytes attempt, string structureId)
        {
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if (sessions.CalculateEntityCount() != 1) return;
            var session = sessions.GetSingleton<SkirmishExpandedSessionComponent>();
            if (session.IsLegacy != 0 || !session.SessionId.Equals(attempt)) return;
            if (session.SizeId != SkirmishSizeId.Standard && session.SizeId != SkirmishSizeId.War && session.SizeId != SkirmishSizeId.LargeWar) return;
            bool refinery = structureId == SkirmishStructureIds.Refinery || structureId == SkirmishStructureIds.RefineryModule;
            int oilRate = 120, materialInput = 10, materialOutput = 50, fuelInput = 30, fuelOutput = 60;
            if (session.SizeId == SkirmishSizeId.War)
            { oilRate = 180; materialInput = 16; materialOutput = 80; fuelInput = 45; fuelOutput = 90; }
            else if (session.SizeId == SkirmishSizeId.LargeWar)
            { oilRate = 240; materialInput = 22; materialOutput = 110; fuelInput = 60; fuelOutput = 120; }

            if (structureId == SkirmishStructureIds.FabricationDepot && em.HasComponent<MaterialFabricationComponent>(building))
            {
                var recipe = em.GetComponentData<MaterialFabricationComponent>(building);
                if (recipe.OilConsumedPerCycle != materialInput || recipe.MaterialsOutputPerCycle != materialOutput || recipe.CycleDurationSeconds != 30)
                {
                    recipe.OilConsumedPerCycle = materialInput;
                    recipe.MaterialsOutputPerCycle = materialOutput;
                    recipe.CycleDurationSeconds = 30;
                    recipe.Version = recipe.Version == uint.MaxValue ? 1u : recipe.Version + 1u;
                    em.SetComponentData(building, recipe);
                }
            }
            if (!em.HasComponent<BuildingResourceStorageComponent>(building)) return;
            if (structureId != SkirmishStructureIds.OilPump && !refinery) return;
            var profile = new BuildingResourceProductionProfileComponent
            {
                PeriodSeconds = 60,
                OilPerPeriod = structureId == SkirmishStructureIds.OilPump ? oilRate : 0,
                FuelPerPeriod = refinery ? fuelOutput * 2 : 0,
                OilPerFuelBatch = refinery ? fuelInput : 0,
                FuelPerBatch = refinery ? fuelOutput : 0,
                FuelBatchSeconds = 30
            };
            if (em.HasComponent<BuildingResourceProductionProfileComponent>(building)) return;
            em.AddComponentData(building, profile);
            var storage = em.GetComponentData<BuildingResourceStorageComponent>(building);
            storage.OilBarrelsPerDay = profile.OilPerPeriod;
            storage.FuelBarrelsPerDay = profile.FuelPerPeriod;
            storage.Version = storage.Version == uint.MaxValue ? 1u : storage.Version + 1u;
            em.SetComponentData(building, storage);
        }
    }
}
