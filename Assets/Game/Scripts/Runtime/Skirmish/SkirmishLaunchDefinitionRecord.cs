using Game.Components;
using Game.Configs;
using Unity.Entities;

namespace Game.Runtime
{
    // Actual selected authored object, retained across native source resolution.
    // These are identity pins; this record never certifies physical source fit.
    public sealed class SkirmishLaunchDefinitionRecord:IComponentData
    {
        public readonly SkirmishScenarioDefinitionConfig Definition;
        public readonly uint SetupHash;
        public readonly string LayoutBindingFingerprint;
        public readonly bool RequiresExplicitSource;
        private readonly string catalogId,definitionId,operationMapId,layoutId,layoutSemantic,sourceId,sourceHash,sourceIdentity,sourceGeneratedMetadata;
        private readonly int version,sourceSchema,sourceVersion;
        private readonly OperationMapDefinition source;
        // Required by Entities managed-component construction. Empty defaults
        // remain invalid: MatchesIdentity rejects Definition == null.
        public SkirmishLaunchDefinitionRecord() {}
        public SkirmishLaunchDefinitionRecord(SkirmishScenarioDefinitionConfig definition,SkirmishResolvedSetup setup,bool requiresExplicitSource)
        {
            Definition=definition;SetupHash=setup.SetupHash;LayoutBindingFingerprint=setup.LayoutBindingFingerprint;RequiresExplicitSource=requiresExplicitSource;
            catalogId=definition.CatalogId;definitionId=definition.DefinitionId;version=definition.ContentVersion;
            operationMapId=definition.OperationMapId;layoutId=definition.MapLayoutId;
            layoutSemantic=definition.MapLayout==null?null:SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout);
            source=definition.CampaignMap;sourceId=source?.OperationMapId;sourceHash=source?.ContentHash;sourceIdentity=source?.SourceIdentityHash;
            sourceGeneratedMetadata=source?.GeneratedMetadataHash;sourceSchema=source?.SchemaVersion??0;sourceVersion=source?.ContentVersion??0;
        }
        public bool MatchesIdentity(SkirmishResolvedSetup setup,in SkirmishExpandedSessionComponent session)=>
            Definition!=null&&setup!=null&&session.IsLegacy==0&&session.CatalogId.ToString()==catalogId&&session.DefinitionId.ToString()==definitionId&&
            session.ContentVersion==version&&session.SetupHash==SetupHash&&setup.CatalogId==catalogId&&setup.DefinitionId==definitionId&&
            setup.ContentVersion==version&&setup.SetupHash==SetupHash&&setup.OperationMapId==operationMapId&&setup.LayoutId==layoutId&&
            setup.LayoutBindingFingerprint==LayoutBindingFingerprint&&Definition.CatalogId==catalogId&&Definition.DefinitionId==definitionId&&
            Definition.ContentVersion==version&&Definition.OperationMapId==operationMapId&&Definition.MapLayoutId==layoutId;
        public bool Matches(SkirmishResolvedSetup setup,in SkirmishExpandedSessionComponent session)=>MatchesIdentity(setup,session)&&
            Definition.MapLayout!=null&&SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(Definition.MapLayout)==layoutSemantic&&
            Definition.CampaignMap==source&&(!RequiresExplicitSource||source!=null)&&
            (source==null||(source.OperationMapId==sourceId&&source.ContentHash==sourceHash&&source.SourceIdentityHash==sourceIdentity&&
                source.GeneratedMetadataHash==sourceGeneratedMetadata&&source.SchemaVersion==sourceSchema&&source.ContentVersion==sourceVersion));
    }

    public static class SkirmishGenericMissionSource
    {
        public static bool TryResolve(EntityManager em,out bool generic,out SkirmishLaunchDefinitionRecord record,
            out OperationMapDefinition source,out string error)
        {
            return TryResolveCore(em, true, out generic, out record, out source, out error);
        }
        // Explicit compiled-data consumers validate original source identity without claiming
        // a native playable catalog transport. Native TryResolve retains its catalog gate.
        public static bool TryResolveCompiledData(EntityManager em,out bool generic,out SkirmishLaunchDefinitionRecord record,
            out OperationMapDefinition source,out string error) => TryResolveCore(em,false,out generic,out record,out source,out error);
        private static bool TryResolveCore(EntityManager em,bool requirePlayableCatalog,out bool generic,out SkirmishLaunchDefinitionRecord record,
            out OperationMapDefinition source,out string error)
        {
            generic=false;record=null;source=null;error=null;
            using var matches=em.CreateEntityQuery(typeof(SkirmishMatchState));
            if(matches.CalculateEntityCount()==0)return true;
            if(matches.CalculateEntityCount()!=1){error="Ambiguous native Skirmish match.";return false;}
            var entity=matches.GetSingletonEntity();var match=em.GetComponentData<SkirmishMatchState>(entity);
            generic=match.ScenarioIndex>7;
            if(em.HasComponent<SkirmishExpandedSessionComponent>(entity))
            {
                var selected=em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                generic|=selected.IsLegacy==0&&(SkirmishMissionDispatch.IsExpandedCounterpart(selected.CatalogId.ToString())||!requirePlayableCatalog&&SkirmishMissionDispatch.RequiresSourceBoundDefinition(selected.CatalogId.ToString(),selected.ContentVersion));
            }
            // A partially lost expanded owner must fail closed, rather than
            // turn the same historical index back into a practice source.
            if(em.HasComponent<SkirmishLaunchDefinitionRecord>(entity))
            {
                var selected=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(entity);
                generic|=selected!=null&&(selected.RequiresExplicitSource||
                    selected.Definition!=null&&SkirmishMissionDispatch.IsExpandedCounterpart(selected.Definition.CatalogId));
            }
            if(em.HasComponent<SkirmishResolvedSetupRecord>(entity))
            {
                var selected=em.GetComponentObject<SkirmishResolvedSetupRecord>(entity)?.Setup;
                generic|=selected!=null&&(SkirmishMissionDispatch.IsExpandedCounterpart(selected.CatalogId)||!requirePlayableCatalog&&SkirmishMissionDispatch.RequiresSourceBoundDefinition(selected.CatalogId,selected.ContentVersion));
            }
            if(!generic)return true;
            error="Exact authored generic mission/source binding missing or changed.";
            if(!em.HasComponent<SkirmishExpandedSessionComponent>(entity)||!em.HasComponent<SkirmishResolvedSetupRecord>(entity)||
                !em.HasComponent<SkirmishLaunchDefinitionRecord>(entity))return false;
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup;
            var actual=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(entity);
            if(!TryResolveCompiledIdentity(actual,setup,state,match,out source,out error))return false;
            if(requirePlayableCatalog && (!SkirmishMissionDispatch.TryGetGenericByIndex(SkirmishBattleCatalogConfig.Load(),match.ScenarioIndex,out var entry)||
                !SkirmishMissionDispatch.MatchesDefinition(entry,actual.Definition))){error="Playable native catalog transport required.";source=null;return false;}
            record=actual;source=actual.Definition.CampaignMap;error=null;return true;
        }
        public static bool TryResolveCompiledIdentity(SkirmishLaunchDefinitionRecord actual,SkirmishResolvedSetup setup,
            in SkirmishExpandedSessionComponent state,in SkirmishMatchState match,out OperationMapDefinition source,out string error)
        {
            source=null;error="Exact compiled mission/source identity missing or changed.";
            if(actual==null||!actual.RequiresExplicitSource||!actual.Matches(setup,state)||!setup.MeasuredLayoutBound||
                string.IsNullOrEmpty(setup.LayoutBindingFingerprint)||state.SessionId.IsEmpty||match.ScenarioIndex < -1||match.SessionId!=state.SessionId||
                state.Seed!=setup.Seed||state.SizeId!=setup.SizeId||state.DifficultyId!=setup.DifficultyId||state.ObjectiveKind!=setup.ObjectiveKind||match.Seed!=setup.Seed||
                setup.SetupHash!=SkirmishSetupCompiler.ComputeHash(setup,setup.Seed)||actual.Definition.CampaignMap==null||
                !actual.Definition.CampaignMap.TryValidateMetadata(out error)||!actual.Definition.CampaignMap.TryValidateLocalContentReferences(out error))return false;
            source=actual.Definition.CampaignMap;error=null;return true;
        }
        // Presentation reads use cheap captured identity. Full source/layout
        // validation belongs to queue/load/save boundaries, not every UI getter.
        public static bool MatchesMission(EntityManager em,string missionId)
        {
            using var query=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishResolvedSetupRecord),typeof(SkirmishLaunchDefinitionRecord),typeof(SkirmishMatchState));
            if(query.CalculateEntityCount()!=1)return false;var entity=query.GetSingletonEntity();
            var record=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(entity);
            return record!=null&&record.RequiresExplicitSource&&
                record.MatchesIdentity(em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup,em.GetComponentData<SkirmishExpandedSessionComponent>(entity))&&
                record.Definition.DefinitionId==missionId;
        }
    }
}
