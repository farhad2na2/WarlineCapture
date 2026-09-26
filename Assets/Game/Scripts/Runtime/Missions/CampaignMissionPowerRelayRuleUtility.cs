using Game.Components;

namespace Game.Runtime
{
    public static class CampaignMissionPowerRelayRuleUtility
    {
        public static int AdvanceHold(int current,int delta,int required,bool active,bool condition)=>
            active&&condition?System.Math.Min(required,System.Math.Max(0,current)+System.Math.Max(0,delta)):System.Math.Max(0,current);
        public static bool IsVictory(in CampaignMissionPowerRelayState state,in CampaignMissionPowerRelayDefinitionBlob rules)=>
            state.Failure==PowerRelayFailure.None && state.SafeRouteConfirmed!=0 && state.FamiliesSheltered!=0 &&
            state.FuelDelivered!=0 && state.PowerRestored!=0 && state.VictoryHoldMilliseconds>=rules.VictoryHoldMilliseconds;
    }
}
