#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        public static void RunAll120SequentialSourceGroups()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode&&ground30MemoryWriter==null,"Idle exclusive source collection required");
            string directory=Environment.GetEnvironmentVariable("WARLINE_ALL120_ALL_GROUPS_OUTPUT_DIRECTORY");
            string allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","SkirmishProduction"))+Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(directory)&&Path.IsPathRooted(directory)&&Path.GetFullPath(directory).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&
                !Directory.Exists(directory)&&!File.Exists(directory),"Fresh owned persistent output directory required");
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);
            string oldGroup=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_SOURCE_GROUP");
            string oldOutput=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_OUTPUT");
            var report=new JObject{["schema"]=1,["scope"]="SequentialExistingSourceLogicalAuthoring_NotMissionAcceptance",
                ["companionPrewarm"]=Ground30PrewarmCompanions(),["sourceFitCertified"]=false,["cameraAccepted"]=false,
                ["normalMissionReady"]=false,["groups"]=new JArray(),["result"]="Incomplete"};
            var baselineWorlds=CaptureMaterializationWorldCensus();var baselineDefault=World.DefaultGameObjectInjectionWorld;
            var baselineScenes=Ground30SceneOwnership();var failures=new List<Exception>();
            bool ownershipSafe=true;int missionCount=0,sizeCount=0;var catalogIds=new HashSet<string>(StringComparer.Ordinal);
            string[] groups={"CC","MP","IB","AP"};int[] expectedCounts={42,24,23,24};
            bool Restored()=>baselineWorlds.SequenceEqual(CaptureMaterializationWorldCensus())&&
                ReferenceEquals(baselineDefault,World.DefaultGameObjectInjectionWorld)&&JToken.DeepEquals(baselineScenes,Ground30SceneOwnership());
            try
            {
                for(int index=0;index<groups.Length;index++)
                {
                    string group=groups[index],path=Path.Combine(directory,group+".json");
                    var row=new JObject{["sourceGroup"]=group,["output"]=path,["result"]="NotRun"};((JArray)report["groups"]).Add(row);
                    if(!ownershipSafe)
                    {
                        row["result"]="Skipped_UnrestoredPriorSourceOwnership";
                        failures.Add(new InvalidOperationException("Source "+group+" not started because prior source ownership was not restored"));continue;
                    }
                    try
                    {
                        Require(Restored(),"Exact baseline ownership required before starting another source World");
                        Environment.SetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_SOURCE_GROUP",group);
                        Environment.SetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_OUTPUT",path);
                        RunAll120SourceGroupLogicalRefinement();
                        var source=JObject.Parse(File.ReadAllText(path));
                        Require((string)source["result"]=="CollectedUnqualified"&&(string)source["sourceGroup"]==group&&
                            (bool?)source["cleanupVerified"]==true&&(bool?)source["sourcePinsRecheckedAfterMeasurement"]==true,
                            "Independent group must complete exact source and cleanup gates");
                        var missions=((JArray)source["missions"]).OfType<JObject>().ToArray();
                        var authorityIds=((JArray)ReadAll120RefinementIndex()["missions"]).OfType<JObject>()
                            .Where(m=>m["protected_binding"].Type==JTokenType.Null).Where(m=>
                            {ResolveAll120RefinementCandidate(m,out _,out var template);return (string)template["collector_source_group"]==group;})
                            .Select(m=>(string)m["catalog_id"]).OrderBy(v=>v,StringComparer.Ordinal).ToArray();
                        Require(authorityIds.Length==expectedCounts[index]&&missions.Select(m=>(string)m["catalogId"]).OrderBy(v=>v,StringComparer.Ordinal).SequenceEqual(authorityIds),
                            "Exact independent source-group mission set required");
                        int groupSizes=0;
                        foreach(var mission in missions)
                        {
                            string id=(string)mission["catalogId"];
                            Require(catalogIds.Add(id)&&(string)mission["result"]=="CollectedUnqualified","Each generic mission must appear exactly once and return its own durable result");
                            var artifact=((JArray)source["missionArtifacts"]).OfType<JObject>().Single(a=>(string)a["catalogId"]==id);
                            Require(Hash((string)artifact["path"])==(string)artifact["sha256"]&&
                                JToken.DeepEquals(mission,JObject.Parse(File.ReadAllText((string)artifact["path"]))),"Exact durable mission artifact join required");
                            var rows=((JArray)mission["measurement"]["passes"]).OfType<JObject>().SelectMany(p=>((JArray)p["sizes"]).OfType<JObject>()).ToArray();
                            Require(rows.Select(s=>(string)s["size"]).Distinct().OrderBy(v=>v,StringComparer.Ordinal).SequenceEqual(new[]{"LargeWar","Standard","War"}),
                                "Actual collected rows for all three sizes required; repeated refinement passes do not add size vectors");
                            foreach(JObject sizeArtifact in (JArray)mission["sizeArtifacts"])
                                Require(Hash((string)sizeArtifact["path"])==(string)sizeArtifact["sha256"],"Durable pass/size artifact changed");
                            Require(((JArray)mission["sizeArtifacts"]).OfType<JObject>().Select(s=>(string)s["size"]).Distinct().Count()==3,"Three distinct durable size rows required");
                            groupSizes+=3;
                        }
                        missionCount+=missions.Length;sizeCount+=groupSizes;
                        row["result"]="CollectedUnqualified";row["missionCount"]=missions.Length;row["distinctSizeRows"]=groupSizes;
                        Debug.Log("[All120SequentialSourceGroups] source="+group+" result=Collected missions="+missions.Length+" distinctSizeRows="+groupSizes);
                    }
                    catch(Exception failure)
                    {row["result"]="Failed";row["error"]=failure.ToString();Debug.LogException(failure);failures.Add(new InvalidOperationException("Independent source group failed: "+group,failure));}
                    finally
                    {
                        if(File.Exists(path))row["outputSha256"]=Hash(path);
                        try{ownershipSafe=Restored();row["ownershipRestored"]=ownershipSafe;if(!ownershipSafe)failures.Add(new InvalidOperationException("Exact source ownership not restored: "+group));}
                        catch(Exception failure){ownershipSafe=false;row["ownershipError"]=failure.ToString();failures.Add(failure);}
                    }
                }
                if(failures.Count==0)Require(missionCount==113&&sizeCount==339&&catalogIds.Count==113,"Exact113 generic missions/339 distinct size rows required");
            }
            catch(Exception failure){failures.Add(failure);}
            finally
            {
                Environment.SetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_SOURCE_GROUP",oldGroup);
                Environment.SetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_OUTPUT",oldOutput);
                bool envRestored=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_SOURCE_GROUP")==oldGroup&&
                    Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_OUTPUT")==oldOutput;
                if(!envRestored)failures.Add(new InvalidOperationException("Original collection environment was not restored"));
                report["environmentRestored"]=envRestored;report["missionCount"]=missionCount;report["distinctSizeRows"]=sizeCount;
                report["errors"]=new JArray(failures.Select(f=>f.ToString()));
                report["failureCount"]=failures.Count;report["result"]=failures.Count==0?"CollectedUnqualified":"Failed";
                string receipt=Path.Combine(directory,"receipt.json");
                using(var file=new FileStream(receipt,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))
                {writer.Write(report.ToString(Newtonsoft.Json.Formatting.None));writer.Flush();file.Flush(true);}
            }
            if(failures.Count!=0)throw new AggregateException("Sequential source collection failed; retain every independent group receipt",failures);
            Debug.Log("[All120SequentialSourceGroups] result=Collected groups=4 missions=113 distinctSizeRows=339 sourceWorldsSequential=4 maxConcurrentSourceWorlds=1 sourceFitCertified=0 cameraAccepted=0");
        }
    }
}
#endif
