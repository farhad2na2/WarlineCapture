using Game.Components;
using Unity.Collections;
using Unity.Entities;
namespace Game.Runtime
{
    public partial struct CampaignMissionPatrolOrderSystem
    {
        private static bool TryFindRoute(
            ref CampaignMissionDefinitionBlob definition,
            Unity.Collections.FixedString64Bytes id, out int index)
        {
            for (int i = 0; i < definition.PatrolRoutes.Length; i++)
                if (definition.PatrolRoutes[i].RouteId.Equals(id)) { index = i; return true; }
            index = -1;
            return false;
        }

        private bool CanReleaseBreachCombat(ref SystemState state,ref CampaignMissionDefinitionBlob definition,FixedString64Bytes role)
        {
            if(definition.Breach.Enabled==0) return true;
            if(!SystemAPI.TryGetSingleton(out CampaignMissionBreachState breach) || (breach.GuidanceCompletedMask&1)==0) return false;
            return !role.Equals(definition.Breach.CounterattackRoleId) || breach.CounterattackReleased!=0;
        }
    }
}
