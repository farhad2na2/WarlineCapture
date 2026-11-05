#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Game.Skirmish.Contracts;
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
            if(recordedFacilityEgressInput!=null){report["scope"]="RecordedCompleteFacilities_EgressEdgesOnly";report["historicalReport"]=recordedFacilityEgressInput["sourceReport"].DeepClone();report["facilityMeasurements"]=0;report["fullRoutesMeasured"]=false;}
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
                var measurement=mission["measurement"] as JObject;
                var passRows=measurement?["passes"] as JArray;
                var routeSeed=measurement?["apJointRouteSeed"] as JObject;
                var refusal=measurement?["refused"]??routeSeed?["refused"];
                Require(passRows!=null,"Requested mission "+(string)mission["catalogId"]+
                    " has no measured passes; "+(refusal?.ToString(Newtonsoft.Json.Formatting.None)??"measurement/passes missing or not an array")+
                    "; route diagnostics are not three-size measurements");
                var passes=passRows.OfType<JObject>().ToArray();
                Require(passes.Length>0&&passRows.Count==passes.Length&&
                    passes.Select(p=>(int)p["pass"]).Distinct().Count()==passes.Length,
                    "At least one unique measured pass required");
                foreach(var pass in passes)
                {
                    var sizeRows=pass["sizes"] as JArray;
                    Require(sizeRows!=null,"Every measured pass needs an actual size-row array");
                    var sizes=sizeRows.OfType<JObject>().ToArray();
                    Require(sizes.Length==3&&sizeRows.Count==3&&
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

        public static void RunRequestedMeasurementShapeControls()
        {
            int controls=0;string[] ids={"S097","S098"};
            JObject Sample()
            {
                var missions=new JArray();var artifacts=new JArray();
                foreach(string id in ids)
                {
                    missions.Add(new JObject{["catalogId"]=id,["result"]="CollectedUnqualified",
                        ["measurement"]=new JObject{["passes"]=new JArray(new JObject{["pass"]=0,
                            ["sizes"]=new JArray(new[]{"Standard","War","LargeWar"}.Select(size=>new JObject{["catalogId"]=id,["size"]=size}))})}});
                    artifacts.Add(new JObject{["catalogId"]=id});
                }
                return new JObject{["sourceGroup"]="AP",["result"]="CollectedUnqualified",["cleanupVerified"]=true,
                    ["sourcePinsRecheckedAfterMeasurement"]=true,["missions"]=missions,["missionArtifacts"]=artifacts};
            }
            void Refuses(JObject report,string expected)
            {
                bool refused=false;
                try{ValidateRequestedSourceCollectionShape(report,ids,"AP");}
                catch(InvalidOperationException e){refused=e.Message.Contains(expected);}
                Require(refused,"Exact fail-closed measurement diagnostic required: "+expected);controls++;
            }
            var report=Sample();ValidateRequestedSourceCollectionShape(report,ids,"AP");controls++;
            report=Sample();report["missions"][0]["measurement"]=new JObject{
                ["apJointRouteSeed"]=new JObject{["trials"]=new JArray(0,1,2,3),
                    ["refused"]="Four explicit logical endpoint seeds exhausted; no legal source fit inferred"}};
            Refuses(report,"Four explicit logical endpoint seeds exhausted");
            report=Sample();((JObject)report["missions"][0]).Remove("measurement");Refuses(report,"no measured passes");
            report=Sample();report["missions"][0]["measurement"]["passes"]=1;Refuses(report,"no measured passes");
            report=Sample();report["missions"][0]["measurement"]["passes"]=new JArray();Refuses(report,"unique measured pass");
            report=Sample();report["missions"][0]["measurement"]["passes"]=new JArray(1);Refuses(report,"unique measured pass");
            report=Sample();var passes=(JArray)report["missions"][0]["measurement"]["passes"];passes.Add(passes[0].DeepClone());Refuses(report,"unique measured pass");
            report=Sample();((JObject)report["missions"][0]["measurement"]["passes"][0]).Remove("sizes");Refuses(report,"size-row array");
            report=Sample();((JArray)report["missions"][0]["measurement"]["passes"][0]["sizes"]).RemoveAt(2);Refuses(report,"exactly3 canonical size");
            report=Sample();report["missions"][0]["measurement"]["passes"][0]["sizes"][2]["size"]="War";Refuses(report,"exactly3 canonical size");
            Require(controls==10,"Exact measurement-shape control count required");
            Debug.Log("[RequestedMeasurementShapeControls] result=Passed controls=10 nativeQueries=0 sourceFitCertified=0 missionReady=0");
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


        static JObject recordedFacilityEgressInput;
        // Diagnostic only: shares the exact source loader and canonical profiles,
        // but never enters facility discovery or emits a three-size checkpoint.
        public static void RunApCoherentRouteEdgeProbe()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&
                !EditorApplication.isUpdating&&ground30MemoryWriter==null,"Idle exclusive AP route probe required");
            string output=Environment.GetEnvironmentVariable("WARLINE_AP_COHERENT_ROUTE_PROBE_OUTPUT");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(output)&&Path.IsPathRooted(output)&&
                Path.GetFullPath(output).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&!File.Exists(output),"Fresh owned route-probe output required");
            var report=new JObject{["schema"]=1,["scope"]="APCoherentNativeRouteEdgesOnly_NotFacilityOrSourceFit",
                ["sourceGroup"]="AP",["requestedCatalogIds"]=new JArray("S097","S098"),["result"]="Incomplete",
                ["sourceFitCertified"]=false,["cameraAccepted"]=false,["normalMissionReady"]=false,["facilityMeasurements"]=0};
            try
            {
                RunRequestedMeasurementShapeControls();
                var rows=ResolveRequestedSourceRows((JArray)ReadAll120RefinementIndex()["missions"],"AP","S097,S098");
                WithApJointPreferenceMode(()=>CollectAll120Refinement(rows,report,output+".route-probe",apRouteProbeOnly:true));
                Require((bool?)report["cleanupVerified"]==true&&(bool?)report["sourcePinsRecheckedAfterMeasurement"]==true,"Actual route probe cleanup/source verification required");
                var missions=(JArray)report["missions"];
                Require(missions.Count==2&&missions.OfType<JObject>().Select(m=>(string)m["catalogId"]).SequenceEqual(new[]{"S097","S098"}),"Exact AP pair measured in order");
                foreach(JObject mission in missions)
                {
                    Require(((JArray)mission["sizeArtifacts"]).Count==0&&mission["measurement"]["passes"]==null,"Route-only probe must not create or invent size rows");
                    var artifact=((JArray)report["missionArtifacts"]).OfType<JObject>().Single(a=>(string)a["catalogId"]==(string)mission["catalogId"]);
                    RequireDurableRequestedMission(mission,artifact,output+".route-probe."+(string)mission["catalogId"]+".mission.json");
                }
                report["result"]="CollectedRouteEdges_NoFitOrAcceptance";
            }
            catch(Exception error){report["result"]="FailedRouteEdgeProbe";report["error"]=error.ToString();throw;}
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                {writer.Write(report.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
            }
            Debug.Log("[APCoherentRouteEdgeProbe] result=Collected missions=2 configurations=4 facilityMeasurements=0 sourceFitCertified=0 cameraAccepted=0 normalMissionReady=0");
        }

        static JObject MeasureApCoherentRouteEdges(JObject mission,GroundObjective30CandidateOwner owner,OperationMapDefinition source,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface)
        {
            Require(apGroundJointPreferencesRequested&&new[]{"S097","S098"}.Contains(owner.Definition.CatalogId)&&
                owner.Definition.CampaignMap==source&&owner.Definition.MapLayout==owner.Layout,"Exact owned AP source/definition/layout required");
            Require(grid.Width==2048&&grid.Height==1024&&grid.CellSize==1&&math.all(grid.Origin==float3.zero),"Exact source-pinned measured AP grid required");
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            var budgets=new System.Collections.Generic.List<SkirmishStaticAuthoringBudget>();
            foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
            {
                Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                    new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var budget,out var reasons)&&budget.ExactCanonicalQuantities,
                    "Exact canonical route probe budget required: "+string.Join(";",reasons));budgets.Add(budget);
            }
            string original=JsonUtility.ToJson(owner.Layout);
            try
            {
                var compounds=CandidateCompoundRects((JObject)mission["layout_input"]["pilot_candidate"]);
                Require(TryNextApJointLogicalFrame(original,compounds,owner.Layout,source,grid,out _,out error),error);
                var padEvidence=ApplyApJointPadInput(mission,owner.Layout,source,grid,budgets);
                string prepared=JsonUtility.ToJson(owner.Layout);
                var profiles=new System.Collections.Generic.List<RouteProfile>();var keys=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);var profileFacts=new JArray();
                foreach(var force in budgets.SelectMany(b=>b.Forces))
                {
                    Entity prefab=FindNativePrefab(em,force.RuntimePrefabKey);
                    Require(prefab!=Entity.Null&&em.HasComponent<UnitFootprint>(prefab),"Actual canonical native route prefab required");
                    if(em.HasComponent<UnitAirMovement>(prefab))continue;
                    Require(em.HasComponent<UnitMovementBehavior>(prefab),"Actual canonical native ground behavior required");
                    var footprint=em.GetComponentData<UnitFootprint>(prefab).Size;var behavior=em.GetComponentData<UnitMovementBehavior>(prefab);
                    string key=Ground30PathQueryKey(force.FactionId,footprint,behavior,default,default);
                    if(!keys.Add(key))continue;
                    profiles.Add(new RouteProfile{Prefab=prefab,Faction=force.FactionId,Size=footprint,Behavior=behavior});
                    profileFacts.Add(new JObject{["key"]=key,["faction"]=force.FactionId,["roleId"]=force.RoleId,["runtimePrefabKey"]=force.RuntimePrefabKey,
                        ["footprintX"]=footprint.x,["footprintZ"]=footprint.y,["usesVehicleMotion"]=behavior.UsesVehicleMotion});
                }
                Require(profiles.Count>0&&profiles.Count<=16,"Bounded actual canonical ground profile set required");
                var portals=new[]{new int2(513,532),new int2(529,532),new int2(545,500),new int2(561,516)};
                var offsets=new[]{new Vector2(-32,0),new Vector2(0,32),new Vector2(0,-32),new Vector2(32,0)};
                var trials=new JArray();
                for(int candidate=0;candidate<4;candidate++)
                {
                    Require(TryEncodeJointPadTranslation(prepared,offsets[candidate],true,out string translated,out error),error);
                    JsonUtility.FromJsonOverwrite(translated,owner.Layout);
                    Require(owner.Layout.TryGetPad(2,SkirmishLegalPadKind.GroundStaging,out var enemyPad),"Actual translated enemy staging pad required");
                    Require(owner.Layout.TryGetAnchorById(enemyPad.AnchorId,out var enemyAnchor),"Actual translated enemy staging endpoint required");
                    int2 enemy=new int2((int)Math.Floor((enemyAnchor.WorldX-grid.Origin.x)/grid.CellSize),(int)Math.Floor((enemyAnchor.WorldZ-grid.Origin.z)/grid.CellSize));
                    var from=new[]{new int2(569,540),new int2(677,540),new int2(783,540),new int2(805,540),new int2(485,540),new int2(489,558),new int2(485,540),portals[candidate]};
                    var to=new[]{new int2(677,540),new int2(783,540),new int2(805,540),enemy,new int2(489,558),new int2(497,596),portals[candidate],new int2(521,488)};
                    var queries=new JArray();var trial=new JObject{["candidate"]=candidate,["offsetX"]=offsets[candidate].x,["offsetZ"]=offsets[candidate].y,
                        ["portalCell"]=new JArray(portals[candidate].x,portals[candidate].y),["queries"]=queries,["maximumAdditionalNativeQueries"]=2048,
                        ["fullRoutesMeasured"]=false,["distinctApproachesMeasured"]=false,["sourceFitCertified"]=false};trials.Add(trial);
                    for(int edge=0;edge<from.Length;edge++)foreach(var profile in profiles)
                    {
                        foreach(var cell in new[]{from[edge],to[edge]})
                        {
                            float x=grid.Origin.x+(cell.x+.5f)*grid.CellSize,z=grid.Origin.z+(cell.y+.5f)*grid.CellSize;
                            Require(x>=source.Bounds.PlayableMin.x&&x<=source.Bounds.PlayableMax.x&&z>=source.Bounds.PlayableMin.z&&z<=source.Bounds.PlayableMax.z&&
                                x>=owner.Layout.OriginX&&x<=owner.Layout.OriginX+owner.Layout.WorldWidthMetres&&z>=owner.Layout.OriginZ&&z<=owner.Layout.OriginZ+owner.Layout.WorldDepthMetres,
                                "Route probe endpoint outside actual clipped source/logical bounds");
                        }
                        Require(queries.Count<2048,"Unchanged per-configuration native query cap");
                        int2 a=profile.Faction==2?to[edge]:from[edge],b=profile.Faction==2?from[edge]:to[edge];
                        var query=new JObject{["edge"]=edge,["profileKey"]=Ground30PathQueryKey(profile.Faction,profile.Size,profile.Behavior,default,default),
                            ["fromCell"]=new JArray(a.x,a.y),["toCell"]=new JArray(b.x,b.y),["unmeasuredBeforeThisProbe"]=true};
                        QueryPath(em,gridEntity,grid,surface,profile.Prefab,a,b,query,profile.Faction);queries.Add(query);
                    }
                    trial["allProbedEdgesExact"]=JointRoutesExact(new JObject{["queries"]=queries});
                    trial["status"]=(bool)trial["allProbedEdgesExact"]?"AllProbedEdgesExact_FullRoutesAndFitPending":"RefusedProbedEdges_NativeQueryFailed";
                    trial["actualAdditionalNativeQueries"]=queries.Count;
                }
                return new JObject{["scope"]="APCoherentNativeRouteEdgesOnly_NotFacilityOrSourceFit",["sourceFitCertified"]=false,["normalMissionReady"]=false,["cameraAccepted"]=false,
                    ["canonicalSizes"]=new JArray("Standard","War","LargeWar"),["profileFacts"]=profileFacts,["apJointPadInput"]=padEvidence,
                    ["maximumEndpointCandidates"]=4,["maximumPreferenceAssignments"]=9,["maximumNativeRepairQueriesPerCandidate"]=2048,["configurations"]=trials,
                    ["airLifecycleMeasured"]=false,["fullRoutesMeasured"]=false,["distinctApproachesMeasured"]=false,["coherentCandidateApplied"]=false,["facilityMeasurements"]=0};
            }
            finally{JsonUtility.FromJsonOverwrite(original,owner.Layout);Require(JsonUtility.ToJson(owner.Layout)==original,"Exact caller-owned layout restoration required");}
        }

        public static void RunRecordedFacilityRouteEgressProbe()
        {
            Require(recordedFacilityEgressInput==null&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&ground30MemoryWriter==null,
                "Idle exclusive recorded-facility probe required");
            string path=Environment.GetEnvironmentVariable("WARLINE_RECORDED_ROUTE_PROBE_INPUT");
            string hash=Environment.GetEnvironmentVariable("WARLINE_RECORDED_ROUTE_PROBE_SHA256");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            bool Owned(string p)=>!string.IsNullOrEmpty(p)&&Path.IsPathRooted(p)&&Path.GetFullPath(p).StartsWith(allowed,StringComparison.OrdinalIgnoreCase);
            Require(Owned(path)&&Hash(path)==hash,"Pinned owned probe input required");
            var input=JObject.Parse(File.ReadAllText(path));var reference=(JObject)input["sourceReport"];
            string priorPath=(string)reference["path"];
            Require((int?)input["schema"]==1&&(string)input["scope"]=="RecordedCompleteFacilities_EgressEdgesOnly"&&
                (bool?)input["sourceFitCertified"]==false&&(bool?)input["missionAccepted"]==false&&
                (int?)input["pass"]==2&&(int?)input["maximumEndpointCandidates"]==9&&
                (int?)input["maximumConfigurationGroups"]==3&&(int?)input["maximumNativeQueries"]==324&&Owned(priorPath)&&
                Hash(priorPath)==(string)reference["sha256"],"Exact diagnostic scope and prior report required");
            var prior=JObject.Parse(File.ReadAllText(priorPath));
            void Recheck()
            {
                Require(Hash(path)==hash&&Hash(priorPath)==(string)reference["sha256"],"Input/report changed during probe");
                Require(input["dependencyPins"] is JObject&&((JObject)input["dependencyPins"]).Count>0,"Explicit unchanged geometry dependencies required");
                foreach(var pin in ((JObject)input["dependencyPins"]).Properties())
                    Require(pin.Name.StartsWith("Assets/",StringComparison.Ordinal)&&Hash(pin.Name)==(string)pin.Value,"Geometry dependency drift: "+pin.Name);
            }
            Recheck();input["_prior"]=prior;
            Require((string)input["catalogId"]==Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG"),"Exact requested own catalog required");
            var authority=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>().Single(r=>(string)r["catalog_id"]==(string)input["catalogId"]);
            Require(authority["protected_binding"].Type==JTokenType.Null,"Protected generic authoring refused");
            try{recordedFacilityEgressInput=input;RunAll120SimultaneousLogicalRefinement();Recheck();}
            finally{recordedFacilityEgressInput=null;}
            Debug.Log("[RecordedFacilityRouteEgressProbe] result=Collected facilityMeasurements=0 fullRoutesMeasured=0 sourceFitCertified=0 normalMissionReady=0");
        }


        static JArray RecordedFacilitySignature(JObject row,SkirmishStaticAuthoringBudget budget,GridConfig grid)
        {
            Require((string)row["catalogId"]==budget.CatalogId&&(string)row["size"]==budget.Size.ToString(),"Exact recorded catalog/size required");
            Require((bool?)row["completeFacilities"]==true&&(bool?)row["allActorsInsidePads"]==true,"Complete actual typed facilities/packing required");
            var actual=row["normalFacilityRelocationProposals"] as JArray;
            Require(actual!=null&&actual.Count==budget.Structures.Count()&&actual.OfType<JObject>().Count()==actual.Count&&
                actual.OfType<JObject>().Select(x=>(int)x["faction"]+"/"+(string)x["structureId"]).Distinct().Count()==actual.Count,"Exact unique complete facility roster required");
            foreach(var structure in budget.Structures)
            {
                var x=actual.OfType<JObject>().Single(r=>(int)r["faction"]==structure.FactionId&&(string)r["structureId"]==structure.StructureId);
                Require((bool?)x["entireConfigurationFound"]==true,"Partial facility configuration refused");
                Require(Hash((string)x["prefabPath"])==(string)x["prefabSha256"],"Original facility prefab changed");
                var rect=x["resolvedEffectiveRect"] as JObject;
                Require(rect!=null&&rect.Properties().Select(p=>p.Name).OrderBy(p=>p,StringComparer.Ordinal).SequenceEqual(new[]{"depth","width","x","z"})&&
                    rect.Properties().All(p=>p.Value.Type==JTokenType.Integer),"Exact integer effective rectangle required");
                int rx=(int)rect["x"],rz=(int)rect["z"],w=(int)rect["width"],h=(int)rect["depth"];
                Require(rx>=0&&rz>=0&&w>0&&h>0&&(long)rx+w<=grid.Width&&(long)rz+h<=grid.Height,"Effective facility rectangle outside source grid");
            }
            return new JArray(actual.OfType<JObject>().OrderBy(x=>(int)x["faction"]).ThenBy(x=>(string)x["structureId"],StringComparer.Ordinal)
                .Select(x=>new JObject{["faction"]=x["faction"],["structureId"]=x["structureId"],["rect"]=x["resolvedEffectiveRect"].DeepClone()}));
        }
        static bool SameRecordedFacilitySignature(JArray first,JArray next)
            =>JToken.DeepEquals(first,next);

        static void RunRecordedFacilityEgressInputControls(JObject row,SkirmishStaticAuthoringBudget budget,GridConfig grid)
        {
            var expected=RecordedFacilitySignature(row,budget,grid);int controls=1;
            void Refuses(Action<JObject> change,string reason)
            {
                var copy=(JObject)row.DeepClone();change(copy);bool refused=false;
                try{RecordedFacilitySignature(copy,budget,grid);}
                catch(InvalidOperationException e){refused=e.Message.Contains(reason);}
                Require(refused,"Recorded input control did not refuse: "+reason);controls++;
            }
            Refuses(r=>r["catalogId"]="Foreign","catalog/size");
            Refuses(r=>r["completeFacilities"]=false,"Complete actual");
            Refuses(r=>r["allActorsInsidePads"]=false,"Complete actual");
            Refuses(r=>((JArray)r["normalFacilityRelocationProposals"]).RemoveAt(0),"unique complete");
            Refuses(r=>{var a=(JArray)r["normalFacilityRelocationProposals"];a[1]=a[0].DeepClone();},"unique complete");
            Refuses(r=>r["normalFacilityRelocationProposals"][0]["entireConfigurationFound"]=false,"Partial facility");
            Refuses(r=>r["normalFacilityRelocationProposals"][0]["prefabSha256"]=new string('0',64),"prefab changed");
            Refuses(r=>r["normalFacilityRelocationProposals"][0]["resolvedEffectiveRect"]["width"]=0,"outside source grid");
            var different=(JArray)expected.DeepClone();different[0]["rect"]["x"]=(int)different[0]["rect"]["x"]+1;
            Require(!SameRecordedFacilitySignature(expected,different)&&SameRecordedFacilitySignature(expected,(JArray)expected.DeepClone()),
                "Different effective configuration cannot share queries; exact clone may share");controls++;
            Require(controls==10&&JToken.DeepEquals(expected,RecordedFacilitySignature(row,budget,grid)),"Ten controls and unchanged original recorded rows required");
            Debug.Log("[RecordedFacilityEgressInputControls] result=Passed controls=10 nativeQueries=0 sourceFitCertified=0");
        }

        static JObject MeasureRecordedFacilityEgress(JObject mission,GroundObjective30CandidateOwner owner,
            EntityManager em,Entity gridEntity,GridConfig grid,MapSurfaceComponent surface)
        {
            var input=recordedFacilityEgressInput;var prior=(JObject)input["_prior"];
            Require((string)input["catalogId"]==(string)mission["catalog_id"]&&(string)input["definitionId"]==(string)mission["definition_id"]&&
                JToken.DeepEquals(input["canonical_size_rows"],mission["canonical_size_rows"])&&
                JToken.DeepEquals(input["sourcePins"],mission["source_candidate"])&&JToken.DeepEquals(prior["sourcePins"],input["sourcePins"]),
                "Own subject/source/three canonical budget authority required");
            Require((string)prior["catalogId"]==(string)mission["catalog_id"]&&(bool?)prior["sourcePinsRecheckedAfterMeasurement"]==true&&
                (bool?)prior["cleanupVerified"]==true,"Historical own measured source/cleanup required, not fit");
            var old=((JArray)prior["missions"]).OfType<JObject>().Single(r=>(string)r["catalogId"]==(string)mission["catalog_id"]);
            var diagnostics=(JObject)old["measurement"];var replays=(JArray)diagnostics["nativePreferenceReplays"];
            Require(replays.Count==1&&(int?)replays[0]["candidate"]==0,"One exact recorded candidate required");
            var measured=(JObject)replays[0]["measurement"];
            var pass=((JArray)measured["passes"]).OfType<JObject>().Single(p=>(int)p["pass"]==(int)input["pass"]);
            var sizes=((JArray)pass["sizes"]).OfType<JObject>().ToArray();
            Require((bool?)pass["discoveryOnly"]==false&&sizes.Length==3&&sizes.Select(s=>(string)s["size"]).OrderBy(s=>s,StringComparer.Ordinal)
                .SequenceEqual(new[]{"LargeWar","Standard","War"}),"Three own final typed measured sizes required");
            var ng=(JObject)prior["nativeGridIdentity"]["grid"];
            Require((int)ng["width"]==grid.Width&&(int)ng["height"]==grid.Height&&(float)ng["cellSize"]==grid.CellSize&&
                (float)ng["originXZ"][0]==grid.Origin.x&&(float)ng["originXZ"][1]==grid.Origin.z,"Exact native source grid required");
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            string original=JsonUtility.ToJson(owner.Layout);
            try
            {
                JsonUtility.FromJsonOverwrite((string)measured["proposedLayoutJson"],owner.Layout);
                var budgets=new System.Collections.Generic.List<SkirmishStaticAuthoringBudget>();
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(owner.Definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=owner.Definition.RequiredFeatureIds},matrix,out var b,out var reasons)&&b.ExactCanonicalQuantities,
                        "Exact own canonical budget required: "+string.Join(";",reasons));budgets.Add(b);
                }
                Require(new JArray(budgets.Select(ApPreferenceBudgetKey)).ToString(Newtonsoft.Json.Formatting.None)==
                    diagnostics["reviewedLogicalPadBudgets"].ToString(Newtonsoft.Json.Formatting.None),"Complete actual own budgets changed");
                RunRecordedFacilityEgressInputControls(sizes.Single(s=>(string)s["size"]=="Standard"),budgets[0],grid);
                var configurations=new System.Collections.Generic.List<JObject>();
                foreach(var b in budgets)
                {
                    var row=sizes.Single(s=>(string)s["size"]==b.Size.ToString());
                    var current=RecordedFacilitySignature(row,b,grid);
                    var group=configurations.SingleOrDefault(g=>SameRecordedFacilitySignature((JArray)g["signature"],current));
                    if(group==null)
                    {
                        group=new JObject{["configuration"]=configurations.Count,["signature"]=current,["facilities"]=row["normalFacilityRelocationProposals"].DeepClone(),
                            ["sizes"]=new JArray(),["recordedInputs"]=new JArray()};configurations.Add(group);
                    }
                    ((JArray)group["sizes"]).Add(b.Size.ToString());
                    var artifact=((JArray)old["sizeArtifacts"]).OfType<JObject>().Single(a=>(int)a["pass"]==(int)input["pass"]&&(string)a["size"]==b.Size.ToString());
                    ((JArray)group["recordedInputs"]).Add(new JObject{["catalogId"]=b.CatalogId,["size"]=b.Size.ToString(),["pass"]=input["pass"],
                        ["historicalSizeArtifactRef"]=artifact.DeepClone(),["facilityCount"]=((JArray)row["normalFacilityRelocationProposals"]).Count});
                }
                Require(TryPreflightAuthoredRouteEndpoints(owner.Layout,grid,out _),"Recorded shared endpoint policy required");
                var baseline=JObject.Parse(JsonUtility.ToJson(owner.Layout));
                Require(owner.Layout.TryGetPad(1,SkirmishLegalPadKind.GroundStaging,out var pad),"Actual unchanged typed player staging required");
                var typed=PilotExactAxisAlignedPadCells(owner.Layout,pad,grid);
                var profiles=new System.Collections.Generic.List<RouteProfile>();var keys=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach(var force in budgets.SelectMany(b=>b.Forces))
                {
                    var prefab=FindNativePrefab(em,force.RuntimePrefabKey);
                    Require(prefab!=Entity.Null&&em.HasComponent<UnitFootprint>(prefab),"Actual own canonical prefab required");
                    if(em.HasComponent<UnitAirMovement>(prefab))continue;
                    Require(em.HasComponent<UnitMovementBehavior>(prefab),"Actual ground movement behavior required");
                    var size=em.GetComponentData<UnitFootprint>(prefab).Size;var behavior=em.GetComponentData<UnitMovementBehavior>(prefab);
                    if(keys.Add(Ground30PathQueryKey(force.FactionId,size,behavior,default,default)))
                        profiles.Add(new RouteProfile{Prefab=prefab,Faction=force.FactionId,Size=size,Behavior=behavior});
                }
                Require(keys.Count==4&&keys.OrderBy(k=>k,StringComparer.Ordinal).SequenceEqual(((JArray)input["expectedProfileKeys"]).Values<string>().OrderBy(k=>k,StringComparer.Ordinal)),
                    "All four actual expected faction/footprint/behavior profiles required");
                var candidates=(JArray)input["candidateCells"];
                Require(candidates.Count==9&&candidates.OfType<JArray>().Count()==9&&candidates.Select(c=>c.ToString()).Distinct().Count()==9,"Nine unique candidate cells required");
                Require(configurations.Count>0&&configurations.Count<=3,"At most three actual complete blocker configurations required");
                var measuredGroups=new JArray();int totalCalls=0;
                foreach(var configuration in configurations)
                {
                    using var blockers=new Unity.Collections.NativeBitArray(grid.Width*grid.Height,Unity.Collections.Allocator.Persistent,Unity.Collections.NativeArrayOptions.ClearMemory);
                    Ground30OccupyFacilities((JArray)configuration["facilities"],grid,blockers);var trials=new JArray();int calls=0;
                    foreach(JArray candidate in candidates)
                    {
                        Require(candidate.Count==2&&candidate.All(v=>v.Type==JTokenType.Integer),"Integer cell pair required");
                        var cell=new int2((int)candidate[0],(int)candidate[1]);
                        var trial=new JObject{["candidate"]=trials.Count,["cell"]=candidate.DeepClone(),["queries"]=new JArray()};trials.Add(trial);
                        if(!profiles.All(p=>{var min=UnitFootprintUtility.GetMinCell(cell,p.Size);return PilotCompoundContains(typed,new RectInt(min.x,min.y,p.Size.x,p.Size.y));}))
                        {trial["status"]="RefusedFullFootprintOutsideExistingTypedPad";continue;}
                        var data=(JObject)baseline.DeepClone();var anchors=(JArray)data["anchors"];var routes=(JArray)data["routes"];
                        string fresh="anchor."+owner.Layout.LayoutId+".recorded-egress";
                        Require(!anchors.OfType<JObject>().Any(a=>(string)a["AnchorId"]==fresh),"Fresh diagnostic anchor identity required");
                        float x=grid.Origin.x+(cell.x+.5f)*grid.CellSize,z=grid.Origin.z+(cell.y+.5f)*grid.CellSize;
                        Require(owner.Layout.TryProjectLocalToMap(x,z,out var px,out var pz)&&Math.Abs(px-x)<.001f&&Math.Abs(pz-z)<.001f,
                            "Source-aligned projection required; no invented inverse frame");
                        anchors.Add(new JObject{["AnchorId"]=fresh,["RoleId"]="route.egress.player",["WorldX"]=x,["WorldZ"]=z,
                            ["NormalizedU"]=(x-owner.Layout.OriginX)/owner.Layout.WorldWidthMetres,["NormalizedV"]=(z-owner.Layout.OriginZ)/owner.Layout.WorldDepthMetres});
                        foreach(JObject route in routes)((JArray)route["WaypointAnchorIds"])[0]=fresh;
                        JsonUtility.FromJsonOverwrite(data.ToString(Newtonsoft.Json.Formatting.None),owner.Layout);
                        Require(TryPreflightAuthoredRouteEndpoints(owner.Layout,grid,out var policy),"Unchanged shared endpoint policy must admit");trial["endpointPolicy"]=policy;
                        bool exact=true;
                        foreach(var route in owner.Layout.Routes)foreach(var p in profiles)
                        {
                            Require(route.AllowsVehicles,"All current approaches must retain four-profile coverage");
                            float nx=0,nz=0;Require(owner.Layout.TryGetAnchorById(route.WaypointAnchorIds[1],out var next)&&owner.Layout.TryProjectLocalToMap(next.WorldX,next.WorldZ,out nx,out nz),"Unchanged first waypoint required");
                            var target=GridUtils.WorldToCell(grid,new float3(nx,0,nz));var from=p.Faction==2?target:cell;var to=p.Faction==2?cell:target;
                            Require(++calls<=108&&++totalCalls<=324&&totalCalls<=2048,"Per-configuration108/total324 diagnostic cap; existing2048 cap unchanged");
                            var q=new JObject{["routeId"]=route.RouteId,["profileKey"]=Ground30PathQueryKey(p.Faction,p.Size,p.Behavior,default,default),
                                ["fromCell"]=new JArray(from.x,from.y),["toCell"]=new JArray(to.x,to.y)};
                            QueryPath(em,gridEntity,grid,surface,p.Prefab,from,to,q,p.Faction,blockers);((JArray)trial["queries"]).Add(q);
                            exact&=(string)q["status"]=="NativeQueryFoundExactGoal"&&(int?)q["failureCode"]==0;
                        }
                        Require(JToken.DeepEquals(data["pads"],baseline["pads"])&&JToken.DeepEquals(new JArray(anchors.Take(anchors.Count-1)),baseline["anchors"]),"Original pads/coded anchors unchanged");
                        trial["allFirstEdgesExact"]=exact;trial["status"]=exact?"AllFirstEdgesExact_FullRoutesPending":"RefusedNonexactFirstEdge";
                    }
                    measuredGroups.Add(new JObject{["configuration"]=configuration["configuration"],["sizes"]=configuration["sizes"].DeepClone(),
                        ["recordedInputs"]=configuration["recordedInputs"].DeepClone(),["facilityConfiguration"]=configuration["signature"].DeepClone(),
                        ["trials"]=trials,["nativeQueries"]=calls,["maximumNativeQueries"]=108});
                }
                return new JObject{["scope"]="RecordedCompleteFacilities_EgressEdgesOnly",["configurations"]=measuredGroups,
                    ["profileKeys"]=new JArray(keys.OrderBy(k=>k,StringComparer.Ordinal)),["nativeQueries"]=totalCalls,
                    ["maximumEndpointCandidates"]=9,["maximumConfigurationGroups"]=3,["maximumNativeQueriesPerGroup"]=108,["maximumNativeQueries"]=324,["existingNativeAuthoringCap"]=2048,
                    ["facilityMeasurements"]=0,["fullRoutesMeasured"]=false,["distinctApproachesMeasured"]=false,["sourceFitCertified"]=false,["normalMissionReady"]=false};
            }
            finally{JsonUtility.FromJsonOverwrite(original,owner.Layout);Require(JsonUtility.ToJson(owner.Layout)==original,"Exact caller layout restoration required");}
        }

        static void CollectAll120Refinement(JObject[] authorities,JObject report,string artifactPrefix,bool counterpart=false,bool apRouteProbeOnly=false)
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
                            if(recordedFacilityEgressInput!=null)
                            {
                                row["measurement"]=MeasureRecordedFacilityEgress(All120MeasurementMission(authority,candidateTemplate),owner,em,gridEntity,grid,surface);
                                row["result"]="CollectedUnqualified";
                                continue;
                            }
                            row["measurement"]=apRouteProbeOnly
                                ?MeasureApCoherentRouteEdges(All120MeasurementMission(authority,candidateTemplate),owner,source,em,gridEntity,grid,surface)
                                :RefineLoadedCandidate(All120MeasurementMission(authority,candidateTemplate),owner,source,em,gridEntity,grid,surface,artifactPrefix+"."+id,checkpoint=>
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
