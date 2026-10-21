using System;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    // Index is a transport token from the existing published library, never a
    // mission identifier inferred from its reused physical map. No publication.
    public static class SkirmishMissionDispatch
    {
        public static bool IsLegacyIndex(int index)=>index>=0&&index<=7;
        public static bool IsExpandedCounterpart(string id)=>id=="S001"||id=="S025"||id=="S073";
        public static bool IsCodedExpanded(string id)=>id=="S002"||id=="S003"||id=="S004"||id=="S005";
        public static int SelectedContentVersion(in SkirmishBattleCatalogEntry entry)
        {
            if(entry.ContentVersion>0)return entry.ContentVersion;
            // Existing coded cards predate the row-version field. Read their
            // actual authored definition; never fabricate a version in the row.
            var catalog=SkirmishExpansionCatalogConfig.Load();
            return entry.ContentVersion==0&&IsCodedExpanded(entry.ScenarioId)&&catalog!=null&&
                catalog.TryGetDefinition(entry.ScenarioId,out var definition)&&definition!=null&&
                definition.DefinitionId==entry.DefinitionId?definition.ContentVersion:0;
        }
        // Expanded setups for these three IDs are counterparts, never legacy
        // practice. The four existing expanded coded missions keep their path.
        public static bool RequiresSourceBoundDefinition(string id)=>Canonical(id)&&
            id!="S002"&&id!="S003"&&id!="S004"&&id!="S005";
        public static bool RequiresSourceBoundDefinition(string id, int version) =>
            RequiresSourceBoundDefinition(id) || id == "S002" && version == 3;
        public static bool TryGetSelectedExpanded(SkirmishBattleCatalogConfig catalog,string id,string definitionId,int version,int index,out SkirmishBattleCatalogEntry entry)
        {
            entry=default;if(!Canonical(id)||version<=0||catalog?.Entries==null)return false;
            int matches=0,indexMatches=0;
            foreach(var row in catalog.Entries)
            {if(row.ScenarioId==id){entry=row;matches++;}if(row.IsPlayable&&row.PlayableScenarioIndex==index)indexMatches++;}
            return matches==1&&indexMatches==1&&entry.IsPlayable&&entry.PlayableScenarioIndex==index&&
                entry.DefinitionId==definitionId&&SelectedContentVersion(entry)==version&&
                definitionId==SkirmishDefinitionId.FromCatalog(new SkirmishCatalogId(id)).Value&&
                (TryGetLegacyIndex(id,out int original)?index==original:index>7);
        }
        public static bool TryGetGenericByIndex(SkirmishBattleCatalogConfig catalog,int index,out SkirmishBattleCatalogEntry entry)
        {
            entry=default;if(index<0||catalog?.Entries==null)return false;int matches=0;
            foreach(var candidate in catalog.Entries)
                if(candidate.IsPlayable&&candidate.PlayableScenarioIndex==index){entry=candidate;matches++;}
            if(matches!=1||!Canonical(entry.ScenarioId)||!RequiresSourceBoundDefinition(entry.ScenarioId, entry.ContentVersion)||
                (index<=7&&(!(IsExpandedCounterpart(entry.ScenarioId) || entry.ScenarioId == "S002" && entry.ContentVersion == 3)||!TryGetLegacyIndex(entry.ScenarioId,out int original)||original!=index))||entry.ContentVersion<=0||
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
