using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Runtime
{
    public partial struct CampaignMissionGuidanceProjectionSystem
    {
        private bool ObservePlayerBuiltDefense(ref SystemState state,ref CampaignMissionDefenseStateComponent defense)
        {
            int count=0;
            foreach(var owned in SystemAPI.Query<DynamicBuffer<BuildingRuntimeOwnedBuildingSummary>>())
            {
                int towers=CampaignMissionAttemptFactProjectionSystem.CountMatchingOwnedBuildings(owned,RadarTower);
                int barriers=CampaignMissionAttemptFactProjectionSystem.CountMatchingOwnedBuildings(owned,RadarBarrier);
                count=math.max(count,towers+barriers);
            }
            return HasNewPlayerDefense(ref defense,count);
        }

        internal static bool HasNewPlayerDefense(ref CampaignMissionDefenseStateComponent defense,int ownedCount)
        {
            if(defense.TutorialDefenseBaselineSet==0)
            {
                defense.TutorialDefenseOwnedBaseline=ownedCount;
                defense.TutorialDefenseBaselineSet=1;
                return false;
            }
            return ownedCount>defense.TutorialDefenseOwnedBaseline;
        }
    }
}
