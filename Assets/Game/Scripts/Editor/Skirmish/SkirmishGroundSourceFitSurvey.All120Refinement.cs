#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        public static void RunAll120SimultaneousLogicalRefinement()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&ground30MemoryWriter==null,
                "Idle checked Editor without another source collector required");
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            string output=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_OUTPUT");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output)&&Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&
                !File.Exists(output),"Fresh persistent one-mission output required");
            var authority=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().SingleOrDefault(r=>(string)r["catalog_id"]==id);
            Require(authority!=null,"One exact Full120 catalog ID required");
            var report=new JObject{["schema"]=1,["catalogId"]=id,["scope"]="NormalAllocatorLogicalAuthoring_NotMissionAcceptance",
                ["fullIndexSha256"]=All120RefinementIndexSha,["sourceFitCertified"]=false,["cameraAccepted"]=false,
                ["normalMissionReady"]=false,["result"]="Incomplete"};
            try
            {
                if(authority["protected_binding"].Type!=JTokenType.Null)
                {
                    report["protectedBinding"]=authority["protected_binding"].DeepClone();
                    report["familySourceApplied"]=false;report["nativeSourceIdentityMeasured"]=false;
                    report["result"]="ProtectedReadOnly_NoGenericAuthoring";
                }
                else CollectAll120Refinement(new[]{authority},report,output+".authoring");
            }
            catch(Exception failure){report["error"]=failure.ToString();throw;}
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                {writer.Write(report.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
            }
            Debug.Log("[All120SimultaneousLogicalRefinement] result=Collected catalog="+id+" sourceFitCertified=0 output="+output);
        }

        public static void RunAll120SourceGroupLogicalRefinement()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&ground30MemoryWriter==null,"Idle exclusive source collector required");
            string group=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_SOURCE_GROUP");
            string output=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_OUTPUT");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(new[]{"CC","AP","MP","IB"}.Contains(group)&&!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output)&&
                Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),"One exact source group and fresh persistent output required");
            var rows=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().Where(r=>r["protected_binding"].Type==JTokenType.Null)
                .Where(r=>{ResolveAll120RefinementCandidate(r,out _,out var template);return (string)template["collector_source_group"]==group;}).ToArray();
            Require(rows.Length>0&&rows.Length<=48,"One bounded physical-source batch required");
            var report=new JObject{["schema"]=1,["scope"]="SequentialOneSourceLogicalAuthoring_NotMissionAcceptance",["sourceGroup"]=group,
                ["catalogIds"]=new JArray(rows.Select(r=>(string)r["catalog_id"])),["sourceFitCertified"]=false,["cameraAccepted"]=false,["normalMissionReady"]=false,["result"]="Incomplete"};
            try{CollectAll120Refinement(rows,report,output+".authoring");}
            catch(Exception failure){report["error"]=failure.ToString();throw;}
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                {writer.Write(report.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
            }
            Debug.Log("[All120SourceGroupLogicalRefinement] result=Collected source="+group+" missions="+rows.Length+" sourceWorlds=1 sourceFitCertified=0 output="+output);
        }

        static void ResolveAll120RefinementCandidate(JObject authority,out JObject candidate,out JObject template,bool counterpart=false)
        {
            string id=(string)authority["catalog_id"];
            bool air=(string)authority["tuple"]["army_profile"]!="G";
            bool groundBa=!air&&(string)authority["tuple"]["objective_id"]=="BA";
            candidate=authority;
            if(air)
            {
                candidate=((JArray)ReadAirCombined80Envelope()["missions"]).OfType<JObject>().Single(r=>(string)r["catalog_id"]==id);
                string templateId=(string)candidate["ground_template_catalog_id"];
                template=((JArray)ReadGround30Envelope()["missions"]).OfType<JObject>().Single(r=>(string)r["catalog_id"]==templateId);
            }
            else if(groundBa)template=GroundBaTemplate(authority,counterpart);
            else
            {
                candidate=((JArray)ReadGround30Envelope()["missions"]).OfType<JObject>().Single(r=>(string)r["catalog_id"]==id);
                template=candidate;
            }
        }

        static void CollectAll120Refinement(JObject[] authorities,JObject report,string artifactPrefix,bool counterpart=false)
        {
            Require(authorities.Length>0&&authorities.Length<=48,"Bounded sequential source rows required");
            ResolveAll120RefinementCandidate(authorities[0],out _,out var template,counterpart);
            var pin=(JObject)template["source_candidate"];
            foreach(var authority in authorities)
            {
                ResolveAll120RefinementCandidate(authority,out _,out var sameSource,counterpart);
                Require(JToken.DeepEquals(pin,sameSource["source_candidate"]),"Every sequential candidate must use the exact same physical source pins");
            }
            string path=(string)pin["sourcePath"],gridPath=(string)pin["gridPath"],surfacePath=(string)pin["surfacePath"];
            Require(Hash(path)==(string)pin["sourceAssetSha256"]&&Hash(gridPath)==(string)pin["gridAssetSha256"]&&
                Hash(surfacePath)==(string)pin["surfaceAssetSha256"],"Exact source/grid/surface bytes required");
            var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
            Require(source!=null&&!EditorUtility.IsDirty(source)&&source.SourceIdentityHash==(string)pin["sourceIdentityHash"]&&
                source.ContentHash==(string)pin["contentHash"]&&source.OperationMapId==(string)pin["operationMapId"]&&
                source.SourceSceneReference.AssetGUID==(string)pin["sourceSceneGuid"]&&
                source.NavigationMetadata.AuthoredSubSceneGuid==(string)pin["authoredSubSceneGuid"],"Exact clean Campaign source identity required");
            Require(AssetDatabase.GUIDToAssetPath(source.GridMetadata.AssetGuid)==gridPath&&source.MapSurfaceDataReference!=null&&
                AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID)==surfacePath,"Actual source grid/surface references differ");
            report["sourcePins"]=pin.DeepClone();report["companionPrewarm"]=Ground30PrewarmCompanions();
            var scenes=Ground30SceneOwnership();var worlds=CaptureMaterializationWorldCensus();
            var previousDefault=World.DefaultGameObjectInjectionWorld;
            try
            {
                using(var world=StreamingWorld())
                {
                    Load(world,source.NavigationMetadata.AuthoredSubSceneGuid);
                    Load(world,AssetDatabase.AssetPathToGUID(Support));
                    using var runtimeSurface=PublishMissingPinnedSurface(world,source,surfacePath,report);
                    var em=world.EntityManager;
                    using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(GridRoad));
                    using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
                    Require(grids.CalculateEntityCount()==1&&surfaces.CalculateEntityCount()==1,"Actual unique native grid/surface required");
                    var gridEntity=grids.GetSingletonEntity();var grid=grids.GetSingleton<GridConfig>();var surface=surfaces.GetSingleton<MapSurfaceComponent>();
                    Require(grid.Width==source.GridMetadata.Dimensions.x&&grid.Height==source.GridMetadata.Dimensions.y&&
                        grid.CellSize==source.GridMetadata.CellSize&&math.all(grid.Origin==(float3)source.GridMetadata.Origin)&&
                        surface.HasSurfaceData!=0&&surface.SurfaceBlob.IsCreated&&surface.Dimensions.Equals(new int2(grid.Width,grid.Height))&&
                        surface.CellSize==grid.CellSize&&math.all(surface.GridOrigin==grid.Origin),"Actual source grid/surface metadata differs");
                    var collected=new JArray();report["missions"]=collected;var artifacts=new JArray();report["missionArtifacts"]=artifacts;
                    var failures=new System.Collections.Generic.List<Exception>();
                    foreach(var authority in authorities)
                    {
                        ResolveAll120RefinementCandidate(authority,out var candidate,out var candidateTemplate,counterpart);
                        string id=(string)authority["catalog_id"];
                        bool air=(string)authority["tuple"]["army_profile"]!="G";
                        bool groundBa=!air&&(string)authority["tuple"]["objective_id"]=="BA";
                        var row=new JObject{["catalogId"]=id,["result"]="Incomplete",["managedBytesBefore"]=GC.GetTotalMemory(false)};collected.Add(row);
                        GroundObjective30CandidateOwner owner=null;AirCombined80CandidateOwner airOwner=null;
                        try
                        {
                            string error;
                            if(air)
                            {
                                Require(TryCreateAirCombined80Candidate(candidate,source,out airOwner,out error),error);
                                owner=new GroundObjective30CandidateOwner{Definition=airOwner.Definition,Layout=airOwner.Layout,AreaDraft=airOwner.AreaDraft};
                            }
                            else if(groundBa)Require(counterpart?TryCreateProtectedExpandedBaseAssaultCandidate(candidate,source,out owner,out error):TryCreateGroundBa10Candidate(candidate,source,out owner,out error),error);
                            else Require(TryCreateGroundObjective30Candidate(candidate,source,out owner,out error),error);
                            var sizeArtifacts=new JArray();row["sizeArtifacts"]=sizeArtifacts;
                            row["measurement"]=RefineLoadedCandidate(All120MeasurementMission(candidate,candidateTemplate),owner,source,em,gridEntity,grid,surface,artifactPrefix+"."+id,checkpoint=>
                            {
                                string sizePath=artifactPrefix+"."+id+".pass"+(int)checkpoint["pass"]+"."+(string)checkpoint["size"]+".size.json";
                                Directory.CreateDirectory(Path.GetDirectoryName(sizePath));
                                using(var file=new FileStream(sizePath,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                                using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                                {writer.Write(checkpoint.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
                                sizeArtifacts.Add(new JObject{["path"]=sizePath,["sha256"]=Hash(sizePath),["size"]=checkpoint["size"],["pass"]=checkpoint["pass"]});
                            });
                            row["result"]="CollectedUnqualified";
                        }
                        catch(Exception failure){row["error"]=failure.ToString();failures.Add(new InvalidOperationException("Logical authoring failed: "+id,failure));}
                        finally
                        {
                            owner?.Dispose();airOwner?.Dispose();row["managedBytesAfterDispose"]=GC.GetTotalMemory(false);
                            string durable=artifactPrefix+"."+id+".mission.json";Directory.CreateDirectory(Path.GetDirectoryName(durable));
                            using(var file=new FileStream(durable,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                            using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                            {writer.Write(row.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
                            artifacts.Add(new JObject{["catalogId"]=id,["path"]=durable,["sha256"]=Hash(durable)});
                        }
                    }
                    if(failures.Count!=0)throw new AggregateException("Independent one-source logical authoring failures",failures);
                }
                Require(!EditorUtility.IsDirty(source)&&Hash(path)==(string)pin["sourceAssetSha256"]&&
                    Hash(gridPath)==(string)pin["gridAssetSha256"]&&Hash(surfacePath)==(string)pin["surfaceAssetSha256"]&&
                    source.SourceIdentityHash==(string)pin["sourceIdentityHash"]&&source.ContentHash==(string)pin["contentHash"]&&
                    source.OperationMapId==(string)pin["operationMapId"]&&source.SourceSceneReference.AssetGUID==(string)pin["sourceSceneGuid"]&&
                    source.NavigationMetadata.AuthoredSubSceneGuid==(string)pin["authoredSubSceneGuid"]&&
                    AssetDatabase.GUIDToAssetPath(source.GridMetadata.AssetGuid)==gridPath&&source.MapSurfaceDataReference!=null&&
                    AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID)==surfacePath,"Loaded Campaign source/grid/surface identity or bytes drifted during refinement");
                report["sourcePinsRecheckedAfterMeasurement"]=true;report["result"]="CollectedUnqualified";
            }
            finally
            {
                bool restored=worlds.SequenceEqual(CaptureMaterializationWorldCensus())&&ReferenceEquals(previousDefault,World.DefaultGameObjectInjectionWorld)&&
                    JToken.DeepEquals(scenes,Ground30SceneOwnership());
                report["cleanupVerified"]=restored;
                Require(restored,"Exact one-source World/default/scene/root cleanup required");
            }
        }
    }
}
#endif
