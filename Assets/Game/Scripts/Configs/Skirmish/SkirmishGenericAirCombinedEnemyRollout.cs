using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    // Bounded migration eligibility, not a per-mission tactical policy. All entries
    // use the same existing ground-group and separate paid-air capability owners.
    // This deliberately excludes coded S003/S004 and the existing S005 C/F policy.
    public static class SkirmishGenericAirCombinedEnemyRollout
    {
        private readonly struct Entry
        {
            public readonly string Id;
            public readonly SkirmishArmyProfileId Army;
            public readonly SkirmishStartPackageId Start;
            public Entry(string id,SkirmishArmyProfileId army,SkirmishStartPackageId start){Id=id;Army=army;Start=start;}
        }
        private static readonly Entry[] Entries={
            new Entry("S006",SkirmishArmyProfileId.CombinedArms,SkirmishStartPackageId.EstablishedBase),
            new Entry("S027",SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.FieldBase),
            new Entry("S028",SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.EstablishedBase),
            new Entry("S030",SkirmishArmyProfileId.CombinedArms,SkirmishStartPackageId.EstablishedBase),
            new Entry("S051",SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.FieldBase),
            new Entry("S052",SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.EstablishedBase),
            new Entry("S054",SkirmishArmyProfileId.CombinedArms,SkirmishStartPackageId.EstablishedBase),
            new Entry("S075",SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.FieldBase),
            new Entry("S076",SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.EstablishedBase),
            new Entry("S078",SkirmishArmyProfileId.CombinedArms,SkirmishStartPackageId.EstablishedBase),
            new Entry("S099",SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.FieldBase),
            new Entry("S100",SkirmishArmyProfileId.AirMobile,SkirmishStartPackageId.EstablishedBase),
            new Entry("S102",SkirmishArmyProfileId.CombinedArms,SkirmishStartPackageId.EstablishedBase)};
        public static string[] ReadCatalogIds(){var result=new string[Entries.Length];for(int i=0;i<result.Length;i++)result[i]=Entries[i].Id;return result;}
        public static bool IncludesCatalog(string id){foreach(var entry in Entries)if(entry.Id==id)return true;return false;}
        public static bool Supports(SkirmishResolvedSetup setup)
        {
            if(setup==null||setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault||setup.PlayerFaction!=1||setup.EnemyFaction!=2||
                !setup.MeasuredLayoutBound||string.IsNullOrEmpty(setup.LayoutBindingFingerprint)||
                setup.DefinitionId!="skirmish."+setup.CatalogId?.ToLowerInvariant()||setup.Forces==null||setup.Forces.Length==0||
                setup.RoleOverlays==null||setup.RoleOverlays.Length==0)return false;
            bool included=false;
            foreach(var entry in Entries)if(entry.Id==setup.CatalogId)
            {
                included=setup.ArmyProfileId==entry.Army&&setup.StartPackageId==entry.Start&&
                    setup.Readiness==(entry.Start==SkirmishStartPackageId.FieldBase?SkirmishReadinessStage.Field:SkirmishReadinessStage.Established);
                break;
            }
            if(!included)return false;
            var army=SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            var identities=new HashSet<string>(StringComparer.Ordinal);int playerAir=0,enemyAir=0;
            foreach(var force in setup.Forces)
            {
                if(force.Quantity<=0||(force.FactionId!=1&&force.FactionId!=2)||
                    force.RoleId!=SkirmishRoleIds.ToId(force.RoleKind)||!army.Allows(force.RoleId)||
                    force.RuntimePrefabKey!=SkirmishRoleCatalogConfig.RuntimePrefabKey(force.RoleKind)||
                    !identities.Add(force.FactionId+":"+force.RoleId))return false;
                if(SkirmishRoleIds.Category(force.RoleKind)!=SkirmishPopulationCategory.Air)continue;
                if(force.RoleKind!=SkirmishRoleKind.TransportHeli)return false;
                if(force.FactionId==1)playerAir+=force.Quantity;else enemyAir+=force.Quantity;
            }
            return playerAir==setup.PlayerAir&&enemyAir==setup.EnemyAir;
        }
        public static bool IsAuthoredStartingTransport(SkirmishResolvedSetup setup,string stableId,SkirmishRoleKind role,int faction)
        {
            if(!Supports(setup)||faction!=2||role!=SkirmishRoleKind.TransportHeli||string.IsNullOrEmpty(stableId))return false;
            int matches=0;
            for(int i=0;i<setup.Forces.Length;i++)
            {
                var force=setup.Forces[i];if(force.FactionId!=faction||force.RoleKind!=role)continue;
                for(int member=0;member<force.Quantity;member++)if(stableId==force.RoleId+"."+faction+"."+i+"."+member)matches++;
            }
            return matches==1;
        }
        public static bool IsReplenishableCombatRole(SkirmishResolvedSetup setup,SkirmishRoleKind role)
        {
            if(!Supports(setup)||!SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId).Allows(SkirmishRoleIds.ToId(role))||
                !SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays,role,out var overlay))return false;
            var category=SkirmishRoleIds.Category(role);
            return overlay.Damage>0&&(category==SkirmishPopulationCategory.Infantry||category==SkirmishPopulationCategory.Ground);
        }
    }
}
