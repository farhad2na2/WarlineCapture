using Game.Skirmish.Contracts;

namespace Game.Configs
{
    // Configuration capability scope, not native acceptance or publication.
    public static class SkirmishEnemyObjectivePolicyScope
    {
        public static bool Supports(SkirmishResolvedSetup setup)
        {
            if(setup==null||(setup.ObjectiveKind!=SkirmishObjectiveKind.FrontlineControl&&
                setup.ObjectiveKind!=SkirmishObjectiveKind.Breakthrough&&setup.ObjectiveKind!=SkirmishObjectiveKind.ConvoyEscort)||
                setup.PlayerFaction!=1||setup.EnemyFaction!=2||!setup.MeasuredLayoutBound||
                string.IsNullOrEmpty(setup.LayoutBindingFingerprint)||string.IsNullOrEmpty(setup.MissionLogicalBindingFingerprint)||
                setup.Forces==null||setup.Forces.Length==0||setup.RoleOverlays==null||setup.RoleOverlays.Length==0)return false;
            var army=SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            if(army==null)return false;
            foreach(var force in setup.Forces)
            {
                if(force.Quantity<=0)return false;
                if(force.RoleKind==SkirmishRoleKind.ConvoyLogic)
                {if(setup.ObjectiveKind!=SkirmishObjectiveKind.ConvoyEscort||force.FactionId!=1) return false;continue;}
                if(!army.Allows(SkirmishRoleIds.ToId(force.RoleKind)))return false;
                // Existing initial helicopter owner is supported explicitly below.
                // Other starting air needs its own immutable/native lifecycle proof.
                if(SkirmishRoleIds.Category(force.RoleKind)==SkirmishPopulationCategory.Air&&force.RoleKind!=SkirmishRoleKind.TransportHeli)return false;
            }
            return true;
        }
        public static bool IsAuthoredStartingTransport(SkirmishResolvedSetup setup,string stableId,SkirmishRoleKind role,int faction)
        {
            if(!Supports(setup)||faction!=2||role!=SkirmishRoleKind.TransportHeli||string.IsNullOrEmpty(stableId))return false;
            int matches=0;
            for(int i=0;i<setup.Forces.Length;i++)
            {
                var force=setup.Forces[i];
                if(force.FactionId!=faction||force.RoleKind!=role||force.RuntimePrefabKey!=SkirmishRoleCatalogConfig.RuntimePrefabKey(role))continue;
                for(int member=0;member<force.Quantity;member++)
                    if(stableId==force.RoleId+"."+force.FactionId+"."+i+"."+member)matches++;
            }
            return matches==1;
        }
    }
}
