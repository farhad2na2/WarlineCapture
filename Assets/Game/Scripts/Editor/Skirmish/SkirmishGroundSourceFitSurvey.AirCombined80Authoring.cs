#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        const string AirCombined80Inputs="Tools/CI/SkirmishProductionDrafts/AirCombined80CoherentAuthority/air-combined-eighty.coherent.authoring.json";
        const string AirCombined80InputsSha="2aa2be19f4bd7d975e6969e6db08170969ee2c6da4f273ac04d03adfa5f97401";

        public sealed class AirCombined80CandidateOwner:IDisposable
        {
            public SkirmishScenarioDefinitionConfig Definition {get;internal set;}
            public SkirmishMapLayoutConfig Layout {get;internal set;}
            public SkirmishObjectiveAreaConfig AreaDraft {get;internal set;}
            public SkirmishCompiledObjectiveAreas Areas=>null;
            public SkirmishMissionViewProfile View=>null;
            readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
            internal T Own<T>(T value) where T:UnityEngine.Object {owned.Add(value);return value;}
            public void Dispose(){for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)UnityEngine.Object.DestroyImmediate(owned[i]);owned.Clear();Definition=null;Layout=null;AreaDraft=null;}
        }

        static JObject ReadAirCombined80Envelope()
        {
            Require(Hash(AirCombined80Inputs)==AirCombined80InputsSha,"Exact reviewed A/C80 input required");
            var document=JObject.Parse(File.ReadAllText(AirCombined80Inputs));
            Require(Hash("Tools/CI/SkirmishProductionDrafts/AirCombined80/air-combined-eighty.authoring.json")=="a869c03469d655f87389961242ffc0dc05720248ab6f6480cec43138793d8926","Original A/C80 authority must remain exact");
            var originalAuthority=JObject.Parse(File.ReadAllText("Tools/CI/SkirmishProductionDrafts/AirCombined80/air-combined-eighty.authoring.json"));
            var comparable=(JObject)document.DeepClone();
            foreach(JObject pin in comparable["requirement_source_pins"])
            {
                string key=(string)pin["path"];
                if(key=="Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupCompiler.cs"||key=="Assets/Game/Scripts/Configs/Skirmish/SkirmishMeasuredLayoutBinder.cs"||key=="Assets/Game/Scripts/Configs/Skirmish/SkirmishMissionLogicalBindingConfig.cs")
                    pin["sha256"]=((JArray)originalAuthority["requirement_source_pins"]).OfType<JObject>().Single(p=>(string)p["path"]==key)["sha256"].DeepClone();
            }
            Require(JToken.DeepEquals(comparable,originalAuthority),"A/C80 successor may change only declared source authority; all mission content must remain identical");
            Require((int?)document["schema_version"]==1&&(string)document["kind"]=="UnqualifiedAirCombined80Authoring"&&
                (int?)document["mission_count"]==80&&(int?)document["canonical_size_vectors"]==240&&
                (bool?)document["publication_enabled"]==false&&(bool?)document["source_fit_certified"]==false,"Exact unpublished A/C80 schema required");
            foreach(var field in new[]{"full_index","ground_authoring","map_reuse_policy"})
            {var pin=document["authority"][field];Require(Hash((string)pin["path"])==(string)pin["sha256"],"A/C80 authority changed: "+field);}
            foreach(JObject pin in document["requirement_source_pins"])
                Require(Hash((string)pin["path"])==(string)pin["sha256"],"Actual compiler/definition requirement source changed");
            foreach(var field in new[]{"A","C"})
            {var pin=document["profile_asset_pins"][field];Require(Hash((string)pin["path"])==(string)pin["sha256"],"Actual typed A/C profile changed");}
            var index=JObject.Parse(File.ReadAllText((string)document["authority"]["full_index"]["path"]));
            var expected=((JArray)index["missions"]).OfType<JObject>().Where(v=>(string)v["tuple"]["army_profile"]=="A"||(string)v["tuple"]["army_profile"]=="C").ToDictionary(v=>(string)v["catalog_id"]);
            var rows=((JArray)document["missions"]).OfType<JObject>().ToArray();
            Require(rows.Length==80&&rows.Select(v=>(string)v["catalog_id"]).Distinct().Count()==80&&rows.All(v=>expected.ContainsKey((string)v["catalog_id"]))&&
                rows.Sum(v=>((JArray)v["canonical_size_rows"]).Count)==240,"All exact eighty IDs and240 size rows required");
            foreach(var row in rows)
            {
                var original=expected[(string)row["catalog_id"]];
                foreach(var key in new[]{"definition_id","scenario_setup_id","tuple","packet","packet_section","canonical_size_rows","canonical_size_rows_sha256","required_capabilities"})
                    Require(JToken.DeepEquals(row[key],original[key]),"A/C80 canonical field changed: "+key);
                Require(JToken.DeepEquals(row["protected_binding"],original["protected_binding"]??JValue.CreateNull())&&
                    (bool?)row["publication_enabled"]==false&&row["camera_scalars"].Type==JTokenType.Null,"Coded binding/publication/camera boundary differs");
            }
            Require(rows.Count(v=>v["protected_binding"].Type!=JTokenType.Null)==3,"Exactly S003/S004/S005 remain read-only in this slice");
            return document;
        }

        static JObject RequireExactAirCombined80Row(JObject mission,Func<JObject> observeEnvelope,out JObject document)
        {
            document=observeEnvelope();
            var rows=((JArray)document["missions"]).OfType<JObject>().Where(v=>(string)v["catalog_id"]==(string)mission?["catalog_id"]).ToArray();
            Require(rows.Length==1&&JToken.DeepEquals(rows[0],mission),"Exact immutable A/C80 row required");return rows[0];
        }

        public static bool TryReadAirCombined80ProtectedRecord(JObject mission,out JObject record,out string error)
            =>TryReadAirCombined80ProtectedRecordCore(mission,ReadAirCombined80Envelope,out record,out error);

        static bool TryReadAirCombined80ProtectedRecordCore(JObject mission,Func<JObject> observeEnvelope,out JObject record,out string error)
        {
            record=null;error=null;
            try
            {
                RequireExactAirCombined80Row(mission,observeEnvelope,out _);var binding=(JObject)mission["protected_binding"];
                Require(binding!=null&&(string)mission["action"]=="PreserveCodedBinding_ReadOnlyNativeCensus"&&
                    mission["family_source_candidate_key"].Type==JTokenType.Null&&mission["ground_template_catalog_id"].Type==JTokenType.Null,
                    "Protected coded binding cannot be replaced by a family template");
                string path=(string)binding["definition_asset"];
                Require(Hash(path)==(string)binding["definition_sha256"]&&Hash(path+".meta")== (string)binding["definition_meta_sha256"],"Actual coded definition/GUID bytes changed");
                string sourcePath=(string)binding["campaign_source_asset"];
                if(!string.IsNullOrEmpty(sourcePath))Require(Hash(sourcePath)==(string)binding["campaign_source_sha256"]&&
                    AssetDatabase.AssetPathToGUID(sourcePath)==(string)binding["campaign_source_guid"],"Known coded Campaign source/GUID bytes changed");
                record=new JObject{["catalogId"]=mission["catalog_id"],["canonicalRowsSha256"]=mission["canonical_size_rows_sha256"],
                    ["scope"]="ProtectedCodedBinding_ReadOnlyAssetPins_NativeIdentityStillRequired",["definitionAsset"]=path,
                    ["definitionSha256"]=binding["definition_sha256"],["binding"]=binding.DeepClone(),["nativeSourceIdentityMeasured"]=false,
                    ["familySourceApplied"]=false,["sourceFitCertified"]=false};return true;
            }
            catch(Exception failure){error=failure.ToString();return false;}
        }

        public static bool TryCreateAirCombined80Candidate(JObject mission,OperationMapDefinition source,out AirCombined80CandidateOwner owner,out string error)
            =>TryCreateAirCombined80CandidateCore(mission,source,ReadAirCombined80Envelope,out owner,out error);

        static bool TryCreateAirCombined80CandidateCore(JObject mission,OperationMapDefinition source,Func<JObject> observeEnvelope,out AirCombined80CandidateOwner owner,out string error)
        {
            owner=null;error=null;var created=new AirCombined80CandidateOwner();
            try
            {
                RequireExactAirCombined80Row(mission,observeEnvelope,out var envelope);
                Require(mission["protected_binding"].Type==JTokenType.Null&&(string)mission["action"]=="DisposableGenericAuthoringCandidate","Coded A/C binding is read-only, not a generic candidate");
                var ground=ReadGround30Envelope();var templateRows=((JArray)ground["missions"]).OfType<JObject>().Where(v=>(string)v["catalog_id"]==(string)mission["ground_template_catalog_id"]).ToArray();
                Require(templateRows.Length==1,"Exact existing ground authoring template required");var template=templateRows[0];
                foreach(var key in new[]{"map_id","start_profile"})Require(JToken.DeepEquals(template["tuple"][key],mission["tuple"][key]),"Template source/start contract differs");
                string code=(string)mission["tuple"]["objective_id"];
                Require(code=="BA"||(string)template["tuple"]["objective_id"]==code,"Non-BA typed objective template differs");
                Require(TryCreateGroundObjective30Candidate(template,source,out var baseOwner,out string baseError),baseError);
                using(baseOwner)
                {
                    // The ground owner already materialized the exact source proposal
                    // and, for CE, appended the three typed original truck bays.
                    // Reapplying the original18-pad proposal to that21-pad layout is
                    // invalid. Preserve the adapted typed layout; change identity only.
                    created.Layout=created.Own(CloneAirCombined80AdaptedLayout(baseOwner.Layout,source,(string)mission["catalog_id"]));
                    var catalog=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
                    Require(catalog!=null&&!EditorUtility.IsDirty(catalog),"Actual clean packaged profile catalog required");var profiles=catalog.ToAuthoredSet();
                    string profileId=(string)mission["tuple"]["army_profile"];
                    var profile=profileId=="A"?profiles.ArmyAir:profileId=="C"?profiles.ArmyCombined:null;
                    Require(profile!=null&&!EditorUtility.IsDirty(profile)&&profile.ProfileId==profileId&&
                        AssetDatabase.GetAssetPath(profile)==(string)envelope["profile_asset_pins"][profileId]["path"],"Actual persistent typed A/C profile required");
                    var objective=created.Own(UnityEngine.Object.Instantiate(baseOwner.Definition.ObjectiveConfig));
                    if(code=="BA")objective.ConfigureBaseAssault();
                    var serialized=new SerializedObject(objective);
                    foreach(JObject row in mission["canonical_size_rows"])
                    {
                        string size=(string)row["size_id"],field=size=="Standard"?"standardDeadlineSeconds":size=="War"?"warDeadlineSeconds":"largeWarDeadlineSeconds";
                        serialized.FindProperty(field).intValue=int.Parse((string)row["deadline_seconds"],System.Globalization.CultureInfo.InvariantCulture);
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    created.Definition=created.Own(ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>());
                    created.Definition.Configure((string)mission["catalog_id"],objective,profile,
                        (string)mission["tuple"]["start_profile"]=="F"?profiles.StartField:profiles.StartEstablished,
                        profiles.Economy,profiles.Upgrades,profiles.Intel,baseOwner.Definition.RoleCatalog,created.Layout,
                        SkirmishSizeId.Standard,mission["required_capabilities"].ToObject<string[]>());
                    created.Definition.BindCampaignMap(source);
                    if(code!="BA")
                    {
                        created.AreaDraft=JsonUtility.FromJson<SkirmishObjectiveAreaConfig>(JsonUtility.ToJson(baseOwner.AreaDraft));
                        created.AreaDraft.AreaSetId="areas."+created.Definition.DefinitionId+".candidate.v1";
                        created.AreaDraft.Binding.DefinitionId=created.Definition.DefinitionId;created.AreaDraft.Binding.LayoutId=created.Layout.LayoutId;
                    }
                    Require(created.Definition.DefinitionId==(string)mission["definition_id"]&&created.Definition.ScenarioSetupId==(string)mission["scenario_setup_id"]&&
                        created.Definition.MissionLogicalBinding==null&&!created.Layout.WorldBinding.HasMeasuredDeployments&&!created.Layout.WorldBinding.BindAllConfigurations,
                        "Unmeasured authoring must preserve own identity and cannot acquire proof/view binding");
                }
                owner=created;return true;
            }
            catch(Exception failure){created.Dispose();error="A/C80 catalog="+(string)mission?["catalog_id"]+
                " groundTemplate="+(string)mission?["ground_template_catalog_id"]+" source="+(source!=null?source.OperationMapId:"null")+" error="+failure;return false;}
        }

        static SkirmishMapLayoutConfig CloneAirCombined80AdaptedLayout(SkirmishMapLayoutConfig adapted,OperationMapDefinition source,string catalogId)
        {
            Require(adapted!=null&&source!=null&&!string.IsNullOrEmpty(catalogId)&&
                adapted.OperationMapId==source.OperationMapId&&adapted.WorldBinding.SourceHash==source.ContentHash&&
                !adapted.WorldBinding.HasMeasuredDeployments&&!adapted.WorldBinding.BindAllConfigurations,
                "Exact unmeasured adapted Campaign layout/source required: "+catalogId);
            var before=JObject.Parse(JsonUtility.ToJson(adapted));
            var clone=UnityEngine.Object.Instantiate(adapted);
            try
            {
                var data=new SerializedObject(clone);data.FindProperty("layoutId").stringValue="layout."+catalogId.ToLowerInvariant()+".measured-pad-candidate.v2";
                data.ApplyModifiedPropertiesWithoutUndo();
                var after=JObject.Parse(JsonUtility.ToJson(clone));
                string expectedId=(string)after["layoutId"];before.Remove("layoutId");after.Remove("layoutId");
                Require(expectedId==clone.LayoutId&&expectedId=="layout."+catalogId.ToLowerInvariant()+".measured-pad-candidate.v2"&&
                    JToken.DeepEquals(before,after),"A/C80 identity-only clone changed actual typed layout: "+catalogId+
                    " padsBefore="+adapted.Pads.Length+" padsAfter="+clone.Pads.Length+" anchorsBefore="+adapted.Anchors.Length+" anchorsAfter="+clone.Anchors.Length);
                foreach(var anchor in adapted.Anchors)
                {
                    Require(adapted.TryProjectLocalToMap(anchor.WorldX,anchor.WorldZ,out float x,out float z)&&
                        clone.TryGetAnchorById(anchor.AnchorId,out var retained)&&
                        clone.TryProjectLocalToMap(retained.WorldX,retained.WorldZ,out float rx,out float rz)&&x==rx&&z==rz,
                        "A/C80 actual world anchor changed: "+catalogId+"/"+anchor.AnchorId);
                }
                return clone;
            }
            catch {UnityEngine.Object.DestroyImmediate(clone);throw;}
        }

        public static void RunAirCombined80LayoutCloneControls()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle owned Editor required");
            var envelope=ReadAirCombined80Envelope();
            var mission=((JArray)envelope["missions"]).OfType<JObject>().Single(v=>(string)v["catalog_id"]=="S021");
            Require((string)mission["ground_template_catalog_id"]=="S019"&&(string)mission["tuple"]["objective_id"]=="CE","Exact first CE A/C case required");
            var ground=ReadGround30Envelope();var template=((JArray)ground["missions"]).OfType<JObject>().Single(v=>(string)v["catalog_id"]=="S019");
            var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)envelope["source_candidates"][(string)mission["family_source_candidate_key"]]["sourcePath"]);
            Require(TryCreateGroundObjective30Candidate(template,source,out var owner,out string error),error);
            using(owner)
            {
                int declared=((JArray)template["layout_input"]["pilot_candidate"]["typed_pad_world_axis_candidates"]).Count;
                Require(declared==18&&owner.Layout.Pads.Length==21,"Actual first CE18+3 pad identity diagnostic required");
                string original=JsonUtility.ToJson(owner.Layout);
                var clone=CloneAirCombined80AdaptedLayout(owner.Layout,source,"S021");
                try
                {
                    for(int slot=1;slot<=3;slot++)Require(clone.TryGetPad("convoy.origin."+slot,out var bay)&&
                        bay.FactionId==1&&bay.Kind==SkirmishLegalPadKind.ObjectiveTruckSpawn,"Exact original convoy bay missing after A/C clone");
                    var mutation=new SerializedObject(clone);var center=mutation.FindProperty("pads").GetArrayElementAtIndex(0).FindPropertyRelative("CenterX");
                    center.floatValue+=1;mutation.ApplyModifiedPropertiesWithoutUndo();
                    Require(JsonUtility.ToJson(owner.Layout)==original,"Disposable A/C clone mutation escaped to ground owner");
                }
                finally {UnityEngine.Object.DestroyImmediate(clone);}
                var wrong=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)envelope["source_candidates"]["MP"]["sourcePath"]);
                Require(wrong!=null&&wrong.OperationMapId!=source.OperationMapId,"Actual different Campaign source required for negative");
                bool rejected=false;
                try {var accidental=CloneAirCombined80AdaptedLayout(owner.Layout,wrong,"S021");UnityEngine.Object.DestroyImmediate(accidental);}
                catch(InvalidOperationException){rejected=true;}
                Require(rejected,"Foreign physical source must reject before layout clone");
            }
            Debug.Log("[SkirmishAirCombined80LayoutCloneControls] result=Passed controls=3 catalog=S021 groundTemplate=S019 originalProposalPads=18 adaptedPads=21 sourceFitCertified=0");
        }

        public static void RunAirCombined80AuthoringControls()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle owned Editor required");
            RunAirCombined80LayoutCloneControls();
            var envelope=ReadAirCombined80Envelope();var census=CaptureMaterializationWorldCensus();var oldDefault=World.DefaultGameObjectInjectionWorld;
            int scenes=UnityEngine.SceneManagement.SceneManager.sceneCount,previews=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            int candidates=0,preserved=0,negative=0;
            string immutableEnvelope=envelope.ToString(Newtonsoft.Json.Formatting.None);
            Func<JObject> observedEnvelope=()=>envelope;
            foreach(JObject mission in envelope["missions"])
            {
                bool protection=mission["protected_binding"].Type!=JTokenType.Null;OperationMapDefinition source=null;
                if(protection)
                {
                    Require(TryReadAirCombined80ProtectedRecordCore(mission,observedEnvelope,out var record,out string error),error);
                    Require((bool?)record["familySourceApplied"]==false&&(bool?)record["nativeSourceIdentityMeasured"]==false,"Protected source census cannot be invented");preserved++;
                }
                else
                {
                    string group=(string)mission["family_source_candidate_key"];
                    source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)envelope["source_candidates"][group]["sourcePath"]);
                    Require(TryCreateAirCombined80CandidateCore(mission,source,observedEnvelope,out var owner,out string error),error);
                    using(owner)
                    {
                        Require(owner.Definition.CatalogId==(string)mission["catalog_id"]&&owner.Definition.CampaignMap==source&&
                            owner.Definition.ArmyProfileConfig.ProfileId==(string)mission["tuple"]["army_profile"]&&
                            owner.Definition.MissionLogicalBinding==null&&owner.Areas==null&&owner.View==null,"Actual candidate identity/profile or unmeasured boundary differs");
                        bool ba=(string)mission["tuple"]["objective_id"]=="BA";
                        Require(ba?owner.AreaDraft==null:owner.AreaDraft!=null&&owner.AreaDraft.Binding.DefinitionId==owner.Definition.DefinitionId&&
                            owner.AreaDraft.Binding.LayoutId==owner.Layout.LayoutId&&owner.AreaDraft.Binding.SourceAssetGuid==source.SourceSceneReference.AssetGUID&&
                            string.IsNullOrEmpty(owner.AreaDraft.Binding.LayoutBindingFingerprint)&&string.IsNullOrEmpty(owner.AreaDraft.Measurement.ArtifactId)&&
                            string.IsNullOrEmpty(owner.AreaDraft.Measurement.ArtifactSha256)&&string.IsNullOrEmpty(owner.AreaDraft.Measurement.GeometryFingerprint),
                            "BA must omit non-BA areas; non-BA draft must retain own identity/source without proof");
                    }
                    candidates++;
                }
                var corrupted=(JObject)mission.DeepClone();corrupted["canonical_size_rows"][0]["player_rifle"]="999";
                bool accepted;string failed;
                if(protection)accepted=TryReadAirCombined80ProtectedRecordCore(corrupted,observedEnvelope,out _,out failed);
                else{accepted=TryCreateAirCombined80CandidateCore(corrupted,source,observedEnvelope,out var rejected,out failed);rejected?.Dispose();}
                Require(!accepted&&failed!=null&&failed.Contains("Exact immutable A/C80 row required"),"Mutated budget must reject at exact row join, not a later unavailable source");negative++;
            }
            Require(immutableEnvelope==envelope.ToString(Newtonsoft.Json.Formatting.None)&&JToken.DeepEquals(envelope,ReadAirCombined80Envelope()),"Control envelope unchanged and fresh complete final observation required");
            RunAirCombined80FreshWrapperControls(envelope);
            Require(candidates==77&&preserved==3&&negative==80&&census.SequenceEqual(CaptureMaterializationWorldCensus())&&
                ReferenceEquals(oldDefault,World.DefaultGameObjectInjectionWorld)&&scenes==UnityEngine.SceneManagement.SceneManager.sceneCount&&previews==UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount,
                "All A/C candidates/protected/negative rows and native ownership must remain exact");
            Debug.Log("[SkirmishAirCombined80Authoring] result=Passed candidates=77 protectedReadOnly=3 negativeControls=80 streamingWorldsCreated=0 sourceFitCertified=0");
        }

        // Four actual public-call observations prove this optimization is control-only.
        static void RunAirCombined80FreshWrapperControls(JObject envelope)
        {
            var rows=((JArray)envelope["missions"]).OfType<JObject>().ToArray();
            var generic=rows.First(m=>m["protected_binding"].Type==JTokenType.Null);
            var protectedRow=rows.First(m=>m["protected_binding"].Type!=JTokenType.Null);
            string group=(string)generic["family_source_candidate_key"];
            var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)envelope["source_candidates"][group]["sourcePath"]);
            Require(TryCreateAirCombined80Candidate(generic,source,out var created,out string error),error);
            using(created)Require(created.Definition.CatalogId==(string)generic["catalog_id"],"Actual fresh public generic identity");
            var corrupt=(JObject)generic.DeepClone();corrupt["canonical_size_rows"][0]["player_rifle"]="999";
            bool accepted=TryCreateAirCombined80Candidate(corrupt,source,out var refused,out error);refused?.Dispose();
            Require(!accepted&&error!=null&&error.Contains("Exact immutable A/C80 row required"),"Fresh public generic mutation refuses");
            Require(TryReadAirCombined80ProtectedRecord(protectedRow,out var record,out error)&&
                (bool?)record["familySourceApplied"]==false&& (bool?)record["nativeSourceIdentityMeasured"]==false,error);
            corrupt=(JObject)protectedRow.DeepClone();corrupt["canonical_size_rows"][0]["player_rifle"]="999";
            Require(!TryReadAirCombined80ProtectedRecord(corrupt,out _,out error)&&error!=null&&error.Contains("Exact immutable A/C80 row required"),
                "Fresh public protected mutation refuses");
            Debug.Log("[AirCombined80FreshWrapperControls] result=Passed controls=4 publicFreshEnvelopeReads=4 crossRunCache=0 nativeSourceFit=Pending");
        }
    }
}
#endif
