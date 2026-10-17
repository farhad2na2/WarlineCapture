#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        public const string GroundObjective30Inputs="Tools/CI/SkirmishProductionDrafts/GroundObjective30/ground-objective-thirty.authoring.json";
        public const string GroundObjective30InputsSha="95a9c7b25b9e4876465c7c7d41111605f6054e65ba5c6a3f37df646678ff10dc";

        public static void RunGroundObjective30AuthoringControls()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle logical authoring control required");
            Require(Hash(GroundObjective30Inputs)==GroundObjective30InputsSha,"Exact thirty-mission envelope required");
            var rows=(JArray)JObject.Parse(File.ReadAllText(GroundObjective30Inputs))["missions"];
            Require(rows.Count==30,"All thirty rows required");
            var worlds=CaptureMaterializationWorldCensus();var defaultWorld=Unity.Entities.World.DefaultGameObjectInjectionWorld;
            int scenes=UnityEngine.SceneManagement.SceneManager.sceneCount,previews=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
            int passed=0,negative=0;var failures=new List<string>();
            foreach(var row in rows.OfType<JObject>())
            {
                string sid=(string)row["catalog_id"];
                try
                {
                    string path=(string)row["source_candidate"]["sourcePath"];var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
                    string before=Hash(path);
                    Require(TryCreateGroundObjective30Candidate(row,source,out var owner,out string error),error);
                    using(owner)
                    {
                        Require(owner.Definition.CatalogId==sid&&owner.Definition.DefinitionId==(string)row["definition_id"]&&
                            owner.Definition.ScenarioSetupId==(string)row["scenario_setup_id"]&&ReferenceEquals(owner.Definition.CampaignMap,source),"Actual definition identity/source readback differs");
                        Require(owner.AreaDraft.Binding.SourceAssetGuid==source.SourceSceneReference.AssetGUID&&
                            owner.AreaDraft.Binding.SourceAssetGuid==(string)row["source_candidate"]["sourceSceneGuid"],"Area source identity must use actual scene GUID domain, not config asset GUID");
                        Require(owner.AreaDraft.Areas.Length==((JArray)row["typed_objective_areas"]["areas"]).Count&&owner.Areas==null&&owner.View==null&&
                            string.IsNullOrEmpty(owner.AreaDraft.Measurement.ArtifactId)&&owner.Definition.MissionLogicalBinding==null,"Logical draft must not acquire measurement/view/normal launch acceptance");
                        foreach(var sizeRow in ((JArray)row["canonical_size_rows"]).OfType<JObject>())
                        {
                            var size=(SkirmishSizeId)Enum.Parse(typeof(SkirmishSizeId),(string)sizeRow["size_id"]);
                            Require(owner.Definition.ObjectiveConfig.DeadlineSeconds(size)==int.Parse((string)sizeRow["deadline_seconds"]),"Canonical actual objective deadline differs");
                        }
                        foreach(var area in owner.AreaDraft.Areas)foreach(var point in area.Points)
                            Require(point.WorldX>=source.Bounds.PlayableMin.x&&point.WorldX<=source.Bounds.PlayableMax.x&&point.WorldZ>=source.Bounds.PlayableMin.z&&point.WorldZ<=source.Bounds.PlayableMax.z,"Authored area escapes actual source");
                        if(owner.Definition.ObjectiveConfig.Kind==SkirmishObjectiveKind.ConvoyEscort)
                            for(int slot=1;slot<=3;slot++)Require(owner.Layout.TryGetPad("convoy.origin."+slot,out var bay)&&bay.FactionId==1&&bay.Kind==SkirmishLegalPadKind.ObjectiveTruckSpawn,"Exact three typed original convoy bays required");
                    }
                    for(int mutation=0;mutation<2;mutation++)
                    {
                        var bad=(JObject)row.DeepClone();
                        if(mutation==0)bad["canonical_size_rows"][0]["player_rifle"]="999";
                        else bad["typed_objective_areas"]["measurement"]=new JObject{["ArtifactId"]="fabricated"};
                        bool accepted=TryCreateGroundObjective30Candidate(bad,source,out var accidental,out _);accidental?.Dispose();
                        Require(!accepted,"Caller budget or invented proof mutation must refuse before logical creation");negative++;
                    }
                    Require(Hash(path)==before&&!EditorUtility.IsDirty(source),"Logical control mutated persistent Campaign source");passed++;
                }
                catch(Exception failure){failures.Add(sid+": "+failure);Debug.LogError("[GroundObjective30AuthoringControl] catalog="+sid+" error="+failure);}
            }
            Require(worlds.SequenceEqual(CaptureMaterializationWorldCensus())&&ReferenceEquals(defaultWorld,Unity.Entities.World.DefaultGameObjectInjectionWorld)&&
                scenes==UnityEngine.SceneManagement.SceneManager.sceneCount&&previews==UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount,"Logical controls changed World/scene ownership");
            Require(passed==30&&negative==60&&failures.Count==0,"All thirty logical rows and sixty negative controls required: "+string.Join("\n",failures));
            Debug.Log("[SkirmishGroundObjective30Authoring] result=Passed candidates=30 negativeControls=60 streamingWorldsCreated=0 sourceFitCertified=0");
        }

        public sealed class GroundObjective30CandidateOwner:IDisposable
        {
            public SkirmishScenarioDefinitionConfig Definition {get;internal set;}
            public SkirmishMapLayoutConfig Layout {get;internal set;}
            public SkirmishObjectiveAreaConfig AreaDraft {get;internal set;}
            public SkirmishCompiledObjectiveAreas Areas=>null;
            public SkirmishMissionViewProfile View=>null;
            readonly List<UnityEngine.Object> objects=new List<UnityEngine.Object>();
            internal T Own<T>(T value) where T:UnityEngine.Object {objects.Add(value);return value;}
            public void Dispose(){for(int i=objects.Count-1;i>=0;i--)if(objects[i]!=null)UnityEngine.Object.DestroyImmediate(objects[i]);objects.Clear();Definition=null;Layout=null;AreaDraft=null;}
        }

        // Disposable logical assets only. No AssetDatabase writes, publication,
        // injected setup, fabricated measured flags or renderer/camera values.
        public static bool TryCreateGroundObjective30Candidate(JObject mission,OperationMapDefinition source,
            out GroundObjective30CandidateOwner owner,out string error)
        {
            owner=null;error=null;var created=new GroundObjective30CandidateOwner();
            try
            {
                Require(Hash(GroundObjective30Inputs)==GroundObjective30InputsSha,"Exact reviewed thirty-mission authoring envelope required");
                var envelope=JObject.Parse(File.ReadAllText(GroundObjective30Inputs));
                var matches=((JArray)envelope["missions"]).OfType<JObject>().Where(v=>(string)v["catalog_id"]==(string)mission?["catalog_id"]).ToArray();
                Require(matches.Length==1&&JToken.DeepEquals(matches[0],mission),"Exact immutable mission row required, not caller-authored geometry/budgets");
                Require((bool?)mission["publication_enabled"]==false&&source!=null&&!EditorUtility.IsDirty(source),"Unpublished candidate on a clean existing source required");
                var pin=(JObject)mission["source_candidate"];string path=AssetDatabase.GetAssetPath(source);
                Require(path==(string)pin["sourcePath"]&&Hash(path)==(string)pin["sourceAssetSha256"]&&
                    source.OperationMapId==(string)pin["operationMapId"]&&source.ContentHash==(string)pin["contentHash"]&&source.SourceIdentityHash==(string)pin["sourceIdentityHash"],"Exact actual Campaign source identity/bytes required");
                foreach(var pair in new[]{new[]{"gridPath","gridAssetSha256"},new[]{"surfacePath","surfaceAssetSha256"}})
                    Require(Hash((string)pin[pair[0]])==(string)pin[pair[1]],"Exact current grid/surface bytes required");
                string group=(string)mission["collector_source_group"];
                var inventory=JObject.Parse(File.ReadAllText(Inventory));SkirmishMapLayoutConfig template;
                using(var templateOwner=new PilotCandidateOwner())
                {
                    if(inventory["layoutSnapshots"]?[group] is JObject snapshot)
                    {
                        string templatePath=(string)snapshot["templatePath"];
                        Require(Hash(templatePath)==(string)snapshot["templateAssetSha256"],"Exact protected source template bytes required");
                        template=AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(templatePath);
                        Require(template!=null&&!EditorUtility.IsDirty(template),"Clean original layout template required");
                    }
                    else template=CreatePilotLogicalCandidate(source,group,new JObject{["sourcePath"]=path,["sourceAssetSha256"]=Hash(path)},templateOwner);
                    created.Layout=created.Own(MaterializePilotClone(template,source,(JObject)mission["layout_input"]["pilot_candidate"],new JObject()));
                }
                var profiles=SkirmishExpansionCatalogConfig.Load().ToAuthoredSet();
                string code=(string)mission["tuple"]["objective_id"];
                var kind=code=="FC"?SkirmishObjectiveKind.FrontlineControl:code=="BT"?SkirmishObjectiveKind.Breakthrough:code=="CE"?SkirmishObjectiveKind.ConvoyEscort:SkirmishObjectiveKind.None;
                Require(kind!=SkirmishObjectiveKind.None&&(string)mission["tuple"]["army_profile"]=="G","Actual ground FC/BT/CE tuple required");
                var objective=created.Own(UnityEngine.Object.Instantiate(profiles.ObjectiveBa));
                var serialized=new SerializedObject(objective);serialized.FindProperty("objectiveId").stringValue=code;
                serialized.FindProperty("kind").intValue=(int)kind;
                foreach(var row in ((JArray)mission["canonical_size_rows"]).OfType<JObject>())
                {
                    string size=(string)row["size_id"],field=size=="Standard"?"standardDeadlineSeconds":size=="War"?"warDeadlineSeconds":"largeWarDeadlineSeconds";
                    serialized.FindProperty(field).intValue=int.Parse((string)row["deadline_seconds"],System.Globalization.CultureInfo.InvariantCulture);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var roles=kind==SkirmishObjectiveKind.ConvoyEscort?AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(SkirmishConvoyTruckAuthoring.RoleCatalogPath):profiles.Roles;
                Require(roles!=null&&!EditorUtility.IsDirty(roles),"Actual persistent canonical role catalog required");
                if(kind==SkirmishObjectiveKind.ConvoyEscort)AddGroundObjective30ConvoyBays(created.Layout);
                created.Definition=created.Own(ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>());
                created.Definition.Configure((string)mission["catalog_id"],objective,profiles.ArmyGround,
                    (string)mission["tuple"]["start_profile"]=="F"?profiles.StartField:profiles.StartEstablished,
                    profiles.Economy,profiles.Upgrades,profiles.Intel,roles,created.Layout,SkirmishSizeId.Standard,
                    new[]{"ground","intel","transport","advanced_ground","objective_"+code.ToLowerInvariant()});
                created.Definition.BindCampaignMap(source);
                var areas=mission["typed_objective_areas"]["areas"].ToObject<SkirmishObjectiveArea[]>();
                created.AreaDraft=new SkirmishObjectiveAreaConfig{AreaSetId="areas."+created.Definition.DefinitionId+".candidate.v1",ObjectiveKind=kind,Areas=areas,
                    Binding=new SkirmishObjectiveAreaBinding{DefinitionId=created.Definition.DefinitionId,SourceConfigId=source.OperationMapId,
                        SourceAssetGuid=source.SourceSceneReference.AssetGUID,SourceContentHash=source.ContentHash,
                        SourceSemanticFingerprint=SkirmishMissionLogicalBindingCompiler.SourceFingerprint(source),LayoutId=created.Layout.LayoutId,
                        SourceMinX=source.Bounds.PlayableMin.x,SourceMinZ=source.Bounds.PlayableMin.z,SourceMaxX=source.Bounds.PlayableMax.x,SourceMaxZ=source.Bounds.PlayableMax.z}};
                // LayoutBindingFingerprint and Measurement intentionally absent:
                // mechanical layout/area data does not prove native source fit.
                Require(!created.Layout.WorldBinding.HasMeasuredDeployments&&!created.Layout.WorldBinding.BindAllConfigurations&&
                    created.Definition.MissionLogicalBinding==null,"Unmeasured candidate must not acquire runtime proof or view binding");
                owner=created;return true;
            }
            catch(Exception failure){created.Dispose();error=failure.ToString();return false;}
        }

        static void AddGroundObjective30ConvoyBays(SkirmishMapLayoutConfig layout)
        {
            Require(layout.TryGetPad(1,SkirmishLegalPadKind.VehicleSpawn,out var vehicle),"Actual player vehicle pad required for authored three-bay proposal");
            var data=JObject.Parse(JsonUtility.ToJson(layout));var pads=(JArray)data["pads"];var anchors=(JArray)data["anchors"];
            for(int i=1;i<=3;i++)
            {
                string role="convoy.origin."+i,id="anchor."+layout.LayoutId+"."+role;
                float width=vehicle.WidthMetres/3,x=vehicle.CenterX-vehicle.WidthMetres*.5f+width*(i-.5f),z=vehicle.CenterZ;
                anchors.Add(new JObject{["AnchorId"]=id,["RoleId"]=role,["WorldX"]=x,["WorldZ"]=z,["NormalizedU"]=(x-layout.OriginX)/layout.WorldWidthMetres,["NormalizedV"]=(z-layout.OriginZ)/layout.WorldDepthMetres});
                pads.Add(new JObject{["PadId"]="pad."+layout.LayoutId+"."+role,["AnchorId"]=id,["RoleId"]=role,["Kind"]=(int)SkirmishLegalPadKind.ObjectiveTruckSpawn,["FactionId"]=1,["CenterX"]=x,["CenterZ"]=z,["WidthMetres"]=width,["DepthMetres"]=vehicle.DepthMetres});
            }
            JsonUtility.FromJsonOverwrite(data.ToString(Newtonsoft.Json.Formatting.None),layout);
        }
    }
}
#endif
