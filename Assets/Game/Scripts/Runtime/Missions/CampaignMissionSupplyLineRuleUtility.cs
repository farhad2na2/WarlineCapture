using Game.Components;
using Unity.Mathematics;
namespace Game.Runtime
{
    public static class CampaignMissionSupplyLineRuleUtility
    {
        public static bool IsAlternateLaneOrder(int2 target,int2 alternateLane) =>
            math.distancesq(target,alternateLane)<=16;
        public static bool IsVictory(in CampaignMissionSupplyLineState s,in CampaignMissionSupplyLineDefinitionBlob rules) =>
            s.Ready!=0 && s.RouteRecovered!=0 && s.Failure==SupplyLineFailure.None && s.OilTransferred!=0 && s.FuelTransferred!=0 &&
            s.AllocatedCivilianBarrels>=rules.CivilianReserveBarrels && s.StoredFuel>=rules.ReserveBarrels && s.HoldMilliseconds>=rules.HoldMilliseconds && s.ElapsedMilliseconds<rules.DeadlineMilliseconds;
        public static int AdvanceHold(int current,int delta,int required,bool active,bool linksAlive,bool enemiesDefeated,float stored,int reserve) =>
            !active?current:!linksAlive || !enemiesDefeated || stored<reserve?0:(int)System.Math.Min(required,(long)current+System.Math.Max(0,delta));
    }
}
