using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionPowerRelayDefinitionConfig
    {
        [SerializeField] private bool enabled;
        [SerializeField] private string shortRouteAnchorId, safeRouteAnchorId, shelterAnchorId, repairAnchorId;
        [SerializeField] private int repairHoldMilliseconds, victoryHoldMilliseconds, deadlineMilliseconds;
        [SerializeField] private float routeRadius, shelterRadius, repairRadius;
        [SerializeField] private MissionCameraTourConfig cameraTour;
        public bool Enabled=>enabled;
        public string ShortRouteAnchorId=>shortRouteAnchorId;
        public string SafeRouteAnchorId=>safeRouteAnchorId;
        public string ShelterAnchorId=>shelterAnchorId;
        public string RepairAnchorId=>repairAnchorId;
        public int RepairHoldMilliseconds=>repairHoldMilliseconds;
        public int VictoryHoldMilliseconds=>victoryHoldMilliseconds;
        public int DeadlineMilliseconds=>deadlineMilliseconds;
        public float RouteRadius=>routeRadius;
        public float ShelterRadius=>shelterRadius;
        public float RepairRadius=>repairRadius;
        public MissionCameraTourConfig CameraTour=>cameraTour;
    }

    public static class MissionPowerRelayDefinitionValidation
    {
        public static bool TryValidate(ScenarioSetupConfig scenario,out string error)
        {
            error=null;var p=scenario.PowerRelay;if(!p.Enabled)return true;
            if(scenario.Defense.Enabled || scenario.Extraction.Enabled || scenario.Breach.Enabled || scenario.Gridlock.Enabled ||
               scenario.SupplyLine.Enabled || scenario.MarketLifeline.Enabled || scenario.MissionRuntime.Enabled ||
               p.RepairHoldMilliseconds<3000 || p.VictoryHoldMilliseconds<3000 || p.DeadlineMilliseconds<=p.RepairHoldMilliseconds ||
               p.RouteRadius<=0 || p.ShelterRadius<=0 || p.RepairRadius<=0 || !p.CameraTour.IsValid)
            {error="Power Relay mode or timing is invalid.";return false;}
            foreach(string id in new[]{p.ShortRouteAnchorId,p.SafeRouteAnchorId,p.ShelterAnchorId,p.RepairAnchorId})
            {
                bool found=false;foreach(var anchor in scenario.RequiredAnchors)found|=anchor.AnchorId==id;
                if(!found){error="Power Relay anchor missing: "+id;return false;}
            }
            int rifles=0,engineers=0,families=0,fuel=0,hostiles=0;
            foreach(var group in scenario.UnitGroups)foreach(var unit in group.Units)
            {
                if(group.FactionIndex==2){hostiles+=unit.Count;continue;}
                switch(unit.MissionRoleId)
                {
                    case "role.power.rifle": rifles+=unit.Count;break;
                    case "role.power.engineer": engineers+=unit.Count;break;
                    case "role.power.family_convoy": families+=unit.Count;break;
                    case "role.power.fuel_service": fuel+=unit.Count;break;
                }
            }
            if(rifles!=8 || engineers!=2 || families!=1 || fuel!=1 || hostiles<1)
            {error="Power Relay requires eight rifles, two engineers, one family convoy, one Fuel service truck and hostiles.";return false;}
            return true;
        }
    }
}
