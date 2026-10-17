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

        // Exact caller-selected subset, same existing one-source collector, no fit promotion.
        // One native source world, two unqualified logical candidates. The checked
        // host must separately run strict_ap_postrun; Collected is never fit credit.
        public static void RunApGroundPairStaticCollection()
        {
            Require(Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_SOURCE_GROUP")=="AP"&&
                Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG_IDS")=="S097,S098",
                "Exact requested AP pair required; no broad-source collection");
            RunAll120RequestedSourceCollectionControls();
            RunAuthoredRouteEndpointPreflightControls();
            RunMpGroundBaZeroDistanceQueryControls();
            RunMpGroundBaServiceRegionControls();
            RunAll120RequestedSourceLogicalRefinement();
            Debug.Log("[APGroundPairStaticCollection] result=Collected requestedMissions=2 requestedSizeVectors=6 staticAdmission=Pending sourceFitCertified=0 normalMissionReady=0");
        }

        static void RequireDurableRequestedMission(JObject mission,JObject artifact,string expectedPath)
        {
            Require((string)artifact["path"]==expectedPath&&Hash((string)artifact["path"])==(string)artifact["sha256"]&&
                string.Equals(mission.ToString(Newtonsoft.Json.Formatting.None),File.ReadAllText((string)artifact["path"]),StringComparison.Ordinal),
                "Exact durable requested mission artifact required");
        }

        public static void RunRequestedDurableMissionSerializationControls()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Idle serialization controls required");
            string root=Path.Combine(Path.GetTempPath(),"warline-durable-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);int controls=0;
            try
            {
                string path=Path.Combine(root,"mission.json");
                var mission=new JObject{["catalogId"]="S097",["single"]=(float)474.4,["double"]=(double)474.4,
                    ["rows"]=new JArray(new JObject{["x"]=(float)0.1,["z"]=(double)0.1})};
                void Write(JObject value)=>File.WriteAllText(path,value.ToString(Newtonsoft.Json.Formatting.None),new System.Text.UTF8Encoding(false));
                JObject Artifact()=>new JObject{["path"]=path,["sha256"]=Hash(path)};
                void Refused(JObject wanted,JObject artifact,string expected)
                {
                    bool refused=false;try{RequireDurableRequestedMission(wanted,artifact,expected);}
                    catch(InvalidOperationException){refused=true;}
                    Require(refused,"Changed/spliced durable payload must refuse");controls++;
                }
                Write(mission);var original=Artifact();RequireDurableRequestedMission(mission,original,path);controls++;
                bool inMemoryDeepEquals=JToken.DeepEquals(mission,JObject.Parse(File.ReadAllText(path)));
                RequireDurableRequestedMission(JObject.Parse(File.ReadAllText(path)),original,path);controls++;
                var changed=(JObject)mission.DeepClone();changed["single"]=475.4;Write(changed);Refused(mission,Artifact(),path);
                changed=(JObject)mission.DeepClone();changed["extra"]=true;Write(changed);Refused(mission,Artifact(),path);
                changed=(JObject)mission.DeepClone();changed["catalogId"]="S098";Write(changed);Refused(mission,Artifact(),path);
                Write(mission);Refused(mission,original,Path.Combine(root,"foreign.json"));
                var bad=Artifact();bad["sha256"]=new string('0',64);Refused(mission,bad,path);
                File.AppendAllText(path," ");Refused(mission,Artifact(),path);
                Require(controls==8,"Exact serialization controls count");
                Debug.Log("[RequestedDurableSerializationControls] result=Passed controls=8 floatDoubleRoundtripDeepEquals="+inMemoryDeepEquals+" tolerance=0 nativePlacementProved=0");
            }
            finally
            {
                Require((File.GetAttributes(root)&FileAttributes.ReparsePoint)==0,"Owned ordinary fixture root required");
                Directory.Delete(root,true);
            }
        }

        public static void RunAll120RequestedSourceLogicalRefinement()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&
                !EditorApplication.isUpdating&&ground30MemoryWriter==null,"Idle exclusive source collector required");
            string group=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_SOURCE_GROUP");
            string requested=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG_IDS");
            string output=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_OUTPUT");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output)&&
                Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),
                "Fresh persistent requested-source output required");
            var rows=ResolveRequestedSourceRows((JArray)ReadAll120RefinementIndex()["missions"],group,requested);
            string[] ids=rows.Select(r=>(string)r["catalog_id"]).ToArray();
            var report=new JObject{["schema"]=1,["scope"]="RequestedSequentialOneSourceLogicalAuthoring_NotMissionAcceptance",
                ["sourceGroup"]=group,["requestedCatalogIds"]=new JArray(ids),["requestedMissionCount"]=ids.Length,
                ["expectedDistinctSizeRows"]=ids.Length*3,["sourceFitCertified"]=false,["cameraAccepted"]=false,
                ["normalMissionReady"]=false,["result"]="Incomplete"};
            try
            {
                CollectAll120Refinement(rows,report,output+".authoring");
                ValidateRequestedSourceCollectionShape(report,ids,group);
                foreach(JObject mission in (JArray)report["missions"])
                {
                    string id=(string)mission["catalogId"];
                    var artifact=((JArray)report["missionArtifacts"]).OfType<JObject>().Single(a=>(string)a["catalogId"]==id);
                    RequireDurableRequestedMission(mission,artifact,output+".authoring."+id+".mission.json");
                    var sizeArtifacts=((JArray)mission["sizeArtifacts"]).OfType<JObject>().ToArray();
                    Require(sizeArtifacts.Select(s=>(string)s["size"]).Distinct().OrderBy(v=>v,StringComparer.Ordinal)
                        .SequenceEqual(new[]{"LargeWar","Standard","War"}),"Every requested mission needs all3 durable size rows");
                    foreach(var a in sizeArtifacts)
                    {
                        string size=(string)a["size"];int pass=(int)a["pass"];
                        Require((string)a["path"]==output+".authoring."+id+".pass"+pass+"."+size+".size.json"&&
                            Hash((string)a["path"])==(string)a["sha256"],"Durable requested pass/size artifact drift");
                        var fact=JObject.Parse(File.ReadAllText((string)a["path"]));
                        Require((string)fact["catalogId"]==id&&(string)fact["size"]==size&&(int)fact["pass"]==pass,
                            "Actual durable checkpoint identity differs");
                    }
                }
                report["requestedSetAndSizeCountsVerified"]=true;
            }
            catch(Exception failure){report["error"]=failure.ToString();report["result"]="FailedRequestedCollection";throw;}
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                {writer.Write(report.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
            }
            Debug.Log("[All120RequestedSourceLogicalRefinement] result=CollectedUnqualified source="+group+
                " missions="+ids.Length+" distinctSizeRows="+(ids.Length*3)+" sourceWorlds=1 sourceFitCertified=0 cameraAccepted=0 normalMissionReady=0");
        }

        static JObject[] ResolveRequestedSourceRows(JArray authority,string group,string text)
        {
            Require(new[]{"CC","AP","MP","IB"}.Contains(group)&&!string.IsNullOrWhiteSpace(text),"Exact source group/requested IDs required");
            string[] ids=text.Split(',').Select(v=>v.Trim()).ToArray();
            Require(ids.Length>0&&ids.Length<=48&&ids.Distinct(StringComparer.Ordinal).Count()==ids.Length&&
                ids.All(SkirmishMissionDispatch.Canonical),"Unique nonempty canonical requested IDs required");
            var rows=new System.Collections.Generic.List<JObject>();JToken source=null;
            foreach(string id in ids)
            {
                var row=authority.OfType<JObject>().SingleOrDefault(r=>(string)r["catalog_id"]==id);
                Require(row!=null&&row["protected_binding"]!=null&&row["protected_binding"].Type==JTokenType.Null,
                    "Unknown/protected catalog cannot enter generic requested collection: "+id);
                ResolveAll120RefinementCandidate(row,out _,out var template);
                Require((string)template["collector_source_group"]==group,"Requested catalog belongs to another source group: "+id);
                if(source==null)source=template["source_candidate"].DeepClone();
                else Require(JToken.DeepEquals(source,template["source_candidate"]),"Requested missions do not share exact source pins");
                rows.Add(row);
            }
            return rows.ToArray();
        }

        static void ValidateRequestedSourceCollectionShape(JObject report,string[] ids,string group)
        {
            Require(ids.Length>0&&ids.Distinct(StringComparer.Ordinal).Count()==ids.Length&&
                (string)report["sourceGroup"]==group&&(string)report["result"]=="CollectedUnqualified"&&
                (bool?)report["cleanupVerified"]==true&&(bool?)report["sourcePinsRecheckedAfterMeasurement"]==true,
                "Completed source identity/cleanup required");
            var missions=((JArray)report["missions"]).OfType<JObject>().ToArray();
            var artifacts=((JArray)report["missionArtifacts"]).OfType<JObject>().ToArray();
            Require(((JArray)report["missions"]).Count==missions.Length&&((JArray)report["missionArtifacts"]).Count==artifacts.Length&&
                missions.Length==ids.Length&&artifacts.Length==ids.Length&&
                missions.Select(m=>(string)m["catalogId"]).SequenceEqual(ids)&&
                artifacts.Select(m=>(string)m["catalogId"]).SequenceEqual(ids),
                "Every requested catalog exactly once, in requested order, without foreign/omitted results");
            foreach(var mission in missions)
            {
                Require((string)mission["result"]=="CollectedUnqualified"&&mission["error"]==null,
                    "Failed requested mission cannot be counted");
                var passes=((JArray)mission["measurement"]["passes"]).OfType<JObject>().ToArray();
                Require(passes.Length>0&&((JArray)mission["measurement"]["passes"]).Count==passes.Length&&
                    passes.Select(p=>(int)p["pass"]).Distinct().Count()==passes.Length,
                    "At least one unique measured pass required");
                foreach(var pass in passes)
                {
                    var sizes=((JArray)pass["sizes"]).OfType<JObject>().ToArray();
                    Require(sizes.Length==3&&((JArray)pass["sizes"]).Count==3&&
                        sizes.All(s=>(string)s["catalogId"]==(string)mission["catalogId"])&&
                        sizes.Select(s=>(string)s["size"]).OrderBy(v=>v,StringComparer.Ordinal)
                        .SequenceEqual(new[]{"LargeWar","Standard","War"}),
                        "Every measured pass must contain exactly3 canonical size rows; no duplicate/missing size success");
                }
            }
        }

        public static void RunAll120RequestedSourceCollectionControls()
        {
            var authority=(JArray)ReadAll120RefinementIndex()["missions"];int controls=0;
            void Check(bool value,string label){Require(value,label);controls++;}
            void Refuses(Action action,string label)
            {bool refused=false;try{action();}catch(InvalidOperationException){refused=true;}Check(refused,label);}
            var pair=ResolveRequestedSourceRows(authority,"AP","S097,S098");
            Check(pair.Select(r=>(string)r["catalog_id"]).SequenceEqual(new[]{"S097","S098"}),"Actual AP pair preserved");
            var ground=ResolveRequestedSourceRows(authority,"AP","S097,S098,S103,S104,S109,S110,S115,S116");
            Check(ground.Length==8,"Actual AP ground8 source-set admitted without loading World");
            Refuses(()=>ResolveRequestedSourceRows(authority,"AP","S121"),"Unknown ID refused");
            Refuses(()=>ResolveRequestedSourceRows(authority,"AP","S004"),"Protected ID refused");
            Refuses(()=>ResolveRequestedSourceRows(authority,"AP","S097,S097"),"Duplicate ID refused");
            Refuses(()=>ResolveRequestedSourceRows(authority,"MP","S097,S098"),"Wrong source group refused");
            Refuses(()=>ResolveRequestedSourceRows(authority,"AP","S097,"),"Blank ID refused");
            JObject Synthetic()
            {
                var missions=new JArray();var artifacts=new JArray();
                foreach(string id in new[]{"S097","S098"})
                {
                    missions.Add(new JObject{["catalogId"]=id,["result"]="CollectedUnqualified",
                        ["measurement"]=new JObject{["passes"]=new JArray(new JObject{["pass"]=0,
                            ["sizes"]=new JArray(new[]{"Standard","War","LargeWar"}.Select(size=>new JObject{["catalogId"]=id,["size"]=size}))})}});
                    artifacts.Add(new JObject{["catalogId"]=id});
                }
                return new JObject{["sourceGroup"]="AP",["result"]="CollectedUnqualified",["cleanupVerified"]=true,
                    ["sourcePinsRecheckedAfterMeasurement"]=true,["missions"]=missions,["missionArtifacts"]=artifacts};
            }
            string[] ids={"S097","S098"};var report=Synthetic();
            ValidateRequestedSourceCollectionShape(report,ids,"AP");Check(true,"Exact requested shape accepted without fit claim");
            report["missions"][1]["catalogId"]="S103";
            Refuses(()=>ValidateRequestedSourceCollectionShape(report,ids,"AP"),"Requested set mismatch refused");
            report=Synthetic();((JArray)report["missions"][0]["measurement"]["passes"][0]["sizes"]).RemoveAt(2);
            Refuses(()=>ValidateRequestedSourceCollectionShape(report,ids,"AP"),"Missing size refused");
            report=Synthetic();report["missions"][0]["measurement"]["passes"][0]["sizes"][2]["size"]="War";
            Refuses(()=>ValidateRequestedSourceCollectionShape(report,ids,"AP"),"Duplicate size refused");
            report=Synthetic();report["cleanupVerified"]=false;
            Refuses(()=>ValidateRequestedSourceCollectionShape(report,ids,"AP"),"Incomplete cleanup refused");
            Debug.Log("[All120RequestedSourceCollectionControls] result=Passed controls="+controls+" nativeQueries=0 sourceWorlds=0 sourceFitCertified=0 missionReady=0");
        }

        public static void RunAll120MeasurementAuthorityControls()
        {
            int controls=0;
            void Check(bool ok,string message){Require(ok,message);controls++;}
            var authority=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().Single(r=>(string)r["catalog_id"]=="S007");
            ResolveAll120RefinementCandidate(authority,out var candidate,out var template);
            string authorityBefore=authority.ToString(),candidateBefore=candidate.ToString(),templateBefore=template.ToString();
            Check(candidate["required_capabilities"]==null&&authority["required_capabilities"] is JArray,"Actual pinned Ground30 capability omission must reproduce the failed call contract");
            var row=All120MeasurementMission(authority,template);
            string[] keys={"catalog_id","definition_id","scenario_setup_id","tuple","canonical_size_rows","required_capabilities"};
            Check(keys.All(k=>authority[k]!=null&&JToken.DeepEquals(row[k],authority[k])&&!ReferenceEquals(row[k],authority[k])),"All six Full120 authority fields must be independent exact clones");
            Check(((JArray)row["canonical_size_rows"]).Count==3&&JToken.DeepEquals(row["canonical_size_rows"],candidate["canonical_size_rows"]),"All three unchanged actual canonical budget vectors required");
            Check(template.Properties().Where(v=>!keys.Contains(v.Name)).All(v=>JToken.DeepEquals(row[v.Name],v.Value)),"Every template source/search/placement field must remain exact");
            ((JArray)row["required_capabilities"]).Add("control.mutation");row["tuple"]["objective_id"]="control.mutation";
            Check(authority.ToString()==authorityBefore&&candidate.ToString()==candidateBefore&&template.ToString()==templateBefore,"Measurement mutation must not alter authority or candidate/template");
            Debug.Log("[All120MeasurementAuthorityControls] result=Passed controls="+controls+" nativeQueries=0 sourceFitCertified=0 missionReady=0");
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
                    report["nativeGridIdentity"]=new JObject{["sourcePins"]=pin.DeepClone(),["grid"]=new JObject{
                        ["width"]=grid.Width,["height"]=grid.Height,["cellSize"]=grid.CellSize,
                        ["originXZ"]=new JArray(grid.Origin.x,grid.Origin.z)}};
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
                            row["measurement"]=RefineLoadedCandidate(All120MeasurementMission(authority,candidateTemplate),owner,source,em,gridEntity,grid,surface,artifactPrefix+"."+id,checkpoint=>
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
