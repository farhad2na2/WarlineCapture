using System;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionGridlockDefinitionConfig
    {
        [SerializeField] private bool enabled;
        [SerializeField] private string siteAAnchorId, siteBAnchorId, hospitalAnchorId, obstructionAAnchorId, obstructionBAnchorId;
        [SerializeField] private string fadiRoleId, workerRoleId, vehicleRoleId, counterattackRoleId;
        [SerializeField] private string obstructionBuildingId;
        [SerializeField] private int workMilliseconds, holdMilliseconds, deadlineMilliseconds, warningMilliseconds;
        [SerializeField] private float workRadius, threatRadius, hospitalRadius;
        public bool Enabled => enabled;
        public string SiteAAnchorId => siteAAnchorId;
        public string ObstructionAAnchorId => obstructionAAnchorId;
        public string ObstructionBAnchorId => obstructionBAnchorId;
        public string SiteBAnchorId => siteBAnchorId;
        public string HospitalAnchorId => hospitalAnchorId;
        public string FadiRoleId => fadiRoleId;
        public string WorkerRoleId => workerRoleId;
        public string VehicleRoleId => vehicleRoleId;
        public string CounterattackRoleId => counterattackRoleId;
        public string ObstructionBuildingId => obstructionBuildingId;
        public int WorkMilliseconds => workMilliseconds;
        public int HoldMilliseconds => holdMilliseconds;
        public int DeadlineMilliseconds => deadlineMilliseconds;
        public int WarningMilliseconds => warningMilliseconds;
        public float WorkRadius => workRadius;
        public float ThreatRadius => threatRadius;
        public float HospitalRadius => hospitalRadius;
    }

    public static class MissionGridlockDefinitionValidation
    {
        public static bool TryValidate(ScenarioSetupConfig scenario, out string error)
        {
            error = null;
            var g = scenario.Gridlock;
            if (!g.Enabled) return true;
            if (scenario.Defense.Enabled || scenario.Extraction.Enabled || scenario.Breach.Enabled || scenario.MissionRuntime.Enabled ||
                !scenario.Restrictions.BuildingDisabled || !scenario.Restrictions.ProductionDisabled ||
                g.WorkMilliseconds <= 0 || g.HoldMilliseconds <= 0 || (long)g.DeadlineMilliseconds <= 2L*g.WorkMilliseconds+g.HoldMilliseconds ||
                g.WarningMilliseconds < 10000 || !Radius(g.WorkRadius) || !Radius(g.ThreatRadius) || !Radius(g.HospitalRadius) ||
                string.IsNullOrWhiteSpace(g.ObstructionBuildingId))
                return Fail("Gridlock has incompatible modes or invalid work, route or timing configuration.", out error);
            string[] anchors = { g.SiteAAnchorId, g.SiteBAnchorId, g.HospitalAnchorId, g.ObstructionAAnchorId, g.ObstructionBAnchorId };
            for (int i=0;i<anchors.Length;i++)
            {
                bool found=false;
                foreach (var anchor in scenario.RequiredAnchors) found |= anchor.AnchorId==anchors[i];
                if (!found) return Fail("Undeclared Gridlock anchor: "+anchors[i], out error);
                for (int j=0;j<i;j++) if (anchors[i]==anchors[j]) return Fail("Gridlock anchors must be distinct.", out error);
            }
            string[] roles = {g.FadiRoleId,g.WorkerRoleId,g.VehicleRoleId,g.CounterattackRoleId};
            for(int i=0;i<roles.Length;i++)
            {
                if(string.IsNullOrWhiteSpace(roles[i])) return Fail("Gridlock role is empty.",out error);
                for(int j=0;j<i;j++) if(roles[i]==roles[j]) return Fail("Gridlock roles must be distinct.",out error);
            }
            int fadi=0,workers=0,vehicle=0,rifles=0,counter=0;
            foreach(var group in scenario.UnitGroups)
                foreach(var unit in group.Units)
                {
                    if(unit.MissionRoleId==g.FadiRoleId) fadi+=group.FactionIndex==1?unit.Count:-100;
                    else if(unit.MissionRoleId==g.WorkerRoleId) workers+=group.FactionIndex==1?unit.Count:-100;
                    else if(unit.MissionRoleId==g.VehicleRoleId) vehicle+=group.FactionIndex==1?unit.Count:-100;
                    else if(unit.MissionRoleId==g.CounterattackRoleId) counter+=group.FactionIndex==2?unit.Count:-100;
                    else if(unit.MissionRoleId=="role.friendly.command_squad" && group.FactionIndex==1) rifles+=unit.Count;
                }
            if(fadi!=1 || workers!=2 || vehicle!=1 || rifles!=8 || counter!=4)
                return Fail("Gridlock requires Fadi, two workers, eight rifles, one vehicle and four counterattack members.",out error);
            return true;
        }
        private static bool Radius(float value) => value>=2 && !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Fail(string message,out string error) {error=message;return false;}
    }
}
