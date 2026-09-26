using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionMarketLifelineDefinitionConfig
    {
        [SerializeField] private bool enabled;
        [SerializeField] private string deliveryAnchorId, manifestAnchorId, corruptManifestAnchorId;
        [SerializeField] private int requiredDeliveries, manifestHoldMilliseconds, victoryHoldMilliseconds, deadlineMilliseconds;
        [SerializeField] private float deliveryRadius, manifestRadius;
        [SerializeField] private MissionCameraTourConfig cameraTour;
        public bool Enabled=>enabled;
        public string DeliveryAnchorId=>deliveryAnchorId;
        public string ManifestAnchorId=>manifestAnchorId;
        public string CorruptManifestAnchorId=>corruptManifestAnchorId;
        public int RequiredDeliveries=>requiredDeliveries;
        public int ManifestHoldMilliseconds=>manifestHoldMilliseconds;
        public int VictoryHoldMilliseconds=>victoryHoldMilliseconds;
        public int DeadlineMilliseconds=>deadlineMilliseconds;
        public float DeliveryRadius=>deliveryRadius;
        public float ManifestRadius=>manifestRadius;
        public MissionCameraTourConfig CameraTour=>cameraTour;
    }

    public static class MissionMarketLifelineDefinitionValidation
    {
        public static bool TryValidate(ScenarioSetupConfig scenario,out string error)
        {
            error=null;var m=scenario.MarketLifeline;if(!m.Enabled)return true;
            if(scenario.Defense.Enabled || scenario.Extraction.Enabled || scenario.Breach.Enabled || scenario.Gridlock.Enabled || scenario.SupplyLine.Enabled || scenario.MissionRuntime.Enabled ||
               m.RequiredDeliveries!=3 || m.ManifestHoldMilliseconds<3000 || m.VictoryHoldMilliseconds<3000 || m.DeadlineMilliseconds<=m.ManifestHoldMilliseconds ||
               m.DeliveryRadius<=0 || m.ManifestRadius<=0 || !m.CameraTour.IsValid)
            {error="Market Lifeline mode or timing is invalid.";return false;}
            foreach(string id in new[]{m.DeliveryAnchorId,m.ManifestAnchorId,m.CorruptManifestAnchorId})
            {
                bool found=false;foreach(var anchor in scenario.RequiredAnchors)found|=anchor.AnchorId==id;
                if(!found){error="Market Lifeline anchor missing: "+id;return false;}
            }
            int rifles=0,convoy=0,corrupt=0,hostiles=0;
            foreach(var group in scenario.UnitGroups)foreach(var unit in group.Units)
            {
                if(group.FactionIndex==1 && unit.MissionRoleId=="role.market.rifle")rifles+=unit.Count;
                else if(group.FactionIndex==1 && unit.MissionRoleId=="role.market.relief_convoy")convoy+=unit.Count;
                else if(group.FactionIndex==1 && unit.MissionRoleId=="role.market.corrupt_transfer")corrupt+=unit.Count;
                else if(group.FactionIndex==2)hostiles+=unit.Count;
            }
            if(rifles!=8 || convoy!=3 || corrupt!=1 || hostiles<1){error="Market Lifeline requires eight rifles, three legitimate relief trucks, one corrupt transfer truck and hostiles.";return false;}
            return true;
        }
    }
}
