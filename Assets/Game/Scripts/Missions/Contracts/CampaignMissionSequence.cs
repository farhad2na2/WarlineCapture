namespace Game.Missions.Contracts
{
    /// <summary>Authored progression links are independent of whether the target content can deploy.</summary>
    public static class CampaignMissionSequence
    {
        public const string Gridlock="saga.ch02.m01.gridlock";
        public const string SupplyLine="saga.ch02.m02.supply_line";
        public const string MarketLifeline="saga.ch02.m03.market_lifeline";
        public const string PowerRelay="saga.ch02.m04.power_relay";
        public const int RegisteredMissionCount=9;
        public static string IdAt(int index) => index switch
        {
            0=>"saga.ch01.m01.first_contact",1=>"saga.ch01.m02.establish_base",
            2=>"saga.ch01.m03.radar_warning",3=>"saga.ch01.m04.airlift",
            4=>"saga.ch01.m05.breach_assault",5=>Gridlock,6=>SupplyLine,7=>MarketLifeline,8=>PowerRelay,_=>string.Empty
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
