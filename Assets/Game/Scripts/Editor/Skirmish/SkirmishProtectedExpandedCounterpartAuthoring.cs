#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // An isolated library view of real authored content. Main catalog rows,
    // practice Resources, publication and acceptance records are never written.
    public sealed class SkirmishProtectedExpandedCounterpartScope : IDisposable
    {
        SkirmishBattleCatalogConfig candidate;
        IDisposable battleScope, authoredScope;

        public static SkirmishExpansionCatalogConfig LoadDevelopmentCatalog(string id)
        {
            if(!SkirmishMissionDispatch.IsExpandedCounterpart(id))throw new ArgumentException("Exact protected counterpart ID required.");
            var value=Resources.Load<SkirmishExpansionCatalogConfig>("SkirmishProtectedCounterpart_"+id);
            string expected="Assets/Game/Configs/SkirmishExpansion/Scenarios/"+id+"/Resources/SkirmishProtectedCounterpart_"+id+".asset";
            if(value==null||AssetDatabase.GetAssetPath(value)!=expected||EditorUtility.IsDirty(value)||
                !value.TryGetDefinition(id,out var definition)||definition==null||definition.ContentVersion<=0)
                throw new InvalidOperationException("Exact clean persisted counterpart development resource missing: "+expected);
            return value;
        }

        public static SkirmishProtectedExpandedCounterpartScope Open(SkirmishExpansionCatalogConfig exactAuthored,string id)
        {
            if(!SkirmishMissionDispatch.IsExpandedCounterpart(id)||exactAuthored==null||
                !exactAuthored.TryGetDefinition(id,out var definition)||definition==null||definition.ContentVersion<=0||
                definition.DefinitionId!="skirmish."+id.ToLowerInvariant()||definition.CampaignMap==null||
                definition.MapLayout==null||definition.MissionLogicalBinding==null||definition.ObjectiveConfig==null||
                definition.ObjectiveConfig.Kind!=SkirmishObjectiveKind.BaseAssault)
                throw new ArgumentException("Exact real source-bound counterpart definition/layout/binding required.");
            if(!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error))throw new InvalidOperationException(error);
            foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                if(!SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,size,1,
                    new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out _,out var reasons))
                    throw new InvalidOperationException("Counterpart normal compiler refused "+size+": "+string.Join(";",reasons));
            var shipped=Resources.Load<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogConfig.ResourceName);
            if(shipped==null||!ReferenceEquals(SkirmishBattleCatalogConfig.Load(),shipped)||!shipped.TryValidate(out error))
                throw new InvalidOperationException("Unscoped actual main library required: "+error);
            var rows=shipped.Entries.ToArray();int found=0;
            SkirmishMissionDispatch.TryGetLegacyIndex(id,out int index);
            for(int i=0;i<rows.Length;i++)
            {
                if(rows[i].ScenarioId!=id)continue;
                if(!rows[i].IsPlayable||rows[i].PlayableScenarioIndex!=index||rows[i].ContentVersion!=0||!string.IsNullOrEmpty(rows[i].DefinitionId))
                    throw new InvalidOperationException("Exact unchanged practice row required before isolated counterpart exposure.");
                found++;rows[i].DefinitionId=definition.DefinitionId;rows[i].ContentVersion=definition.ContentVersion;
            }
            if(found!=1)throw new InvalidOperationException("Exactly one existing canonical row required.");
            var value=new SkirmishProtectedExpandedCounterpartScope();
            try
            {
                value.candidate=ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();
                value.candidate.Configure(rows);
                if(!value.candidate.TryValidate(out error))throw new InvalidOperationException(error);
                value.authoredScope=SkirmishExpansionCatalogConfig.UseEditorValidationCatalog(exactAuthored);
                value.battleScope=SkirmishBattleCatalogConfig.UseEditorValidationCatalog(value.candidate);
                Debug.Log("[SkirmishCounterpartCandidateAvailability] catalog="+id+" index="+index+
                    " kind=ExpandedDefinition definition="+definition.DefinitionId+" version="+definition.ContentVersion+
                    " scope=EditorMemoryOnly mainBindingsChanged=0 legacyWinTransfer=0 readinessGranted=0");
                return value;
            }
            catch{value.Dispose();throw;}
        }
        public void Dispose()
        {
            battleScope?.Dispose();battleScope=null;authoredScope?.Dispose();authoredScope=null;
            if(candidate!=null)UnityEngine.Object.DestroyImmediate(candidate);candidate=null;
        }
    }

    public static partial class SkirmishGroundSourceFitSurvey
    {
        public static JObject ProtectedCounterpartSourcePins(SkirmishScenarioDefinitionConfig definition)
        {
            Require(definition!=null&&SkirmishMissionDispatch.IsExpandedCounterpart(definition.CatalogId)&&definition.MapLayout!=null,
                "Exact counterpart definition/layout required for source report join");
            var pin=(JObject)GroundBaTemplate(GroundBa10Rows().Single(r=>(string)r["catalog_id"]==definition.CatalogId),true)["source_candidate"];
            string path=(string)pin["sourcePath"],gridPath=(string)pin["gridPath"],surfacePath=(string)pin["surfacePath"];
            var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
            Require(source!=null&&ReferenceEquals(definition.CampaignMap,source)&&!EditorUtility.IsDirty(source)&&
                Hash(path)==(string)pin["sourceAssetSha256"]&&Hash(gridPath)==(string)pin["gridAssetSha256"]&&Hash(surfacePath)==(string)pin["surfaceAssetSha256"]&&
                source.SourceIdentityHash==(string)pin["sourceIdentityHash"]&&source.ContentHash==(string)pin["contentHash"]&&
                source.OperationMapId==(string)pin["operationMapId"]&&source.SourceSceneReference.AssetGUID==(string)pin["sourceSceneGuid"]&&
                source.NavigationMetadata.AuthoredSubSceneGuid==(string)pin["authoredSubSceneGuid"]&&
                AssetDatabase.GUIDToAssetPath(source.GridMetadata.AssetGuid)==gridPath&&source.MapSurfaceDataReference!=null&&
                AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID)==surfacePath,
                "Actual counterpart Campaign/grid/surface reference, clean state, identity or bytes differ");
            return (JObject)pin.DeepClone();
        }
        public static void ValidateProtectedCounterpartSourceLayoutReport(SkirmishScenarioDefinitionConfig definition,JToken reportSourcePins,JObject measurement)
        {
            var expected=ProtectedCounterpartSourcePins(definition);
            Require(measurement!=null&&JToken.DeepEquals(expected,reportSourcePins)&&JToken.DeepEquals(expected,measurement["sourcePins"]),
                "Report and measured source pins differ from actual canonical counterpart Campaign/grid/surface");
            Require((string)measurement["proposedLayoutSemanticSha256"]==SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout),
                "Measured layout semantic fingerprint differs from actual counterpart layout");
        }

        static bool protectedCounterpartRefinementRequested;
        // Run in a fresh checked Editor after materialization. Reads persisted
        // resource references and the same normal Start command, never a saved
        // rejected setup or an Editor-only direct queue shortcut.
        public static void VerifyPersistedProtectedExpandedCounterpart()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle fresh resource readback required");
            string id=Environment.GetEnvironmentVariable("WARLINE_PROTECTED_COUNTERPART_CATALOG");
            var development=SkirmishProtectedExpandedCounterpartScope.LoadDevelopmentCatalog(id);
            Require(development.TryGetDefinition(id,out var definition)&&AssetDatabase.Contains(definition)&&
                AssetDatabase.Contains(definition.MapLayout)&&AssetDatabase.Contains(definition.MissionLogicalBinding)&&
                definition.MissionLogicalBinding.Variants.All(v=>v!=null&&AssetDatabase.Contains(v.View)),"Actual persisted complete logical graph required");
            int[] seeds=id=="S001"?new[]{104730,130364,155922}:id=="S025"?new[]{104754,130388,155946}:new[]{104802,130436,155994};
            string temp=Path.Combine(Path.GetTempPath(),"warline-counterpart-readback-"+Guid.NewGuid().ToString("N"));
            var readbacks=new JArray();var oldDefault=World.DefaultGameObjectInjectionWorld;
            string oldSave=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            string pref="Warline.Skirmish.LastScenarioId";bool hadPref=PlayerPrefs.HasKey(pref);string oldPref=PlayerPrefs.GetString(pref);
            try
            {
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",temp);
                using(SkirmishProtectedExpandedCounterpartScope.Open(development,id))
                {
                    AssertAll120DefinitionBindings(definition,true);
                    SkirmishMissionDispatch.TryGetLegacyIndex(id,out int index);
                    foreach(int seed in seeds)
                    {
                        var store=new QuickCustomGameConfigStore(new SaveService(new JsonSaveRepository(temp)));
                        var config=store.Current;config.ScenarioIndex=index;store.Apply(config);
                        using var world=new World("Persisted counterpart ordinary Start readback");var em=world.EntityManager;
                        var shell=em.CreateEntity(typeof(UiShellRootComponent),typeof(UiShellStateComponent));em.AddBuffer<UiShellRouteRequestComponent>(shell);
                        World.DefaultGameObjectInjectionWorld=world;
                        var fixture=new GameObject("Persisted counterpart public setup fixture",typeof(RectTransform));
                        try
                        {
                            var view=fixture.AddComponent<QuickCustomScreenView>();var fields=new SerializedObject(view);
                            fields.FindProperty("baseAssaultPreset").boolValue=true;fields.ApplyModifiedPropertiesWithoutUndo();
                            view.BindRuntimeDependencies(store,new MatchLaunchCommand(store));view.SelectScenarioId(id);
                            view.PrepareScenarioAdjustment(index,seed);
                            var selected=store.Current.SkirmishSelection;
                            Require(selected.Kind==UiSkirmishLibraryLaunchKind.ExpandedDefinition&&selected.CatalogId==id&&
                                selected.DefinitionId==definition.DefinitionId&&selected.ContentVersion==definition.ContentVersion,
                                "Public existing setup selection lost exact expanded descriptor");
                            view.LaunchMatch();
                        }
                        finally{UnityEngine.Object.DestroyImmediate(fixture);World.DefaultGameObjectInjectionWorld=oldDefault;}
                        Require(SkirmishLaunchProjection.TryGet(em,out var session,out var match)&&
                            match.ScenarioIndex==index&&match.Seed==seed&&em.HasComponent<SkirmishExpandedSessionComponent>(session)&&
                            em.HasComponent<SkirmishResolvedSetupRecord>(session)&&em.HasComponent<SkirmishLaunchDefinitionRecord>(session),"Normal Start omitted exact actual expanded ownership");
                        var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                        var record=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
                        Require(ReferenceEquals(record.Definition,definition)&&record.RequiresExplicitSource&&
                            record.Matches(setup,em.GetComponentData<SkirmishExpandedSessionComponent>(session))&&
                            setup.SizeId==SkirmishSizeId.Standard&&setup.DifficultyId==SkirmishDifficultyId.Regular&&setup.Seed==seed,
                            "Normal Start consumed a different resource/version/source/size/difficulty");
                        readbacks.Add(new JObject{["seed"]=seed,["scenario_index"]=index,["catalog_id"]=setup.CatalogId,
                            ["definition_id"]=setup.DefinitionId,["definition_version"]=setup.ContentVersion,
                            ["size"]=setup.SizeId.ToString(),["difficulty"]=setup.DifficultyId.ToString(),
                            ["native_session_id"]=match.SessionId.ToString(),["native_world_sequence"]=world.SequenceNumber.ToString(),
                            ["native_setup_hash_uint32"]=setup.SetupHash,["native_content_identity"]=setup.ContentHash});
                    }
                }
            }
            finally
            {
                World.DefaultGameObjectInjectionWorld=oldDefault;Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",oldSave);
                if(hadPref)PlayerPrefs.SetString(pref,oldPref);else PlayerPrefs.DeleteKey(pref);PlayerPrefs.Save();
                if(Directory.Exists(temp))Directory.Delete(temp,true);
            }
            SkirmishProtectedCounterpartAuthority.Write(definition,development,readbacks);
            Debug.Log("[ProtectedExpandedCounterpartPersistedReadback] result=Passed catalog="+id+
                " standardNormalStarts=3 compiledSizeVariants=3 nativeSimulationRuns=0 baselineCredit=0");
        }

        // Uses the exact protected authority row as an expanded logical request;
        // it does not mutate or remove its protected practice binding.
        public static void RunProtectedExpandedCounterpartLogicalRefinement()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&ground30MemoryWriter==null&&
                !protectedCounterpartRefinementRequested&&!refinementCompoundSearchRequested&&!refinementLogicalFrameSearchRequested,
                "Idle exclusive checked authoring Editor required");
            string id=Environment.GetEnvironmentVariable("WARLINE_PROTECTED_COUNTERPART_CATALOG");
            string output=Environment.GetEnvironmentVariable("WARLINE_PROTECTED_COUNTERPART_OUTPUT");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(SkirmishMissionDispatch.IsExpandedCounterpart(id)&&!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output)&&
                Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),
                "One exact counterpart ID and fresh durable output required");
            var authority=GroundBa10Rows().Single(r=>(string)r["catalog_id"]==id);
            var report=new JObject{["catalogId"]=id,["scope"]="ExpandedCounterpartLogicalAuthoring",
                ["fullIndexSha256"]=All120RefinementIndexSha,["practiceBindingPreserved"]=authority["protected_binding"].DeepClone(),
                ["legacyWinTransfer"]=false,["mainCatalogChanged"]=false,["sourceFitCertified"]=false,["normalMissionReady"]=false,["result"]="Incomplete"};
            try
            {
                protectedCounterpartRefinementRequested=true;
                refinementCompoundSearchRequested=true;refinementLogicalFrameSearchRequested=true;
                CollectAll120Refinement(new[]{authority},report,output+".authoring",true);
            }
            catch(Exception failure){report["error"]=failure.ToString();throw;}
            finally
            {
                protectedCounterpartRefinementRequested=false;
                refinementCompoundSearchRequested=false;refinementLogicalFrameSearchRequested=false;
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                {writer.Write(report.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
            }
            Debug.Log("[ProtectedExpandedCounterpartLogicalRefinement] result=Collected catalog="+id+
                " sourceFitCertified=0 mainCatalogChanged=0 legacyWinTransfer=0 output="+output);
        }

        public static void RunProtectedExpandedCounterpartAuthoringControls()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle source-backed authoring controls required");
            var worlds=CaptureMaterializationWorldCensus();var oldDefault=Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var scenes=Ground30SceneOwnership();int definitions=0,vectors=0,refusals=0,residualRefusals=0;
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            Require(!protectedCounterpartRefinementRequested&&ProtectedRefinementCatalog("S001")&&ProtectedRefinementCatalog("S005"),"Ordinary protected logical-frame exclusion changed");
            try
            {
                protectedCounterpartRefinementRequested=true;
                Require(!ProtectedRefinementCatalog("S001")&&!ProtectedRefinementCatalog("S025")&&!ProtectedRefinementCatalog("S073")&&
                    ProtectedRefinementCatalog("S002")&&ProtectedRefinementCatalog("S003")&&ProtectedRefinementCatalog("S004")&&ProtectedRefinementCatalog("S005"),
                    "Explicit counterpart search widened coded-four authority");
            }
            finally{protectedCounterpartRefinementRequested=false;}
            foreach(var id in new[]{"S001","S025","S073"})
            {
                var row=GroundBa10Rows().Single(r=>(string)r["catalog_id"]==id);
                var template=GroundBaTemplate(row,true);var pin=template["source_candidate"];
                var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)pin["sourcePath"]);
                string before=Hash((string)pin["sourcePath"]);
                Require(!TryCreateGroundBa10Candidate(row,source,out var forbidden,out _),"Ordinary generic factory must preserve protected refusal");forbidden?.Dispose();refusals++;
                Require(TryCreateProtectedExpandedBaseAssaultCandidate(row,source,out var owner,out error),error);
                using(owner)
                {
                    Require(owner.Definition.CatalogId==id&&owner.Definition.DefinitionId==(string)row["definition_id"]&&
                        owner.Definition.CampaignMap==source&&owner.AreaDraft==null&&owner.Definition.MissionLogicalBinding==null&&
                        !owner.Layout.WorldBinding.HasMeasuredDeployments,"Exact unmeasured expanded counterpart required");
                    var reportPin=ProtectedCounterpartSourcePins(owner.Definition);
                    var reportMeasurement=new JObject{["sourcePins"]=reportPin.DeepClone(),
                        ["proposedLayoutSemanticSha256"]=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(owner.Layout)};
                    ValidateProtectedCounterpartSourceLayoutReport(owner.Definition,reportPin,reportMeasurement);
                    for(int corruption=0;corruption<3;corruption++)
                    {
                        var wrong=(JObject)reportMeasurement.DeepClone();var wrongReport=(JObject)reportPin.DeepClone();
                        if(corruption==0)wrong["proposedLayoutSemanticSha256"]=new string('0',64);
                        if(corruption==1)wrong["sourcePins"]["sourceAssetSha256"]=new string('0',64);
                        if(corruption==2)wrongReport["gridAssetSha256"]=new string('0',64);
                        bool refused=false;try{ValidateProtectedCounterpartSourceLayoutReport(owner.Definition,wrongReport,wrong);}
                        catch(InvalidOperationException){refused=true;}
                        Require(refused,"Wrong report layout/source accepted against actual source-backed counterpart");
                    }
                    using(var world=new World("Counterpart residual native owner refusal"))
                    {
                        var em=world.EntityManager;var entity=em.CreateEntity(typeof(SkirmishMatchState));
                        SkirmishMissionDispatch.TryGetLegacyIndex(id,out int index);
                        em.SetComponentData(entity,new SkirmishMatchState{ScenarioIndex=index,Seed=1});
                        var setup=new SkirmishResolvedSetup{CatalogId=id,DefinitionId=owner.Definition.DefinitionId,
                            ContentVersion=owner.Definition.ContentVersion,OperationMapId=owner.Definition.OperationMapId,LayoutId=owner.Definition.MapLayoutId};
                        var record=new SkirmishLaunchDefinitionRecord(owner.Definition,setup,true);
                        em.AddComponentObject(entity,record);em.AddComponentObject(entity,new SkirmishResolvedSetupRecord{Setup=setup});
                        for(int state=0;state<3;state++)
                        {
                            if(state==1)em.RemoveComponent<SkirmishLaunchDefinitionRecord>(entity);
                            if(state==2){em.RemoveComponent<SkirmishResolvedSetupRecord>(entity);em.AddComponentObject(entity,record);}
                            Require(!SkirmishGenericMissionSource.TryResolve(em,out bool generic,out _,out var resolved,out string reason)&&
                                generic&&resolved==null&&!string.IsNullOrEmpty(reason),
                                "Residual actual expanded definition/setup owner downgraded to legacy practice source");residualRefusals++;
                        }
                    }
                    foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                    {
                        Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                            new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&
                            budget.ExactCanonicalQuantities&&!budget.NormalCompilerPassed,"Exact canonical counterpart budget with normal launch still refused: "+string.Join(";",reasons));vectors++;
                    }
                    var bad=(JObject)row.DeepClone();bad["canonical_size_rows"][0]["player_rifle"]="999";
                    Require(!TryCreateProtectedExpandedBaseAssaultCandidate(bad,source,out var rejected,out _),"Altered canonical counterpart quantity accepted");rejected?.Dispose();refusals++;
                }
                Require(before==Hash((string)pin["sourcePath"])&&!EditorUtility.IsDirty(source),"Persistent Campaign source changed");definitions++;
            }
            Require(worlds.SequenceEqual(CaptureMaterializationWorldCensus())&&ReferenceEquals(oldDefault,Unity.Entities.World.DefaultGameObjectInjectionWorld)&&
                JToken.DeepEquals(scenes,Ground30SceneOwnership()),"Source-backed controls changed World/default/scene ownership");
            Debug.Log("[ProtectedExpandedCounterpartAuthoringControls] result=Passed definitions="+definitions+" canonicalVectors="+vectors+
                " refusals="+refusals+" residualOwnershipRefusals="+residualRefusals+" streamingWorlds=0 sourceFitCertified=0 legacyWinTransfer=0");
        }
    }
}
#endif
