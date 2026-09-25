using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Game.Composition
{
    public sealed partial class MatchSceneView
    {
        private SkirmishProductionCatalog expandedProductionCatalog;
        private BuildingPlacementSystemConfig ResolvedSkirmishBuildingPlacement
        {
            get
            {
                var baseline = SkirmishPreset.buildingPlacement;
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return baseline;
                var em = world.EntityManager;
                using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupRecord));
                if (sessions.CalculateEntityCount() != 1) return baseline;
                var session = sessions.GetSingletonEntity();
                if (em.GetComponentData<SkirmishExpandedSessionComponent>(session).IsLegacy != 0) return baseline;
                var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                if (expandedProductionCatalog == null)
                    expandedProductionCatalog = SkirmishProductionCatalog.Create(setup, baseline, buildingPlacementConfig);
                if (!em.HasComponent<SkirmishProductionCatalogRecord>(session))
                    em.AddComponentObject(session, new SkirmishProductionCatalogRecord { Catalog = expandedProductionCatalog });
                return expandedProductionCatalog.Config;
            }
        }

        private SkirmishPresetConfig skirmishPreset;
        private int loadedSkirmishScenarioIndex = -1;
        private bool IsSkirmishSession =>
            MissionId == SkirmishLaunchProjection.MissionId ||
            MissionId == "skirmish.city_crossroads" ||
            MissionId == SkirmishPresetConfig.IndustrialBasinMissionId;

        private SkirmishPresetConfig SkirmishPreset
        {
            get
            {
                if (!IsSkirmishSession) return null;
                int index = MissionId == SkirmishPresetConfig.IndustrialBasinMissionId
                    ? SkirmishPresetConfig.IndustrialBasinScenarioIndex
                    : MissionId == "skirmish.city_crossroads"
                        ? SkirmishPresetConfig.CityCrossroadsScenarioIndex
                        : SkirmishPresetConfig.DesertBaseScenarioIndex;
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null && world.IsCreated &&
                    SkirmishLaunchProjection.TryGet(world.EntityManager, out _, out var match))
                    index = match.ScenarioIndex;
                if (skirmishPreset == null || loadedSkirmishScenarioIndex != index)
                {
                    skirmishPreset = SkirmishPresetConfig.Load(index);
                    loadedSkirmishScenarioIndex = index;
                }
                if (skirmishPreset == null || skirmishPreset.buildingPlacement == null ||
                    skirmishPreset.buildingPlacement.InitialUnitsConfig == null)
                    throw new System.InvalidOperationException("Base Assault preset is missing its startup configuration.");
                return skirmishPreset;
            }
        }
    }
}
