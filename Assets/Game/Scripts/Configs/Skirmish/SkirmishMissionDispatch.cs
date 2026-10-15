using System;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    // Index is a transport token from the existing published library, never a
    // mission identifier inferred from its reused physical map. No publication.
    public static class SkirmishMissionDispatch
    {
        public static bool IsLegacyIndex(int index)=>index>=0&&index<=7;
        public static bool TryGetGenericByIndex(SkirmishBattleCatalogConfig catalog,int index,out SkirmishBattleCatalogEntry entry)
        {
            entry=default;if(index<=7||catalog?.Entries==null)return false;int matches=0;
            foreach(var candidate in catalog.Entries)
                if(candidate.IsPlayable&&candidate.PlayableScenarioIndex==index){entry=candidate;matches++;}
            if(matches!=1||!Canonical(entry.ScenarioId)||TryGetLegacyIndex(entry.ScenarioId,out _)||entry.ContentVersion<=0||
                entry.DefinitionId!=SkirmishDefinitionId.FromCatalog(new SkirmishCatalogId(entry.ScenarioId)).Value)return false;
            int ids=0;foreach(var candidate in catalog.Entries)if(candidate.ScenarioId==entry.ScenarioId)ids++;
            return ids==1;
        }
        public static bool TryGetGenericById(SkirmishBattleCatalogConfig catalog,string catalogId,out SkirmishBattleCatalogEntry entry)
        {
            entry=default;if(!Canonical(catalogId)||catalog?.Entries==null)return false;int matches=0;
            foreach(var candidate in catalog.Entries)if(candidate.ScenarioId==catalogId){entry=candidate;matches++;}
            return matches==1&&TryGetGenericByIndex(catalog,entry.PlayableScenarioIndex,out var exact)&&exact.ScenarioId==catalogId;
        }
        public static bool MatchesDefinition(in SkirmishBattleCatalogEntry entry,SkirmishScenarioDefinitionConfig definition)=>
            definition!=null&&entry.IsPlayable&&entry.ScenarioId==definition.CatalogId&&entry.DefinitionId==definition.DefinitionId&&
            entry.ContentVersion==definition.ContentVersion;
        public static bool TryGetLegacyIndex(string catalogId,out int index)
        {
            index=catalogId=="S001"?0:catalogId=="S025"?1:catalogId=="S073"?3:catalogId=="S002"?4:
                catalogId=="S003"?5:catalogId=="S004"?6:catalogId=="S005"?7:-1;
            return index>=0;
        }
        public static bool Canonical(string id)
        {
            if(id==null||id.Length!=4||id[0]!='S')return false;
            int number=0;for(int i=1;i<4;i++){if(id[i]<'0'||id[i]>'9')return false;number=number*10+id[i]-'0';}
            return number>=1&&number<=120;
        }
        public static bool PayloadMatches(SkirmishLaunchPayload payload,SkirmishResolvedSetup setup)=>
            payload!=null&&setup!=null&&!payload.IsLegacy&&Canonical(payload.CatalogId)&&payload.CatalogId==setup.CatalogId&&
            payload.DefinitionId==setup.DefinitionId&&payload.ContentVersion==setup.ContentVersion&&payload.Seed==setup.Seed&&
            payload.SizeId==setup.SizeId&&payload.DifficultyId==setup.DifficultyId&&payload.MapId==setup.OperationMapId&&
            payload.LayoutId==setup.LayoutId&&payload.AllConfigHashes==setup.SetupHash.ToString("X8",System.Globalization.CultureInfo.InvariantCulture);
    }
}
