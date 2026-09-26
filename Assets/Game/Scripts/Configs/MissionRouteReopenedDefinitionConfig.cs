using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionRouteReopenedDefinitionConfig
    {
        [SerializeField] private bool enabled;
        [SerializeField] private string reliefGoalAnchorId, fuelGoalAnchorId, disruptedLinkAnchorId, hubGateAnchorId, recordsAnchorId;
        [SerializeField] private int linkRepairHoldMilliseconds, recordsHoldMilliseconds, deadlineMilliseconds;
        [SerializeField] private float deliveryRadius, repairRadius, hubRadius, recordsRadius;
        [SerializeField] private MissionCameraTourConfig cameraTour;
        public bool Enabled=>enabled;
        public string ReliefGoalAnchorId=>reliefGoalAnchorId;
        public string FuelGoalAnchorId=>fuelGoalAnchorId;
        public string DisruptedLinkAnchorId=>disruptedLinkAnchorId;
        public string HubGateAnchorId=>hubGateAnchorId;
        public string RecordsAnchorId=>recordsAnchorId;
        public int LinkRepairHoldMilliseconds=>linkRepairHoldMilliseconds;
        public int RecordsHoldMilliseconds=>recordsHoldMilliseconds;
        public int DeadlineMilliseconds=>deadlineMilliseconds;
        public float DeliveryRadius=>deliveryRadius;
        public float RepairRadius=>repairRadius;
        public float HubRadius=>hubRadius;
        public float RecordsRadius=>recordsRadius;
        public MissionCameraTourConfig CameraTour=>cameraTour;
    }

    public static class MissionRouteReopenedDefinitionValidation
    {
        public static bool TryValidate(ScenarioSetupConfig scenario,out string error)
        {
            error=null;var p=scenario.RouteReopened;if(!p.Enabled)return true;
            if(scenario.Defense.Enabled || scenario.Extraction.Enabled || scenario.Breach.Enabled || scenario.Gridlock.Enabled ||
               scenario.SupplyLine.Enabled || scenario.MarketLifeline.Enabled || scenario.PowerRelay.Enabled || scenario.MissionRuntime.Enabled ||
               p.LinkRepairHoldMilliseconds<3000 || p.RecordsHoldMilliseconds<3000 || p.DeadlineMilliseconds<=p.LinkRepairHoldMilliseconds ||
               p.DeliveryRadius<=0 || p.RepairRadius<=0 || p.HubRadius<=0 || p.RecordsRadius<=0 || !p.CameraTour.IsValid)
            {error="Route Reopened mode or timing is invalid.";return false;}
            foreach(string id in new[]{p.ReliefGoalAnchorId,p.FuelGoalAnchorId,p.DisruptedLinkAnchorId,p.HubGateAnchorId,p.RecordsAnchorId})
            {
                bool found=false;foreach(var anchor in scenario.RequiredAnchors)found|=anchor.AnchorId==id;
                if(!found){error="Route Reopened anchor missing: "+id;return false;}
            }
            int rifles=0,engineers=0,relief=0,fuel=0,hostiles=0;
            foreach(var group in scenario.UnitGroups)foreach(var unit in group.Units)
            {
                if(group.FactionIndex==2){hostiles+=unit.Count;continue;}
                switch(unit.MissionRoleId)
                {
                    case "role.route.rifle": rifles+=unit.Count;break;
                    case "role.route.engineer": engineers+=unit.Count;break;
                    case "role.route.relief_convoy": relief+=unit.Count;break;
                    case "role.route.fuel_convoy": fuel+=unit.Count;break;
                }
            }
            if(rifles!=8 || engineers!=2 || relief!=1 || fuel!=1 || hostiles<1)
            {error="Route Reopened requires eight rifles, two road engineers, one relief truck, one Fuel truck and hostiles.";return false;}
            return true;
        }
    }
}
