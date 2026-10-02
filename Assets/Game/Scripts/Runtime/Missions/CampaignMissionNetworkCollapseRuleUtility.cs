using Game.Components;
using Game.Missions.Contracts;
namespace Game.Runtime
{
    public static class CampaignMissionNetworkCollapseRuleUtility
    {
        public const int HoldMilliseconds=6000, DeadlineMilliseconds=900000, OriginalCount=21, MilitaryCount=9;
        public static bool Matches(in CampaignMissionNetworkCollapseState s,in CampaignMissionRuntimeComponent r)=>s.Initialized!=0&&s.SessionToken.Equals(r.SessionToken)&&s.AttemptOrdinal==r.AttemptOrdinal&&s.SourceVersion==r.SourceVersion;
        public static int Stage(in CampaignMissionNetworkCollapseState s)=>s.NodeOneVerified==0?1:s.NodeOneDisabled==0?2:s.NodeTwoVerified==0?3:s.NodeTwoDisabled==0?4:s.NodeThreeVerified==0?5:s.NodeThreeDisabled==0?6:s.AuditRecovered==0?7:8;
        public static bool CanVerify(int node,in CampaignMissionNetworkCollapseState s,int guardsAlive)=>guardsAlive==0&&(node==0||node==1&&s.NodeOneVerified!=0&&s.NodeOneDisabled!=0||node==2&&s.NodeTwoVerified!=0&&s.NodeTwoDisabled!=0);
        public static int AdvanceHold(int current,int delta,bool qualified)=>!qualified?0:current>=HoldMilliseconds-System.Math.Max(0,delta)?HoldMilliseconds:current+System.Math.Max(0,delta);
        public static bool VictoryQualified(in CampaignMissionNetworkCollapseState s,in CampaignMissionAttemptFactsComponent f)=>s.Ready!=0&&s.Failure==NetworkCollapseFailure.None&&s.NodeOneVerified!=0&&s.NodeTwoVerified!=0&&s.NodeThreeVerified!=0&&s.NodeOneDisabled!=0&&s.NodeTwoDisabled!=0&&s.NodeThreeDisabled!=0&&s.AuditRecovered!=0&&s.EngineerAboard!=0&&s.Extracted!=0&&s.ExtractionMilliseconds>=HoldMilliseconds&&s.MilitaryCleared!=0&&f.HostileTotalCount==MilitaryCount&&f.HostileDefeatedCount==MilitaryCount&&f.CivilianTotalCount==4&&f.CivilianLossCount==0;
        public static bool TryAdvance(in CampaignMissionRuntimeComponent runtime,in CampaignMissionAttemptFactsComponent facts,in CampaignMissionNetworkCollapseState mission,bool opening,out CampaignMissionRuntimeComponent next)
        {
            next=runtime;if(!Matches(in mission,in runtime)||runtime.Outcome!=MissionOutcomeKind.None)return false;
            if(mission.Failure!=NetworkCollapseFailure.None){next.Phase=MissionPhaseKind.Result;next.Outcome=MissionOutcomeKind.Defeat;next.ReturnDestination=MissionReturnDestinationKind.CampaignOperations;}
            else if(runtime.Phase==MissionPhaseKind.FindSquad&&mission.Ready!=0&&opening)next.Phase=MissionPhaseKind.Engage;
            else if(runtime.Phase==MissionPhaseKind.Engage&&VictoryQualified(in mission,in facts)){next.Phase=MissionPhaseKind.Result;next.Outcome=MissionOutcomeKind.Victory;next.ReturnDestination=MissionReturnDestinationKind.CampaignOperations;}
            else return false;
            next.Version=runtime.Version==uint.MaxValue?1:runtime.Version+1;return true;
        }
    }
}
