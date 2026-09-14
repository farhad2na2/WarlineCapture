using System;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M05BreachAssaultEditorProbe
    {
        public static void RunRadarFootprint()
        {
            SessionState.SetBool("Warline.M05.RadarFootprint", true);
            Run();
        }

        private static bool InspectRadarFootprint(EntityManager em, CampaignMissionBreachState breach)
        {
            if (!SessionState.GetBool("Warline.M05.RadarFootprint", false)) return false;
            if (breach.Ready == 0) return true;
            var footprint = em.GetComponentData<RuntimeBuildingCombatInfo>(breach.Core);
            if (!math.all(footprint.FootprintCells == new int2(10, 10)))
                throw new InvalidOperationException("M5 enemy radar uses an oversized runtime footprint: " + footprint.FootprintCells);
            if (!math.all(em.GetComponentData<UnitFootprint>(breach.Core).Size == new int2(10,10)))
                throw new InvalidOperationException("M5 radar selection footprint differs from its combat footprint.");
            var match = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            bool found = false;
            foreach (var building in match.MatchBootstrap.BuildingUiQueryContext.RuntimeBuildings.Values)
            {
                if (building.Id != footprint.RuntimeBuildingId) continue;
                var bounds = building.Definition.LocalBounds;
                if (bounds.size.x > 10 || bounds.size.z > 10 || bounds.size.x < 9 || bounds.size.z < 9)
                    throw new InvalidOperationException("M5 radar visual does not fit its 10x10 footprint.");
                found = true;
            }
            if (!found) throw new InvalidOperationException("M5 radar visual is missing.");
            SessionState.SetBool("Warline.M05.RadarFootprint", false);
            Complete(true, "M5 enemy radar spawned through the real mission: model ~9.74x9.72, combat/grid footprint 10x10.");
            return true;
        }
    }
}
