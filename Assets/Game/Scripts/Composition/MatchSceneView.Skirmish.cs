using Game.Components;
using Game.Configs;
using Unity.Entities;
using UnityEngine;

namespace Game.Composition
{
    public sealed partial class MatchSceneView
    {
        private SkirmishPresetConfig skirmishPreset;
        private int loadedSkirmishScenarioIndex = -1;
        private bool IsSkirmishSession => MissionId == SkirmishLaunchProjection.MissionId || MissionId == "skirmish.city_crossroads";

        private SkirmishPresetConfig SkirmishPreset
        {
            get
            {
                if (!IsSkirmishSession) return null;
                int index = MissionId == "skirmish.city_crossroads" ? SkirmishPresetConfig.CityCrossroadsScenarioIndex : 0;
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
