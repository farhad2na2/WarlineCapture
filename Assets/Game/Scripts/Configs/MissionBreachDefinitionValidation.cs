using System;

namespace Game.Configs
{
    public static class MissionBreachDefinitionValidation
    {
        public static bool TryValidate(ScenarioSetupConfig scenario,out string error)
        {
            error=null; var breach=scenario.Breach;
            if(!breach.Enabled) return true;
            if(scenario.Defense.Enabled || scenario.Extraction.Enabled || scenario.MissionRuntime.Enabled || !breach.CameraTour.IsValid ||
                !scenario.Restrictions.BuildingDisabled || !scenario.Restrictions.ProductionDisabled || !scenario.Restrictions.EconomyDisabled || breach.GateHealth<=0 || breach.CoreHealth<=0 ||
                breach.SecureHoldMilliseconds<=0 || breach.DeadlineMilliseconds<=breach.SecureHoldMilliseconds ||
                breach.CounterattackWarningMilliseconds<5000 || float.IsNaN(breach.ArchiveRadius) || float.IsInfinity(breach.ArchiveRadius) || breach.ArchiveRadius<4 ||
                string.IsNullOrWhiteSpace(breach.GateBuildingId) || string.IsNullOrWhiteSpace(breach.CoreBuildingId))
            {error="Breach scenario has incompatible modes or invalid targets/timing.";return false;}
            string[] anchors={breach.ApproachAnchorId,breach.GateAnchorId,breach.CoreAnchorId,breach.ArchiveAnchorId};
            for(int i=0;i<anchors.Length;i++)
            {
                bool found=false;
                foreach(var anchor in scenario.RequiredAnchors) if(string.Equals(anchor.AnchorId,anchors[i],StringComparison.Ordinal)) found=true;
                if(!found) {error="Breach anchor is not declared: "+anchors[i];return false;}
                for(int prior=0;prior<i;prior++) if(anchors[prior]==anchors[i]) {error="Breach objectives need distinct anchors.";return false;}
            }
            int support=0,counterattack=0,rifles=0;
            foreach(var group in scenario.UnitGroups)
                foreach(var unit in group.Units)
                {
                    if(unit.MissionRoleId==breach.SupportRoleId) support+=group.FactionIndex==1?unit.Count:-100;
                    else if(group.FactionIndex==1) rifles+=unit.Count;
                    if(unit.MissionRoleId==breach.CounterattackRoleId) counterattack+=group.FactionIndex>1?unit.Count:-100;
                }
            if(support!=1 || counterattack<1 || rifles<1 || breach.SupportRoleId==breach.CounterattackRoleId) {error="Breach support and counterattack roles must exist in the authored roster.";return false;}
            return true;
        }
    }
}
