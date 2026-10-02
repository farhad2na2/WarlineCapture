namespace Game.Missions.Contracts
{
    /// <summary>Authored progression links are independent of whether the target content can deploy.</summary>
    public static class CampaignMissionSequence
    {
        public const string Gridlock="saga.ch02.m01.gridlock";
        public const string SupplyLine="saga.ch02.m02.supply_line";
        public const string MarketLifeline="saga.ch02.m03.market_lifeline";
        public const string PowerRelay="saga.ch02.m04.power_relay";
        public const string RouteReopened="saga.ch02.m05.route_reopened";
        public const string SignalTrace="saga.ch03.m01.signal_trace";
        public const string SafehouseSweep="saga.ch03.m02.safehouse_sweep";
        public const string FalseFront="saga.ch03.m03.false_front";
        public const string EvidenceChain="saga.ch03.m04.evidence_chain";
        public const string NetworkBreak="saga.ch03.m05.network_break";
        public const string AirCorridor="saga.ch04.m01.air_corridor";
        public const string SteelPush="saga.ch04.m02.steel_push";
        public const string SplitFront="saga.ch04.m03.split_front";
        public const string GroundedSignal="saga.ch04.m04.grounded_signal";
        public const string ArmorBreak="saga.ch04.m05.armor_break";
        public const string CitywideAlert="saga.ch05.m01.citywide_alert";
        public const string TrustUnderFire="saga.ch05.m02.trust_under_fire";
        public const int RegisteredMissionCount=22;
        public static string IdAt(int index) => index switch
        {
            0=>"saga.ch01.m01.first_contact",1=>"saga.ch01.m02.establish_base",
            2=>"saga.ch01.m03.radar_warning",3=>"saga.ch01.m04.airlift",
            4=>"saga.ch01.m05.breach_assault",5=>Gridlock,6=>SupplyLine,7=>MarketLifeline,8=>PowerRelay,9=>RouteReopened,10=>SignalTrace,11=>SafehouseSweep,12=>FalseFront,13=>EvidenceChain,14=>NetworkBreak,15=>AirCorridor,16=>SteelPush,17=>SplitFront,18=>GroundedSignal,19=>ArmorBreak,20=>CitywideAlert,21=>TrustUnderFire,_=>string.Empty
        };
        public static int IndexOf(string id)
        {
            for(int i=0;i<RegisteredMissionCount;i++) if(IdAt(i)==id) return i;
            return -1;
        }
        public static string Next(string id)
        {
            int index=IndexOf(id);
            return index<0?string.Empty:index==RegisteredMissionCount-1?string.Empty:IdAt(index+1);
        }
    }
}
