using Game.Components;

namespace Game.Runtime
{
    public static class CampaignMissionRouteReopenedRuleUtility
    {
        public static int AdvanceHold(int current,int delta,int required,bool active,bool condition)=>
            active&&condition?System.Math.Min(required,System.Math.Max(0,current)+System.Math.Max(0,delta)):System.Math.Max(0,current);
        public static bool IsVictory(in CampaignMissionRouteReopenedState state,in CampaignMissionRouteReopenedDefinitionBlob rules)=>
            state.Failure==RouteReopenedFailure.None && state.ReliefDelivered!=0 && state.FuelDelivered!=0 &&
            state.LinkRestored!=0 && state.HubEntered!=0 && state.GarrisonCleared!=0 &&
            state.RecordsPreserved!=0 && state.RecordsHoldMilliseconds>=rules.RecordsHoldMilliseconds;
    }
}
