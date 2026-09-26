using Game.Components;

namespace Game.Runtime
{
    public static class CampaignMissionMarketLifelineRuleUtility
    {
        public static int AdvanceHold(int current,int delta,int required,bool active,bool condition)=>active&&condition?System.Math.Min(required,current+System.Math.Max(0,delta)):0;
        public static bool IsVictory(in CampaignMissionMarketLifelineState state,in CampaignMissionMarketLifelineDefinitionBlob rules)=>
            state.Failure==MarketLifelineFailure.None && state.DeliveredCount>=rules.RequiredDeliveries && state.ManifestVerified!=0 && state.VictoryHoldMilliseconds>=rules.VictoryHoldMilliseconds;
    }
}
