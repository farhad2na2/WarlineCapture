using System;
using UnityEngine;
namespace Game.Configs
{
    [Serializable]
    public struct MissionSupplyLineDefinitionConfig
    {
        [SerializeField] private bool enabled;
        [SerializeField] private string oilAnchorId, refineryAnchorId, storageAnchorId, alternateLaneAnchorId;
        [SerializeField] private string oilBuildingId, refineryBuildingId, storageBuildingId;
        [SerializeField] private int reserveBarrels, civilianReserveBarrels, holdMilliseconds, deadlineMilliseconds;
        public bool Enabled => enabled;
        public string OilAnchorId => oilAnchorId;
        public string AlternateLaneAnchorId => alternateLaneAnchorId;
        public string RefineryAnchorId => refineryAnchorId;
        public string StorageAnchorId => storageAnchorId;
        public string OilBuildingId => oilBuildingId;
        public string RefineryBuildingId => refineryBuildingId;
        public string StorageBuildingId => storageBuildingId;
        public int ReserveBarrels => reserveBarrels;
        public int CivilianReserveBarrels => civilianReserveBarrels;
        public int HoldMilliseconds => holdMilliseconds;
        public int DeadlineMilliseconds => deadlineMilliseconds;
    }
    public static class MissionSupplyLineDefinitionValidation
    {
        public static bool TryValidate(ScenarioSetupConfig scenario, out string error)
        {
            error=null;var s=scenario.SupplyLine;if(!s.Enabled)return true;
            if(scenario.Gridlock.Enabled || scenario.Defense.Enabled || scenario.Extraction.Enabled || scenario.Breach.Enabled || scenario.MissionRuntime.Enabled ||
                s.ReserveBarrels<=0 || s.CivilianReserveBarrels<=0 || s.CivilianReserveBarrels>=s.ReserveBarrels || s.HoldMilliseconds<=0 || s.DeadlineMilliseconds<=s.HoldMilliseconds ||
                string.IsNullOrEmpty(s.OilBuildingId) || string.IsNullOrEmpty(s.RefineryBuildingId) || string.IsNullOrEmpty(s.StorageBuildingId))
            {error="Supply Line mode, chain or reserve configuration is invalid.";return false;}
            var anchors=new[]{s.OilAnchorId,s.RefineryAnchorId,s.StorageAnchorId,s.AlternateLaneAnchorId};
            for(int i=0;i<anchors.Length;i++)
            {
                bool found=false;foreach(var a in scenario.RequiredAnchors)found|=a.AnchorId==anchors[i];
                if(!found){error="Supply Line anchor missing: "+anchors[i];return false;}
                for(int j=0;j<i;j++)if(anchors[j]==anchors[i]){error="Supply Line anchors must be distinct.";return false;}
            }
            int oil=0,fuel=0,rifles=0,hostiles=0;
            foreach(var g in scenario.UnitGroups)foreach(var u in g.Units)
            {
                if(g.FactionIndex==1 && u.MissionRoleId=="role.supply.oil_hauler")oil+=u.Count;
                else if(g.FactionIndex==1 && u.MissionRoleId=="role.supply.fuel_hauler")fuel+=u.Count;
                else if(g.FactionIndex==1 && u.MissionRoleId=="role.friendly.command_squad")rifles+=u.Count;
                else if(g.FactionIndex==2)hostiles+=u.Count;
            }
            if(oil!=1 || fuel!=1 || rifles!=8 || hostiles<1){error="Supply Line requires eight rifles, two distinct haulers and hostiles.";return false;}
            return true;
        }
    }
}
