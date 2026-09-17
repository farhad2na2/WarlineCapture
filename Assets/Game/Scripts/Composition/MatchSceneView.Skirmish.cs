using Game.Components;
using Game.Configs;
using Unity.Entities;
using UnityEngine;

namespace Game.Composition
{
    public sealed partial class MatchSceneView
    {
        private SkirmishPresetConfig skirmishPreset;
        private bool IsSkirmishSession => MissionId == SkirmishLaunchProjection.MissionId;

        private SkirmishPresetConfig SkirmishPreset
        {
            get
            {
                if (!IsSkirmishSession) return null;
                if (skirmishPreset == null)
                    skirmishPreset = Resources.Load<SkirmishPresetConfig>(SkirmishPresetConfig.ResourceName);
                if (skirmishPreset == null || skirmishPreset.buildingPlacement == null ||
                    skirmishPreset.buildingPlacement.InitialUnitsConfig == null)
                    throw new System.InvalidOperationException("Base Assault preset is missing its startup configuration.");
                return skirmishPreset;
            }
        }
    }
}
