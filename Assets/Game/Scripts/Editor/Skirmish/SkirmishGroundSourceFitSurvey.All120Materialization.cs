#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // Separate checked authoring entry: collector World/scene cleanup must
        // already be durably verified before any persistent reference changes.
        public static void MaterializeAll120ReviewedLogicalCandidate()=>MaterializeReviewedLogicalCandidate(false);
        public static void MaterializeReviewedProtectedExpandedCounterpart()=>MaterializeReviewedLogicalCandidate(true);

        static void MaterializeReviewedLogicalCandidate(bool counterpart)
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle root-owned logical authoring required");
            string reportPath=Environment.GetEnvironmentVariable("WARLINE_ALL120_MATERIALIZATION_REPORT");
            string reviewedSha=Environment.GetEnvironmentVariable("WARLINE_ALL120_MATERIALIZATION_REPORT_SHA256");
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_MATERIALIZATION_CATALOG");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(reportPath)&&Path.IsPathRooted(reportPath)&&Path.GetFullPath(reportPath).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&
                SkirmishMissionLogicalBindingCompiler.Hex(reviewedSha)&&Hash(reportPath)==reviewedSha,"Exact independently reviewed durable collector report required");
            var report=JObject.Parse(File.ReadAllText(reportPath));
            Require((bool?)report["cleanupVerified"]==true&&(string)report["result"]=="CollectedUnqualified","Completed exact source World/default/scene/root cleanup required before materialization");
            var row=((JArray)report["missions"]).OfType<JObject>().Single(v=>(string)v["catalogId"]==id);
            var artifact=((JArray)report["missionArtifacts"]).OfType<JObject>().Single(v=>(string)v["catalogId"]==id);
            Require(Hash((string)artifact["path"])==(string)artifact["sha256"]&&JToken.DeepEquals(row,JObject.Parse(File.ReadAllText((string)artifact["path"]))),
                "Exact durable native mission row must match the reviewed cleaned-up report");
            Require((string)row["result"]=="CollectedUnqualified"&&row["error"]==null,"Failed mission rows cannot materialize");
            var authority=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().Single(v=>(string)v["catalog_id"]==id);
            Require(counterpart?SkirmishMissionDispatch.IsExpandedCounterpart(id):authority["protected_binding"].Type==JTokenType.Null,"Protected binding is read-only except explicitly selected expanded counterpart authoring");
            ResolveAll120RefinementCandidate(authority,out var candidate,out var template,counterpart);
            var measurement=(JObject)row["measurement"];
            Require(JToken.DeepEquals(measurement["sourcePins"],template["source_candidate"]),"Measured physical source pins must match exact authority");
            var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)template["source_candidate"]["sourcePath"]);
            GroundObjective30CandidateOwner owner=null;AirCombined80CandidateOwner airOwner=null;
            var receipt=new JObject{["catalogId"]=id,["reviewedReportSha256"]=reviewedSha,["publicationManifestChanged"]=false,["developmentOnly"]=true,["normalMissionReady"]=false};
            string output=reportPath+"."+id+".materialization.json";Require(!File.Exists(output),"Fresh materialization receipt required");
            try
            {
                string error;
                if((string)authority["tuple"]["army_profile"]!="G")
                {Require(TryCreateAirCombined80Candidate(candidate,source,out airOwner,out error),error);owner=new GroundObjective30CandidateOwner{Definition=airOwner.Definition,Layout=airOwner.Layout,AreaDraft=airOwner.AreaDraft};}
                else if((string)authority["tuple"]["objective_id"]=="BA")Require(counterpart?TryCreateProtectedExpandedBaseAssaultCandidate(candidate,source,out owner,out error):TryCreateGroundBa10Candidate(candidate,source,out owner,out error),error);
                else Require(TryCreateGroundObjective30Candidate(candidate,source,out owner,out error),error);
                JsonUtility.FromJsonOverwrite((string)measurement["proposedLayoutJson"],owner.Layout);
                Require(SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(owner.Layout)==(string)measurement["proposedLayoutSemanticSha256"],"Exact measured layout graph required");
                var graph=(JObject)measurement["authoredDefinitionLogicalBindingGraph"];
                var binding=owner.Own(ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>());
                var scalars=(JObject)graph.DeepClone();scalars.Remove("variants");JsonUtility.FromJsonOverwrite(scalars.ToString(Newtonsoft.Json.Formatting.None),binding);
                binding.Variants=((JArray)graph["variants"]).OfType<JObject>().Select(encoded=>
                {
                    var values=(JObject)encoded.DeepClone();values.Remove("View");values.Remove("Areas");
                    var variant=JsonUtility.FromJson<SkirmishMissionLogicalVariant>(values.ToString(Newtonsoft.Json.Formatting.None));
                    variant.Areas=ReadAll120LogicalAreas(encoded["Areas"]);
                    variant.View=owner.Own(ScriptableObject.CreateInstance<SkirmishMissionViewProfile>());
                    JsonUtility.FromJsonOverwrite(encoded["View"].ToString(Newtonsoft.Json.Formatting.None),variant.View);
                    if(variant.Areas!=null)Require(Hash(variant.Areas.Measurement.ArtifactId)==variant.Areas.Measurement.ArtifactSha256,"Exact measured typed-area artifact required");
                    return variant;
                }).ToArray();
                owner.Definition.BindMissionLogicalBinding(binding);
                MaterializeMeasuredAll120Candidate(owner,measurement,receipt,counterpart);
                receipt["result"]="MaterializedDevelopmentOnly";
            }
            catch(Exception failure){receipt["error"]=failure.ToString();throw;}
            finally
            {
                owner?.Dispose();airOwner?.Dispose();
                using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                {writer.Write(receipt.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
            }
            Debug.Log("[All120LogicalMaterialization] result=MaterializedDevelopmentOnly catalog="+id+" ordinaryQueuedSizeVariants=3 publicationManifestChanged=0 developmentOnly=1 normalMissionReady=0 output="+output);
        }

        // Explicit root-owned authoring option. This creates development catalog
        // references only after the same live native refinement completed all3
        // strict placement/packing passes and ordinary compiler checks. Publication,
        // camera, route independence and normal mission acceptance remain separate.
        static void MaterializeMeasuredAll120Candidate(GroundObjective30CandidateOwner owner,JObject measurement,JObject receipt,bool counterpart=false)
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&owner?.Definition!=null&&
                measurement["strictThreeSizePlacementPass"]!=null&&(bool?)measurement["normalThreeSizeCompilerPassed"]==true,
                "Actual same-owner strict three-size native placement and ordinary compilation required");
            var strict=((JArray)measurement["passes"]).OfType<JObject>().Single(p=>(int)p["pass"]==(int)measurement["strictThreeSizePlacementPass"]);
            var sizes=((JArray)strict["sizes"]).OfType<JObject>().ToArray();
            Require(sizes.Length==3&&sizes.Select(s=>(string)s["size"]).Distinct().Count()==3&&sizes.All(s=>
                (bool?)s["completeFacilities"]==true&&(bool?)s["allActorsInsidePads"]==true&&
                s["routeQueriesWithSameFacilityReservations"]?["queries"] is JArray queries&&queries.Count>0&&
                queries.OfType<JObject>().All(q=>(string)q["status"]=="NativeQueryFoundExactGoal"&&(int?)q["failureCode"]==0)),
                "Three complete native configurations and exact native paths with the same facility reservations required; route independence remains separate");
            var id=owner.Definition.CatalogId;
            var row=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().Single(v=>(string)v["catalog_id"]==id);
            Require(counterpart?SkirmishMissionDispatch.IsExpandedCounterpart(id):row["protected_binding"].Type==JTokenType.Null&&!SkirmishMissionDispatch.TryGetLegacyIndex(id,out _),"Coded bindings cannot be replaced; only explicit counterpart logical assets allowed");
            var definitionPath=(string)row["intended_new_definition_asset"];
            string prefix="Assets/Game/Configs/SkirmishExpansion/Scenarios/"+id+"/";
            Require(!string.IsNullOrEmpty(definitionPath)&&definitionPath.StartsWith(prefix,StringComparison.Ordinal)&&
                definitionPath.EndsWith(".asset",StringComparison.Ordinal)&&!definitionPath.Contains(".."),"Exact Full120 definition destination required");
            var expansion=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
            var battle=Resources.Load<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogConfig.ResourceName);
            Require(expansion!=null&&battle!=null&&ReferenceEquals(SkirmishExpansionCatalogConfig.Load(),expansion)&&
                ReferenceEquals(SkirmishBattleCatalogConfig.Load(),battle)&&AssetDatabase.Contains(expansion)&&AssetDatabase.Contains(battle)&&
                !EditorUtility.IsDirty(expansion)&&!EditorUtility.IsDirty(battle)&&!expansion.Definitions.Any(d=>d!=null&&d.CatalogId==id),
                "Clean persistent normal catalogs without an existing candidate required; never replace a definition");
            var entries=battle.Entries.ToArray();int entryIndex=System.Array.FindIndex(entries,e=>e.ScenarioId==id);
            Require(entryIndex>=0&&entries.Count(e=>e.ScenarioId==id)==1,"One existing canonical battle row required");
            if(!counterpart)Require(entries[entryIndex].Status==SkirmishBattleCatalogStatus.Planned&&entries[entryIndex].PlayableScenarioIndex<0,"Only an unpublished draft row may receive development assets");
            int runtimeIndex=counterpart?entries[entryIndex].PlayableScenarioIndex:entries[entryIndex].PlayableScenarioIndex>7?entries[entryIndex].PlayableScenarioIndex:
                1000+int.Parse(id.Substring(1),System.Globalization.CultureInfo.InvariantCulture);
            if(counterpart)Require(SkirmishMissionDispatch.TryGetLegacyIndex(id,out int protectedIndex)&&runtimeIndex==protectedIndex&&
                entries[entryIndex].ContentVersion==0&&string.IsNullOrEmpty(entries[entryIndex].DefinitionId),"Exact unchanged practice row required");
            string mainBattlePath=AssetDatabase.GetAssetPath(battle),mainExpansionPath=AssetDatabase.GetAssetPath(expansion);
            string mainBattleSha=Hash(mainBattlePath),mainExpansionSha=Hash(mainExpansionPath);
            Require(!entries.Where((e,i)=>i!=entryIndex).Any(e=>e.IsPlayable&&e.PlayableScenarioIndex==runtimeIndex),"Unique generic runtime transport index required");
            PersistAll120LogicalAssets(owner.Definition,owner.Layout,expansion,battle,definitionPath,prefix,runtimeIndex,counterpart,receipt,
                (definition,development)=>
                {
                if(counterpart)
                {
                    Require(Hash(mainBattlePath)==mainBattleSha&&Hash(mainExpansionPath)==mainExpansionSha&&
                        !EditorUtility.IsDirty(battle)&&!EditorUtility.IsDirty(expansion),"Practice/main catalog bytes changed during counterpart authoring");
                    var loaded=SkirmishProtectedExpandedCounterpartScope.LoadDevelopmentCatalog(id);
                    Require(ReferenceEquals(loaded,development)&&loaded.TryGetDefinition(id,out var exact)&&
                        ReferenceEquals(exact,definition)&&AssetDatabase.GetAssetPath(exact)==definitionPath,"Exact persisted development resources required");
                    using(SkirmishProtectedExpandedCounterpartScope.Open(loaded,id))AssertAll120DefinitionBindings(definition,true);
                    receipt["counterpartDevelopmentResource"]=new JObject{["path"]=AssetDatabase.GetAssetPath(development),
                        ["sha256"]=Hash(AssetDatabase.GetAssetPath(development)),["mainCatalogChanged"]=false,["practiceBindingChanged"]=false};
                }
                else
                {
                    Require(ReferenceEquals(Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName),expansion)&&
                        expansion.TryGetDefinition(id,out var normalDefinition)&&ReferenceEquals(normalDefinition,definition)&&
                        AssetDatabase.GetAssetPath(normalDefinition)==definitionPath,"Normal resource catalog must resolve the exact persisted definition");
                    AssertAll120DraftBindings(expansion,battle,definition,runtimeIndex);
                }
                });
        }

        // Shared persistence/rollback plumbing only; never source-fit admission.
        private static void PersistAll120LogicalAssets(SkirmishScenarioDefinitionConfig sourceDefinition,SkirmishMapLayoutConfig sourceLayout,
            SkirmishExpansionCatalogConfig expansion,SkirmishBattleCatalogConfig battle,string definitionPath,string prefix,
            int runtimeIndex,bool counterpart,JObject receipt,Action<SkirmishScenarioDefinitionConfig,SkirmishExpansionCatalogConfig> readback)
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&sourceDefinition!=null&&sourceLayout!=null&&readback!=null,
                "Idle exact logical transaction inputs required");
            string id=sourceDefinition.CatalogId;
            Require(expansion!=null&&battle!=null&&AssetDatabase.Contains(expansion)&&AssetDatabase.Contains(battle)&&
                !EditorUtility.IsDirty(expansion)&&!EditorUtility.IsDirty(battle)&&
                !expansion.Definitions.Any(d=>d!=null&&d.CatalogId==id),"Clean persistent transaction catalogs without candidate required");
            Require(!string.IsNullOrEmpty(prefix)&&prefix.StartsWith("Assets/",StringComparison.Ordinal)&&prefix.EndsWith("/",StringComparison.Ordinal)&&
                !prefix.Contains("..")&&!string.IsNullOrEmpty(definitionPath)&&definitionPath.StartsWith(prefix,StringComparison.Ordinal)&&
                definitionPath.EndsWith(".asset",StringComparison.Ordinal),"Exact ordinary owned logical asset paths required");
            var entries=battle.Entries.ToArray();
            Require(entries.Count(e=>e.ScenarioId==id)==1&&
                !entries.Any(e=>e.ScenarioId!=id&&e.IsPlayable&&e.PlayableScenarioIndex==runtimeIndex),"Unique transaction row/index required");
            if(!counterpart)Require(entries.Single(e=>e.ScenarioId==id).Status==SkirmishBattleCatalogStatus.Planned&&
                entries.Single(e=>e.ScenarioId==id).PlayableScenarioIndex<0,"Transaction cannot publish an existing playable row");
            string mainBattlePath=AssetDatabase.GetAssetPath(battle),mainBattleSha=Hash(mainBattlePath);
            var created=new List<UnityEngine.Object>();var written=new List<string>();int originalDefinitions=expansion.Definitions.Count;
            bool expansionChanged=false;
            try
            {
                var definition=UnityEngine.Object.Instantiate(sourceDefinition);created.Add(definition);
                var layout=UnityEngine.Object.Instantiate(sourceLayout);created.Add(layout);
                var objective=UnityEngine.Object.Instantiate(sourceDefinition.ObjectiveConfig);created.Add(objective);
                var binding=UnityEngine.Object.Instantiate(sourceDefinition.MissionLogicalBinding);created.Add(binding);
                Require(binding!=null&&binding.Variants?.Length==3,"Three actual compiled binding variants required");
                for(int i=0;i<binding.Variants.Length;i++)
                    if(sourceDefinition.MissionLogicalBinding.Variants[i].Areas==null)binding.Variants[i].Areas=null;
                SkirmishExpansionCatalogConfig development=null;
                if(counterpart){development=UnityEngine.Object.Instantiate(expansion);created.Add(development);}
                var planned=new List<(UnityEngine.Object asset,string path)>();
                if(counterpart)planned.Add((development,prefix+"Resources/SkirmishProtectedCounterpart_"+id+".asset"));
                planned.Add((layout,prefix+"Logical/Layout_"+id+".asset"));planned.Add((objective,prefix+"Logical/Objective_"+id+".asset"));
                foreach(var variant in binding.Variants)
                {
                    var view=UnityEngine.Object.Instantiate(variant.View);created.Add(view);variant.View=view;
                    planned.Add((view,prefix+"Logical/View_"+id+"_"+variant.Size+"_"+variant.Difficulty+".asset"));
                }
                planned.Add((binding,prefix+"Logical/Binding_"+id+".asset"));planned.Add((definition,definitionPath));
                Require(planned.Select(p=>p.path).Distinct().Count()==planned.Count&&planned.All(p=>!File.Exists(p.path)&&AssetDatabase.LoadMainAssetAtPath(p.path)==null),
                    "Every logical destination must be fresh; existing assets are never overwritten");
                var definitionFields=new SerializedObject(definition);
                definitionFields.FindProperty("mapLayout").objectReferenceValue=layout;
                definitionFields.FindProperty("objectiveConfig").objectReferenceValue=objective;
                definitionFields.FindProperty("missionLogicalBinding").objectReferenceValue=binding;
                definitionFields.ApplyModifiedPropertiesWithoutUndo();
                AssertAll120DefinitionBindings(definition,false);
                foreach(var item in planned)
                {Directory.CreateDirectory(Path.GetDirectoryName(item.path));AssetDatabase.CreateAsset(item.asset,item.path);written.Add(item.path);}
                var expansionFields=new SerializedObject(counterpart?development:expansion);var definitions=expansionFields.FindProperty("definitions");
                definitions.arraySize=originalDefinitions+1;definitions.GetArrayElementAtIndex(originalDefinitions).objectReferenceValue=definition;
                expansionChanged=!counterpart;expansionFields.ApplyModifiedPropertiesWithoutUndo();
                // Persist draft content, never player-facing availability. QA exposure is scoped below.
                if(counterpart)EditorUtility.SetDirty(development);
                AssetDatabase.SaveAssets();
                Require(Hash(mainBattlePath)==mainBattleSha&&!EditorUtility.IsDirty(battle),"Persistent battle catalog must remain byte-identical");
                readback(definition,development);
                receipt["developmentCatalogMaterialization"]=new JObject{["definitionPath"]=definitionPath,["definitionGuid"]=AssetDatabase.AssetPathToGUID(definitionPath),
                    ["definitionSha256"]=Hash(definitionPath),["layoutPath"]=AssetDatabase.GetAssetPath(layout),["bindingPath"]=AssetDatabase.GetAssetPath(binding),
                    ["runtimeIndex"]=runtimeIndex,["ordinaryQueuedSizeVariants"]=3,["ordinaryQueuedSeedChecks"]=12,["authoredDifficulty"]="Regular",["publicationManifestChanged"]=false,
                    ["catalogPlayableChanged"]=false,["battleCatalogModified"]=false,["ordinaryQueueScope"]="EditorMemoryOnly",
                    ["developmentCatalogReferencesChanged"]=true,["developmentOnly"]=true,["normalMissionReady"]=false};
            }
            catch
            {
                if(expansionChanged)
                {var fields=new SerializedObject(expansion);fields.FindProperty("definitions").arraySize=originalDefinitions;fields.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(expansion);}
                foreach(var path in written.AsEnumerable().Reverse())AssetDatabase.DeleteAsset(path);
                if(expansionChanged)AssetDatabase.SaveAssets();
                throw;
            }
            finally{foreach(var value in created)if(value!=null&&!AssetDatabase.Contains(value))UnityEngine.Object.DestroyImmediate(value);}
        }

        // Reports intentionally retain plain field JSON; managed-reference metadata is a Unity asset concern.
        static SkirmishObjectiveAreaConfig ReadAll120LogicalAreas(JToken encoded)
        {
            if(encoded==null||encoded.Type==JTokenType.Null)return null;
            Require(encoded.Type==JTokenType.Object,"Typed logical Areas must be an object or absent");
            return JsonUtility.FromJson<SkirmishObjectiveAreaConfig>(encoded.ToString(Newtonsoft.Json.Formatting.None));
        }

        static JArray AssertAll120DefinitionBindings(SkirmishScenarioDefinitionConfig definition,bool throughNormalCatalog)
        {
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            var facts=new JArray();
            foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
            {
                string logical=null,view=null;var hashes=new HashSet<uint>();
                foreach(int seed in new[]{1,104734,130368,155926})
                {
                var manifest=new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds};
                Require(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,size,seed,manifest,matrix,out var setup,out var reasons),string.Join(";",reasons));
                Require(SkirmishMissionLogicalBindingProvider.TryCompileForLaunch(definition,setup,out var compiled,out error),error);
                var variant=definition.MissionLogicalBinding.Variants.Single(v=>v.Size==size&&v.Difficulty==SkirmishDifficultyId.Regular);
                Require(ReferenceEquals(compiled.View,variant.View)&&compiled.ViewFingerprint==variant.ReviewedViewFingerprint,
                    "Actual ordinary compiler must consume this size's emitted view binding");
                if(logical==null){logical=compiled.Fingerprint;view=compiled.ViewFingerprint;}
                Require(compiled.Fingerprint==logical&&compiled.ViewFingerprint==view&&setup.Seed==seed&&hashes.Add(setup.SetupHash),
                    "Emitted binding/view must remain seed-independent while each tested seed has its own setup hash");
                facts.Add(new JObject{["size"]=size.ToString(),["difficulty"]="Regular",["seed"]=seed,["setupHash"]=setup.SetupHash.ToString("X8"),
                    ["logicalBindingFingerprint"]=compiled.Fingerprint,["viewFingerprint"]=compiled.ViewFingerprint});
                if(!throughNormalCatalog)continue;
                using var world=new World("Persisted all120 ordinary queue binding readback");
                Require(SkirmishExpandedLaunchResolver.TryCompileAndQueue(world.EntityManager,definition.CatalogId,SkirmishDifficultyId.Regular,size,seed,
                    SkirmishExpansionCatalogConfig.Load().ToAuthoredSet(),matrix,manifest,out var queuedSetup,out _,out reasons),string.Join(";",reasons));
                using var sessions=world.EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
                Require(sessions.CalculateEntityCount()==1,"Exactly one ordinary queued session required");var session=sessions.GetSingletonEntity();
                Require(ReferenceEquals(world.EntityManager.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition,definition)&&
                    SkirmishMissionLogicalBindingProvider.TryRead(world.EntityManager,session,out var queued)&&ReferenceEquals(queued.View,variant.View)&&
                    queued.Fingerprint==queuedSetup.MissionLogicalBindingFingerprint,"Normal Catalog/resolver queue must use exact persisted per-size binding, not a disconnected candidate");
                }
            }
            return facts;
        }

        static void AssertAll120DraftBindings(SkirmishExpansionCatalogConfig expansion,SkirmishBattleCatalogConfig battle,
            SkirmishScenarioDefinitionConfig definition,int index)
        {
            Require(ReferenceEquals(Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName),expansion)&&
                ReferenceEquals(Resources.Load<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogConfig.ResourceName),battle)&&
                ReferenceEquals(SkirmishExpansionCatalogConfig.Load(),expansion)&&ReferenceEquals(SkirmishBattleCatalogConfig.Load(),battle)&&
                battle.TryGet(definition.CatalogId,out var draft)&&draft.Status==SkirmishBattleCatalogStatus.Planned&&draft.PlayableScenarioIndex<0,
                "Actual persistent unpublished row and Resources required before QA scope");
            string battlePath=AssetDatabase.GetAssetPath(battle),expansionPath=AssetDatabase.GetAssetPath(expansion);
            string battleSha=Hash(battlePath),expansionSha=Hash(expansionPath);
            using(SkirmishGenericMissionAvailabilityScope.Open(expansion,definition.CatalogId,index))
                AssertAll120DefinitionBindings(definition,true);
            Require(ReferenceEquals(SkirmishExpansionCatalogConfig.Load(),expansion)&&ReferenceEquals(SkirmishBattleCatalogConfig.Load(),battle)&&
                Hash(battlePath)==battleSha&&Hash(expansionPath)==expansionSha&&!EditorUtility.IsDirty(battle)&&!EditorUtility.IsDirty(expansion),
                "QA scope must restore exact persistent catalogs without publishing");
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            using var rejected=new World("Unpublished draft outside QA scope");
            var manifest=new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds};
            Require(!SkirmishExpandedLaunchResolver.TryCompileAndQueue(rejected.EntityManager,definition.CatalogId,
                SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,1,expansion.ToAuthoredSet(),matrix,manifest,
                out _,out _,out _)&&rejected.EntityManager.UniversalQuery.CalculateEntityCount()==0,
                "Unpublished draft must refuse ordinary queue outside explicit QA scope");
        }

        // Assets must reload from actual Resources before temporary QA availability.
        // Ordinary queue readback is scoped; persistent battle availability never changes.
        public static void ValidateAll120PersistedCatalogBindings()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle persisted binding validation required");
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_MATERIALIZATION_CATALOG");
            var row=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().Single(v=>(string)v["catalog_id"]==id);
            Require(row["protected_binding"].Type==JTokenType.Null,"Protected definitions are outside generic adoption validation");
            var expansion=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
            var battle=Resources.Load<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogConfig.ResourceName);
            SkirmishScenarioDefinitionConfig definition=null;
            Require(expansion!=null&&battle!=null&&ReferenceEquals(expansion,SkirmishExpansionCatalogConfig.Load())&&
                ReferenceEquals(battle,SkirmishBattleCatalogConfig.Load())&&expansion.TryGetDefinition(id,out definition)&&
                AssetDatabase.GetAssetPath(definition)==(string)row["intended_new_definition_asset"]&&AssetDatabase.Contains(definition)&&
                AssetDatabase.Contains(definition.MapLayout)&&AssetDatabase.Contains(definition.MissionLogicalBinding)&&
                definition.MissionLogicalBinding.Variants.All(v=>v?.View!=null&&AssetDatabase.Contains(v.View)),
                "Normal persistent catalog must resolve exact serialized definition/layout/binding/views without overrides");
            AssertAll120DraftBindings(expansion,battle,definition,1000+int.Parse(id.Substring(1),System.Globalization.CultureInfo.InvariantCulture));
            Debug.Log("[All120PersistedCatalogBindings] result=Passed catalog="+id+" ordinaryQueuedSizeVariants=3 ordinaryQueuedSeedChecks=12 difficulty=Regular availability=EditorMemoryOnly battleCatalogModified=0 normalMissionAcceptance=Pending cameraAcceptance=Pending");
        }
    }
}
#endif
